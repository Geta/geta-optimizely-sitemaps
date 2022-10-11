// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Cache;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.Optimizely.Sitemaps.Entities;
using Geta.Optimizely.Sitemaps.Models;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Services;
using Geta.Optimizely.Sitemaps.SpecializedProperties;
using Geta.Optimizely.Sitemaps.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Geta.Optimizely.Sitemaps.XML
{
    public abstract class SitemapXmlGenerator : ISitemapXmlGenerator
    {
        protected const int MaxSitemapEntryCount = 50000;
        protected ISet<string> UrlSet { get; private set; }
        protected bool StopGeneration { get; private set; }
        protected string HostLanguageBranch { get; set; }

        protected const string DateTimeFormat = "yyyy-MM-ddTHH:mm:sszzz";

        protected readonly ISitemapRepository SitemapRepository;
        protected readonly IContentRepository ContentRepository;
        protected readonly IUrlResolver UrlResolver;
        protected readonly ISiteDefinitionRepository SiteDefinitionRepository;
        protected readonly ILanguageBranchRepository LanguageBranchRepository;
        protected readonly IContentFilter ContentFilter;
        private readonly IUriAugmenterService _uriAugmenterService;
        private readonly ISynchronizedObjectInstanceCache _objectCache;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<SitemapXmlGenerator> _logger;

        protected SitemapData SitemapData { get; set; }
        protected SiteDefinition SiteSettings { get; set; }
        protected IEnumerable<LanguageBranch> EnabledLanguages { get; set; }
        protected IEnumerable<CurrentLanguageContent> HrefLanguageContents { get; set; }

        protected static XNamespace SitemapXmlNamespace => @"http://www.sitemaps.org/schemas/sitemap/0.9";
        protected static XNamespace SitemapXhtmlNamespace => @"http://www.w3.org/1999/xhtml";

        public bool IsDebugMode { get; set; }

        private readonly Regex _dashRegex = new Regex("[-]+", RegexOptions.Compiled);

        protected SitemapXmlGenerator(
            ISitemapRepository sitemapRepository,
            IContentRepository contentRepository,
            IUrlResolver urlResolver,
            ISiteDefinitionRepository siteDefinitionRepository,
            ILanguageBranchRepository languageBranchRepository,
            IContentFilter contentFilter,
            IUriAugmenterService uriAugmenterService,
            ISynchronizedObjectInstanceCache objectCache,
            IMemoryCache memoryCache,
            ILogger<SitemapXmlGenerator> logger)
        {
            SitemapRepository = sitemapRepository;
            ContentRepository = contentRepository;
            UrlResolver = urlResolver;
            SiteDefinitionRepository = siteDefinitionRepository;
            LanguageBranchRepository = languageBranchRepository;
            EnabledLanguages = LanguageBranchRepository.ListEnabled();
            UrlSet = new HashSet<string>();
            ContentFilter = contentFilter;
            _uriAugmenterService = uriAugmenterService;
            _objectCache = objectCache;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        protected virtual XElement GenerateRootElement()
        {
            var rootElement = new XElement(SitemapXmlNamespace + "urlset");

            if (SitemapData.IncludeAlternateLanguagePages)
            {
                rootElement.Add(new XAttribute(XNamespace.Xmlns + "xhtml", SitemapXhtmlNamespace));
            }

            return rootElement;
        }

        /// <summary>
        /// Generates a xml sitemap about pages on site
        /// </summary>
        /// <param name="sitemapData">SitemapData object containing configuration info for sitemap</param>
        /// <param name="persistData">True if the sitemap data should be persisted in DDS</param>
        /// <param name="entryCount">out count of site entries in generated sitemap</param>
        /// <returns>True if sitemap generation successful, false if error encountered</returns>
        public virtual bool Generate(SitemapData sitemapData, bool persistData, out int entryCount)
        {
            try
            {
                SitemapData = sitemapData;
                var sitemapSiteUri = new Uri(SitemapData.SiteUrl);
                SiteSettings = GetSiteDefinitionFromSiteUri(sitemapSiteUri);
                HostLanguageBranch = GetHostLanguageBranch();
                SiteDefinition.Current = SiteSettings;
                var sitemap = CreateSitemapXmlContents(out entryCount);

                var doc = new XDocument(new XDeclaration("1.0", "utf-8", null));
                doc.Add(sitemap);

                using (var ms = new MemoryStream())
                {
                    var xtw = new XmlTextWriter(ms, new UTF8Encoding(false));
                    doc.Save(xtw);
                    xtw.Flush();
                    sitemapData.Data = ms.ToArray();
                }

                if (persistData && !StopGeneration)
                {
                    SitemapRepository.Save(sitemapData);
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error on generating xml sitemap");
                entryCount = 0;
                return false;
            }
        }

        public void Stop()
        {
            StopGeneration = true;
        }

        /// <summary>
        /// Creates xml content for a given sitemap configuration entity
        /// </summary>
        /// <param name="entryCount">out: count of sitemap entries in the returned element</param>
        /// <returns>XElement that contains sitemap entries according to the configuration</returns>
        private XElement CreateSitemapXmlContents(out int entryCount)
        {
            var sitemapXmlElements = GetSitemapXmlElements();

            var sitemapElement = GenerateRootElement();

            sitemapElement.Add(sitemapXmlElements);

            entryCount = UrlSet.Count;
            return sitemapElement;
        }

        protected virtual IEnumerable<XElement> GetSitemapXmlElements()
        {
            if (SiteSettings == null)
            {
                return Enumerable.Empty<XElement>();
            }

            var rootPage = SitemapData.RootPageId < 0 ? SiteSettings.StartPage : new ContentReference(SitemapData.RootPageId);

            var descendants = ContentRepository.GetDescendents(rootPage).ToList();

            if (!ContentReference.RootPage.CompareToIgnoreWorkID(rootPage))
            {
                descendants.Add(rootPage);
            }

            return GenerateXmlElements(descendants);
        }

        protected virtual IEnumerable<XElement> GenerateXmlElements(IEnumerable<ContentReference> pages)
        {
            var sitemapXmlElements = new List<XElement>();

            foreach (var contentReference in pages)
            {
                if (StopGeneration)
                {
                    return Enumerable.Empty<XElement>();
                }

                if (TryGet<IExcludeFromSitemap>(contentReference, out _))
                {
                    continue;
                }

                if (ContentReference.IsNullOrEmpty(contentReference))
                {
                    continue;
                }

                var contentLanguages = GetLanguageBranches(contentReference)
                    .Where(x => x.Content is not ILocale localeContent
                                || !ExcludeContentLanguageFromSitemap(localeContent.Language));

                foreach (var contentLanguageInfo in contentLanguages)
                {
                    if (StopGeneration)
                    {
                        return Enumerable.Empty<XElement>();
                    }

                    if (UrlSet.Count >= MaxSitemapEntryCount)
                    {
                        SitemapData.ExceedsMaximumEntryCount = true;
                        return sitemapXmlElements;
                    }

                    AddFilteredContentElement(contentLanguageInfo, sitemapXmlElements);
                }
            }

            return sitemapXmlElements;
        }

        protected virtual IEnumerable<CurrentLanguageContent> GetLanguageBranches(ContentReference contentLink)
        {
            var isSpecificLanguage = !string.IsNullOrWhiteSpace(SitemapData.Language);

            if (isSpecificLanguage)
            {
                var languageSelector = !SitemapData.EnableLanguageFallback
                    ? new LanguageSelector(SitemapData.Language)
                    : LanguageSelector.Fallback(SitemapData.Language, false);

                return TryGet<IContent>(contentLink, out var contentData, languageSelector)
                    ? new[]
                    {
                        new CurrentLanguageContent
                        {
                            Content = contentData,
                            CurrentLanguage = new CultureInfo(SitemapData.Language),
                            MasterLanguage = GetMasterLanguage(contentData)
                        }
                    }
                    : Enumerable.Empty<CurrentLanguageContent>();
            }

            if (SitemapData.EnableLanguageFallback)
            {
                return GetFallbackLanguageBranches(contentLink);
            }

            if (TryGetLanguageBranches<IContent>(contentLink, out var contentLanguages))
            {
                return contentLanguages.Select(x => new CurrentLanguageContent
                {
                    Content = x,
                    CurrentLanguage = GetCurrentLanguage(x),
                    MasterLanguage = GetMasterLanguage(x)
                });
            }

            return Enumerable.Empty<CurrentLanguageContent>();
        }

        protected virtual IEnumerable<CurrentLanguageContent> GetFallbackLanguageBranches(ContentReference contentLink)
        {
            return EnabledLanguages
                .Select(languageBranch => new
                {
                    languageBranch,
                    languageContent =
                        ContentRepository.Get<IContent>(contentLink,
                                                        LanguageSelector.Fallback(languageBranch.Culture.Name, false))
                })
                .Where(@t => @t.languageContent != null)
                .Select(@t => new CurrentLanguageContent
                {
                    Content = @t.languageContent,
                    CurrentLanguage = @t.languageBranch.Culture,
                    MasterLanguage = GetMasterLanguage(@t.languageContent)
                });
        }

        protected virtual IEnumerable<HrefLangData> GetHrefLangDataFromCache(ContentReference contentLink)
        {
            var cacheKey = $"HrefLangData-{contentLink.ToReferenceWithoutVersion()}";

            if (_objectCache.Get(cacheKey) is IEnumerable<HrefLangData> cachedObject) return cachedObject;

            cachedObject = GetHrefLangData(contentLink);
            var policy = new CacheEvictionPolicy(TimeSpan.FromMinutes(10),
                                                 CacheTimeoutType.Absolute,
                                                 new[] { "SitemapGenerationKey" });
            _objectCache.Insert(cacheKey, cachedObject, policy);

            return cachedObject;
        }

        protected virtual IEnumerable<HrefLangData> GetHrefLangData(ContentReference contentLink)
        {
            var languageData = EnabledLanguages
                .Select(x => (languageBranch: x, languageContent: GetLanguageContent(contentLink, x)))
                .Where(x => x.languageContent != null && !ContentFilter.ShouldExcludeContent(x.languageContent));

            foreach (var (languageBranch, languageContent) in languageData)
            {
                var hrefLangData =
                    CreateHrefLangData(languageContent, languageBranch.Culture, GetMasterLanguage(languageContent));
                yield return hrefLangData;

                if (hrefLangData.HrefLang == "x-default")
                {
                    yield return new() { HrefLang = languageBranch.Culture.Name.ToLowerInvariant(), Href = hrefLangData.Href };
                }
            }
        }

        private IContent GetLanguageContent(ContentReference contentLink, LanguageBranch languageBranch)
        {
            return ContentRepository.Get<IContent>(contentLink, LanguageSelector.Fallback(languageBranch.Culture.Name, false));
        }

        protected virtual HrefLangData CreateHrefLangData(IContent content, CultureInfo language, CultureInfo masterLanguage)
        {
            string languageUrl;
            string masterLanguageUrl;

            if (SitemapData.EnableSimpleAddressSupport
                && content is PageData pageData
                && !string.IsNullOrWhiteSpace(pageData.ExternalURL))
            {
                languageUrl = pageData.ExternalURL;

                TryGet(content.ContentLink, out IContent masterContent, new LanguageSelector(masterLanguage.Name));

                masterLanguageUrl = string.Empty;
                if (masterContent is PageData masterPageData && !string.IsNullOrWhiteSpace(masterPageData.ExternalURL))
                {
                    masterLanguageUrl = masterPageData.ExternalURL;
                }
            }
            else
            {
                languageUrl = UrlResolver.GetUrl(content.ContentLink, language.Name);
                masterLanguageUrl = UrlResolver.GetUrl(content.ContentLink, masterLanguage.Name);
            }


            var data = new HrefLangData();

            if (languageUrl.Equals(masterLanguageUrl) && content.ContentLink.CompareToIgnoreWorkID(SiteSettings.StartPage))
            {
                data.HrefLang = "x-default";
            }
            else
            {
                data.HrefLang = language.Name.ToLowerInvariant();
            }

            data.Href = GetAbsoluteUrl(languageUrl);
            return data;
        }

        protected virtual XElement GenerateSiteElement(IContent contentData, string url)
        {
            var modified = DateTime.Now.AddMonths(-1);

            if (contentData is IChangeTrackable changeTrackableContent)
            {
                modified = changeTrackableContent.Saved;
            }
            else if (contentData is IVersionable versionableContent && versionableContent.StartPublish.HasValue)
            {
                modified = versionableContent.StartPublish.Value;
            }

            var property = contentData.Property[PropertySEOSitemaps.PropertyName] as PropertySEOSitemaps;

            var element = new XElement(
                SitemapXmlNamespace + "url",
                new XElement(SitemapXmlNamespace + "loc", url),
                new XElement(SitemapXmlNamespace + "lastmod", modified.ToString(DateTimeFormat, CultureInfo.InvariantCulture)),
                new XElement(SitemapXmlNamespace + "changefreq",
                             (property != null && !property.IsNull) ? property.ChangeFreq : "weekly"),
                new XElement(SitemapXmlNamespace + "priority",
                             (property != null && !property.IsNull) ? property.Priority : GetPriority(url))
            );

            if (SitemapData.IncludeAlternateLanguagePages)
            {
                AddHrefLangToElement(contentData, element);
            }

            if (IsDebugMode)
            {
                var language = contentData is ILocale localeContent ? localeContent.Language : CultureInfo.InvariantCulture;
                var contentName = _dashRegex.Replace(contentData.Name, string.Empty);

                element.AddFirst(
                    new XComment($"page ID: '{contentData.ContentLink.ID}', name: '{contentName}', language: '{language.Name}'"));
            }

            return element;
        }

        protected virtual void AddHrefLangToElement(IContent content, XElement element)
        {
            var localeContent = content as ILocalizable;

            if (localeContent == null)
            {
                return;
            }

            var hrefLangDatas = GetHrefLangDataFromCache(content.ContentLink).ToList();
            var count = hrefLangDatas.Count;

            if (count < 2)
            {
                return;
            }

            if (count == 2 && hrefLangDatas.Count(x => x.HrefLang == "x-default") == 1)
            {
                return;
            }

            foreach (var hrefLangData in hrefLangDatas)
            {
                element.Add(CreateHrefLangElement(hrefLangData));
            }
        }

        protected virtual void AddFilteredContentElement(
            CurrentLanguageContent languageContentInfo,
            IList<XElement> xmlElements)
        {
            if (ContentFilter.ShouldExcludeContent(languageContentInfo, SiteSettings, SitemapData))
            {
                return;
            }

            var content = languageContentInfo.Content;

            var url = GetContentUrl(languageContentInfo, content);

            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            url = GetAbsoluteUrl(url);

            var contentUrl = new Uri(url);

            foreach (var fullContentUrl in _uriAugmenterService.GetAugmentUris(content, languageContentInfo, contentUrl))
            {
                var fullUrl = fullContentUrl.ToString();

                if (UrlSet.Contains(fullUrl) || UrlFilter.IsUrlFiltered(fullContentUrl.AbsolutePath, SitemapData))
                {
                    continue;
                }

                var contentElement = GenerateSiteElement(content, fullUrl);

                if (contentElement == null)
                {
                    continue;
                }

                xmlElements.Add(contentElement);
                UrlSet.Add(fullUrl);
            }
        }

        protected virtual XElement CreateHrefLangElement(HrefLangData data)
        {
            return new XElement(
                SitemapXhtmlNamespace + "link",
                new XAttribute("rel", "alternate"),
                new XAttribute("hreflang", data.HrefLang),
                new XAttribute("href", data.Href)
            );
        }

        protected virtual string GetPriority(string url)
        {
            var depth = new Uri(url).Segments.Length - 1;

            return Math.Max(1.0 - (depth / 10.0), 0.5).ToString(CultureInfo.InvariantCulture);
        }

#pragma warning disable CA1822
        protected CultureInfo GetCurrentLanguage(IContent content)
#pragma warning restore CA1822
        {
            if (content is ILocalizable localizableContent)
            {
                return localizableContent.Language;
            }

            return CultureInfo.InvariantCulture;
        }

#pragma warning disable CA1822
        protected CultureInfo GetMasterLanguage(IContent content)
#pragma warning restore CA1822
        {
            if (content is ILocalizable localizableContent)
            {
                return localizableContent.MasterLanguage;
            }

            return CultureInfo.InvariantCulture;
        }

        public SiteDefinition GetSiteDefinitionFromSiteUri(Uri sitemapSiteUri)
        {
            return SiteDefinitionRepository
                .List()
                .FirstOrDefault(siteDef => siteDef.SiteUrl == sitemapSiteUri || siteDef.Hosts.Any(
                                    hostDef => hostDef.Name.Equals(sitemapSiteUri.Authority,
                                                                   StringComparison.InvariantCultureIgnoreCase)));
        }

        protected string GetHostLanguageBranch()
        {
            var hostDefinition = GetHostDefinition();
            return hostDefinition?.Language?.Name;
        }

        protected bool HostDefinitionExistsForLanguage(string languageBranch)
        {
            var cacheKey = $"HostDefinitionExistsFor{SitemapData.SiteUrl}-{languageBranch}";
            var cachedObject = _memoryCache.Get(cacheKey);

            if (cachedObject != null)
            {
                return (bool)cachedObject;
            }

            cachedObject =
                SiteSettings.Hosts.Any(
                    x =>
                        x.Language != null
                        && x.Language.ToString().Equals(languageBranch, StringComparison.InvariantCultureIgnoreCase));

            _memoryCache.Set(cacheKey, cachedObject, DateTime.Now.AddMinutes(10));

            return (bool)cachedObject;
        }

        protected HostDefinition GetHostDefinition()
        {
            var siteUrl = new Uri(SitemapData.SiteUrl);
            var sitemapHost = siteUrl.Authority;

            return SiteSettings.Hosts.FirstOrDefault(x => x.Name.Equals(sitemapHost, StringComparison.InvariantCultureIgnoreCase))
                   ?? SiteSettings.Hosts.FirstOrDefault(x => x.Name.Equals(SiteDefinition.WildcardHostName));
        }

        protected bool ExcludeContentLanguageFromSitemap(CultureInfo language)
        {
            return HostLanguageBranch != null
                   && !HostLanguageBranch.Equals(language.Name, StringComparison.InvariantCultureIgnoreCase)
                   && HostDefinitionExistsForLanguage(language.Name);
        }

        protected string GetAbsoluteUrl(string url)
        {
            return UriUtil.Combine(SitemapData.SiteUrl,
                                   IsAbsoluteUrl(url, out var absoluteUri)
                                       ? absoluteUri.AbsolutePath
                                       : url);
        }

#pragma warning disable CA1822
        protected bool IsAbsoluteUrl(string url, out Uri absoluteUri)
#pragma warning restore CA1822
        {
            return Uri.TryCreate(url, UriKind.Absolute, out absoluteUri);
        }

        protected bool TryGet<T>(ContentReference contentLink, out T content, LoaderOptions settings = null)
            where T : IContentData
        {
            content = default;
            try
            {
                T local;
                var status = settings != null
                    ? ContentRepository.TryGet(contentLink, settings, out local)
                    : ContentRepository.TryGet(contentLink, out local);
                content = local;
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error TryGet for {ContentLinkName}: {ContentLinkId}", nameof(contentLink), contentLink?.ID);
            }

            return false;
        }

        protected bool TryGetLanguageBranches<T>(ContentReference contentLink, out IEnumerable<T> content) where T : IContentData
        {
            content = Enumerable.Empty<T>();
            try
            {
                content = ContentRepository.GetLanguageBranches<T>(contentLink);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                                 "Error TryGetLanguageBranches for {ContentLinkName}: {ContentLinkId}",
                                 nameof(contentLink),
                                 contentLink?.ID);
            }

            return false;
        }

        private string GetContentUrl(CurrentLanguageContent languageContentInfo, IContent content)
        {
            string url = null;

            if (SitemapData.EnableSimpleAddressSupport
                && content is PageData pageData
                && !string.IsNullOrWhiteSpace(pageData.ExternalURL))
            {
                url = pageData.ExternalURL;
            }

            if (!string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            if (content is not ILocalizable localizableContent)
            {
                return UrlResolver.GetUrl(content.ContentLink);
            }

            var language = string.IsNullOrWhiteSpace(SitemapData.Language)
                ? languageContentInfo.CurrentLanguage.Name
                : SitemapData.Language;

            url = UrlResolver.GetUrl(content.ContentLink, language);
            url = EnsureCorrectUrlHostLanguage(localizableContent, url);

            return url;
        }

        private string EnsureCorrectUrlHostLanguage(ILocalizable localizableContent, string url)
        {
            if (string.IsNullOrEmpty(url)) return url;

            // Make 100% sure we remove the language part in the URL if the sitemap host is mapped to the page's LanguageBranch.
            if (HostLanguageBranch != null
                && localizableContent.Language.Name.Equals(HostLanguageBranch,
                                                           StringComparison.InvariantCultureIgnoreCase))
            {
                url = url.Replace($"/{HostLanguageBranch}/", "/");
            }

            return url;
        }
    }
}
