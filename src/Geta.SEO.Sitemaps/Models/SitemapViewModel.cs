using System.Collections.Generic;
using Castle.Core.Internal;
using EPiServer.Validation.Internal;
using Geta.Mapping;
using Geta.SEO.Sitemaps.Entities;

namespace Geta.SEO.Sitemaps.Models
{
    public class SitemapViewModel
    {
        protected const string SitemapHostPostfix = "Sitemap.xml";

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

        public void MapToViewModel(SitemapData from)
        {
            Host = from.Host;
            EnableLanguageFallback = from.EnableLanguageFallback;
            IncludeAlternateLanguagePages = from.IncludeAlternateLanguagePages;
            EnableSimpleAddressSupport = from.EnableSimpleAddressSupport;
            PathsToAvoid = from.PathsToAvoid != null ? string.Join("; ", from.PathsToAvoid) : string.Empty;
            PathsToInclude = from.PathsToInclude != null ? string.Join("; ", from.PathsToInclude) : string.Empty;
            IncludeDebugInfo = from.IncludeDebugInfo;
            RootPageId = from.RootPageId.ToString();
            SitemapFormFormat = from.SitemapFormat.ToString();

        }

        public class Mapper : Mapper<SitemapViewModel, SitemapData>
        {
            public override void Map(SitemapViewModel @from, SitemapData to)
            {
                var host = to.Host.IsNullOrEmpty()
                    ? from.Host + SitemapHostPostfix
                    : from.Host;

                to.SiteUrl = from.SiteUrl;
                to.Host = host;
                to.Language = from.LanguageBranch;
                to.EnableLanguageFallback = from.EnableLanguageFallback;
                to.IncludeAlternateLanguagePages = from.IncludeAlternateLanguagePages;
                to.EnableSimpleAddressSupport = from.EnableSimpleAddressSupport;
                to.PathsToAvoid = GetList(from.PathsToAvoid);
                to.PathsToInclude = GetList(from.PathsToInclude);
                to.IncludeDebugInfo = from.IncludeDebugInfo;
                to.SitemapFormat = GetSitemapFormat(from.SitemapFormFormat);
                to.RootPageId = TryParse(from.RootPageId);
            }

            private IList<string> GetList(string input)
            {
                if (input == null)
                {
                    return null;
                }

                var strValue = input.Trim();

                if (string.IsNullOrEmpty(strValue))
                {
                    return null;
                }

                return new List<string>(strValue.Split(';'));
            }

            private SitemapFormat GetSitemapFormat(string format)
            {
                if (format == SitemapFormat.Mobile.ToString())
                {
                    return SitemapFormat.Mobile;
                }

                if (format == SitemapFormat.Commerce.ToString())
                {
                    return SitemapFormat.Commerce;
                }

                if (format == SitemapFormat.StandardAndCommerce.ToString())
                {
                    return SitemapFormat.StandardAndCommerce;
                }

                return SitemapFormat.Standard;
            }

            private int TryParse(string id)
            {
                int rootId;
                int.TryParse(id, out rootId);

                return rootId;
            }
        }
    }
}
