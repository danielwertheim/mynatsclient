using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MyNatsClient.Ops;

namespace MyNatsClient
{
    public class NatsOpStreamReader : IDisposable
    {
        private static readonly Dictionary<string, Func<BinaryReader, IOp>> Ops;
        private const char DelimMarker1 = ' ';
        private const char DelimMarker2 = '\t';
        private const char Cr = '\r';
        private const char Lf = '\n';

        static NatsOpStreamReader()
        {
            Ops = new Dictionary<string, Func<BinaryReader, IOp>>
            {
                { "+OK", ParseOkOp },
                { "MSG", ParseMsgOp },
                { "-ERR", ParseErrorOp },
                { "INFO", ParseInfoOp },
                { "PING", ParsePingOp },
                { "PONG", ParsePongOp }
            };
        }

        private readonly Func<bool> _hasData;
        private BinaryReader _reader;
        private bool _isDisposed;

        public NatsOpStreamReader(Stream stream, Func<bool> hasData)
        {
            _hasData = hasData;
            _reader = new BinaryReader(stream, Encoding.UTF8, true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
                return;

            _reader?.Close();
            _reader?.Dispose();
            _reader = null;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public IEnumerable<IOp> ReadOp()
        {
            ThrowIfDisposed();

            if (!_hasData())
                yield break;

            var opMarkerChars = new List<char>();
            while (_hasData())
            {
                var c = _reader.ReadChar();
                if (!IsDelimMarker(c) && c != Cr && c != Lf)
                {
                    opMarkerChars.Add(c);
                    continue;
                }

                if (!opMarkerChars.Any())
                    continue;

                var op = new string(opMarkerChars.ToArray());
                opMarkerChars.Clear();

                if (Ops.ContainsKey(op))
                    yield return Ops[op](_reader);
                else
                    throw CreateUnsupportedOpException(op);
            }
        }

        private static Exception CreateUnsupportedOpException(string foundOp)
            => new Exception($"Unsupported OP, don't know how to parse OP '{foundOp}'.");

        private static Exception CreateParserException(string op, char expected, char got)
            => new Exception($"Error while parsing {op}. Expected char code '{(byte)expected}' got '{(byte)got}'.");

        private static InfoOp ParseInfoOp(BinaryReader reader)
        {
            var msg = new StringBuilder();
            while (true)
            {
                var c = reader.ReadChar();
                if (c == Cr)
                {
                    var burn = reader.ReadChar();
                    if (burn != Lf)
                        throw CreateParserException(nameof(InfoOp), Lf, burn);
                    break;
                }

                msg.Append(c);
            }

            return new InfoOp(msg.ToString());
        }

        private static OkOp ParseOkOp(BinaryReader reader)
        {
            var burn = reader.ReadChar();
            if (burn != Lf)
                throw CreateParserException(nameof(OkOp), Lf, burn);

            return OkOp.Instance;
        }

        private static PingOp ParsePingOp(BinaryReader reader)
        {
            var burn = reader.ReadChar();
            if (burn != Lf)
                throw CreateParserException(nameof(PingOp), Lf, burn);

            return PingOp.Instance;
        }

        private static PongOp ParsePongOp(BinaryReader reader)
        {
            var burn = reader.ReadChar();
            if (burn != Lf)
                throw CreateParserException(nameof(PongOp), Lf, burn);

            return PongOp.Instance;
        }

        private static ErrOp ParseErrorOp(BinaryReader reader)
        {
            var msg = new StringBuilder();
            while (true)
            {
                var c = reader.ReadChar();
                if (c == Cr)
                {
                    var burn = reader.ReadChar();
                    if (burn != Lf)
                        throw CreateParserException(nameof(ErrOp), Lf, burn);
                    break;
                }

                msg.Append(c);
            }

            return new ErrOp(msg.ToString());
        }

        private static MsgOp ParseMsgOp(BinaryReader reader)
        {
            var segments = new List<char[]>();
            var segment = new List<char>();
            int payloadSize;
            char burn;

            while (true)
            {
                var c = reader.ReadChar();
                if (c == Cr)
                {
                    payloadSize = int.Parse(new string(segment.ToArray()));
                    burn = reader.ReadChar();
                    if (burn != Lf)
                        throw CreateParserException(nameof(MsgOp), Lf, burn);
                    break;
                }

                if (!IsDelimMarker(c))
                    segment.Add(c);
                else
                {
                    segments.Add(segment.ToArray());
                    segment.Clear();
                }
            }

            var msg = new MsgOp(
                new string(segments.First()),
                new string(segments.Last()),
                reader.ReadBytes(payloadSize),
                segments.Count > 2 ? new string(segments[1]) : null);

            burn = reader.ReadChar();
            if (burn != Cr)
                throw CreateParserException(nameof(MsgOp), Cr, burn);

            burn = reader.ReadChar();
            if (burn != Lf)
                throw CreateParserException(nameof(MsgOp), Lf, burn);

            return msg;
        }

        private static bool IsDelimMarker(char c) => c == DelimMarker1 || c == DelimMarker2;
    }
}