// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EPiServer;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Geta.Optimizely.Sitemaps.Repositories;
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
    [ServiceConfiguration(typeof(ICommerceAndStandardSitemapXmlGenerator))] // TODO: Move to extension
    public class CommerceAndStandardSitemapXmlGenerator
        : CommerceSitemapXmlGenerator, ICommerceAndStandardSitemapXmlGenerator
    {
        public CommerceAndStandardSitemapXmlGenerator(
            ISitemapRepository sitemapRepository,
            IContentRepository contentRepository,
            IUrlResolver urlResolver,
            ISiteDefinitionRepository siteDefinitionRepository,
            ILanguageBranchRepository languageBranchRepository,
            ReferenceConverter referenceConverter,
            IContentFilter contentFilter,
            ISynchronizedObjectInstanceCache objectCache,
            IMemoryCache memoryCache,
            ILogger<SitemapXmlGenerator> logger)
            : base(
                sitemapRepository,
                contentRepository,
                urlResolver,
                siteDefinitionRepository,
                languageBranchRepository,
                referenceConverter,
                contentFilter,
                objectCache,
                memoryCache,
                logger)
        {
        }

        protected override IEnumerable<XElement> GetSitemapXmlElements()
        {
            var contentDescendants = ContentRepository.GetDescendents(this.SiteSettings.StartPage).ToList();

            contentDescendants.Insert(0, SiteSettings.StartPage);

            var contentElements = GenerateXmlElements(contentDescendants);
            return contentElements.Union(base.GetSitemapXmlElements());
        }
    }
}