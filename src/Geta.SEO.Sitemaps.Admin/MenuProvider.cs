using EPiServer.Shell;
using EPiServer.Shell.Navigation;
using System.Collections.Generic;

namespace Geta.SEO.Sitemaps.Admin
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
                SortIndex = 100
            };

            return new List<MenuItem>
            {
                link
            };
        }
    }
}
