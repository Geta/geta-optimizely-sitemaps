using System;
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

        public Type UriAugmenterService { get; set; }

        public void SetAugmenterService<T>() where T : class, IUriAugmenterService
        {
            UriAugmenterService = typeof(T);
        }
    }
}
