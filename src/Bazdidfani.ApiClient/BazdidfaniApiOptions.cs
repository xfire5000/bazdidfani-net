namespace Bazdidfani.ApiClient
{
    /// <summary>
    /// تنظیمات کلاینت وب‌سرویس بازدیدفنی. معادل فایل config پکیج Laravel است.
    /// کد سازمان اینجا نگهداری نمی‌شود و باید در هر فراخوانی ارسال شود.
    /// </summary>
    public sealed class BazdidfaniApiOptions
    {
        /// <summary>آدرس پایهٔ سرور، مثلاً https://api.example.com (بدون مسیر وب‌سرویس).</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>توکن اختصاصی سایت که سرور بازدید فنی صادر کرده است (bdf_...).</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>نام هدری که کد سازمان داخل آن ارسال می‌شود.</summary>
        public string OrganizationHeader { get; set; } = "X-Organization-Code";

        /// <summary>مهلت هر درخواست بر حسب ثانیه.</summary>
        public int TimeoutSeconds { get; set; } = 15;

        /// <summary>حداکثر تعداد تلاش برای هر درخواست (۱ یعنی بدون تلاش مجدد).</summary>
        public int RetryTimes { get; set; } = 2;

        /// <summary>فاصلهٔ بین تلاش‌ها بر حسب میلی‌ثانیه.</summary>
        public int RetrySleepMilliseconds { get; set; } = 200;

        /// <summary>
        /// حداقل فاصله (ساعت) از دریافت کد سباف آخرین بازدید تا مجاز شدن ثبت بازدید جدید؛ فقط وقتی
        /// استفاده می‌شود که کد سباف تاریخ انقضا (<c>sabaf_code_expires_at</c>) نداشته باشد.
        /// </summary>
        public int ResubmissionCooldownHours { get; set; } = 24;
    }
}
