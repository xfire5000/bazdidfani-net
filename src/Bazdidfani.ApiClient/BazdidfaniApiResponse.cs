using Bazdidfani.ApiClient.Json;

namespace Bazdidfani.ApiClient
{
    /// <summary>
    /// پاسخ خام سرور بازدید فنی. معادل آرایهٔ برگشتی متدهای پکیج Laravel است و همان ساختار
    /// <c>{ success, message, data }</c> را نگه می‌دارد.
    /// </summary>
    public sealed class BazdidfaniApiResponse
    {
        internal BazdidfaniApiResponse(JsonValue root)
        {
            Root = root;
        }

        /// <summary>ریشهٔ بدنهٔ پاسخ.</summary>
        public JsonValue Root { get; }

        /// <summary>مقدار کلید <c>success</c>.</summary>
        public bool Success =>
            Root.ValueKind == JsonValueKind.Object
            && Root.TryGetProperty("success", out var value)
            && value.ValueKind == JsonValueKind.True;

        /// <summary>پیام فارسی سرور؛ در نبود آن <c>null</c>.</summary>
        public string? Message =>
            Root.ValueKind == JsonValueKind.Object
            && Root.TryGetProperty("message", out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        /// <summary>
        /// محتوای کلید <c>data</c>. برای اندپوینت‌های فهرستی، خودِ این مقدار صفحه‌بندی‌شده است
        /// (آرایه به‌همراه <c>links</c> و <c>meta</c> در ریشه).
        /// </summary>
        public JsonValue Data =>
            Root.ValueKind == JsonValueKind.Object && Root.TryGetProperty("data", out var value)
                ? value
                : default;

        /// <summary>کل بدنهٔ پاسخ را به نوع <typeparamref name="T"/> تبدیل می‌کند.</summary>
        public T? Deserialize<T>() => Root.Deserialize<T>();

        /// <summary>فقط کلید <c>data</c> را به نوع <typeparamref name="T"/> تبدیل می‌کند.</summary>
        public T? DeserializeData<T>() => Data.Deserialize<T>();

        /// <summary>بدنهٔ خام پاسخ به‌صورت متن JSON.</summary>
        public override string ToString() => Root.GetRawText();
    }
}
