// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Web.Routing;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Services;
using Geta.Optimizely.Sitemaps.Utils;
using Geta.Optimizely.Sitemaps.XML;
using Mediachase.Commerce.Catalog;
using Microsoft.Extensions.Logging;

namespace Geta.Optimizely.Sitemaps.Commerce
{
    /// <summary>
    /// Known bug: You need to add * (wildcard) url in sitedefinitions in admin mode for this job to run.
    /// See: http://world.episerver.com/forum/developer-forum/EPiServer-Commerce/Thread-Container/2013/12/Null-exception-in-GetUrl-in-search-provider-indexer/
    /// </summary>
    public class CommerceSitemapXmlGenerator : SitemapXmlGenerator, ICommerceSitemapXmlGenerator
    {
        protected readonly ReferenceConverter ReferenceConverter;

        public CommerceSitemapXmlGenerator(
            ISitemapRepository sitemapRepository,
            IContentRepository contentRepository,
            IUrlResolver urlResolver,
            IApplicationRepository applicationRepository,
            ILanguageBranchRepository languageBranchRepository,
            ReferenceConverter referenceConverter,
            IContentFilter contentFilter,
            IUriAugmenterService uriAugmenterService,
            ILogger<CommerceSitemapXmlGenerator> logger)
            : base(
                sitemapRepository,
                contentRepository,
                urlResolver,
                applicationRepository,
                languageBranchRepository,
                contentFilter,
                uriAugmenterService,
                logger)
        {
            ReferenceConverter = referenceConverter ?? throw new ArgumentNullException(nameof(referenceConverter));
        }

        protected override IEnumerable<XElement> GetSitemapXmlElements()
        {
            // Other negative values are real IDs: a catalog's content ID is always negative.
            var root = SitemapData.RootPageId is 0 or Constants.DefaultRootPageId
                ? ReferenceConverter.GetRootLink()
                : new ContentReference(SitemapData.RootPageId) { ProviderName = ReferenceConverter.CatalogProviderKey };

            return GenerateCatalogXmlElements(root);
        }

        protected IEnumerable<XElement> GenerateCatalogXmlElements(ContentReference root)
        {
            var descendants = ContentRepository.GetDescendents(root).ToList();

            // The catalog root is only a container, but a selected catalog or node is listed along with its descendants.
            if (!ReferenceConverter.GetRootLink().CompareToIgnoreWorkID(root))
            {
                descendants.Insert(0, root);
            }

            return GenerateXmlElements(descendants);
        }
    }
}
