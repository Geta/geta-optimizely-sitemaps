using System;
using Geta.Optimizely.Sitemaps.Services;

namespace Geta.Optimizely.Sitemaps.Configuration
{
    public class SitemapOptions
    {
        public bool EnableRealtimeSitemap { get; set; } = false;
        public bool EnableRealtimeCaching { get; set; } = true;
        public bool EnableLanguageDropDownInAdmin { get; set; } = false;
        public Func<IServiceProvider, IUriAugmenterService> UriAugmenterServiceImplementationFactory { get; set; } = null;
    }
}
