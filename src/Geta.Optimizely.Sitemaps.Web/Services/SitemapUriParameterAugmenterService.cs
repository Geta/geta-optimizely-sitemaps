using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using Foundation.Features.People.PersonItemPage;
using Geta.Optimizely.Sitemaps.Services;

namespace Geta.Optimizely.Sitemaps.Web.Services
{
    public class SitemapUriParameterAugmenterService : IUriAugmenterService
    {
        private readonly IContentTypeRepository _contentTypeRepository;
        private readonly IContentModelUsage _contentModelUsage;
        private readonly IContentRepository _contentRepository;

        public SitemapUriParameterAugmenterService(IContentTypeRepository contentTypeRepository, IContentModelUsage contentModelUsage, IContentRepository contentRepository)
        {
            _contentTypeRepository = contentTypeRepository;
            _contentModelUsage = contentModelUsage;
            _contentRepository = contentRepository;
        }

        public IEnumerable<Uri> GetAugmentUris(IContent content, CurrentLanguageContent languageContentInfo, Uri fullUri)
        {
            if (content is PageData pageContent)
            {
                if (pageContent.PageTypeName == nameof(Foundation.Features.People.PersonListPage))
                {
                    var fullUriString = fullUri.ToString();

                    var personPageType = _contentTypeRepository.Load<PersonPage>();
                    var usages = _contentModelUsage.ListContentOfContentType(personPageType).Select(c => _contentRepository.Get<PersonPage>(c.ContentLink));
                    // Group all of the results by the querystring parameters that drive the page.
                    var nameSectorLocations = usages.GroupBy(k => new { k.Name, k.Sector, k.Location });

                    // Enumerate the total set of expected name/sectors/locations in ordr for them to be indexed.
                    foreach (var nameSectorLocation in nameSectorLocations)
                    {
                        var augmentedUri = new Uri($"{fullUriString}?name={nameSectorLocation.Key.Name}&sector={nameSectorLocation.Key.Sector}&location={nameSectorLocation.Key.Location}");
                        yield return augmentedUri;
                    }
                }
                else
                {
                    yield return fullUri;
                }
            }
        }
    }
}
