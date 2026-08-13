// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer;
using EPiServer.Applications;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Cache;
using EPiServer.Web.Routing;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Services;
using Geta.Optimizely.Sitemaps.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Geta.Optimizely.Sitemaps.XML
{
    public class StandardSitemapXmlGenerator : SitemapXmlGenerator, IStandardSitemapXmlGenerator
    {
        public StandardSitemapXmlGenerator(
            ISitemapRepository sitemapRepository,
            IContentRepository contentRepository,
            IUrlResolver urlResolver,
            IApplicationRepository applicationRepository,
            ILanguageBranchRepository languageBranchRepository,
            IContentFilter contentFilter,
            IUriAugmenterService uriAugmenterService,
            ISynchronizedObjectInstanceCache objectCache,
            IMemoryCache cache,
            ILogger<StandardSitemapXmlGenerator> logger)
            : base(
                sitemapRepository,
                contentRepository,
                urlResolver,
                applicationRepository,
                languageBranchRepository,
                contentFilter,
                uriAugmenterService,
                objectCache,
                cache,
                logger)
        {
        }
    }
}
