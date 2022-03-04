using System.Collections.Generic;
using EPiServer.Shell;
using EPiServer.Shell.Navigation;

namespace Geta.Optimizely.Sitemaps
{
    [MenuProvider]
    public class MenuProvider : IMenuProvider
    {
        public IEnumerable<MenuItem> GetMenuItems()
        {
            var url = Paths.ToResource(GetType(), "container");

            var link = new UrlMenuItem(
                "Seo sitemaps",
                MenuPaths.Global + "/cms/seositemaps",
                url)
            {
                SortIndex = 100,
                AuthorizationPolicy = Constants.PolicyName
            };

            return new List<MenuItem>
            {
                link
            };
        }
    }
}
