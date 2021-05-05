// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.Core;
using EPiServer.Web;
using Geta.Optimizely.Sitemaps.Entities;

namespace Geta.Optimizely.Sitemaps.Utils
{
    public interface IContentFilter
    {
        bool ShouldExcludeContent(IContent content);
        bool ShouldExcludeContent(
            CurrentLanguageContent languageContentInfo, SiteDefinition siteSettings, SitemapData sitemapData);
    }
}
