using System;
#if NET46
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
#endif

namespace Bazdidfani.ApiClient.Json
{
    /// <summary>سریالایز/تجزیهٔ JSON مستقل از موتور زیرین.</summary>
    internal static class JsonCompat
    {
        public static string Serialize(object value) =>
#if NET46
            JsonConvert.SerializeObject(value);
#else
            System.Text.Json.JsonSerializer.Serialize(value);
#endif

        public static JsonValue Parse(string? json)
        {
            var text = string.IsNullOrWhiteSpace(json) ? "{}" : json!;

#if NET46
            return new JsonValue(JToken.Parse(text));
#else
            using var document = System.Text.Json.JsonDocument.Parse(text);

            return new JsonValue(document.RootElement.Clone());
#endif
        }
    }
}
