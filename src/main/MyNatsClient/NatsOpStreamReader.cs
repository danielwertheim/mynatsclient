using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    /// <summary>
    /// Reads <see cref="IOp"/> from a Stream.
    /// </summary>
    /// <remarks>
    /// Intentionally not locking so currently not safe for parallel use.
    /// </remarks>
    public class NatsOpStreamReader : IDisposable
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

        private Stream _source;
        private readonly MemoryStream _workspace;
        private readonly byte[] _opMarkerChars = new byte[4];
        private readonly byte[] _readSingleByteBuff = new byte[1];

        private NatsOpStreamReader(Stream source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _workspace = new MemoryStream();
        }

        public static NatsOpStreamReader Use(Stream stream)
            => new(stream);

        public void Dispose()
        {
            _workspace.Dispose();
        }

        private void Reset()
        {
            _opMarkerChars[0] = Empty;
            _opMarkerChars[1] = Empty;
            _opMarkerChars[2] = Empty;
            _opMarkerChars[3] = Empty;
            _workspace.Position = 0;
            _readSingleByteBuff[0] = Empty;
        }

        public void SetNewSource(Stream source)
        {
            _source = source;
            Reset();
        }

        private static bool IsDelimiter(byte c)
            => c is SpaceDelimiter or TabDelimiter;

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

        public IOp ReadOp()
        {
            Reset();

            var i = -1;

            while (true)
            {
                var curr = _source.CanRead ? _source.Read(_readSingleByteBuff, 0, 1) : 0;
                if (curr == 0)
                    return NullOp.Instance;

                var c = _readSingleByteBuff[0];
                if (!IsDelimiter(c) && c != Cr && c != Lf)
                {
                    _opMarkerChars[++i] = c;
                    continue;
                }

                if (i == -1)
                    continue;

                if (_opMarkerChars[0] == M)
                    return ParseMsgOp(_source, _workspace);
                if (_opMarkerChars[0] == H)
                    return ParseHMsgOp(_source, _workspace);
                if (_opMarkerChars[0] == P)
                {
                    if (_opMarkerChars[1] == I)
                        return ParsePingOp(_source);
                    if (_opMarkerChars[1] == O)
                        return ParsePongOp(_source);
                }

                if (_opMarkerChars[0] == I)
                    return ParseInfoOp(_source, _workspace);
                if (_opMarkerChars[0] == Plus)
                    return ParseOkOp(_source);
                if (_opMarkerChars[0] == Minus)
                    return ParseErrorOp(_source, _workspace);

                var opMarker = string.Create(i + 1, _opMarkerChars, (t, v) =>
                {
                    if (t.Length == 4)
                        t[3] = (char) v[3];

                    t[2] = (char) v[2];
                    t[1] = (char) v[1];
                    t[0] = (char) v[0];
                });

                throw NatsException.OpParserUnsupportedOp(opMarker);
            }
        }
    }
}
