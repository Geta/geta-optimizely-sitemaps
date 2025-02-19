// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using AspNetCore;
using EPiServer.Core;
using EPiServer.Framework.Web;
using EPiServer.Security;
using EPiServer.Web;
using Geta.Optimizely.Sitemaps.Configuration;
using Geta.Optimizely.Sitemaps.Entities;
using Geta.Optimizely.Sitemaps.SpecializedProperties;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Sitemaps.Utils
{
    public class ContentFilter : IContentFilter
    {
        private readonly TemplateResolver _templateResolver;
        private readonly ILogger<ContentFilter> _logger;
        private readonly SitemapOptions _sitemapOptions;

        public ContentFilter(TemplateResolver templateResolver, ILogger<ContentFilter> logger, IOptions<SitemapOptions> sitemapOptions)
        {
            _templateResolver = templateResolver;
            _logger = logger;
            _sitemapOptions = sitemapOptions.Value;
        }

        public virtual bool ShouldExcludeContent(IContent content)
        {
            if (content == null)
            {
                return true;
            }

            if (!IsAccessibleToEveryone(content))
            {
                return true;
            }

            if (content.IsDeleted)
            {
                return true;
            }


            if (!IsPublished(content))
            {
                return true;
            }

            if (!IsSitemapPropertyEnabled(content))
            {
                return true;
            }

            if (_sitemapOptions.SiteArchitecture == SiteArchitecture.Mvc)
            {
                if (!IsVisibleOnSite(content))
                {
                    return true;
                }
            }

            if (content.ContentLink.CompareToIgnoreWorkID(ContentReference.WasteBasket))
            {
                return true;
            }

            if (content is BlockData || content is MediaData)
            {
                return true;
            }

            var page = content as PageData;

            if (page != null && IsLink(page))
            {
                return true;
            }

            return false;
        }

        public virtual bool ShouldExcludeContent(CurrentLanguageContent languageContentInfo, SiteDefinition siteSettings, SitemapData sitemapData)
        {
            return ShouldExcludeContent(languageContentInfo.Content);
        }

        private bool IsVisibleOnSite(IContent content)
        {
            return _templateResolver.HasTemplate(content, TemplateTypeCategories.Request);
        }

        private static bool IsLink(PageData page)
        {
            return page.LinkType == PageShortcutType.External ||
                          page.LinkType == PageShortcutType.Shortcut ||
                          page.LinkType == PageShortcutType.Inactive;
        }

        private static bool IsSitemapPropertyEnabled(IContentData content)
        {
            var property = content.Property[PropertySEOSitemaps.PropertyName] as PropertySEOSitemaps;
            if (property == null) //not set on the page, check if there are default values for a page type perhaps
            {
                var page = content as PageData;
                if (page == null)
                    return true;

                var seoProperty = page.GetType().GetProperty(PropertySEOSitemaps.PropertyName);
                if (seoProperty?.GetValue(page) is PropertySEOSitemaps) //check unlikely situation when the property name is the same as defined for SEOSiteMaps
                {
                    var isEnabled = ((PropertySEOSitemaps)seoProperty.GetValue(page)).Enabled;
                    return isEnabled;
                }

            }

            if (null != property && !property.Enabled)
            {
                return false;
            }

            return true;
        }

        private bool IsAccessibleToEveryone(IContent content)
        {
            try
            {
                if (content is ISecurable securableContent)
                {
                    var visitorPrinciple = new System.Security.Principal.GenericPrincipal(
                        new System.Security.Principal.GenericIdentity("visitor"),
                        new[] { "Everyone" });

                    return securableContent.GetSecurityDescriptor().HasAccess(visitorPrinciple, AccessLevel.Read);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error on content parent " + content.ContentLink.ID + Environment.NewLine + e);
            }

            return false;
        }

        private bool IsPublished(IContent content)
        {
            if (content is IVersionable versionableContent)
            {
                var isPublished = versionableContent.Status == VersionStatus.Published;

                if (!isPublished || versionableContent.IsPendingPublish)
                {
                    return false;
                }

                var now = DateTime.Now.ToUniversalTime();
                var startPublish = versionableContent.StartPublish.GetValueOrDefault(DateTime.MinValue).ToUniversalTime();
                var stopPublish = versionableContent.StopPublish.GetValueOrDefault(DateTime.MaxValue).ToUniversalTime();

                if (startPublish > now || stopPublish < now)
                {
                    return false;
                }

                return true;
            }

            return !_sitemapOptions.IsStrictPublishCheckingEnabled;
        }
    }
}
