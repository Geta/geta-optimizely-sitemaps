// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

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
    public class CommerceAndStandardSitemapXmlGenerator
        : CommerceSitemapXmlGenerator, ICommerceAndStandardSitemapXmlGenerator
    {
        public CommerceAndStandardSitemapXmlGenerator(
            ISitemapRepository sitemapRepository,
            IContentRepository contentRepository,
            IUrlResolver urlResolver,
            IApplicationRepository applicationRepository,
            ILanguageBranchRepository languageBranchRepository,
            ReferenceConverter referenceConverter,
            IContentFilter contentFilter,
            IUriAugmenterService uriAugmenterService,
            ILogger<CommerceAndStandardSitemapXmlGenerator> logger)
            : base(
                sitemapRepository,
                contentRepository,
                urlResolver,
                applicationRepository,
                languageBranchRepository,
                referenceConverter,
                contentFilter,
                uriAugmenterService,
                logger)
        {
        }

        protected override IEnumerable<XElement> GetSitemapXmlElements()
        {
            var rootPage = GetRootPageLink();
            var contentDescendants = ContentRepository.GetDescendents(rootPage).ToList();

            contentDescendants.Insert(0, rootPage);

            var contentElements = GenerateXmlElements(contentDescendants);

            // The root page ID of this format selects the CMS branch, so it can't also select a catalog.
            return contentElements.Concat(GenerateCatalogXmlElements(ReferenceConverter.GetRootLink()));
        }
    }
}
