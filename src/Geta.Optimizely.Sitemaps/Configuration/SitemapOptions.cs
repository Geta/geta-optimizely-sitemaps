using System;
using Geta.Optimizely.Sitemaps.Services;

namespace Geta.Optimizely.Sitemaps.Configuration
{
    public class SitemapOptions
    {
        public bool EnableRealtimeSitemap { get; set; } = false;
        public bool EnableRealtimeCaching { get; set; } = true;
        public bool EnableLanguageDropDownInAdmin { get; set; } = false;

        /// <summary>
        /// The default is Mvc, this runs a check in the default content filter to ensure there's a page for every piece of content
        /// Set this to headless if you are running a headless site to skip this check
        /// </summary>
        public SiteArchitecture SiteArchitecture { get; set; } = SiteArchitecture.Mvc;

        /// <summary>
        /// Enabled by default, this will, when using the default Content Filter, assume that content that can't be cast to IVersionable is unpublished
        /// Consider disabling if you are finding that the default content filter is not generating content you're expecting for your sitemap
        /// </summary>
        public bool IsStrictPublishCheckingEnabled { get; set; } = true;

        public Type UriAugmenterService { get; set; } = typeof(DefaultUriAugmenterService);

        public void SetAugmenterService<T>() where T : class, IUriAugmenterService
        {
            UriAugmenterService = typeof(T);
        }
    }
}
