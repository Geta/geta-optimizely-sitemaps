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
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Models;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.SpecializedProperties;
using Geta.SEO.Sitemaps.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Geta.SEO.Sitemaps.XML
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
        private readonly IMemoryCache _cache;
        private readonly ILogger<SitemapXmlGenerator> _logger;

        protected SitemapData SitemapData { get; set; }
        protected SiteDefinition SiteSettings { get; set; }
        protected IEnumerable<LanguageBranch> EnabledLanguages { get; set; }
        protected IEnumerable<CurrentLanguageContent> HrefLanguageContents { get; set; }

        protected XNamespace SitemapXmlNamespace => @"http://www.sitemaps.org/schemas/sitemap/0.9";

        protected XNamespace SitemapXhtmlNamespace => @"http://www.w3.org/1999/xhtml";

        public bool IsDebugMode { get; set; }

        protected SitemapXmlGenerator(
            ISitemapRepository sitemapRepository, 
            IContentRepository contentRepository, 
            IUrlResolver urlResolver, 
            ISiteDefinitionRepository siteDefinitionRepository, 
            ILanguageBranchRepository languageBranchRepository,
            IContentFilter contentFilter, 
            IMemoryCache cache,
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
            _cache = cache;
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
                _logger.LogError("Error on generating xml sitemap" + Environment.NewLine + e);
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
            IEnumerable<XElement> sitemapXmlElements = GetSitemapXmlElements();

            XElement sitemapElement = GenerateRootElement();

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

            IList<ContentReference> descendants = ContentRepository.GetDescendents(rootPage).ToList();

            if (!ContentReference.RootPage.CompareToIgnoreWorkID(rootPage))
            {
                descendants.Add(rootPage);
            }

            return GenerateXmlElements(descendants);
        }

        protected virtual IEnumerable<XElement> GenerateXmlElements(IEnumerable<ContentReference> pages)
        {
            IList<XElement> sitemapXmlElements = new List<XElement>();

            foreach (ContentReference contentReference in pages)
            {
                if (StopGeneration)
                {
                    return Enumerable.Empty<XElement>();
                }

                if (TryGet<IExcludeFromSitemap>(contentReference, out _))
                {
                    continue;
                }

                var contentLanguages = GetLanguageBranches(contentReference);

                foreach (var contentLanguageInfo in contentLanguages)
                {
                    if (StopGeneration)
                    {
                        return Enumerable.Empty<XElement>();
                    }

                    var localeContent = contentLanguageInfo.Content as ILocale;

                    if (localeContent != null && ExcludeContentLanguageFromSitemap(localeContent.Language))
                    {
                        continue;
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
            bool isSpecificLanguage = !string.IsNullOrWhiteSpace(SitemapData.Language);

            if (isSpecificLanguage)
            {
                var languageSelector = !SitemapData.EnableLanguageFallback
                    ? new LanguageSelector(SitemapData.Language)
                    : LanguageSelector.Fallback(SitemapData.Language, false);

                if (TryGet<IContent>(contentLink, out var contentData, languageSelector))
                {
                    return new[] { new CurrentLanguageContent { Content = contentData, CurrentLanguage = new CultureInfo(SitemapData.Language), MasterLanguage = GetMasterLanguage(contentData) } };
                }

                return Enumerable.Empty<CurrentLanguageContent>();
            }

            if (SitemapData.EnableLanguageFallback)
            {
                return GetFallbackLanguageBranches(contentLink);
            }

            if (TryGetLanguageBranches<IContent>(contentLink, out var contentLanguages))
            {
                return contentLanguages.Select(x => new CurrentLanguageContent { Content = x, CurrentLanguage = GetCurrentLanguage(x), MasterLanguage = GetMasterLanguage(x) });
            }
            return Enumerable.Empty<CurrentLanguageContent>();
        }

        protected virtual IEnumerable<CurrentLanguageContent> GetFallbackLanguageBranches(ContentReference contentLink)
        {
            foreach (var languageBranch in EnabledLanguages)
            {
                var languageContent = ContentRepository.Get<IContent>(contentLink, LanguageSelector.Fallback(languageBranch.Culture.Name, false));

                if (languageContent == null)
                {
                    continue;
                }

                yield return new CurrentLanguageContent { Content = languageContent, CurrentLanguage = languageBranch.Culture, MasterLanguage = GetMasterLanguage(languageContent) };
            }
        }

        protected virtual IEnumerable<HrefLangData> GetHrefLangDataFromCache(ContentReference contentLink)
        {
            var cacheKey = $"HrefLangData-{contentLink.ToReferenceWithoutVersion()}";
            var cachedObject = CacheManager.Get(cacheKey) as IEnumerable<HrefLangData>;

            if (cachedObject == null)
            {
                cachedObject = GetHrefLangData(contentLink);
                CacheManager.Insert(cacheKey, cachedObject, new CacheEvictionPolicy(null, new[] { "SitemapGenerationKey" }, TimeSpan.FromMinutes(10), CacheTimeoutType.Absolute));
            }

            return cachedObject;
        }

        protected virtual IEnumerable<HrefLangData> GetHrefLangData(ContentReference contentLink)
        {
            foreach (var languageBranch in EnabledLanguages)
            {
                var languageContent = ContentRepository.Get<IContent>(contentLink, LanguageSelector.Fallback(languageBranch.Culture.Name, false));

                if (languageContent == null || ContentFilter.ShouldExcludeContent(languageContent))
                {
                    continue;
                }

                var hrefLangData = CreateHrefLangData(languageContent, languageBranch.Culture, GetMasterLanguage(languageContent));
                yield return hrefLangData;

                if (hrefLangData.HrefLang == "x-default")
                {
                    yield return new HrefLangData
                    {
                        HrefLang = languageBranch.Culture.Name.ToLowerInvariant(),
                        Href = hrefLangData.Href
                    };
                }
            }
        }

        protected virtual HrefLangData CreateHrefLangData(IContent content, CultureInfo language, CultureInfo masterLanguage)
        {
            string languageUrl;
            string masterLanguageUrl;

            if (SitemapData.EnableSimpleAddressSupport && content is PageData pageData && !string.IsNullOrWhiteSpace(pageData.ExternalURL))
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

            var changeTrackableContent = contentData as IChangeTrackable;
            var versionableContent = contentData as IVersionable;

            if (changeTrackableContent != null)
            {
                modified = changeTrackableContent.Saved;
            }
            else if (versionableContent != null && versionableContent.StartPublish.HasValue)
            {
                modified = versionableContent.StartPublish.Value;
            }

            var property = contentData.Property[PropertySEOSitemaps.PropertyName] as PropertySEOSitemaps;

            var element = new XElement(
                SitemapXmlNamespace + "url",
                new XElement(SitemapXmlNamespace + "loc", url),
                new XElement(SitemapXmlNamespace + "lastmod", modified.ToString(DateTimeFormat, CultureInfo.InvariantCulture)),
                new XElement(SitemapXmlNamespace + "changefreq", (property != null && !property.IsNull) ? property.ChangeFreq : "weekly"),
                new XElement(SitemapXmlNamespace + "priority", (property != null && !property.IsNull) ? property.Priority : GetPriority(url))
            );

            if (SitemapData.IncludeAlternateLanguagePages)
            {
                AddHrefLangToElement(contentData, element);
            }

            if (IsDebugMode)
            {
                var localeContent = contentData as ILocale;
                var language = localeContent != null ? localeContent.Language : CultureInfo.InvariantCulture;
                var contentName = Regex.Replace(contentData.Name, "[-]+", "", RegexOptions.None);

                element.AddFirst(new XComment($"page ID: '{contentData.ContentLink.ID}', name: '{contentName}', language: '{language.Name}'"));
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
            var count = hrefLangDatas.Count();

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

        protected virtual void AddFilteredContentElement(CurrentLanguageContent languageContentInfo,
            IList<XElement> xmlElements)
        {
            if (ContentFilter.ShouldExcludeContent(languageContentInfo, SiteSettings, SitemapData))
            {
                return;
            }

            var content = languageContentInfo.Content;
            string url = null;

            if (SitemapData.EnableSimpleAddressSupport && content is PageData pageData && !string.IsNullOrWhiteSpace(pageData.ExternalURL))
            {
                url = pageData.ExternalURL;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                var localizableContent = content as ILocalizable;

                if (localizableContent != null)
                {
                    var language = string.IsNullOrWhiteSpace(SitemapData.Language)
                        ? languageContentInfo.CurrentLanguage.Name
                        : SitemapData.Language;

                    url = UrlResolver.GetUrl(content.ContentLink, language);

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        return;
                    }

                    // Make 100% sure we remove the language part in the URL if the sitemap host is mapped to the page's LanguageBranch.
                    if (HostLanguageBranch != null
                        && localizableContent.Language.Name.Equals(HostLanguageBranch, StringComparison.InvariantCultureIgnoreCase))
                    {
                        url = url.Replace($"/{HostLanguageBranch}/", "/");
                    }
                }
                else
                {
                    url = UrlResolver.GetUrl(content.ContentLink);

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        return;
                    }
                }
            }

            url = GetAbsoluteUrl(url);

            var fullContentUrl = new Uri(url);

            if (UrlSet.Contains(fullContentUrl.ToString()) || UrlFilter.IsUrlFiltered(fullContentUrl.AbsolutePath, SitemapData))
            {
                return;
            }

            var contentElement = GenerateSiteElement(content, fullContentUrl.ToString());

            if (contentElement == null)
            {
                return;
            }

            xmlElements.Add(contentElement);
            UrlSet.Add(fullContentUrl.ToString());
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

        protected CultureInfo GetCurrentLanguage(IContent content)
        {
            var localizableContent = content as ILocalizable;

            if (localizableContent != null)
            {
                return localizableContent.Language;
            }

            return CultureInfo.InvariantCulture;
        }

        protected CultureInfo GetMasterLanguage(IContent content)
        {
            var localizableContent = content as ILocalizable;

            if (localizableContent != null)
            {
                return localizableContent.MasterLanguage;
            }

            return CultureInfo.InvariantCulture;
        }

        public SiteDefinition GetSiteDefinitionFromSiteUri(Uri sitemapSiteUri)
        {
            return SiteDefinitionRepository
                .List()
                .FirstOrDefault(siteDef => siteDef.SiteUrl == sitemapSiteUri || siteDef.Hosts.Any(hostDef => hostDef.Name.Equals(sitemapSiteUri.Authority, StringComparison.InvariantCultureIgnoreCase)));
        }

        protected string GetHostLanguageBranch()
        {
            var hostDefinition = GetHostDefinition();
            return hostDefinition?.Language?.Name;
        }

        protected bool HostDefinitionExistsForLanguage(string languageBranch)
        {
            var cacheKey = $"HostDefinitionExistsFor{SitemapData.SiteUrl}-{languageBranch}";
            var cachedObject = _cache.Get(cacheKey);

            if (cachedObject == null)
            {
                cachedObject =
                    SiteSettings.Hosts.Any(
                        x =>
                        x.Language != null &&
                        x.Language.ToString().Equals(languageBranch, StringComparison.InvariantCultureIgnoreCase));

                _cache.Set(cacheKey, cachedObject, DateTime.Now.AddMinutes(10));
            }

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
            // if the URL is relative we add the base site URL (protocol and hostname)
            if (!IsAbsoluteUrl(url, out var absoluteUri))
            {
                url = UriUtil.Combine(SitemapData.SiteUrl, url);
            }
            // Force the SiteUrl
            else
            {
                url = UriUtil.Combine(SitemapData.SiteUrl, absoluteUri.AbsolutePath);
            }

            return url;
        }

        protected bool IsAbsoluteUrl(string url, out Uri absoluteUri)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out absoluteUri);
        }

        protected bool TryGet<T>(ContentReference contentLink, out T content, LoaderOptions settings = null) where T : IContentData
        {
            content = default;
            try
            {
                T local;
                var status = settings != null
                    ? ContentRepository.TryGet(contentLink, settings, out local)
                    : ContentRepository.TryGet<T>(contentLink, out local);
                content = local;
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error TryGet for {nameof(contentLink)}: {contentLink?.ID}", ex);
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
                _logger.LogError($"Error TryGetLanguageBranches for {nameof(contentLink)}: {contentLink?.ID}", ex);
            }
            return false;
        }
    }
}
