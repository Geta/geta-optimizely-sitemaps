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
using EPiServer.Applications;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.Optimizely.Sitemaps.Entities;
using Geta.Optimizely.Sitemaps.Models;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Services;
using Geta.Optimizely.Sitemaps.SpecializedProperties;
using Geta.Optimizely.Sitemaps.Utils;
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
        protected readonly IApplicationRepository ApplicationRepository;
        protected readonly ILanguageBranchRepository LanguageBranchRepository;
        protected readonly IContentFilter ContentFilter;
        private readonly IUriAugmenterService _uriAugmenterService;
        private readonly ILogger<SitemapXmlGenerator> _logger;

        // A page's language variants share their alternates. These depend on the sitemap's settings and on the content
        // as it is now, so they live only as long as this generator.
        private readonly Dictionary<ContentReference, List<HrefLangData>> _hrefLangData = new(ContentReferenceComparer.IgnoreVersion);
        private readonly Dictionary<CultureInfo, string> _languageHostUrls = new();

        protected SitemapData SitemapData { get; set; }
        protected IRoutableApplication CurrentSite { get; set; }
        protected IEnumerable<LanguageBranch> EnabledLanguages { get; set; }
        protected IEnumerable<CurrentLanguageContent> HrefLanguageContents { get; set; }

        protected static XNamespace SitemapXmlNamespace => @"http://www.sitemaps.org/schemas/sitemap/0.9";
        protected static XNamespace SitemapXhtmlNamespace => @"http://www.w3.org/1999/xhtml";

        public bool IsDebugMode { get; set; }
        public string LastError { get; protected set; }

        private readonly Regex _dashRegex = new("[-]+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

        protected SitemapXmlGenerator(
            ISitemapRepository sitemapRepository,
            IContentRepository contentRepository,
            IUrlResolver urlResolver,
            IApplicationRepository applicationRepository,
            ILanguageBranchRepository languageBranchRepository,
            IContentFilter contentFilter,
            IUriAugmenterService uriAugmenterService,
            ILogger<SitemapXmlGenerator> logger)
        {
            SitemapRepository = sitemapRepository;
            ContentRepository = contentRepository;
            UrlResolver = urlResolver;
            ApplicationRepository = applicationRepository;
            LanguageBranchRepository = languageBranchRepository;
            EnabledLanguages = LanguageBranchRepository.ListEnabled();
            UrlSet = new HashSet<string>();
            ContentFilter = contentFilter;
            _uriAugmenterService = uriAugmenterService;
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
                SitemapData.ExceedsMaximumEntryCount = false;
                EnsureSiteUrl();
                CurrentSite = GetApplicationFromSiteUri(new Uri(SitemapData.SiteUrl))
                              ?? throw new InvalidOperationException($"No site is configured for '{SitemapData.SiteUrl}'.");
                HostLanguageBranch = GetHostLanguageBranch();
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
                    SaveGeneratedSitemap(sitemapData);
                }

                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
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
        /// The configuration can have been edited or deleted during generation, so this only writes back what generation produced.
        /// </summary>
        private void SaveGeneratedSitemap(SitemapData generated)
        {
            var current = generated.Id == null ? generated : SitemapRepository.GetSitemapData(generated.Id);

            if (current == null)
            {
                return;
            }

            current.Data = generated.Data;
            current.ExceedsMaximumEntryCount = generated.ExceedsMaximumEntryCount;

            if (string.IsNullOrWhiteSpace(current.SiteUrl))
            {
                current.SiteUrl = generated.SiteUrl;
            }

            SitemapRepository.Save(current);
        }

        /// <summary>
        /// A configuration without a site URL, such as the default one the scheduled job creates, belongs to the default site.
        /// </summary>
        private void EnsureSiteUrl()
        {
            if (!string.IsNullOrWhiteSpace(SitemapData.SiteUrl))
            {
                return;
            }

            var sites = ApplicationRepository.List().OfType<IRoutableApplication>().ToList();
            var site = sites.FirstOrDefault(x => x.IsDefault) ?? (sites.Count == 1 ? sites[0] : null);

            SitemapData.SiteUrl = site?.Url?.ToString()
                                  ?? throw new InvalidOperationException("The sitemap has no host and there is no default site to use instead.");
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
            var rootPage = GetRootPageLink();

            var descendants = ContentRepository.GetDescendents(rootPage).ToList();

            if (!ContentReference.RootPage.CompareToIgnoreWorkID(rootPage))
            {
                descendants.Add(rootPage);
            }

            return GenerateXmlElements(descendants);
        }

        protected ContentReference GetRootPageLink()
        {
            return SitemapData.RootPageId > 0 ? new ContentReference(SitemapData.RootPageId) : CurrentSite.EntryPoint;
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

                if (ContentReference.IsNullOrEmpty(contentReference) || TryGet<IExcludeFromSitemap>(contentReference, out _))
                {
                    continue;
                }

                // Fallback content is the fallback language's variant, but its URL is for the current language.
                var contentLanguages = GetLanguageBranches(contentReference)
                    .Where(x => x.Content is not ILocale
                                || !ExcludeContentLanguageFromSitemap(x.CurrentLanguage));

                foreach (var contentLanguageInfo in contentLanguages)
                {
                    if (StopGeneration)
                    {
                        return Enumerable.Empty<XElement>();
                    }

                    if (SitemapData.ExceedsMaximumEntryCount)
                    {
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
            if (!_hrefLangData.TryGetValue(contentLink, out var hrefLangData))
            {
                hrefLangData = GetHrefLangData(contentLink).ToList();
                _hrefLangData[contentLink] = hrefLangData;
            }

            return hrefLangData;
        }

        protected virtual IEnumerable<HrefLangData> GetHrefLangData(ContentReference contentLink)
        {
            var languageData = EnabledLanguages
                .Select(x => (languageBranch: x, languageContent: GetLanguageContent(contentLink, x)))
                .Where(x => x.languageContent != null
                            && IsAlternateForLanguage(x.languageContent, x.languageBranch)
                            && !ContentFilter.ShouldExcludeContent(x.languageContent));

            foreach (var (languageBranch, languageContent) in languageData)
            {
                var hrefLangData =
                    CreateHrefLangData(languageContent, languageBranch.Culture, GetMasterLanguage(languageContent));

                if (hrefLangData.Href == null)
                {
                    continue;
                }

                yield return hrefLangData;

                if (hrefLangData.HrefLang == "x-default")
                {
                    yield return new() { HrefLang = languageBranch.Culture.Name.ToLowerInvariant(), Href = hrefLangData.Href };
                }
            }
        }

        /// <summary>
        /// Content loaded through a CMS language fallback is the fallback language's variant, not a translation,
        /// so it is only an alternate when the sitemap opted in to language fallback.
        /// </summary>
        private bool IsAlternateForLanguage(IContent content, LanguageBranch languageBranch)
        {
            return SitemapData.EnableLanguageFallback
                   || content is not ILocale localeContent
                   || localeContent.Language.Name.Equals(languageBranch.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        private IContent GetLanguageContent(ContentReference contentLink, LanguageBranch languageBranch)
        {
            return ContentRepository.Get<IContent>(contentLink, LanguageSelector.Fallback(languageBranch.Culture.Name, false));
        }

        protected virtual HrefLangData CreateHrefLangData(IContent content, CultureInfo language, CultureInfo masterLanguage)
        {
            string languageUrl;
            string masterLanguageUrl;
            string href;

            if (SitemapData.EnableSimpleAddressSupport
                && content is PageData pageData
                && !string.IsNullOrWhiteSpace(pageData.ExternalURL))
            {
                languageUrl = pageData.ExternalURL;
                href = languageUrl;

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
                href = EnsureCorrectUrlHostLanguage(language.Name, languageUrl);
            }

            var data = new HrefLangData();

            // Content without a URL in this language has no alternate to link to.
            if (string.IsNullOrWhiteSpace(languageUrl))
            {
                return data;
            }

            if (languageUrl.Equals(masterLanguageUrl) && content.ContentLink.CompareToIgnoreWorkID(CurrentSite.EntryPoint))
            {
                data.HrefLang = "x-default";
            }
            else
            {
                data.HrefLang = language.Name.ToLowerInvariant();
            }

            data.Href = GetAbsoluteUrl(href, language);
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
                new XElement(SitemapXmlNamespace + "lastmod", modified.ToString(DateTimeFormat, CultureInfo.InvariantCulture))
            );

            if (!SitemapData.ExcludeChangeFrequencyAndPriority)
            {
                element.Add(
                    new XElement(SitemapXmlNamespace + "changefreq",
                                 (property != null && !property.IsNull) ? property.ChangeFreq : "weekly"),
                    new XElement(SitemapXmlNamespace + "priority",
                                 (property != null && !property.IsNull) ? property.Priority : GetPriority(url)));
            }

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
            if (ContentFilter.ShouldExcludeContent(languageContentInfo, SitemapData))
            {
                return;
            }

            var content = languageContentInfo.Content;

            var url = GetContentUrl(languageContentInfo, content);

            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            url = content is ILocalizable
                ? GetAbsoluteUrl(url, languageContentInfo.CurrentLanguage)
                : GetAbsoluteUrl(url);

            var contentUrl = new Uri(url);

            foreach (var fullContentUrl in _uriAugmenterService.GetAugmentUris(content, languageContentInfo, contentUrl))
            {
                var fullUrl = fullContentUrl.ToString();

                if (UrlSet.Contains(fullUrl) || UrlFilter.IsUrlFiltered(fullContentUrl.AbsolutePath, SitemapData))
                {
                    continue;
                }

                if (UrlSet.Count >= MaxSitemapEntryCount)
                {
                    SitemapData.ExceedsMaximumEntryCount = true;
                    return;
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

        public IRoutableApplication GetApplicationFromSiteUri(Uri sitemapSiteUri)
        {
            return ApplicationRepository.List().OfType<IRoutableApplication>().FirstOrDefault(s =>
                s.Url == sitemapSiteUri ||
                s.Hosts.Any(x => x.HasAuthority(sitemapSiteUri.Authority)));
        }

        protected string GetHostLanguageBranch()
        {
            return GetApplicationHost()?.Locale?.Name;
        }

        protected bool HostDefinitionExistsForLanguage(string languageBranch)
        {
            return GetHostsOfLanguage(languageBranch).Any();
        }

        protected ApplicationHost GetApplicationHost()
        {
            var sitemapHost = new Uri(SitemapData.SiteUrl).Authority;

            return CurrentSite.Hosts.FirstOrDefault(x => x.HasAuthority(sitemapHost));
        }

        /// <summary>
        /// A language with its own host belongs in that host's sitemap. A sitemap on a host without a language can still be configured for one of those languages.
        /// </summary>
        protected bool ExcludeContentLanguageFromSitemap(CultureInfo language)
        {
            return (HostLanguageBranch != null || string.IsNullOrWhiteSpace(SitemapData.Language))
                   && !IsHostLanguage(language.Name)
                   && HostDefinitionExistsForLanguage(language.Name);
        }

        protected string GetAbsoluteUrl(string url)
        {
            return UriUtil.Combine(SitemapData.SiteUrl,
                                   IsAbsoluteUrl(url, out var absoluteUri)
                                       ? absoluteUri.AbsolutePath
                                       : url);
        }

        /// <summary>
        /// Gets the absolute URL of content in the given language. URLs on a language's own host have no language segment,
        /// so they only resolve on a host of that language. Everything else belongs on the sitemap's host.
        /// </summary>
        protected string GetAbsoluteUrl(string url, CultureInfo language)
        {
            if (IsHostLanguage(language.Name))
            {
                return GetAbsoluteUrl(url);
            }

            if (IsAbsoluteUrl(url, out var absoluteUri))
            {
                return IsHostOfLanguage(absoluteUri.Authority, language.Name)
                    ? absoluteUri.AbsoluteUri
                    : GetAbsoluteUrl(url);
            }

            // A relative URL for a language with its own host has no language segment either: it's a simple address,
            // or the current request is on that host.
            var languageHostUrl = GetLanguageHostUrl(language);

            return languageHostUrl != null
                ? UriUtil.Combine(languageHostUrl, url)
                : GetAbsoluteUrl(url);
        }

        private string GetLanguageHostUrl(CultureInfo language)
        {
            if (!_languageHostUrls.TryGetValue(language, out var url))
            {
                url = CurrentSite.GetPrimaryHost(language)?.Url?.ToString();
                _languageHostUrls[language] = url;
            }

            return url;
        }

        private bool IsHostLanguage(string language)
        {
            return string.Equals(language, HostLanguageBranch, StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<ApplicationHost> GetHostsOfLanguage(string language)
        {
            return CurrentSite.Hosts.Where(x => string.Equals(language, x.Locale?.Name, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsHostOfLanguage(string authority, string language)
        {
            return GetHostsOfLanguage(language).Any(x => x.HasAuthority(authority));
        }

#pragma warning disable CA1822
        protected bool IsAbsoluteUrl(string url, out Uri absoluteUri)
#pragma warning restore CA1822
        {
            // On Linux a rooted path such as "/en/page" parses as the absolute URI "file:///en/page".
            return Uri.TryCreate(url, UriKind.Absolute, out absoluteUri) && !absoluteUri.IsFile;
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

            if (content is not ILocalizable)
            {
                return UrlResolver.GetUrl(content.ContentLink);
            }

            var language = string.IsNullOrWhiteSpace(SitemapData.Language)
                ? languageContentInfo.CurrentLanguage.Name
                : SitemapData.Language;

            url = UrlResolver.GetUrl(content.ContentLink, language);
            url = EnsureCorrectUrlHostLanguage(language, url);

            return url;
        }

        /// <summary>
        /// Removes the language part of a URL for the language the sitemap's host is mapped to.
        /// </summary>
        private string EnsureCorrectUrlHostLanguage(string language, string url)
        {
            if (string.IsNullOrEmpty(url) || !IsHostLanguage(language))
            {
                return url;
            }

            var isAbsolute = IsAbsoluteUrl(url, out var absoluteUri);

            // URLs on a host of their own language have no language part, so what looks like one there belongs to the page.
            if (isAbsolute && IsHostOfLanguage(absoluteUri.Authority, language))
            {
                return url;
            }

            var path = isAbsolute ? absoluteUri.AbsolutePath : url;
            var languagePart = $"/{LanguageBranchRepository.Load(language)?.URLSegment ?? language}/";

            return path.StartsWith(languagePart, StringComparison.OrdinalIgnoreCase)
                ? path.Substring(languagePart.Length - 1)
                : url;
        }
    }
}
