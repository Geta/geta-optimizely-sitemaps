using Castle.Core.Internal;
using EPiServer.Web;
using Geta.Mapping;
using Geta.Optimizely.Sitemaps.Entities;
using System;
using System.Collections.Generic;
using EPiServer.DataAbstraction;

namespace Geta.Optimizely.Sitemaps.Models
{
    public class SitemapViewModel
    {
        protected const string SitemapHostPostfix = "sitemap.xml";

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
        public string RootPageId { get; set; } = Constants.DefaultRootPageId.ToString();
        public string SitemapFormat { get; set; }

        public class MapperFromEntity : Mapper<SitemapData, SitemapViewModel>
        {
            private readonly ILanguageBranchRepository _languageBranchRepository;

            public MapperFromEntity(ILanguageBranchRepository languageBranchRepository)
            {
                _languageBranchRepository = languageBranchRepository;
            }

            public override void Map(SitemapData @from, SitemapViewModel to)
            {
                to.Id = from.Id.ToString();
                to.SiteUrl = GetSiteUrl(from);
                to.RelativePath = from.Host;
                to.RelativePathEditPart = GetRelativePathEditPart(from.Host);
                to.EnableLanguageFallback = from.EnableLanguageFallback;
                to.IncludeAlternateLanguagePages = from.IncludeAlternateLanguagePages;
                to.EnableSimpleAddressSupport = from.EnableSimpleAddressSupport;
                to.PathsToAvoid = from.PathsToAvoid != null ? string.Join("; ", from.PathsToAvoid) : string.Empty;
                to.PathsToInclude = from.PathsToInclude != null ? string.Join("; ", from.PathsToInclude) : string.Empty;
                to.IncludeDebugInfo = from.IncludeDebugInfo;
                to.RootPageId = from.RootPageId.ToString();
                to.SitemapFormat = from.SitemapFormat.ToString();
                to.LanguageBranch = from.Language;
            }

            private string GetLanguage(string language)
            {
                if (string.IsNullOrWhiteSpace(language) || SiteDefinition.WildcardHostName.Equals(language))
                {
                    return string.Empty;
                }

                var languageBranch = _languageBranchRepository.Load(language);
                return $"{languageBranch.URLSegment}/";
            }

            private string GetSiteUrl(SitemapData sitemapData)
            {
                var language = GetLanguage(sitemapData.Language);

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

                return hostName.Substring(0, hostName.IndexOf(SitemapHostPostfix, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public class MapperToEntity : Mapper<SitemapViewModel, SitemapData>
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
                var value = input?.Trim();

                return string.IsNullOrEmpty(value)
                    ? new List<string>()
                    : new List<string>(value.Split(';'));
            }

            private int TryParse(string id)
            {
                if (int.TryParse(id, out var rootId))
                {
                    return rootId;
                };
                return Constants.DefaultRootPageId;
            }

            private SitemapFormat GetSitemapFormat(string format)
            {
                return Enum.TryParse<SitemapFormat>(format, out var sitemapFormat)
                    ? sitemapFormat
                    : Entities.SitemapFormat.Standard;
            }
        }
    }
}
