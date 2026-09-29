using System;
using Microsoft.Extensions.DependencyInjection;

namespace Bazdidfani.ApiClient.DependencyInjection
{
    /// <summary>
    /// ثبت کلاینت در کانتینر تزریق وابستگی. همتای <c>BazdidfaniApiClientServiceProvider</c> پکیج Laravel.
    /// </summary>
    public static class BazdidfaniApiClientServiceCollectionExtensions
    {
        /// <summary>
        /// کلاینت را به‌صورت typed client روی <c>IHttpClientFactory</c> ثبت می‌کند. کد سازمان جزو
        /// تنظیمات نیست و در هر فراخوانی ارسال می‌شود.
        /// </summary>
        public static IHttpClientBuilder AddBazdidfaniApiClient(
            this IServiceCollection services,
            Action<BazdidfaniApiOptions> configure)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var options = new BazdidfaniApiOptions();
            configure(options);

            services.AddSingleton(options);

            return services
                .AddHttpClient<IBazdidfaniApiClient, BazdidfaniApiClient>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
                });
        }
    }
}
