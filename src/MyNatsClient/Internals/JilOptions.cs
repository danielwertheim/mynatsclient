using Jil;

namespace NatsFun.Internals
{
    internal class JilOptions
    {
        internal static readonly Options Instance = new Options(
            dateFormat: DateTimeFormat.ISO8601,
            unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
    }
}