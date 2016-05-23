using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MyNatsClient.Internals
{
    internal class JsonOptions
    {
        internal static readonly JsonSerializerSettings Instance = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter()
            }
        };
    }
}