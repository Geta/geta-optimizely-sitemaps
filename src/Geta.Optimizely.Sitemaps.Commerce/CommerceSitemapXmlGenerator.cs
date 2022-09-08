// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Cache;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Services;
using Geta.Optimizely.Sitemaps.Utils;
using Geta.Optimizely.Sitemaps.XML;
using Mediachase.Commerce.Catalog;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Geta.Optimizely.Sitemaps.Commerce
{
    /// <summary>
    /// Known bug: You need to add * (wildcard) url in sitedefinitions in admin mode for this job to run.
    /// See: http://world.episerver.com/forum/developer-forum/EPiServer-Commerce/Thread-Container/2013/12/Null-exception-in-GetUrl-in-search-provider-indexer/
    /// </summary>
    public class CommerceSitemapXmlGenerator : SitemapXmlGenerator, ICommerceSitemapXmlGenerator
    {
        private readonly ReferenceConverter _referenceConverter;

        public CommerceSitemapXmlGenerator(
            ISitemapRepository sitemapRepository,
            IContentRepository contentRepository,
            IUrlResolver urlResolver,
            ISiteDefinitionRepository siteDefinitionRepository,
            ILanguageBranchRepository languageBranchRepository,
            ReferenceConverter referenceConverter,
            IContentFilter contentFilter,
            IUriAugmenterService uriAugmenterService,
            ISynchronizedObjectInstanceCache objectCache,
            IMemoryCache memoryCache,
            ILogger<CommerceSitemapXmlGenerator> logger)
            : base(
                sitemapRepository,
                contentRepository,
                urlResolver,
                siteDefinitionRepository,
                languageBranchRepository,
                contentFilter,
                uriAugmenterService,
                objectCache,
                memoryCache,
                logger)
        {
            _referenceConverter = referenceConverter ?? throw new ArgumentNullException(nameof(referenceConverter));
        }

        protected override IEnumerable<XElement> GetSitemapXmlElements()
        {
            var rootContentReference = _referenceConverter.GetRootLink();

            if (SitemapData.RootPageId != Constants.DefaultRootPageId)
            {
                rootContentReference = new ContentReference(SitemapData.RootPageId)
                {
                    ProviderName = "CatalogContent"
                };
            }

            var descendants = ContentRepository.GetDescendents(rootContentReference).ToList();

            return GenerateXmlElements(descendants);
        }
    }
}
