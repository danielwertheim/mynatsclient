using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsOpStreamReader
    {
        private const byte Empty = (byte) '\0';
        private const byte SpaceDelimiter = (byte) ' ';
        private const byte TabDelimiter = (byte) '\t';
        private const byte Cr = (byte) '\r';
        private const byte Lf = (byte) '\n';
        private const byte H = (byte) 'H';
        private const byte M = (byte) 'M';
        private const byte P = (byte) 'P';
        private const byte I = (byte) 'I';
        private const byte O = (byte) 'O';
        private const byte Plus = (byte) '+';
        private const byte Minus = (byte) '-';

        private readonly Stream _stream;

        public NatsOpStreamReader(Stream stream)
            => _stream = stream ?? throw new ArgumentNullException(nameof(stream));

        private static bool IsDelimiter(byte c)
            => c == SpaceDelimiter || c == TabDelimiter;

        private static ReadOnlySpan<char> ToChars(ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty)
                return ReadOnlySpan<char>.Empty;

            var result = new Span<char>(new char[source.Length]);

            for (var i = 0; i < source.Length; i++)
                result[i] = (char) source[i];

            return result;
        }

        private static ReadOnlySpan<char> ToChars(Stream source)
        {
            if (source.Position == 0)
                return ReadOnlySpan<char>.Empty;

            var sourceSpan = new Span<byte>(new byte[source.Position]);
            source.Position = 0;

            if (source.Read(sourceSpan) == -1)
                throw NatsException.OpParserError("Error while trying to read from source stream.");

            return ToChars(sourceSpan);
        }

        private static InfoOp ParseInfoOp(Stream source, Stream workspace)
        {
            while (true)
            {
                var b = (byte) source.ReadByte();
                if (b == Cr)
                {
                    b = (byte) source.ReadByte();
                    if (b != Lf)
                        throw NatsException.OpParserOpParsingError(InfoOp.OpMarker, Lf, b);
                    break;
                }

                workspace.WriteByte(b);
            }

            var message = ToChars(workspace);

            return new InfoOp(message);
        }

        private static OkOp ParseOkOp(Stream source)
        {
            var c = (byte) source.ReadByte();
            if (c != Lf)
                throw NatsException.OpParserOpParsingError(OkOp.OpMarker, Lf, c);

            return OkOp.Instance;
        }

        private static PingOp ParsePingOp(Stream source)
        {
            var c = (byte) source.ReadByte();
            if (c != Lf)
                throw NatsException.OpParserOpParsingError(PingOp.OpMarker, Lf, c);

            return PingOp.Instance;
        }

        private static PongOp ParsePongOp(Stream source)
        {
            var c = (byte) source.ReadByte();
            if (c != Lf)
                throw NatsException.OpParserOpParsingError(PongOp.OpMarker, Lf, c);

            return PongOp.Instance;
        }

        private static ErrOp ParseErrorOp(Stream source, Stream workspace)
        {
            while (true)
            {
                var b = (byte) source.ReadByte();
                if (b == Cr)
                {
                    b = (byte) source.ReadByte();
                    if (b != Lf)
                        throw NatsException.OpParserOpParsingError(ErrOp.OpMarker, Lf, b);
                    break;
                }

                workspace.WriteByte(b);
            }

            var buff = new Memory<byte>(new byte[workspace.Position]);
            workspace.Position = 0;
            workspace.Read(buff.Span);

            return new ErrOp(string.Create(buff.Length, buff, (t, s) =>
            {
                var x = s.Span;
                for (var i = 0; i < t.Length; i++)
                    t[i] = (char) x[i];
            }));
        }

        private static ReadOnlySpan<byte> ReadMsgOpBytes(Stream source, int size, bool hasHeaders)
        {
            if (size == 0)
                return ReadOnlySpan<byte>.Empty;

            var payload = new Span<byte>(new byte[size]);

            var consumed = 0;
            while (true)
            {
                var read = source.Read(payload.Slice(consumed));
                if (read < 1)
                    throw NatsException.OpParserOpParsingError(MsgOp.GetMarker(hasHeaders), "Could not read bytes from the stream.");

                consumed += read;

                if (consumed == size)
                    break;

                if (consumed > size)
                    throw NatsException.OpParserOpParsingError(MsgOp.GetMarker(hasHeaders), "Read to many bytes from the stream.");
            }

            return payload;
        }

        private static MsgOp ParseMsgOp(Stream source, Stream workspace)
        {
            var opMarker = MsgOp.GetMarker(false);
            var sub = ReadOnlySpan<char>.Empty;
            var sid = ReadOnlySpan<char>.Empty;
            var replyTo = ReadOnlySpan<char>.Empty;
            int payloadSize;
            byte b;

            while (true)
            {
                b = (byte) source.ReadByte();
                if (b == Cr)
                {
                    b = (byte) source.ReadByte();
                    if (b != Lf)
                        throw NatsException.OpParserOpParsingError(opMarker, Lf, b);

                    payloadSize = int.Parse(ToChars(workspace));

                    break;
                }

                if (!IsDelimiter(b))
                    workspace.WriteByte(b);
                else
                {
                    if (sub.IsEmpty)
                    {
                        sub = ToChars(workspace);
                        workspace.Position = 0;
                        continue;
                    }

                    if (sid.IsEmpty)
                    {
                        sid = ToChars(workspace);
                        workspace.Position = 0;
                        continue;
                    }

                    if (replyTo.IsEmpty)
                    {
                        replyTo = ToChars(workspace);
                        workspace.Position = 0;
                        continue;
                    }

                    throw NatsException.OpParserOpParsingError(opMarker, "Message does not conform to expected protocol format.");
                }
            }

            var payload = ReadMsgOpBytes(source, payloadSize, false);

            b = (byte) source.ReadByte();
            if (b != Cr)
                throw NatsException.OpParserOpParsingError(opMarker, Cr, b);

            b = (byte) source.ReadByte();
            if (b != Lf)
                throw NatsException.OpParserOpParsingError(opMarker, Lf, b);

            return MsgOp.CreateMsg(
                sub,
                sid,
                replyTo,
                payload);
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> ToHeaderKeyValues(ReadOnlySpan<byte> source)
        {
            var opMarker = MsgOp.GetMarker(true);
            var headers = new Dictionary<string, IReadOnlyList<string>>();
            while (source.Length > 2)
            {
                var crPos = source.IndexOf(Cr);
                if (source[crPos + 1] != Lf)
                    throw NatsException.OpParserOpParsingError(opMarker, Lf, source[crPos + 1]);

                var kv = ToChars(source.Slice(0, crPos));
                var colonPos = kv.IndexOf(':');
                var k = kv.Slice(0, colonPos).ToString();
                var v = kv.Slice(colonPos + 1).ToString();
                if (headers.TryGetValue(k, out var values))
                    headers[k] = ((IImmutableList<string>) values).Add(v);
                else
                    headers[k] = ImmutableList.Create(v);

                source = source.Slice(crPos + 2);
            }

            return headers;
        }

        private static MsgOp ParseHMsgOp(Stream source, Stream workspace)
        {
            var opMarker = MsgOp.GetMarker(true);
            var part1 = ReadOnlySpan<char>.Empty;
            var part2 = ReadOnlySpan<char>.Empty;
            var part3 = ReadOnlySpan<char>.Empty;
            var part4 = ReadOnlySpan<char>.Empty;
            var part5 = ReadOnlySpan<char>.Empty;
            ReadOnlySpan<char> headersProtocol;
            byte b;

            while (true)
            {
                b = (byte) source.ReadByte();
                if (b == Cr)
                {
                    b = (byte) source.ReadByte();
                    if (b != Lf)
                        throw NatsException.OpParserOpParsingError(MsgOp.GetMarker(true), Lf, b);

                    headersProtocol = ToChars(workspace);

                    break;
                }

                if (!IsDelimiter(b))
                    workspace.WriteByte(b);
                else
                {
                    if (part1.IsEmpty)
                    {
                        part1 = ToChars(workspace);
                        workspace.Position = 0;
                        continue;
                    }

                    if (part2.IsEmpty)
                    {
                        part2 = ToChars(workspace);
                        workspace.Position = 0;
                        continue;
                    }

                    if (part3.IsEmpty)
                    {
                        part3 = ToChars(workspace);
                        workspace.Position = 0;
                        continue;
                    }

                    if (part4.IsEmpty)
                    {
                        part4 = ToChars(workspace);
                        workspace.Position = 0;
                        continue;
                    }

                    if (part5.IsEmpty)
                    {
                        part5 = ToChars(workspace);
                        workspace.Position = 0;
                        continue;
                    }

                    throw NatsException.OpParserOpParsingError(opMarker, "Message does not conform to expected protocol format.");
                }
            }

            var hasReplyTo = !part5.IsEmpty;
            var headersSize = int.Parse(hasReplyTo ? part4 : part3);
            var payloadSize = int.Parse(hasReplyTo ? part5 : part4) - headersSize;

            var headerBytes = ReadMsgOpBytes(source, headersSize - headersProtocol.Length - 2, true);
            var payload = ReadMsgOpBytes(source, payloadSize, true);

            b = (byte) source.ReadByte();
            if (b != Cr)
                throw NatsException.OpParserOpParsingError(opMarker, Cr, b);

            b = (byte) source.ReadByte();
            if (b != Lf)
                throw NatsException.OpParserOpParsingError(opMarker, Lf, b);

            var headers = ReadOnlyMsgHeaders.Create(headersProtocol, ToHeaderKeyValues(headerBytes));

            return MsgOp.CreateHMsg(
                part1,
                part2,
                hasReplyTo ? part3 : ReadOnlySpan<char>.Empty,
                headers,
                payload);
        }

        public IEnumerable<IOp> ReadOps()
        {
            IOp op = null;
            using var workspace = new MemoryStream();
            var opMarkerChars = new byte[4];
            var i = -1;

            while (true)
            {
                var curr = _stream.ReadByte();
                if (curr == -1)
                    yield break;

                var c = (byte) curr;
                if (!IsDelimiter(c) && c != Cr && c != Lf)
                {
                    opMarkerChars[++i] = c;
                    continue;
                }

                if (i == -1)
                    continue;

                if (opMarkerChars[0] == M)
                    op = ParseMsgOp(_stream, workspace);
                else if (opMarkerChars[0] == H)
                    op = ParseHMsgOp(_stream, workspace);
                else if (opMarkerChars[0] == P)
                {
                    if (opMarkerChars[1] == I)
                        op = ParsePingOp(_stream);
                    else if (opMarkerChars[1] == O)
                        op = ParsePongOp(_stream);
                }
                else if (opMarkerChars[0] == I)
                    op = ParseInfoOp(_stream, workspace);
                else if (opMarkerChars[0] == Plus)
                    op = ParseOkOp(_stream);
                else if (opMarkerChars[0] == Minus)
                    op = ParseErrorOp(_stream, workspace);

                if (op == null)
                {
                    var opMarker = string.Create(i + 1, opMarkerChars, (t, v) =>
                    {
                        if (t.Length == 4)
                            t[3] = (char) v[3];

                        t[2] = (char) v[2];
                        t[1] = (char) v[1];
                        t[0] = (char) v[0];
                    });

                    throw NatsException.OpParserUnsupportedOp(opMarker);
                }

                i = -1;
                opMarkerChars[0] = Empty;
                opMarkerChars[1] = Empty;
                opMarkerChars[2] = Empty;
                opMarkerChars[3] = Empty;
                workspace.Position = 0;

                yield return op;

                op = null;
            }
        }

        public IOp ReadOneOp() => ReadOps().First();
    }
}
