using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bazdidfani.ApiClient.Exceptions;
using Xunit;

namespace Bazdidfani.ApiClient.Tests
{
    public class BazdidfaniApiClientTests
    {
        private const string BaseUrl = "https://api.example.test";

        [Fact]
        public async Task It_sends_authentication_and_organization_headers()
        {
            var handler = new FakeHttpMessageHandler()
                .Respond("/webservice/technical-inspections", "{\"success\":true,\"data\":[]}");
            var client = CreateClient(handler);

            await client.TechnicalInspectionsAsync("12345678", new Dictionary<string, object?> { ["page"] = 2 });

            var request = Assert.Single(handler.Requests);
            Assert.Equal("Bearer test-token", request.Header("Authorization"));
            Assert.Equal("12345678", request.Header("X-Organization-Code"));
            Assert.Contains("page=2", request.Url);
        }

        [Fact]
        public async Task It_forwards_full_search_and_smart_number_parameters()
        {
            var handler = new FakeHttpMessageHandler()
                .Respond("/webservice/self-statements", "{\"success\":true,\"data\":[]}");
            var client = CreateClient(handler);

            await client.SelfStatementsAsync("87654321", new Dictionary<string, object?>
            {
                ["query"] = "راننده آزمایشی",
                ["smart_number"] = 1234567,
            });

            var request = Assert.Single(handler.Requests);
            Assert.Equal("87654321", request.Header("X-Organization-Code"));
            Assert.Contains("query=" + Uri.EscapeDataString("راننده آزمایشی"), request.Url);
            Assert.Contains("smart_number=1234567", request.Url);
        }

        [Fact]
        public async Task It_sends_a_different_organization_code_per_call_on_one_client()
        {
            var handler = new FakeHttpMessageHandler()
                .Respond("/webservice/fleet", "{\"success\":true,\"data\":[]}");
            var client = CreateClient(handler);

            await client.FleetAsync("11111111");
            await client.FleetAsync("22222222");
            await client.ForOrganization("33333333").FleetAsync();

            Assert.Equal(
                new[] { "11111111", "22222222", "33333333" },
                handler.Requests.ConvertAll(request => request.Header("X-Organization-Code")));
        }

        [Fact]
        public async Task It_uses_the_configured_organization_header_name()
        {
            var handler = new FakeHttpMessageHandler()
                .Respond("/webservice/states", "{\"success\":true,\"data\":[]}");
            var client = CreateClient(handler, options => options.OrganizationHeader = "X-Company-Code");

            await client.StatesAsync("12345678");

            var request = Assert.Single(handler.Requests);
            Assert.Equal("12345678", request.Header("X-Company-Code"));
            Assert.Null(request.Header("X-Organization-Code"));
        }

        [Fact]
        public async Task It_rejects_an_empty_organization_code()
        {
            var client = CreateClient(new FakeHttpMessageHandler());

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => client.TechnicalInspectionsAsync("   "));

            Assert.Contains("کد سازمان برای ارسال درخواست الزامی است.", exception.Message);
        }

