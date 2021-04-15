using Castle.Core.Internal;
using Geta.Mapping;
using Geta.SEO.Sitemaps.Entities;
using System;
using System.Collections.Generic;

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
                var emptyList = new List<string>();
                if (input == null)
                {
                    return emptyList;
                }

                var strValue = input.Trim();

                if (string.IsNullOrEmpty(strValue))
                {
                    return emptyList;
                }

                return new List<string>(strValue.Split(';'));
            }

            private SitemapFormat GetSitemapFormat(string format)
            {
                if (format == null)
                {
                    return SitemapFormat.Standard;
                }

                var sitemapFormat = Enum.Parse<SitemapFormat>(format);
                return sitemapFormat switch
                {
                    SitemapFormat.Mobile => SitemapFormat.Mobile,
                    SitemapFormat.Commerce => SitemapFormat.Commerce,
                    SitemapFormat.StandardAndCommerce => SitemapFormat.StandardAndCommerce,
                    _ => SitemapFormat.Standard
                };
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
