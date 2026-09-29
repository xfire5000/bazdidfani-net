using System.Collections.Generic;
#if NET46
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
#endif

namespace Bazdidfani.ApiClient.Json
{
    /// <summary>
    /// نمای فقط‌خواندنیِ یکسانِ یک مقدار JSON، مستقل از موتور سریالایز زیرین: در net46 روی
    /// Newtonsoft.Json.Linq.JToken سوار است (چون System.Text.Json حتی در قدیمی‌ترین نسخه‌اش هم
    /// net461 را حداقل نیاز دارد)؛ در netstandard2.0/net6.0 روی System.Text.Json.JsonElement.
    /// امضای عمومی این نوع در همهٔ targetها یکسان است.
    /// </summary>
    public readonly struct JsonValue
    {
#if NET46
        private readonly JToken? _token;

        internal JsonValue(JToken? token) => _token = token;

        /// <summary>نوع مقدار.</summary>
        public JsonValueKind ValueKind => _token switch
        {
            null => JsonValueKind.Undefined,
            JValue { Type: JTokenType.Null } => JsonValueKind.Null,
            JValue { Type: JTokenType.Boolean } v => (bool)v.Value! ? JsonValueKind.True : JsonValueKind.False,
            JValue { Type: JTokenType.Integer or JTokenType.Float } => JsonValueKind.Number,
            JValue { Type: JTokenType.String } => JsonValueKind.String,
            JObject => JsonValueKind.Object,
            JArray => JsonValueKind.Array,
            _ => JsonValueKind.Undefined,
        };

        /// <summary>در صورتی که این مقدار یک شیء باشد و پراپرتی با این نام داشته باشد، آن را برمی‌گرداند.</summary>
        public bool TryGetProperty(string name, out JsonValue value)
        {
            if (_token is JObject obj && obj.TryGetValue(name, out var token))
            {
                value = new JsonValue(token);

                return true;
            }

            value = default;

            return false;
        }

        /// <summary>مقدار رشته‌ای؛ اگر مقدار رشته نباشد <c>null</c>.</summary>
        public string? GetString() => _token is JValue { Type: JTokenType.String } value ? (string?)value.Value : null;

        /// <summary>پراپرتی‌های این مقدار در صورتی که یک شیء باشد.</summary>
        public IEnumerable<KeyValuePair<string, JsonValue>> EnumerateObject()
        {
            if (_token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    yield return new KeyValuePair<string, JsonValue>(property.Name, new JsonValue(property.Value));
                }
            }
        }

        /// <summary>متن خام JSON این مقدار.</summary>
        public string GetRawText() => _token?.ToString(Formatting.None) ?? "null";
#else
        private readonly JsonElement _element;

        internal JsonValue(JsonElement element) => _element = element;

        /// <summary>نوع مقدار.</summary>
        public JsonValueKind ValueKind => (JsonValueKind)(int)_element.ValueKind;

        /// <summary>در صورتی که این مقدار یک شیء باشد و پراپرتی با این نام داشته باشد، آن را برمی‌گرداند.</summary>
        public bool TryGetProperty(string name, out JsonValue value)
        {
            if (_element.ValueKind == System.Text.Json.JsonValueKind.Object && _element.TryGetProperty(name, out var element))
            {
                value = new JsonValue(element);

                return true;
            }

            value = default;

            return false;
        }

        /// <summary>مقدار رشته‌ای؛ اگر مقدار رشته نباشد <c>null</c>.</summary>
        public string? GetString() => _element.ValueKind == System.Text.Json.JsonValueKind.String ? _element.GetString() : null;

        /// <summary>پراپرتی‌های این مقدار در صورتی که یک شیء باشد.</summary>
        public IEnumerable<KeyValuePair<string, JsonValue>> EnumerateObject()
        {
            if (_element.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var property in _element.EnumerateObject())
                {
                    yield return new KeyValuePair<string, JsonValue>(property.Name, new JsonValue(property.Value));
                }
            }
        }

        /// <summary>متن خام JSON این مقدار.</summary>
        public string GetRawText() => ValueKind == JsonValueKind.Undefined ? "null" : _element.GetRawText();
#endif

        /// <summary>در صورتی که این مقدار یک شیء باشد و پراپرتی با این نام داشته باشد، آن را برمی‌گرداند؛ در غیر این صورت خطا می‌دهد.</summary>
        public JsonValue GetProperty(string name) =>
            TryGetProperty(name, out var value)
                ? value
                : throw new KeyNotFoundException($"پراپرتی «{name}» در این مقدار JSON یافت نشد.");

        /// <summary>این مقدار را به نوع <typeparamref name="T"/> تبدیل می‌کند.</summary>
        public T? Deserialize<T>()
        {
            if (ValueKind == JsonValueKind.Undefined)
            {
                return default;
            }

#if NET46
            return JsonConvert.DeserializeObject<T>(GetRawText());
#else
            return System.Text.Json.JsonSerializer.Deserialize<T>(GetRawText());
#endif
        }

        /// <summary>متن خام JSON این مقدار.</summary>
        public override string ToString() => GetRawText();
    }
}
