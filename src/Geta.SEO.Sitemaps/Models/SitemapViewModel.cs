namespace Geta.SEO.Sitemaps.Models
{
    public class SitemapViewModel
    {
        public string SiteUrl { get; set; }
        public string LanguageBranch { get; set; }
        public string Host { get; set; }
        public bool EnableLanguageFallback { get; set; }
        public bool IncludeAlternateLanguagePages { get; set; }
        public bool EnableSimpleAddressSupport { get; set; }
        public string PathsToAvoid { get; set; }
        public string PathsToInclude { get; set; }
        public bool IncludeDebugInfo { get; set; }
        public string RootPageId { get; set; }
        public string SitemapFormFormat { get; set; }
    }
}
