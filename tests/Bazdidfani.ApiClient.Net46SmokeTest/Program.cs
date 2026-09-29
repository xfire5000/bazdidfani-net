using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bazdidfani.ApiClient;
using Bazdidfani.ApiClient.Exceptions;

namespace Bazdidfani.ApiClient.Net46SmokeTest
{
    /// <summary>
    /// اجرای دستی روی net46 برای اثبات کارکرد صحیح شاخهٔ Newtonsoft.Json در زمان اجرا (نه فقط
    /// کامپایل)، چون xunit.runner.visualstudio خودش پایین‌تر از net462 را پشتیبانی نمی‌کند و
    /// نمی‌توان تست‌های xUnit را مستقیماً روی net46 اجرا کرد.
    /// </summary>
    internal static class Program
    {
        private static int _failures;

        private static int Main()
        {
            RunAsync().GetAwaiter().GetResult();

            if (_failures == 0)
            {
                Console.WriteLine("همهٔ آزمایش‌های net46 موفق بودند.");

                return 0;
            }

            Console.WriteLine($"{_failures} آزمایش net46 شکست خورد.");

            return 1;
        }

        private static async Task RunAsync()
        {
            await Test("ارسال هدرهای احراز هویت و کد سازمان", async () =>
            {
                var handler = new RecordingHandler("{\"success\":true,\"data\":[]}");
                using var client = new BazdidfaniApiClient(new HttpClient(handler), NewOptions());

                await client.TechnicalInspectionsAsync("12345678", new { page = 2 });

                Assert(handler.LastRequest!.Headers.Authorization?.ToString() == "Bearer test-token", "Authorization header");
                Assert(handler.LastRequest.Headers.GetValues("X-Organization-Code").Contains("12345678"), "X-Organization-Code header");
                Assert(handler.LastRequestUri!.Contains("page=2"), "query string");
            });

            await Test("پیمایش JSON پاسخ (Data/Success/Message)", async () =>
            {
                var handler = new RecordingHandler("{\"success\":true,\"message\":\"سلام\",\"data\":{\"bazdidfani\":{\"id\":9,\"code\":\"BZ-9\"}}}");
                using var client = new BazdidfaniApiClient(new HttpClient(handler), NewOptions());

                var response = await client.FleetAsync("12345678");

                Assert(response.Success, "Success == true");
                Assert(response.Message == "سلام", "Message");
                Assert(response.Data.GetProperty("bazdidfani").GetProperty("code").GetString() == "BZ-9", "nested property");
            });

            await Test("ثبت بازدید و افزودن force_create به بدنه", async () =>
            {
                var handler = new RecordingHandler("{\"success\":true,\"data\":{\"bazdidfani\":{\"id\":1,\"code\":\"BZ-1\"}}}");
                using var client = new BazdidfaniApiClient(new HttpClient(handler), NewOptions());

                await client.SubmitTechnicalInspectionAsync(
                    "12345678",
                    new { smart_number = "1234567" },
                    forceCreate: true);

                Assert(handler.LastRequestBody!.Contains("\"force_create\":true"), "force_create in body");
                Assert(handler.LastRequestBody.Contains("\"smart_number\":\"1234567\""), "smart_number preserved");
            });

            await Test("بلوکه شدن ثبت به‌خاطر بازدید فعال (TechnicalInspectionNotAllowedException)", async () =>
            {
                var handler = new RecordingHandler(
                    "{\"success\":true,\"data\":{\"sabaf_code\":null,\"last_visit\":{\"code\":\"TECH-1\"}}}");
                using var client = new BazdidfaniApiClient(new HttpClient(handler), NewOptions());

                try
                {
                    await client.SubmitTechnicalInspectionAsync("12345678", new { smart_number = "1234567" });
                    Assert(false, "باید استثنا پرتاب می‌شد");
                }
                catch (TechnicalInspectionNotAllowedException exception)
                {
                    Assert(exception.VehicleData.GetProperty("last_visit").GetProperty("code").GetString() == "TECH-1", "vehicle data readable");
                }
            });

            await Test("پرتاب BazdidfaniApiException با پیام سرور روی پاسخ 4xx", async () =>
            {
                var handler = new RecordingHandler(
                    "{\"success\":false,\"message\":\"دسترسی مجاز نیست\"}",
                    HttpStatusCode.Forbidden);
                using var client = new BazdidfaniApiClient(new HttpClient(handler), NewOptions());

                try
                {
                    await client.CitiesAsync("12345678");
                    Assert(false, "باید استثنا پرتاب می‌شد");
                }
                catch (BazdidfaniApiException exception)
                {
                    Assert(exception.StatusCode == HttpStatusCode.Forbidden, "status code");
                    Assert(exception.Message == "دسترسی مجاز نیست", "server message");
                }
            });
        }

        private static BazdidfaniApiOptions NewOptions() => new BazdidfaniApiOptions
        {
            BaseUrl = "https://api.example.test",
            Token = "test-token",
            RetrySleepMilliseconds = 0,
        };

        private static async Task Test(string name, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
                Console.WriteLine($"[PASS] {name}");
            }
            catch (Exception exception)
            {
                _failures++;
                Console.WriteLine($"[FAIL] {name}: {exception.Message}");
            }
        }

        private static void Assert(bool condition, string what)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"شرط برقرار نیست: {what}");
            }
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly string _responseBody;
            private readonly HttpStatusCode _statusCode;

            public RecordingHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
            {
                _responseBody = responseBody;
                _statusCode = statusCode;
            }

            public HttpRequestMessage? LastRequest { get; private set; }

            public string? LastRequestUri { get; private set; }

            public string? LastRequestBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                LastRequest = request;
                LastRequestUri = request.RequestUri!.AbsoluteUri;
                LastRequestBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

                return new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_responseBody, Encoding.UTF8, "application/json"),
                };
            }
        }
    }
}
