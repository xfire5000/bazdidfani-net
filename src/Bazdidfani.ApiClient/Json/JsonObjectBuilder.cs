using System;
#if NET46
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
using System.Text.Json.Nodes;
#endif

namespace Bazdidfani.ApiClient.Json
{
    /// <summary>
    /// شیء JSON قابل‌ویرایش برای ساختن بدنهٔ درخواست‌های POST (مثلاً افزودن <c>force_create</c> پیش
    /// از ارسال)، مستقل از موتور سریالایز زیرین.
    /// </summary>
    internal sealed class JsonObjectBuilder
    {
#if NET46
        private readonly JObject _object;

        private JsonObjectBuilder(JObject value) => _object = value;

        public static JsonObjectBuilder FromObject(object payload)
        {
            if (payload is JObject existing)
            {
                return new JsonObjectBuilder(existing);
            }

            var parsed = JObject.Parse(JsonConvert.SerializeObject(payload));

            return new JsonObjectBuilder(parsed);
        }

        public void SetBool(string key, bool value) => _object[key] = value;

        public string? GetString(string key) =>
            _object.TryGetValue(key, out var token) ? token.ToString() : null;

        public string ToJsonString() => _object.ToString(Formatting.None);
#else
        private readonly JsonObject _object;

        private JsonObjectBuilder(JsonObject value) => _object = value;

        public static JsonObjectBuilder FromObject(object payload)
        {
            if (payload is JsonObject existing)
            {
                return new JsonObjectBuilder(existing);
            }

            var parsed = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(payload)) as JsonObject
                         ?? throw new ArgumentException("بدنهٔ درخواست باید به یک شیء JSON تبدیل شود.", nameof(payload));

            return new JsonObjectBuilder(parsed);
        }

        public void SetBool(string key, bool value) => _object[key] = value;

        public string? GetString(string key) =>
            _object.TryGetPropertyValue(key, out var node) && node is not null
                ? node.ToJsonString().Trim('"')
                : null;

        public string ToJsonString() => _object.ToJsonString();
#endif
    }
}
