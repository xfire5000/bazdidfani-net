using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bazdidfani.ApiClient
{
    /// <summary>
    /// نمای یک شرکت مشخص از کلاینت: کد سازمان یک‌بار داده می‌شود و روی همهٔ فراخوانی‌های این نما
    /// اعمال می‌شود. برای نرم‌افزاری که همزمان به چند شرکت سرویس می‌دهد، به‌ازای هر شرکت یک نما
    /// بسازید؛ خود کلاینت و <see cref="System.Net.Http.HttpClient"/> مشترک باقی می‌مانند.
    /// </summary>
    public sealed class BazdidfaniOrganizationScope
    {
        private readonly IBazdidfaniApiClient _client;

        internal BazdidfaniOrganizationScope(IBazdidfaniApiClient client, string organizationCode)
        {
            if (string.IsNullOrWhiteSpace(organizationCode))
            {
                throw new ArgumentException("کد سازمان برای ارسال درخواست الزامی است.", nameof(organizationCode));
            }

            _client = client ?? throw new ArgumentNullException(nameof(client));
            OrganizationCode = organizationCode.Trim();
        }

        /// <summary>کد سازمانی که روی همهٔ فراخوانی‌های این نما ارسال می‌شود.</summary>
        public string OrganizationCode { get; }

        /// <inheritdoc cref="IBazdidfaniApiClient.TechnicalInspectionsAsync" />
        public Task<BazdidfaniApiResponse> TechnicalInspectionsAsync(
            object? query = null,
            CancellationToken cancellationToken = default) =>
            _client.TechnicalInspectionsAsync(OrganizationCode, query, cancellationToken);

        /// <inheritdoc cref="IBazdidfaniApiClient.SelfStatementsAsync" />
        public Task<BazdidfaniApiResponse> SelfStatementsAsync(
            object? query = null,
            CancellationToken cancellationToken = default) =>
            _client.SelfStatementsAsync(OrganizationCode, query, cancellationToken);

        /// <inheritdoc cref="IBazdidfaniApiClient.SubmitTechnicalInspectionAsync" />
        public Task<BazdidfaniApiResponse> SubmitTechnicalInspectionAsync(
            object payload,
            bool forceCreate = false,
            CancellationToken cancellationToken = default) =>
            _client.SubmitTechnicalInspectionAsync(OrganizationCode, payload, forceCreate, cancellationToken);

        /// <inheritdoc cref="IBazdidfaniApiClient.EnsureNewTechnicalInspectionAllowedAsync" />
        public Task EnsureNewTechnicalInspectionAllowedAsync(
            long smartNumber,
            bool forceCreate = false,
            CancellationToken cancellationToken = default) =>
            _client.EnsureNewTechnicalInspectionAllowedAsync(
                OrganizationCode,
                smartNumber,
                forceCreate,
                cancellationToken);

        /// <inheritdoc cref="IBazdidfaniApiClient.TechnicalInspectionInquiryCargoAsync" />
        public Task<BazdidfaniApiResponse> TechnicalInspectionInquiryCargoAsync(
            string externalTechnicalInspectionId,
            CancellationToken cancellationToken = default) =>
            _client.TechnicalInspectionInquiryCargoAsync(
                OrganizationCode,
                externalTechnicalInspectionId,
                cancellationToken);

        /// <inheritdoc cref="IBazdidfaniApiClient.TechnicalInspectionInquiryPassengerAsync" />
        public Task<BazdidfaniApiResponse> TechnicalInspectionInquiryPassengerAsync(
            string externalTechnicalInspectionId,
            CancellationToken cancellationToken = default) =>
            _client.TechnicalInspectionInquiryPassengerAsync(
                OrganizationCode,
                externalTechnicalInspectionId,
                cancellationToken);

        /// <inheritdoc cref="IBazdidfaniApiClient.TechnicalManagersAsync" />
        public Task<BazdidfaniApiResponse> TechnicalManagersAsync(
            object? query = null,
            CancellationToken cancellationToken = default) =>
            _client.TechnicalManagersAsync(OrganizationCode, query, cancellationToken);

        /// <inheritdoc cref="IBazdidfaniApiClient.FleetAsync" />
        public Task<BazdidfaniApiResponse> FleetAsync(
            object? query = null,
            CancellationToken cancellationToken = default) =>
            _client.FleetAsync(OrganizationCode, query, cancellationToken);

        /// <inheritdoc cref="IBazdidfaniApiClient.VehicleAsync" />
        public Task<BazdidfaniApiResponse> VehicleAsync(
            long smartNumber,
            CancellationToken cancellationToken = default) =>
            _client.VehicleAsync(OrganizationCode, smartNumber, cancellationToken);

        /// <inheritdoc cref="IBazdidfaniApiClient.CitiesAsync" />
        public Task<BazdidfaniApiResponse> CitiesAsync(
            object? query = null,
            CancellationToken cancellationToken = default) =>
            _client.CitiesAsync(OrganizationCode, query, cancellationToken);

        /// <inheritdoc cref="IBazdidfaniApiClient.StatesAsync" />
        public Task<BazdidfaniApiResponse> StatesAsync(
            object? query = null,
            CancellationToken cancellationToken = default) =>
            _client.StatesAsync(OrganizationCode, query, cancellationToken);

        /// <inheritdoc cref="IBazdidfaniApiClient.LoaderTypesAsync" />
        public Task<BazdidfaniApiResponse> LoaderTypesAsync(
            object? query = null,
            CancellationToken cancellationToken = default) =>
            _client.LoaderTypesAsync(OrganizationCode, query, cancellationToken);
    }
}
