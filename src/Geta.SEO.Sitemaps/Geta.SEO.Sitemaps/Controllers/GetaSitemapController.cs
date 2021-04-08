// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using Geta.SEO.Sitemaps.Configuration;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Reflection;

namespace Geta.SEO.Sitemaps.Controllers
{
    [Route("sitemap.xml")]
    public class GetaSitemapController : Controller
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISitemapRepository _sitemapRepository;
        private readonly SitemapXmlGeneratorFactory _sitemapXmlGeneratorFactory;
        private readonly IContentCacheKeyCreator _contentCacheKeyCreator;

        // This constructor was added to support web forms projects without dependency injection configured.
        public GetaSitemapController(IContentCacheKeyCreator contentCacheKeyCreator) : this(ServiceLocator.Current.GetInstance<ISitemapRepository>(), ServiceLocator.Current.GetInstance<SitemapXmlGeneratorFactory>(), contentCacheKeyCreator)
        {
        }

        public GetaSitemapController(ISitemapRepository sitemapRepository, SitemapXmlGeneratorFactory sitemapXmlGeneratorFactory, IContentCacheKeyCreator contentCacheKeyCreator)
        {
            _sitemapRepository = sitemapRepository;
            _sitemapXmlGeneratorFactory = sitemapXmlGeneratorFactory;
            _contentCacheKeyCreator = contentCacheKeyCreator;
        }

        [Route("", Name = "Sitemap without path")]
        [Route("{path}sitemap.xml", Name = "Sitemap with path")]
        [Route("{language}/sitemap.xml", Name = "Sitemap with language")]
        [Route("{language}/{path}sitemap.xml", Name = "Sitemap with language and path")]
        public ActionResult Index()
        {
            var sitemapData = _sitemapRepository.GetSitemapData(Request.GetDisplayUrl());

            if (sitemapData == null)
            {
                Log.Error("Xml sitemap data not found!");
                return new NotFoundResult();
            }

            if (sitemapData.Data == null || (SitemapSettings.Instance.EnableRealtimeSitemap))
            {
                if (!GetSitemapData(sitemapData))
                {
                    Log.Error("Xml sitemap data not found!");
                    return new NotFoundResult();
                }
            }

            return new FileContentResult(sitemapData.Data, "text/xml; charset=utf-8");
        }

        private bool GetSitemapData(SitemapData sitemapData)
        {
            int entryCount;
            var userAgent = Request.HttpContext.GetServerVariable("USER_AGENT");

            var isGoogleBot = userAgent != null &&
                              userAgent.IndexOf("Googlebot", StringComparison.InvariantCultureIgnoreCase) > -1;

            var googleBotCacheKey = isGoogleBot ? "Google-" : string.Empty;

            if (SitemapSettings.Instance.EnableRealtimeSitemap)
            {
                var cacheKey = googleBotCacheKey + _sitemapRepository.GetSitemapUrl(sitemapData);

                var sitemapDataData = CacheManager.Get(cacheKey) as byte[];

                if (sitemapDataData != null)
                {
                    sitemapData.Data = sitemapDataData;
                    return true;
                }

                if (_sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapData).Generate(sitemapData, false, out entryCount))
                {
                    if (SitemapSettings.Instance.EnableRealtimeCaching)
                    {
                        CacheEvictionPolicy cachePolicy;

                        cachePolicy = isGoogleBot
                            ? new CacheEvictionPolicy(TimeSpan.Zero, CacheTimeoutType.Sliding, new[] { _contentCacheKeyCreator.VersionKey })
                            : null;

                        CacheManager.Insert(cacheKey, sitemapData.Data, cachePolicy);
                    }

                    return true;
                }

                return false;
            }

            return _sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapData).Generate(sitemapData, !SitemapSettings.Instance.EnableRealtimeSitemap, out entryCount);
        }
    }
}