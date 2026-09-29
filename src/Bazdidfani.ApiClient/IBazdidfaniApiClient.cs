using System.Threading;
using System.Threading.Tasks;

namespace Bazdidfani.ApiClient
{
    /// <summary>
    /// کلاینت وب‌سرویس بازدیدفنی. کد سازمان به توکن گره نخورده و در هر فراخوانی جداگانه ارسال
    /// می‌شود، بنابراین یک نمونه از کلاینت می‌تواند به هر تعداد شرکت سرویس بدهد.
    /// </summary>
    public interface IBazdidfaniApiClient
    {
        /// <summary>فهرست صفحه‌بندی‌شدهٔ بازدیدهای فنی شرکت.</summary>
        /// <param name="organizationCode">کد سازمان شرکت موردنظر.</param>
        /// <param name="query">پارامترهای اختیاری مثل <c>page</c>، <c>per_page</c>، <c>status</c>، <c>query</c>.</param>
        /// <param name="cancellationToken">توکن لغو.</param>
        Task<BazdidfaniApiResponse> TechnicalInspectionsAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default);

        /// <summary>فهرست صفحه‌بندی‌شدهٔ خوداظهاری‌های شرکت.</summary>
        Task<BazdidfaniApiResponse> SelfStatementsAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// ثبت یک بازدید فنی جدید برای شرکت متعلق به کد سازمان داده‌شده. پیش از ارسال با
        /// <see cref="EnsureNewTechnicalInspectionAllowedAsync"/> بررسی می‌شود که آخرین بازدید همان
        /// ناوگان نزد همین شرکت مانع ثبت بازدید جدید نباشد.
        /// </summary>
        /// <param name="organizationCode">کد سازمان شرکت موردنظر.</param>
        /// <param name="payload">بدنهٔ درخواست؛ دیکشنری یا شیئی که کلیدهای snake_case تولید کند.</param>
        /// <param name="forceCreate">با <c>true</c> بررسی بازدید فعال نادیده گرفته و <c>force_create</c> به بدنه اضافه می‌شود.</param>
        /// <param name="cancellationToken">توکن لغو.</param>
        Task<BazdidfaniApiResponse> SubmitTechnicalInspectionAsync(
            string organizationCode,
            object payload,
            bool forceCreate = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// بررسی می‌کند ثبت یک بازدید فنی جدید برای این ناوگان نزد همین شرکت مجاز است یا نه.
        /// </summary>
        /// <exception cref="Exceptions.TechnicalInspectionNotAllowedException">وقتی ثبت بازدید جدید مجاز نباشد.</exception>
        Task EnsureNewTechnicalInspectionAllowedAsync(
            string organizationCode,
            long smartNumber,
            bool forceCreate = false,
            CancellationToken cancellationToken = default);

        /// <summary>استعلام بازدید فنی باری از سازمان.</summary>
        Task<BazdidfaniApiResponse> TechnicalInspectionInquiryCargoAsync(
            string organizationCode,
            string externalTechnicalInspectionId,
            CancellationToken cancellationToken = default);

        /// <summary>استعلام بازدید فنی مسافری از سازمان.</summary>
        Task<BazdidfaniApiResponse> TechnicalInspectionInquiryPassengerAsync(
            string organizationCode,
            string externalTechnicalInspectionId,
            CancellationToken cancellationToken = default);

        /// <summary>فهرست مدیران فنی فعال شرکت.</summary>
        Task<BazdidfaniApiResponse> TechnicalManagersAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default);

        /// <summary>فهرست ناوگان شرکت.</summary>
        Task<BazdidfaniApiResponse> FleetAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// اطلاعات کامل یک ناوگان بر اساس کد هوشمند: مشخصات ناوگان، کد سباف و مدیر فنیِ آخرین بازدید،
        /// و اطلاعات شرکت.
        /// </summary>
        /// <exception cref="Exceptions.TechnicalInspectionNotAllowedException">
        /// وقتی آخرین بازدید این ناوگان هنوز کد سباف ندارد. دادهٔ خام پاسخ از طریق
        /// <c>VehicleData</c> همان استثنا در دسترس است.
        /// </exception>
        Task<BazdidfaniApiResponse> VehicleAsync(
            string organizationCode,
            long smartNumber,
            CancellationToken cancellationToken = default);

        /// <summary>فهرست شهرها (دادهٔ مرجع، مستقل از شرکت).</summary>
        Task<BazdidfaniApiResponse> CitiesAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default);

        /// <summary>فهرست استان‌ها (دادهٔ مرجع، مستقل از شرکت).</summary>
        Task<BazdidfaniApiResponse> StatesAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default);

        /// <summary>فهرست انواع بارگیر، برای پر کردن <c>loader_code</c> هنگام ثبت بازدید.</summary>
        Task<BazdidfaniApiResponse> LoaderTypesAsync(
            string organizationCode,
            object? query = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// یک نمای سبک از همین کلاینت که کد سازمان را برای همهٔ فراخوانی‌هایش نگه می‌دارد؛ مناسب
        /// جایی که نرم‌افزار همزمان به چند شرکت سرویس می‌دهد.
        /// </summary>
        BazdidfaniOrganizationScope ForOrganization(string organizationCode);
    }
}
