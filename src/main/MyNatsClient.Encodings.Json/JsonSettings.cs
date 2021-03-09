using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MyNatsClient.Encodings.Json
{
    public class JsonSettings
    {
        public static JsonSerializerSettings Create()
        {
            var settings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new DefaultNamingStrategy
                    {
                        ProcessDictionaryKeys = false
                    }
                }
            };
            settings.Converters.Add(new StringEnumConverter());

            return settings;
        }
    }
}