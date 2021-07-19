using Geta.Optimizely.Sitemaps.XML;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.Optimizely.Sitemaps.Commerce
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSitemapsCommerce(this IServiceCollection services)
        {
            services.AddTransient<ICommerceAndStandardSitemapXmlGenerator, CommerceAndStandardSitemapXmlGenerator>();
            services.AddTransient<ICommerceSitemapXmlGenerator, CommerceSitemapXmlGenerator>();

            return services;
        }
    }
}