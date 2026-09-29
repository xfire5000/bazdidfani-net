using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bazdidfani.ApiClient.Exceptions;
using Bazdidfani.ApiClient.Json;

namespace Bazdidfani.ApiClient
{
    /// <summary>
    /// پیاده‌سازی کلاینت وب‌سرویس بازدیدفنی روی <see cref="HttpClient"/>.
    /// همتای دات‌نتیِ پکیج <c>bazdidfani/laravel-api-client</c>.
    /// </summary>
    public sealed class BazdidfaniApiClient : IBazdidfaniApiClient, IDisposable
    {
        private const string TechnicalInspectionsPath = "/api/v1/webservice/technical-inspections";
        private const string SelfStatementsPath = "/api/v1/webservice/self-statements";
        private const string InquiryCargoPath = "/api/v1/webservice/technical-inspections/inquiry-cargo";
        private const string InquiryPassengerPath = "/api/v1/webservice/technical-inspections/inquiry-passenger";
        private const string TechnicalManagersPath = "/api/v1/webservice/technical-managers";
        private const string FleetPath = "/api/v1/webservice/fleet";
        private const string VehiclePath = "/api/v1/webservice/fleet/vehicle";
        private const string CitiesPath = "/api/v1/webservice/cities";
        private const string StatesPath = "/api/v1/webservice/states";
        private const string LoaderTypesPath = "/api/v1/webservice/loader-types";

        private readonly HttpClient _httpClient;
        private readonly BazdidfaniApiOptions _options;
        private readonly bool _ownsHttpClient;

        /// <summary>
        /// کلاینت را با یک <see cref="HttpClient"/> ساختهٔ خودش می‌سازد؛ مناسب برنامه‌های بدون DI
        /// (مثلاً WinForms یا WPF). نمونهٔ ساخته‌شده باید Dispose شود.
        /// </summary>
        public BazdidfaniApiClient(BazdidfaniApiOptions options)
            : this(new HttpClient(), options, ownsHttpClient: true)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
        }

        /// <summary>
        /// کلاینت را با یک <see cref="HttpClient"/> مدیریت‌شده از بیرون می‌سازد؛ مسیر استاندارد
        /// <c>IHttpClientFactory</c> و تزریق وابستگی. مهلت درخواست باید روی همان HttpClient تنظیم شود.
        /// </summary>
        public BazdidfaniApiClient(HttpClient httpClient, BazdidfaniApiOptions options)
            : this(httpClient, options, ownsHttpClient: false)
        {
        }

        private BazdidfaniApiClient(HttpClient httpClient, BazdidfaniApiOptions options, bool ownsHttpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _ownsHttpClient = ownsHttpClient;
        }

