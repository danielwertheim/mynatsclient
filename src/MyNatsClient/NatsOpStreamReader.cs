using System;
using System.Collections.Generic;
using System.IO;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsOpStreamReader
    {
        private const byte EmptyOpMarker = (byte)'\0';
        private const byte DelimMarker1 = (byte)' ';
        private const byte DelimMarker2 = (byte)'\t';
        private const byte Cr = (byte)'\r';
        private const byte Lf = (byte)'\n';
        private const byte M = (byte)'M';
        private const byte P = (byte)'P';
        private const byte I = (byte)'I';
        private const byte O = (byte)'O';
        private const byte Plus = (byte)'+';
        private const byte Minus = (byte)'-';

        private readonly Stream _stream;

        public NatsOpStreamReader(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        private static bool IsDelimMarker(byte c) => c == DelimMarker1 || c == DelimMarker2;

        private static Memory<char> ToChars(Stream workspace)
        {
            if (workspace.Position == 0)
                return Memory<char>.Empty;

            var sourceSpan = new Span<byte>(new byte[workspace.Position]);
            workspace.Position = 0;

            if (workspace.Read(sourceSpan) == -1)
                throw NatsException.OpParserError("Error while trying to read from workspace stream.");

            var result = new Memory<char>(new char[sourceSpan.Length]);
            var resultSpan = result.Span;
            for (var i = 0; i < sourceSpan.Length; i++)
                resultSpan[i] = (char)sourceSpan[i];

            return result;
        }

        private static InfoOp ParseInfoOp(Stream source, Stream workspace)
        {
            while (true)
            {
                var b = (byte)source.ReadByte();
                if (b == Cr)
                {
                    b = (byte)source.ReadByte();
                    if (b != Lf)
                        throw NatsException.OpParserOpParsingError(InfoOp.Name, Lf, b);
                    break;
                }

                workspace.WriteByte(b);
            }

            var m = ToChars(workspace);

            return new InfoOp(m);
        }

        private static OkOp ParseOkOp(Stream source)
        {
            var c = (byte)source.ReadByte();
            if (c != Lf)
                throw NatsException.OpParserOpParsingError(OkOp.Name, Lf, c);

            return OkOp.Instance;
        }

        private static PingOp ParsePingOp(Stream source)
        {
            var c = (byte)source.ReadByte();
            if (c != Lf)
                throw NatsException.OpParserOpParsingError(PingOp.Name, Lf, c);

            return PingOp.Instance;
        }

        private static PongOp ParsePongOp(Stream source)
        {
            var c = (byte)source.ReadByte();
            if (c != Lf)
                throw NatsException.OpParserOpParsingError(PongOp.Name, Lf, c);

            return PongOp.Instance;
        }

        private static ErrOp ParseErrorOp(Stream source, Stream workspace)
        {
            while (true)
            {
                var b = (byte)source.ReadByte();
                if (b == Cr)
                {
                    b = (byte)source.ReadByte();
                    if (b != Lf)
                        throw NatsException.OpParserOpParsingError(ErrOp.Name, Lf, b);
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
                    t[i] = (char)x[i];
            }));
        }

        private static MsgOp ParseMsgOp(Stream source, Stream workspace)
        {
            var sub = ReadOnlySpan<char>.Empty;
            var sid = ReadOnlySpan<char>.Empty;
            var replyTo = ReadOnlySpan<char>.Empty;
            int payloadSize;
            byte b;

            while (true)
            {
                b = (byte)source.ReadByte();
                if (b == Cr)
                {
                    payloadSize = int.Parse(ToChars(workspace).Span);

                    b = (byte)source.ReadByte();
                    if (b != Lf)
                        throw NatsException.OpParserOpParsingError(MsgOp.Name, Lf, b);
                    break;
                }

                if (!IsDelimMarker(b))
                    workspace.WriteByte(b);
                else
                {
                    if (sub.IsEmpty)
                    {
                        sub = ToChars(workspace).Span;
                        workspace.Position = 0;
                        continue;
                    }

                    if (sid.IsEmpty)
                    {
                        sid = ToChars(workspace).Span;
                        workspace.Position = 0;
                        continue;
                    }

                    if (replyTo.IsEmpty)
                    {
                        replyTo = ToChars(workspace).Span;
                        workspace.Position = 0;
                        continue;
                    }

                    throw NatsException.OpParserOpParsingError(MsgOp.Name, "Message does not conform to expected protocol format.");
                }
            }

            var payload = new Memory<byte>(new byte[payloadSize]);
            if (source.Read(payload.Span) == -1)
                throw NatsException.OpParserOpParsingError(MsgOp.Name, "Could not read payload from stream.");

            b = (byte)source.ReadByte();
            if (b != Cr)
                throw NatsException.OpParserOpParsingError(MsgOp.Name, Cr, b);

            b = (byte)source.ReadByte();
            if (b != Lf)
                throw NatsException.OpParserOpParsingError(MsgOp.Name, Lf, b);

            return new MsgOp(
                sub,
                sid,
                replyTo,
                payload);
        }

        public IEnumerable<IOp> ReadOp()
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

                var c = (byte)curr;
                if (!IsDelimMarker(c) && c != Cr && c != Lf)
                {
                    opMarkerChars[++i] = c;
                    continue;
                }

                if (i == -1)
                    continue;

                if (opMarkerChars[0] == M)
                    op = ParseMsgOp(_stream, workspace);
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
                            t[3] = (char)v[3];

                        t[2] = (char)v[2];
                        t[1] = (char)v[1];
                        t[0] = (char)v[0];
                    });

                    throw NatsException.OpParserUnsupportedOp(opMarker);
                }

                i = -1;
                opMarkerChars[0] = EmptyOpMarker;
                opMarkerChars[1] = EmptyOpMarker;
                opMarkerChars[2] = EmptyOpMarker;
                opMarkerChars[3] = EmptyOpMarker;
                workspace.Position = 0;

                yield return op;

                op = null;
            }
        }

        public IOp ReadOneOp()
        {
            IOp op = null;
            using var workspace = new MemoryStream();
            var opMarkerChars = new byte[4];
            var i = -1;

            while (true)
            {
                var curr = _stream.ReadByte();
                if (curr == -1)
                    return null;

                var c = (byte)curr;
                if (!IsDelimMarker(c) && c != Cr && c != Lf)
                {
                    opMarkerChars[++i] = c;
                    continue;
                }

                if (i == -1)
                    continue;

                if (opMarkerChars[0] == M)
                    op = ParseMsgOp(_stream, workspace);
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
                            t[3] = (char)v[3];

                        t[2] = (char)v[2];
                        t[1] = (char)v[1];
                        t[0] = (char)v[0];
                    });

                    throw NatsException.OpParserUnsupportedOp(opMarker);
                }

                i = -1;
                opMarkerChars[0] = EmptyOpMarker;
                opMarkerChars[1] = EmptyOpMarker;
                opMarkerChars[2] = EmptyOpMarker;
                opMarkerChars[3] = EmptyOpMarker;
                workspace.Position = 0;

                return op;
            }
        }
    }
}