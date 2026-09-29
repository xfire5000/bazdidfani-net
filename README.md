# Bazdidfani .NET API Client

کلاینت دات‌نت برای وب‌سرویس بازدید فنی و خوداظهاری بازدیدفنی — همتای C# پکیج
`bazdidfani/laravel-api-client` با همان متدها و همان قواعد.

مثل نسخهٔ PHP، **کد سازمان در تنظیمات نگهداری نمی‌شود** و در هر فراخوانی به‌صورت اجباری ارسال
می‌شود؛ بنابراین یک نمونه از کلاینت می‌تواند همزمان به هر تعداد شرکت سرویس بدهد.

## سازگاری

| هدف | پشتیبانی | موتور JSON | تزریق وابستگی (`AddBazdidfaniApiClient`) |
| --- | --- | --- | --- |
| `net46` | .NET Framework 4.6.0 (WinForms/WPF) | Newtonsoft.Json | ندارد — از سازندهٔ بدون DI استفاده کنید |
| `netstandard2.0` | .NET Framework 4.6.1+، Mono، Xamarin | System.Text.Json | دارد |
| `net6.0` | .NET 6 و بالاتر (ASP.NET Core، Worker، Console) | System.Text.Json | دارد |

`Microsoft.Extensions.Http` و `System.Text.Json` (حتی قدیمی‌ترین نسخه‌شان) پایین‌تر از net461 پابرجا
نیستند؛ برای همین target مربوط به net46 موتور JSON جداگانه‌ای (Newtonsoft.Json) دارد و اکستنشن DI را
شامل نمی‌شود. نوع عمومی `JsonValue` (در `Bazdidfani.ApiClient.Json`) این تفاوت را از کد مصرف‌کننده
پنهان می‌کند — روی هر سه target همان اعضا (`ValueKind`، `GetProperty`، `GetString`، `Deserialize<T>` و
…) را دارد، فقط پیاده‌سازی داخلی‌اش بسته به target فرق می‌کند.

## نصب

پروژه را به‌صورت ProjectReference اضافه کنید:

```bash
dotnet add YourApp.csproj reference packages/bazdidfani-api-client-dotnet/src/Bazdidfani.ApiClient/Bazdidfani.ApiClient.csproj
```

یا بسته را بسازید و از فید داخلی نصب کنید:

```bash
dotnet pack packages/bazdidfani-api-client-dotnet/src/Bazdidfani.ApiClient -c Release
```

## تنظیمات

```csharp
var options = new BazdidfaniApiOptions
{
    BaseUrl = "https://api.example.com",   // بدون مسیر وب‌سرویس
    Token = "bdf_replace-with-the-issued-site-token",
    OrganizationHeader = "X-Organization-Code", // اختیاری
    TimeoutSeconds = 15,
    RetryTimes = 2,
    RetrySleepMilliseconds = 200,
    ResubmissionCooldownHours = 24,
};
```

برای هر سایت باید از سرور بازدید فنی یک توکن اختصاصی `bdf_...` دریافت کنید. این توکن به هیچ سازمان
خاصی محدود نیست و برای هر کد سازمانی معتبر در سرور قابل استفاده است. `OrganizationHeader` فقط نام
هدری است که کد سازمان داخل آن می‌رود و معمولاً نیازی به تغییرش نیست.

## استفاده با تزریق وابستگی (ASP.NET Core / Worker)

```csharp
using Bazdidfani.ApiClient.DependencyInjection;

builder.Services.AddBazdidfaniApiClient(options =>
{
    options.BaseUrl = builder.Configuration["Bazdidfani:BaseUrl"]!;
    options.Token = builder.Configuration["Bazdidfani:Token"]!;
});
```

```csharp
public sealed class InspectionService
{
    private readonly IBazdidfaniApiClient _bazdidfaniApi;

    public InspectionService(IBazdidfaniApiClient bazdidfaniApi) => _bazdidfaniApi = bazdidfaniApi;

    public async Task<BazdidfaniApiResponse> ListAsync(string organizationCode) =>
        await _bazdidfaniApi.TechnicalInspectionsAsync(organizationCode, new
        {
            page = 1,
            per_page = 20,
            status = 1,
        });
}
```

## استفاده بدون DI (WinForms/WPF/Console)

