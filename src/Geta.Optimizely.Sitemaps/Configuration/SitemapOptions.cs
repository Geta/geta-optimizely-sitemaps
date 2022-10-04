using System.Linq;
using Geta.Optimizely.Sitemaps.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Geta.Optimizely.Sitemaps.Configuration
{
    public class SitemapOptions
    {
        public bool EnableRealtimeSitemap { get; set; } = false;
        public bool EnableRealtimeCaching { get; set; } = true;
        public bool EnableLanguageDropDownInAdmin { get; set; } = false;

        public void SetAugmenterService<T>(IServiceCollection services) where T : class, IUriAugmenterService
        {
            var augmenterService = services.First(sd => sd.ServiceType == typeof(IUriAugmenterService));
            // Remove the existing service in order to replace it.
            services.Remove(augmenterService);
            
            services.AddSingleton<IUriAugmenterService, T>();
        }
    }
}
