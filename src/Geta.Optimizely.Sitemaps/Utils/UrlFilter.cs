// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using Geta.Optimizely.Sitemaps.Entities;

namespace Geta.Optimizely.Sitemaps.Utils
{
    /// <summary>
    /// Administrators are able to specify specific paths to exclude (blacklist) or include (whitelist) in sitemaps.
    /// This class is used to check this.
    /// </summary>
    public static class UrlFilter
    {
        public static bool IsUrlFiltered(string url, SitemapData sitemapConfig)
        {
            var whiteList = sitemapConfig.PathsToInclude;
            var blackList = sitemapConfig.PathsToAvoid;

            return IsNotInWhiteList(url, whiteList) || IsInBlackList(url, blackList);
        }

        private static bool IsNotInWhiteList(string url, IList<string> paths)
        {
            return IsPathInUrl(url, paths, true);
        }

        private static bool IsInBlackList(string url, IList<string> paths)
        {
            return IsPathInUrl(url, paths, false);
        }

        private static bool IsPathInUrl(string url, ICollection<string> paths, bool mustContainPath)
        {
            if (paths == null || paths.Count <= 0)
            {
                return false;
            }

            var anyPathIsInUrl = paths.Any(x =>
            {
                var dir = AddStartSlash(AddTailingSlash(x.ToLower().Trim()));
                return url.ToLower().StartsWith(dir);
            });

            return anyPathIsInUrl != mustContainPath;
        }

        private static string AddTailingSlash(string url)
        {
            if (url[url.Length - 1] != '/')
            {
                url = url + "/";
            }

            return url;
        }

        private static string AddStartSlash(string url)
        {
            if (!url.StartsWith("/"))
            {
                url = "/" + url;
            }

            return url;
        }
    }
}
