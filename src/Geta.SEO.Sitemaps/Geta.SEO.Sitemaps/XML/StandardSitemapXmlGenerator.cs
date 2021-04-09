// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer;
using EPiServer.DataAbstraction;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Geta.SEO.Sitemaps.XML
{
    public class StandardSitemapXmlGenerator : SitemapXmlGenerator, IStandardSitemapXmlGenerator
    {
        public StandardSitemapXmlGenerator(
            ISitemapRepository sitemapRepository,
            IContentRepository contentRepository,
            IUrlResolver urlResolver,
            ISiteDefinitionRepository siteDefinitionRepository,
            ILanguageBranchRepository languageBranchRepository,
            IContentFilter contentFilter,
            IMemoryCache cache,
            ILogger<SitemapXmlGenerator> logger)
            : base(sitemapRepository, contentRepository, urlResolver, siteDefinitionRepository, languageBranchRepository, contentFilter, cache, logger)
        {
        }
    }
}