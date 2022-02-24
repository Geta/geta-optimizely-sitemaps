// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using Geta.Optimizely.Sitemaps.Configuration;
using Geta.Optimizely.Sitemaps.Entities;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Sitemaps.Controllers
{
    [Route("sitemap.xml", Name = "Sitemap without path and/or language.")]
    [Route("{path}sitemap.xml", Name = "Sitemap with path")]
    [Route("{language}/sitemap.xml", Name = "Sitemap with language")]
    [Route("{language}/{path}sitemap.xml", Name = "Sitemap with language and path")]
    public class GetaSitemapController : Controller
    {
        private readonly ISitemapRepository _sitemapRepository;
        private readonly SitemapXmlGeneratorFactory _sitemapXmlGeneratorFactory;
        private readonly IContentCacheKeyCreator _contentCacheKeyCreator;
        private readonly ILogger<GetaSitemapController> _logger;
        private readonly SitemapOptions _configuration;

        public GetaSitemapController(
            ISitemapRepository sitemapRepository,
            SitemapXmlGeneratorFactory sitemapXmlGeneratorFactory,
            IContentCacheKeyCreator contentCacheKeyCreator,
            IOptions<SitemapOptions> options,
            ILogger<GetaSitemapController> logger)
        {
            _sitemapRepository = sitemapRepository;
            _sitemapXmlGeneratorFactory = sitemapXmlGeneratorFactory;
            _contentCacheKeyCreator = contentCacheKeyCreator;
            _logger = logger;
            _configuration = options.Value;
        }

        public ActionResult Index()
        {
            var sitemapData = _sitemapRepository.GetSitemapData(Request.GetDisplayUrl());

            if (sitemapData == null)
            {
                _logger.LogError("Xml sitemap data not found!");
                return new NotFoundResult();
            }

            if (sitemapData.Data == null || (_configuration.EnableRealtimeSitemap))
            {
                if (!GetSitemapData(sitemapData))
                {
                    _logger.LogError("Xml sitemap data not found!");
                    return new NotFoundResult();
                }
            }

            return new FileContentResult(sitemapData.Data, "text/xml; charset=utf-8");
        }

        private bool GetSitemapData(SitemapData sitemapData)
        {
            var userAgent = Request.HttpContext.GetServerVariable("USER_AGENT");

            var isGoogleBot = userAgent != null &&
                              userAgent.IndexOf("Googlebot", StringComparison.InvariantCultureIgnoreCase) > -1;

            var googleBotCacheKey = isGoogleBot ? "Google-" : string.Empty;

            if (_configuration.EnableRealtimeSitemap)
            {
                var cacheKey = googleBotCacheKey + _sitemapRepository.GetSitemapUrl(sitemapData);

                var sitemapDataData = CacheManager.Get(cacheKey) as byte[];

                if (sitemapDataData != null)
                {
                    sitemapData.Data = sitemapDataData;
                    return true;
                }

                if (_sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapData).Generate(sitemapData, false, out _))
                {
                    if (_configuration.EnableRealtimeCaching)
                    {
                        var cachePolicy = isGoogleBot
                            ? new CacheEvictionPolicy(TimeSpan.Zero, CacheTimeoutType.Sliding, new[] { _contentCacheKeyCreator.VersionKey })
                            : null;

                        CacheManager.Insert(cacheKey, sitemapData.Data, cachePolicy);
                    }

                    return true;
                }

                return false;
            }

            return _sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapData).Generate(sitemapData, !_configuration.EnableRealtimeSitemap, out _);
        }
    }
}