        /// <inheritdoc />
        public Task<BazdidfaniApiResponse> TechnicalInspectionsAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default) =>
            GetAsync(TechnicalInspectionsPath, organizationCode, query, cancellationToken);

        /// <inheritdoc />
        public Task<BazdidfaniApiResponse> SelfStatementsAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default) =>
            GetAsync(SelfStatementsPath, organizationCode, query, cancellationToken);

        /// <inheritdoc />
        public async Task<BazdidfaniApiResponse> SubmitTechnicalInspectionAsync(
            string organizationCode,
            object payload,
            bool forceCreate = false,
            CancellationToken cancellationToken = default)
        {
            if (payload is null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            var body = JsonObjectBuilder.FromObject(payload);

            await EnsureNewTechnicalInspectionAllowedAsync(
                organizationCode,
                ReadSmartNumber(body),
                forceCreate,
                cancellationToken).ConfigureAwait(false);

            if (forceCreate)
            {
                body.SetBool("force_create", true);
            }

            return await PostAsync(TechnicalInspectionsPath, organizationCode, body.ToJsonString(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task EnsureNewTechnicalInspectionAllowedAsync(
            string organizationCode,
            long smartNumber,
            bool forceCreate = false,
            CancellationToken cancellationToken = default)
        {
            if (forceCreate)
            {
                return;
            }

            BazdidfaniApiResponse vehicle;

            try
            {
                vehicle = await VehicleAsync(organizationCode, smartNumber, cancellationToken).ConfigureAwait(false);
            }
            catch (TechnicalInspectionNotAllowedException exception)
            {
                EnsureNoActiveVisitBlocksNewTechnicalInspection(exception.VehicleData);

                return;
            }
            catch (BazdidfaniApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            EnsureSabafCodeHasExpired(vehicle.Data);
        }

        /// <inheritdoc />
        public Task<BazdidfaniApiResponse> TechnicalInspectionInquiryCargoAsync(
            string organizationCode,
            string externalTechnicalInspectionId,
            CancellationToken cancellationToken = default) =>
            PostAsync(
                InquiryCargoPath,
                organizationCode,
                JsonCompat.Serialize(new { external_technical_inspection_id = externalTechnicalInspectionId }),
                cancellationToken);

        /// <inheritdoc />
        public Task<BazdidfaniApiResponse> TechnicalInspectionInquiryPassengerAsync(
            string organizationCode,
            string externalTechnicalInspectionId,
            CancellationToken cancellationToken = default) =>
            PostAsync(
                InquiryPassengerPath,
                organizationCode,
                JsonCompat.Serialize(new { external_technical_inspection_id = externalTechnicalInspectionId }),
                cancellationToken);

        /// <inheritdoc />
        public Task<BazdidfaniApiResponse> TechnicalManagersAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default) =>
            GetAsync(TechnicalManagersPath, organizationCode, query, cancellationToken);

        /// <inheritdoc />
        public Task<BazdidfaniApiResponse> FleetAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default) =>
            GetAsync(FleetPath, organizationCode, query, cancellationToken);

        /// <inheritdoc />
        public async Task<BazdidfaniApiResponse> VehicleAsync(
            string organizationCode,
            long smartNumber,
            CancellationToken cancellationToken = default)
        {
            var vehicle = await GetAsync(
                VehiclePath,
                organizationCode,
                new { smart_number = smartNumber },
                cancellationToken).ConfigureAwait(false);

            var data = vehicle.Data;

            if (IsMissingValue(data, "sabaf_code"))
            {
                throw new TechnicalInspectionNotAllowedException("کد سباف برای این ناوگان موجود نیست.", data);
            }

            return vehicle;
        }

        /// <inheritdoc />
        public Task<BazdidfaniApiResponse> CitiesAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default) =>
            GetAsync(CitiesPath, organizationCode, query, cancellationToken);

        /// <inheritdoc />
        public Task<BazdidfaniApiResponse> StatesAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default) =>
            GetAsync(StatesPath, organizationCode, query, cancellationToken);

        /// <inheritdoc />
        public Task<BazdidfaniApiResponse> LoaderTypesAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default) =>
            GetAsync(LoaderTypesPath, organizationCode, query, cancellationToken);

        /// <inheritdoc />
        public BazdidfaniOrganizationScope ForOrganization(string organizationCode) =>
            new BazdidfaniOrganizationScope(this, organizationCode);

        /// <summary>در صورتی که کلاینت خودش <see cref="HttpClient"/> را ساخته باشد آن را آزاد می‌کند.</summary>
        public void Dispose()
        {
            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }
        }

        private Task<BazdidfaniApiResponse> GetAsync(
            string path,
            string organizationCode,
            object? query,
            CancellationToken cancellationToken) =>
            SendAsync(HttpMethod.Get, path, organizationCode, query, payloadJson: null, cancellationToken);

        private Task<BazdidfaniApiResponse> PostAsync(
            string path,
            string organizationCode,
            string payloadJson,
            CancellationToken cancellationToken) =>
            SendAsync(HttpMethod.Post, path, organizationCode, query: null, payloadJson, cancellationToken);

        private async Task<BazdidfaniApiResponse> SendAsync(
            HttpMethod method,
            string path,
            string organizationCode,
            object? query,
            string? payloadJson,
            CancellationToken cancellationToken)
        {
            organizationCode = (organizationCode ?? string.Empty).Trim();

            EnsureConfigured(organizationCode);

            var requestUri = BuildRequestUri(path, query);
            var attempts = Math.Max(1, _options.RetryTimes);
            var retrySleep = Math.Max(0, _options.RetrySleepMilliseconds);

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    using var request = BuildRequest(method, requestUri, organizationCode, payloadJson);
                    using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        return new BazdidfaniApiResponse(JsonCompat.Parse(body));
                    }

                    if (attempt < attempts && IsTransient(response.StatusCode))
                    {
                        await DelayAsync(retrySleep, cancellationToken).ConfigureAwait(false);

                        continue;
                    }

                    throw BazdidfaniApiException.FromResponse(response.StatusCode, body);
                }
                catch (HttpRequestException) when (attempt < attempts)
                {
                    await DelayAsync(retrySleep, cancellationToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < attempts)
                {
                    await DelayAsync(retrySleep, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private HttpRequestMessage BuildRequest(
            HttpMethod method,
            string requestUri,
            string organizationCode,
            string? payloadJson)
        {
            var request = new HttpRequestMessage(method, requestUri);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
            request.Headers.TryAddWithoutValidation(OrganizationHeaderName(), organizationCode);

            if (payloadJson != null)
            {
                request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private string BuildRequestUri(string path, object? query)
        {
            var url = _options.BaseUrl.TrimEnd('/') + path;
            var queryString = BuildQueryString(query);

            return queryString.Length == 0 ? url : url + "?" + queryString;
        }

        private static string BuildQueryString(object? query)
        {
            if (query is null)
            {
                return string.Empty;
            }

            var root = JsonCompat.Parse(JsonCompat.Serialize(query));

            if (root.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var property in root.EnumerateObject())
            {
                var value = FormatQueryValue(property.Value);

                if (value is null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('&');
                }

                builder.Append(Uri.EscapeDataString(property.Key));
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(value));
            }

            return builder.ToString();
        }

        private static string? FormatQueryValue(JsonValue value) => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };

        private static long ReadSmartNumber(JsonObjectBuilder payload)
        {
            var raw = payload.GetString("smart_number");

            return raw != null && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var smartNumber)
                ? smartNumber
                : 0;
        }

        private static void EnsureNoActiveVisitBlocksNewTechnicalInspection(JsonValue vehicleData)
        {
            if (IsMissingValue(vehicleData, "last_visit"))
            {
                return;
            }

            throw new TechnicalInspectionNotAllowedException(
                "این ناوگان بازدید فنی فعالی نزد این شرکت دارد؛ ثبت بازدید فنی جدید مجاز نیست. برای ابطال یا ادامهٔ بازدید در سامانهٔ بازدید فنی اقدام کنید.",
                vehicleData);
        }

        /// <summary>
        /// تا وقتی کد سباف آخرین بازدید معتبر است (<c>sabaf_code_expires_at</c> در آینده)، ثبت بازدید جدید
        /// مجاز نیست. برای کدهایی که تاریخ انقضا ندارند (کدهای قدیمی و مسیر خوداظهاری) همان قاعدهٔ
        /// <see cref="EnsureResubmissionCooldownHasElapsed"/> بر اساس <c>sabaf_received_at</c> اعمال می‌شود.
        /// </summary>
        private void EnsureSabafCodeHasExpired(JsonValue vehicleData)
        {
            if (!TryGetDateTimeOffset(vehicleData, "sabaf_code_expires_at", out var sabafCodeExpiresAt))
            {
                EnsureResubmissionCooldownHasElapsed(vehicleData);

                return;
            }

            if (sabafCodeExpiresAt <= DateTimeOffset.UtcNow)
            {
                return;
            }

            throw new TechnicalInspectionNotAllowedException(
                $"کد سباف آخرین بازدید فنی این ناوگان نزد این شرکت تا {sabafCodeExpiresAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} معتبر است؛ ثبت بازدید فنی جدید مجاز نیست.",
                vehicleData);
        }

        private void EnsureResubmissionCooldownHasElapsed(JsonValue vehicleData)
        {
            if (!TryGetDateTimeOffset(vehicleData, "sabaf_received_at", out var sabafReceivedAt))
            {
                return;
            }

            var elapsedHours = (DateTimeOffset.UtcNow - sabafReceivedAt).TotalHours;

            if (elapsedHours >= _options.ResubmissionCooldownHours)
            {
                return;
            }

            throw new TechnicalInspectionNotAllowedException(
                $"کمتر از {_options.ResubmissionCooldownHours} ساعت از دریافت کد سباف آخرین بازدید فنی این ناوگان نزد این شرکت گذشته است؛ ثبت بازدید فنی جدید مجاز نیست.",
                vehicleData);
        }

        /// <summary>
        /// تاریخ یک فیلد رشته‌ای را می‌خواند. تاریخ ISO با offset (مثل <c>+03:30</c>) همان offset را نگه
        /// می‌دارد؛ تاریخ بدون offset (مثل <c>sabaf_received_at</c>) UTC فرض می‌شود.
        /// </summary>
        private static bool TryGetDateTimeOffset(JsonValue element, string propertyName, out DateTimeOffset value)
        {
            value = default;

            if (element.ValueKind != JsonValueKind.Object
                || !element.TryGetProperty(propertyName, out var property)
                || property.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var raw = property.GetString();

            return !string.IsNullOrWhiteSpace(raw)
                && DateTimeOffset.TryParse(
                    raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out value);
        }

        private static bool IsMissingValue(JsonValue element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
            {
                return true;
            }

            return value.ValueKind switch
            {
                JsonValueKind.Undefined => true,
                JsonValueKind.Null => true,
                JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()),
                _ => false,
            };
        }

        private static bool IsTransient(HttpStatusCode statusCode) =>
            statusCode == HttpStatusCode.RequestTimeout
            || (int)statusCode == 429
            || (int)statusCode >= 500;

        private static Task DelayAsync(int milliseconds, CancellationToken cancellationToken) =>
            milliseconds <= 0 ? Task.CompletedTask : Task.Delay(milliseconds, cancellationToken);

        private string OrganizationHeaderName() =>
            string.IsNullOrWhiteSpace(_options.OrganizationHeader)
                ? "X-Organization-Code"
                : _options.OrganizationHeader.Trim();

        private void EnsureConfigured(string organizationCode)
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                throw new InvalidOperationException("مقدار BaseUrl (BAZDIDFANI_API_BASE_URL) تنظیم نشده است.");
            }

            if (string.IsNullOrWhiteSpace(_options.Token))
            {
                throw new InvalidOperationException("مقدار Token (BAZDIDFANI_API_TOKEN) تنظیم نشده است.");
            }

            if (organizationCode.Length == 0)
            {
                throw new ArgumentException("کد سازمان برای ارسال درخواست الزامی است.", nameof(organizationCode));
            }
        }
    }
}
