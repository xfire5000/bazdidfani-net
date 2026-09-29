using System;
using System.Net;
using Bazdidfani.ApiClient.Json;

namespace Bazdidfani.ApiClient.Exceptions
{
    /// <summary>
    /// پرتاب می‌شود وقتی سرور بازدید فنی پاسخی با کد وضعیت ناموفق (4xx/5xx) برمی‌گرداند.
    /// معادل <c>RequestException</c> در پکیج Laravel است.
    /// </summary>
    public sealed class BazdidfaniApiException : Exception
    {
        /// <param name="message">پیام خطا؛ در صورت وجود، پیام فارسیِ خود سرور.</param>
        /// <param name="statusCode">کد وضعیت HTTP پاسخ.</param>
        /// <param name="responseBody">بدنهٔ خام پاسخ سرور.</param>
        /// <param name="body">بدنهٔ پاسخ به‌صورت JSON در صورت قابل تجزیه بودن؛ در غیر این صورت <see cref="JsonValueKind.Undefined"/>.</param>
        public BazdidfaniApiException(
            string message,
            HttpStatusCode statusCode,
            string responseBody,
            JsonValue body = default)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
            Body = body;
        }

        /// <summary>کد وضعیت HTTP پاسخ ناموفق.</summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>بدنهٔ خام پاسخ سرور.</summary>
        public string ResponseBody { get; }

        /// <summary>بدنهٔ پاسخ به‌صورت JSON؛ اگر پاسخ JSON معتبر نبود، <see cref="JsonValueKind.Undefined"/> است.</summary>
        public JsonValue Body { get; }

        internal static BazdidfaniApiException FromResponse(HttpStatusCode statusCode, string responseBody)
        {
            var message = $"درخواست وب‌سرویس بازدید فنی با کد وضعیت {(int)statusCode} ناموفق بود.";
            JsonValue body = default;

            try
            {
                body = JsonCompat.Parse(responseBody);

                if (body.ValueKind == JsonValueKind.Object
                    && body.TryGetProperty("message", out var serverMessage)
                    && serverMessage.ValueKind == JsonValueKind.String)
                {
                    var text = serverMessage.GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        message = text!;
                    }
                }
            }
            catch (Exception)
            {
                // پاسخ JSON معتبر نبود؛ همان پیام پیش‌فرض و بدنهٔ خام برگردانده می‌شود.
            }

            return new BazdidfaniApiException(message, statusCode, responseBody, body);
        }
    }
}
