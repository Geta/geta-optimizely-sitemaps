using Castle.Core.Internal;
using EPiServer.Web;
using Geta.Mapping;
using Geta.SEO.Sitemaps.Entities;
using System;
using System.Collections.Generic;

namespace Geta.SEO.Sitemaps.Models
{
    public class SitemapViewModel
    {
        protected const string SitemapHostPostfix = "Sitemap.xml";

        public string Id { get; set; }
        public string SiteUrl { get; set; }
        public string LanguageBranch { get; set; }
        public string RelativePath { get; set; }
        public string RelativePathEditPart { get; set; }
        public bool EnableLanguageFallback { get; set; }
        public bool IncludeAlternateLanguagePages { get; set; }
        public bool EnableSimpleAddressSupport { get; set; }
        public string PathsToAvoid { get; set; }
        public string PathsToInclude { get; set; }
        public bool IncludeDebugInfo { get; set; }
        public string RootPageId { get; set; }
        public string SitemapFormat { get; set; }


        public void MapToViewModel(SitemapData from, string language)
        {
            Id = from.Id.ToString();
            SiteUrl = GetSiteUrl(from, language);
            RelativePath = from.Host;
            RelativePathEditPart = GetRelativePathEditPart(from.Host);
            EnableLanguageFallback = from.EnableLanguageFallback;
            IncludeAlternateLanguagePages = from.IncludeAlternateLanguagePages;
            EnableSimpleAddressSupport = from.EnableSimpleAddressSupport;
            PathsToAvoid = from.PathsToAvoid != null ? string.Join("; ", from.PathsToAvoid) : string.Empty;
            PathsToInclude = from.PathsToInclude != null ? string.Join("; ", from.PathsToInclude) : string.Empty;
            IncludeDebugInfo = from.IncludeDebugInfo;
            RootPageId = from.RootPageId.ToString();
            SitemapFormat = from.SitemapFormat.ToString();

        }

        public class Mapper : Mapper<SitemapViewModel, SitemapData>
        {
            public override void Map(SitemapViewModel @from, SitemapData to)
            {
                var relativePart = !from.RelativePath.IsNullOrEmpty()
                    ? from.RelativePath + SitemapHostPostfix
                    : from.RelativePathEditPart + SitemapHostPostfix;

                to.SiteUrl = from.SiteUrl;
                to.Host = relativePart;
                to.Language = from.LanguageBranch;
                to.EnableLanguageFallback = from.EnableLanguageFallback;
                to.IncludeAlternateLanguagePages = from.IncludeAlternateLanguagePages;
                to.EnableSimpleAddressSupport = from.EnableSimpleAddressSupport;
                to.PathsToAvoid = GetList(from.PathsToAvoid);
                to.PathsToInclude = GetList(from.PathsToInclude);
                to.IncludeDebugInfo = from.IncludeDebugInfo;
                to.RootPageId = TryParse(from.RootPageId);
                to.SitemapFormat = GetSitemapFormat(from.SitemapFormat);
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

            private int TryParse(string id)
            {
                int.TryParse(id, out var rootId);
                return rootId;
            }

            private SitemapFormat GetSitemapFormat(string format)
            {
                return Enum.TryParse<SitemapFormat>(format, out var sitemapFormat)
                    ? sitemapFormat
                    : Entities.SitemapFormat.Standard;
            }
        }

        private string GetSiteUrl(SitemapData sitemapData, string language)
        {
            if (sitemapData.SiteUrl != null)
            {
                return $"{sitemapData.SiteUrl}{language}{sitemapData.Host}";
            }

            var site = SiteDefinition.Current.SiteUrl.ToString();

            return $"{site}{language}{sitemapData.Host}";
        }

        private string GetRelativePathEditPart(string hostName)
        {
            if (hostName == null)
            {
                return string.Empty;
            }

            return hostName.Substring(0, hostName.IndexOf(SitemapHostPostfix, StringComparison.InvariantCulture));
        }
    }
}
