// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using Geta.Optimizely.Sitemaps.Configuration;
using Geta.Optimizely.Sitemaps.Entities;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Utils;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Sitemaps.Controllers;

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

    [Route("sitemap.xml", Name = "Sitemap without path")]
    [Route("{path}sitemap.xml", Name = "Sitemap with path")]
    [Route("{language}/sitemap.xml", Name = "Sitemap with language")]
    [Route("{language}/{path}sitemap.xml", Name = "Sitemap with language and path")]
    public ActionResult Index()
    {
        var sitemapData = _sitemapRepository.GetSitemapData(Request.GetDisplayUrl());

        if (sitemapData == null)
        {
            return SitemapDataNotFound();
        }

        if (_configuration.EnableRealtimeSitemap)
        {
            return RealtimeSitemapData(sitemapData);
        }

        return sitemapData.Data != null
            ? FileContentResult(sitemapData)
            : SitemapData(sitemapData);
    }

    private ActionResult RealtimeSitemapData(SitemapData sitemapData)
    {
        var cacheKey = GetCacheKey(sitemapData);
        var cachedData = GetCachedSitemapData(cacheKey);

        if (cachedData != null)
        {
            sitemapData.Data = cachedData;
            return FileContentResult(sitemapData);
        }

        var sitemapGenerator = _sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapData);
        var hasGenerated = sitemapGenerator.Generate(sitemapData, false, out _);

        if (hasGenerated)
        {
            if (_configuration.EnableRealtimeCaching)
            {
                CacheSitemapData(sitemapData, cacheKey);
            }

            return FileContentResult(sitemapData);
        }

        return SitemapDataNotFound();
    }

    private ActionResult SitemapData(SitemapData sitemapData)
    {
        var sitemapGenerator = _sitemapXmlGeneratorFactory.GetSitemapXmlGenerator(sitemapData);
        var hasGenerated = sitemapGenerator.Generate(sitemapData, true, out _);

        return hasGenerated
            ? FileContentResult(sitemapData)
            : SitemapDataNotFound();
    }

    private ActionResult SitemapDataNotFound()
    {
        _logger.LogError("Xml sitemap data not found!");
        return new NotFoundResult();
    }

    private void CacheSitemapData(SitemapData sitemapData, string cacheKey)
    {
        var cacheExpiration = TimeSpan.FromMinutes(Math.Max(0, _configuration.RealtimeCacheExpirationInMinutes));
        var cachePolicy = new CacheEvictionPolicy(cacheExpiration, CacheTimeoutType.Absolute, new[] { _contentCacheKeyCreator.VersionKey });

        CacheManager.Insert(cacheKey, sitemapData.Data, cachePolicy);
    }

    private static byte[] GetCachedSitemapData(string cacheKey)
    {
        return CacheManager.Get(cacheKey) as byte[];
    }

    private string GetCacheKey(SitemapData sitemapData)
    {
        return _sitemapRepository.GetSitemapUrl(sitemapData);
    }

    private static FileContentResult FileContentResult(SitemapData sitemapData)
    {
        return new(sitemapData.Data, "text/xml; charset=utf-8");
    }
}
