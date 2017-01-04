using System.Collections.Generic;

namespace MyNatsClient
{
    public interface INatsServerInfo
    {
        string ServerId { get; }
        string Version { get; }
        string Go { get; }
        string Host { get; }
        int Port { get; }
        bool AuthRequired { get; }
        bool SslRequired { get; }
        bool TlsRequired { get; }
        bool TlsVerify { get; }
        long MaxPayload { get; }
        List<string> ConnectUrls { get; }
    }
}