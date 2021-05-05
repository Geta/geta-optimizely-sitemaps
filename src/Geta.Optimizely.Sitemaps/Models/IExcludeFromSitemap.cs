using EPiServer.Core;

namespace Geta.Optimizely.Sitemaps.Models
{
    /// <summary>
    /// Apply this interface to pagetypes you do not want to include in the index
    /// </summary>
    public interface IExcludeFromSitemap : IContent
    {
    }
}