        [Fact]
        public async Task It_submits_a_technical_inspection_as_a_post_request()
        {
            var handler = new FakeHttpMessageHandler()
                .Respond("/webservice/fleet/vehicle", "{\"success\":true,\"data\":{\"last_visit\":null}}")
                .Respond("/webservice/technical-inspections", "{\"success\":true,\"data\":{\"bazdidfani\":{\"id\":1,\"code\":\"BZ-1\"}}}");
            var client = CreateClient(handler);

            var response = await client.SubmitTechnicalInspectionAsync("12345678", new Dictionary<string, object?>
            {
                ["usage"] = "freighter",
                ["company_usage"] = 1,
                ["user_type"] = "company",
                ["smart_number"] = "1234567",
                ["loader_code"] = 100,
                ["technical_manager_national_code"] = "0012345678",
            });

            var submission = handler.Requests[handler.Requests.Count - 1];
            Assert.Equal(HttpMethod.Post, submission.Method);
            Assert.Contains("/webservice/technical-inspections", submission.Url);
            Assert.Equal("12345678", submission.Header("X-Organization-Code"));
            Assert.Contains("\"smart_number\":\"1234567\"", submission.Body);
            Assert.DoesNotContain("force_create", submission.Body);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task It_allows_submission_when_the_vehicle_was_never_visited()
        {
            var handler = new FakeHttpMessageHandler()
                .Respond("/webservice/fleet/vehicle", "{\"success\":false,\"message\":\"یافت نشد\"}", HttpStatusCode.NotFound)
                .Respond("/webservice/technical-inspections", "{\"success\":true,\"data\":{\"bazdidfani\":{\"id\":2,\"code\":\"BZ-2\"}}}");
            var client = CreateClient(handler);

            var response = await client.SubmitTechnicalInspectionAsync("12345678", new Dictionary<string, object?>
            {
                ["smart_number"] = "1234567",
            });

            Assert.True(response.Success);
        }

        [Fact]
        public async Task It_blocks_submission_while_an_active_visit_has_no_sabaf_code()
        {
            var handler = new FakeHttpMessageHandler()
                .Respond(
                    "/webservice/fleet/vehicle",
                    "{\"success\":true,\"data\":{\"sabaf_code\":null,\"last_visit\":{\"code\":\"TECH-1\",\"status\":4}}}");
            var client = CreateClient(handler);

            var exception = await Assert.ThrowsAsync<TechnicalInspectionNotAllowedException>(
                () => client.SubmitTechnicalInspectionAsync("12345678", new Dictionary<string, object?>
                {
                    ["smart_number"] = "1234567",
                }));

            Assert.Contains("بازدید فنی فعالی", exception.Message);
            Assert.Equal("TECH-1", exception.VehicleData.GetProperty("last_visit").GetProperty("code").GetString());
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task It_blocks_submission_inside_the_resubmission_cooldown()
        {
            var receivedAt = FormatUtc(DateTimeOffset.UtcNow.AddHours(-2));
            var handler = new FakeHttpMessageHandler()
                .Respond(
                    "/webservice/fleet/vehicle",
                    "{\"success\":true,\"data\":{\"sabaf_code\":\"SABAF-1\",\"sabaf_received_at\":\"" + receivedAt + "\"}}");
            var client = CreateClient(handler);

            var exception = await Assert.ThrowsAsync<TechnicalInspectionNotAllowedException>(
                () => client.SubmitTechnicalInspectionAsync("12345678", new Dictionary<string, object?>
                {
                    ["smart_number"] = "1234567",
                }));

            Assert.Contains("کمتر از 24 ساعت", exception.Message);
        }

        [Fact]
        public async Task It_allows_submission_once_the_cooldown_has_elapsed()
        {
            var receivedAt = FormatUtc(DateTimeOffset.UtcNow.AddHours(-30));
            var handler = new FakeHttpMessageHandler()
                .Respond(
                    "/webservice/fleet/vehicle",
                    "{\"success\":true,\"data\":{\"sabaf_code\":\"SABAF-1\",\"sabaf_received_at\":\"" + receivedAt + "\"}}")
                .Respond("/webservice/technical-inspections", "{\"success\":true,\"data\":{\"bazdidfani\":{\"id\":3,\"code\":\"BZ-3\"}}}");
            var client = CreateClient(handler);

            var response = await client.SubmitTechnicalInspectionAsync("12345678", new Dictionary<string, object?>
            {
                ["smart_number"] = "1234567",
            });

            Assert.True(response.Success);
        }

        [Fact]
        public async Task It_blocks_submission_while_the_sabaf_code_is_still_valid_even_after_the_cooldown()
        {
            var receivedAt = FormatUtc(DateTimeOffset.UtcNow.AddHours(-48));
            var expiresAt = DateTimeOffset.UtcNow.AddHours(1).ToOffset(TimeSpan.FromHours(3.5));
            var handler = new FakeHttpMessageHandler()
                .Respond(
                    "/webservice/fleet/vehicle",
                    "{\"success\":true,\"data\":{\"sabaf_code\":\"SABAF-1\",\"sabaf_received_at\":\"" + receivedAt
                    + "\",\"sabaf_code_expires_at\":\"" + expiresAt.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture) + "\"}}");
            var client = CreateClient(handler);

            var exception = await Assert.ThrowsAsync<TechnicalInspectionNotAllowedException>(
                () => client.SubmitTechnicalInspectionAsync("12345678", new Dictionary<string, object?>
                {
                    ["smart_number"] = "1234567",
                }));

            Assert.Contains("تا " + expiresAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " معتبر است", exception.Message);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task It_allows_submission_once_the_sabaf_code_has_expired_even_inside_the_cooldown()
        {
            var receivedAt = FormatUtc(DateTimeOffset.UtcNow.AddHours(-2));
            var expiresAt = FormatUtc(DateTimeOffset.UtcNow.AddMinutes(-1));
            var handler = new FakeHttpMessageHandler()
                .Respond(
                    "/webservice/fleet/vehicle",
                    "{\"success\":true,\"data\":{\"sabaf_code\":\"SABAF-1\",\"sabaf_received_at\":\"" + receivedAt
                    + "\",\"sabaf_code_expires_at\":\"" + expiresAt + "\"}}")
                .Respond("/webservice/technical-inspections", "{\"success\":true,\"data\":{\"bazdidfani\":{\"id\":5,\"code\":\"BZ-5\"}}}");
            var client = CreateClient(handler);

            var response = await client.SubmitTechnicalInspectionAsync("12345678", new Dictionary<string, object?>
            {
                ["smart_number"] = "1234567",
            });

            Assert.True(response.Success);
        }

        [Fact]
        public async Task Force_create_skips_the_check_and_marks_the_payload()
        {
            var handler = new FakeHttpMessageHandler()
                .Respond("/webservice/technical-inspections", "{\"success\":true,\"data\":{\"bazdidfani\":{\"id\":4,\"code\":\"BZ-4\"}}}");
            var client = CreateClient(handler);

            await client.SubmitTechnicalInspectionAsync(
                "12345678",
                new Dictionary<string, object?> { ["smart_number"] = "1234567" },
                forceCreate: true);

            var submission = Assert.Single(handler.Requests);
            Assert.Contains("\"force_create\":true", submission.Body);
        }

        [Fact]
        public async Task Vehicle_throws_when_the_latest_visit_has_no_sabaf_code()
        {
            var handler = new FakeHttpMessageHandler()
                .Respond("/webservice/fleet/vehicle", "{\"success\":true,\"data\":{\"sabaf_code\":null,\"last_visit\":null}}");
            var client = CreateClient(handler);

            var exception = await Assert.ThrowsAsync<TechnicalInspectionNotAllowedException>(
                () => client.VehicleAsync("12345678", 1234567));

            Assert.Equal("کد سباف برای این ناوگان موجود نیست.", exception.Message);
        }

        [Fact]
        public async Task It_throws_the_server_message_on_a_failed_response()
        {
            var handler = new FakeHttpMessageHandler()
                .Respond(
                    "/webservice/technical-inspections",
                    "{\"success\":false,\"message\":\"دسترسی برای کد سازمانی ارسال‌شده مجاز نیست.\"}",
                    HttpStatusCode.Forbidden);
            var client = CreateClient(handler);

            var exception = await Assert.ThrowsAsync<BazdidfaniApiException>(
                () => client.TechnicalInspectionsAsync("12345678"));

            Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
            Assert.Equal("دسترسی برای کد سازمانی ارسال‌شده مجاز نیست.", exception.Message);
        }

        [Fact]
        public async Task It_does_not_retry_a_client_error()
        {
            var handler = new FakeHttpMessageHandler()
                .Respond("/webservice/cities", "{\"success\":false,\"message\":\"خطا\"}", HttpStatusCode.UnprocessableEntity);
            var client = CreateClient(handler);

            await Assert.ThrowsAsync<BazdidfaniApiException>(() => client.CitiesAsync("12345678"));

            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task It_retries_a_server_error_and_returns_the_successful_retry()
        {
            var handler = new FakeHttpMessageHandler()
                .RespondInSequence(
                    "/webservice/loader-types",
                    (HttpStatusCode.InternalServerError, "{\"success\":false}"),
                    (HttpStatusCode.OK, "{\"success\":true,\"data\":[]}"));
            var client = CreateClient(handler, options => options.RetrySleepMilliseconds = 0);

            var response = await client.LoaderTypesAsync("12345678");

            Assert.True(response.Success);
            Assert.Equal(2, handler.Requests.Count);
        }

        // فرمت میلادیِ ISO مستقل از تقویم سیستم (روی ویندوز فارسی، ToString پیش‌فرض تاریخ شمسی می‌دهد).
        private static string FormatUtc(DateTimeOffset value) =>
            value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        private static BazdidfaniApiClient CreateClient(
            FakeHttpMessageHandler handler,
            Action<BazdidfaniApiOptions>? configure = null)
        {
            var options = new BazdidfaniApiOptions
            {
                BaseUrl = BaseUrl,
                Token = "test-token",
                RetrySleepMilliseconds = 0,
            };

            configure?.Invoke(options);

            return new BazdidfaniApiClient(new HttpClient(handler), options);
        }
    }
}
