using System;
using Bazdidfani.ApiClient.Json;

namespace Bazdidfani.ApiClient.Exceptions
{
    /// <summary>
    /// پرتاب می‌شود وقتی <c>VehicleAsync</c> فراخوانی می‌شود ولی آخرین بازدید ناوگان هنوز کد سباف
    /// ندارد (چه هیچ بازدیدی ثبت نشده باشد، چه بازدید فعلی در حال انجام باشد)، یا وقتی ناوگان بازدید
    /// فنیِ تکمیل‌شده‌ای در کمتر از بازهٔ خنک‌سازی (پیش‌فرض ۲۴ ساعت) دارد.
    /// </summary>
    public sealed class TechnicalInspectionNotAllowedException : Exception
    {
        /// <param name="message">پیام خطا.</param>
        /// <param name="vehicleData">
        /// دادهٔ خامِ پاسخ ناوگان که این خطا از آن نتیجه شده؛ مثلاً برای استخراج دستیِ
        /// <c>external_technical_inspection_id</c> یا بررسی <c>last_visit</c>.
        /// </param>
        /// <param name="innerException">استثنای قبلی در صورت وجود.</param>
        public TechnicalInspectionNotAllowedException(
            string message,
            JsonValue vehicleData = default,
            Exception? innerException = null)
            : base(message, innerException)
        {
            VehicleData = vehicleData;
        }

        /// <summary>دادهٔ خام ناوگان (محتوای کلید <c>data</c> پاسخ سرور).</summary>
        public JsonValue VehicleData { get; }
    }
}