```csharp
using var client = new BazdidfaniApiClient(options);

var inspections = await client.TechnicalInspectionsAsync("12345678", new { page = 1 });
```

در برنامه‌های بدون DI یک نمونه از کلاینت را برای تمام عمر برنامه نگه دارید و در پایان Dispose کنید؛
ساختن نمونهٔ جدید برای هر درخواست، سوکت‌های سیستم را مصرف می‌کند.

## کد سازمان به‌ازای هر شرکت

کد سازمان پارامتر اول همهٔ متدهاست:

```csharp
var companyA = await client.FleetAsync("12345678");
var companyB = await client.FleetAsync("87654321");
```

اگر نرم‌افزار شما همزمان چند شرکت را مدیریت می‌کند، به‌ازای هر شرکت یک «نما» بسازید تا کد سازمان
یک‌بار داده شود و دیگر در هیچ فراخوانی تکرار (و اشتباه) نشود. کلاینت و `HttpClient` مشترک می‌مانند:

```csharp
var company = client.ForOrganization(selectedCompany.OrganizationCode);

var fleet = await company.FleetAsync(new { per_page = 50 });
var managers = await company.TechnicalManagersAsync();
var vehicle = await company.VehicleAsync(1234567);
```

## متدهای موجود

| متد | مسیر | توضیح |
| --- | --- | --- |
| `TechnicalInspectionsAsync` | `GET /technical-inspections` | فهرست بازدیدهای فنی |
| `SelfStatementsAsync` | `GET /self-statements` | فهرست خوداظهاری‌ها |
| `SubmitTechnicalInspectionAsync` | `POST /technical-inspections` | ثبت بازدید فنی جدید |
| `EnsureNewTechnicalInspectionAllowedAsync` | `GET /fleet/vehicle` | بررسی مجاز بودن ثبت بازدید جدید |
| `TechnicalInspectionInquiryCargoAsync` | `POST /technical-inspections/inquiry-cargo` | استعلام بازدید باری |
| `TechnicalInspectionInquiryPassengerAsync` | `POST /technical-inspections/inquiry-passenger` | استعلام بازدید مسافری |
| `TechnicalManagersAsync` | `GET /technical-managers` | مدیران فنی فعال شرکت |
| `FleetAsync` | `GET /fleet` | ناوگان شرکت |
| `VehicleAsync` | `GET /fleet/vehicle` | اطلاعات کامل یک ناوگان |
| `CitiesAsync` / `StatesAsync` / `LoaderTypesAsync` | `GET /cities` و … | داده‌های مرجع |

## ثبت بازدید فنی

```csharp
var response = await client.SubmitTechnicalInspectionAsync("12345678", new Dictionary<string, object?>
{
    ["usage"] = "freighter",                         // freighter | passenger
    ["company_usage"] = 1,                           // 1=باری، 2=مسافری، 3=هردو
    ["user_type"] = "company",
    ["smart_number"] = "1234567",
    ["loader_code"] = 100,
    ["technical_manager_national_code"] = "0012345678",
    ["driver_national_code"] = "0011223344",
});

var code = response.Data.GetProperty("bazdidfani").GetProperty("code").GetString();
```

کلیدهای بدنه دقیقاً همان نام‌های snake_case سرور هستند. اگر به‌جای دیکشنری از DTO استفاده می‌کنید،
روی هر پراپرتی `[JsonPropertyName("smart_number")]` بگذارید.

پیش از ارسال، کلاینت خودش `EnsureNewTechnicalInspectionAllowedAsync` را صدا می‌زند و اگر همان ناوگان
نزد همین شرکت بازدید فعالی داشته باشد یا کد سباف آخرین بازدیدش هنوز معتبر باشد (`sabaf_code_expires_at`
هنوز نرسیده باشد)، `TechnicalInspectionNotAllowedException` پرتاب می‌شود. اگر کد سباف تاریخ انقضا نداشته
باشد (کدهای قدیمی یا مسیر خوداظهاری)، قاعدهٔ قبلی اعمال می‌شود: باید حداقل `ResubmissionCooldownHours`
ساعت از `sabaf_received_at` گذشته باشد. برای نادیده گرفتن این
بررسی (مثل ابطال بازدید فعلی و ثبت بازدید جدید در خود سامانه):

```csharp
await client.SubmitTechnicalInspectionAsync("12345678", payload, forceCreate: true);
```

## خواندن پاسخ

