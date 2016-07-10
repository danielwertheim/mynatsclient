using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MyNatsClient.Internals.Extensions;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsOpStreamReader
    {
        private const char DelimMarker1 = ' ';
        private const char DelimMarker2 = '\t';
        private const char Cr = '\r';
        private const char Lf = '\n';

        private readonly Stream _stream;

        public NatsOpStreamReader(Stream stream)
        {
            _stream = stream;
        }

        public IEnumerable<IOp> ReadOp()
        {
            var opMarkerChars = new char[4];
            var i = -1;

            while (true)
            {
                var curr = _stream.ReadByte();
                if (curr == -1)
                    break;

                var c = (char)curr;
                if (!IsDelimMarker(c) && c != Cr && c != Lf)
                {
                    opMarkerChars[++i] = c;
                    continue;
                }

                if (i == -1)
                    continue;

                var op = new string(opMarkerChars.ToArray(), 0, i + 1);
                i = -1;
                opMarkerChars[0] = '\0';
                opMarkerChars[1] = '\0';
                opMarkerChars[2] = '\0';
                opMarkerChars[3] = '\0';

                switch (op)
                {
                    case "MSG":
                        yield return ParseMsgOp(_stream);
                        break;
                    case "PING":
                        yield return ParsePingOp(_stream);
                        break;
                    case "PONG":
                        yield return ParsePongOp(_stream);
                        break;
                    case "+OK":
                        yield return ParseOkOp(_stream);
                        break;
                    case "INFO":
                        yield return ParseInfoOp(_stream);
                        break;
                    case "-ERR":
                        yield return ParseErrorOp(_stream);
                        break;
                    default:
                        throw CreateUnsupportedOpException(op);
                }
            }
        }

        private static Exception CreateUnsupportedOpException(string foundOp)
            => new Exception($"Unsupported OP, don't know how to parse OP '{foundOp}'.");

        private static Exception CreateParserException(string op, char expected, char got)
            => new Exception($"Error while parsing {op}. Expected char code '{(byte)expected}' got '{(byte)got}'.");

        private static InfoOp ParseInfoOp(Stream stream)
        {
            var msg = new StringBuilder();
            while (true)
            {
                var c = stream.ReadChar();
                if (c == Cr)
                {
                    var burn = stream.ReadChar();
                    if (burn != Lf)
                        throw CreateParserException(nameof(InfoOp), Lf, burn);
                    break;
                }

                msg.Append(c);
            }

            return new InfoOp(msg.ToString());
        }

        private static OkOp ParseOkOp(Stream stream)
        {
            var burn = stream.ReadChar();
            if (burn != Lf)
                throw CreateParserException(nameof(OkOp), Lf, burn);

            return OkOp.Instance;
        }

        private static PingOp ParsePingOp(Stream stream)
        {
            var burn = stream.ReadChar();
            if (burn != Lf)
                throw CreateParserException(nameof(PingOp), Lf, burn);

            return PingOp.Instance;
        }

        private static PongOp ParsePongOp(Stream stream)
        {
            var burn = stream.ReadChar();
            if (burn != Lf)
                throw CreateParserException(nameof(PongOp), Lf, burn);

            return PongOp.Instance;
        }

        private static ErrOp ParseErrorOp(Stream stream)
        {
            var msg = new StringBuilder();
            while (true)
            {
                var c = stream.ReadChar();
                if (c == Cr)
                {
                    var burn = stream.ReadChar();
                    if (burn != Lf)
                        throw CreateParserException(nameof(ErrOp), Lf, burn);
                    break;
                }

                msg.Append(c);
            }

            return new ErrOp(msg.ToString());
        }

        private static MsgOp ParseMsgOp(Stream stream)
        {
            var segments = new string[3];
            var segmentsI = -1;
            var segment = new StringBuilder();
            int payloadSize;
            char burn;

            while (true)
            {
                var c = stream.ReadChar();
                if (c == Cr)
                {
                    payloadSize = int.Parse(segment.ToString());
                    burn = stream.ReadChar();
                    if (burn != Lf)
                        throw CreateParserException(MsgOp.Name, Lf, burn);
                    break;
                }

                if (!IsDelimMarker(c))
                    segment.Append(c);
                else
                {
                    segments[++segmentsI] = segment.ToString();
                    segment.Clear();
                }
            }

            var payload = new byte[payloadSize];
            var bytesRead = 0;
            while (bytesRead < payloadSize)
                bytesRead += stream.Read(payload, bytesRead, payloadSize - bytesRead);

            var msg = new MsgOp(
                segments[0],
                segments[1],
                payload,
                segments[2]);

            burn = stream.ReadChar();
            if (burn != Cr)
                throw CreateParserException(MsgOp.Name, Cr, burn);

            burn = stream.ReadChar();
            if (burn != Lf)
                throw CreateParserException(MsgOp.Name, Lf, burn);

            return msg;
        }

        private static bool IsDelimMarker(char c) => c == DelimMarker1 || c == DelimMarker2;
    }
}