```csharp
var response = await client.FleetAsync("12345678", new { per_page = 50 });

bool success = response.Success;      // کلید success
string? message = response.Message;   // پیام فارسی سرور
JsonValue data = response.Data;       // کلید data — نوع مستقل از target، نه JsonElement

// تبدیل به مدل خودتان
var vehicles = response.DeserializeData<List<FleetVehicleDto>>();
```

## استعلام بازدید فنی از سازمان

شناسهٔ `external_technical_inspection_id` را از پاسخ `VehicleAsync` بگیرید و بسته به نوع ناوگان یکی
از دو متد استعلام را صدا بزنید. پاسخ موفق کد سباف و تاریخ انقضای اعتبار آن را دارد و سامانه هر دو را
ذخیره می‌کند (از این پس `sabaf_code_expires_at` در پاسخ `VehicleAsync` و در `technical_inspection`
آیتم‌های `TechnicalInspectionsAsync` هم برمی‌گردد):

```csharp
var result = await company.TechnicalInspectionInquiryCargoAsync(externalTechnicalInspectionId);

var inquiry = result.DeserializeData<SabafInquiryDto>();
bool isExpired = inquiry?.SabafCodeExpiresAt is { } expiresAt && expiresAt <= DateTimeOffset.Now;

public sealed class SabafInquiryDto
{
    [JsonPropertyName("sabaf_code")]
    public string SabafCode { get; set; } = "";

    // ISO-8601 به وقت تهران، مثل "2026-09-29T14:55:00+03:30"؛ برای کدهای قدیمی null است
    [JsonPropertyName("sabaf_code_expires_at")]
    public DateTimeOffset? SabafCodeExpiresAt { get; set; }
}
```

اگر سازمان کد سباف صادر نکند، سرور `400` با پیام سازمان برمی‌گرداند و `BazdidfaniApiException`
پرتاب می‌شود.

## مدیریت خطا

```csharp
try
{
    var vehicle = await client.VehicleAsync("12345678", 1234567);
}
catch (TechnicalInspectionNotAllowedException exception)
{
    // آخرین بازدید هنوز کد سباف ندارد؛ دادهٔ خام پاسخ در دسترس است
    var raw = exception.VehicleData;
}
catch (BazdidfaniApiException exception)
{
    // پاسخ 4xx/5xx سرور
    var status = exception.StatusCode;   // مثلاً HttpStatusCode.Forbidden
    var message = exception.Message;     // پیام فارسی خود سرور
    var body = exception.Body;           // بدنهٔ JSON پاسخ
}
```

## تلاش مجدد

درخواست‌ها فقط روی خطاهای گذرا (قطعی شبکه، تایم‌اوت، `408`، `429` و `5xx`) تا `RetryTimes` بار تکرار
می‌شوند. برخلاف نسخهٔ PHP، خطاهای `4xx` (مثل `422` اعتبارسنجی یا `403` کد سازمان) تکرار نمی‌شوند؛
این کار هم بی‌فایده است و هم برای درخواست‌های POST خطر ثبت تکراری دارد.

## اجرای تست پکیج

شاخهٔ System.Text.Json (مشترک بین net6.0 و netstandard2.0) با xUnit:

```bash
dotnet test packages/bazdidfani-api-client-dotnet/tests/Bazdidfani.ApiClient.Tests
```

شاخهٔ Newtonsoft.Json/net46 را یک برنامهٔ کنسولی جداگانه پوشش می‌دهد — چون `xunit.runner.visualstudio`
خودش پایین‌تر از net462 را پشتیبانی نمی‌کند و نمی‌توان تست‌های xUnit را مستقیماً روی net46 اجرا کرد:

```bash
dotnet run --project packages/bazdidfani-api-client-dotnet/tests/Bazdidfani.ApiClient.Net46SmokeTest -c Release
```

## نکتهٔ تقویم

تاریخ‌های ارسالی و دریافتی همگی میلادی (ISO-8601) هستند. کلاینت همیشه با `InvariantCulture` تجزیه
می‌کند، اما اگر در کد خودتان تاریخ را با `ToString()` می‌سازید، حتماً `CultureInfo.InvariantCulture`
بدهید؛ روی ویندوزِ با زبان فارسی، فرمت پیش‌فرض تاریخ شمسی تولید می‌کند و سرور آن را نمی‌پذیرد.
