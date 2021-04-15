using EPiServer.Data;
using EPiServer.DataAbstraction;
using EPiServer.Security;
using EPiServer.Web;
using Geta.SEO.Sitemaps.Configuration;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Models;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Internal;

namespace Geta.SEO.Sitemaps.Pages.Geta.SEO.Sitemaps
{
    public class IndexModel : PageModel
    {
        private readonly ISitemapRepository _sitemapRepository;
        private readonly ISiteDefinitionRepository _siteDefinitionRepository;
        private readonly ILanguageBranchRepository _languageBranchRepository;

        public IndexModel(ISiteDefinitionRepository siteDefinitionRepository, ILanguageBranchRepository languageBranchRepository, ISitemapRepository sitemapRepository)
        {
            _siteDefinitionRepository = siteDefinitionRepository;
            _languageBranchRepository = languageBranchRepository;
            _sitemapRepository = sitemapRepository;
        }

        protected const string SitemapHostPostfix = "Sitemap.xml";

        public bool CreateMenuIsVisible { get; set; }
        public string EditItemId { get; set; }
        [BindProperty]
        public IList<SelectListItem> SiteHosts { get; set; }
        public bool ShowHostsDropDown { get; set; }
        public bool ShowHostsLabel { get; set; }

        protected bool ShowLanguageDropDown { get; set; }

        [BindProperty]
        public IList<SelectListItem> LanguageBranches { get; set; }

        protected int EditIndex { get; set; }
        protected InsertItemPosition InsertItemPosition { get; set; }

        [BindProperty]
        public SitemapViewModel SitemapViewModel { get; set; }

        [BindProperty]
        public IList<SitemapData> SitemapDataList { get; set; }

        public void OnGet()
        {
            GetSiteHosts();
            ShowLanguageDropDown = ShouldShowLanguageDropDown();

            LoadLanguageBranches();

            if (!PrincipalInfo.CurrentPrincipal.IsInRole("admin"))
            {
                /*return Unauthorized();*/
            }

            BindSitemapDataList();
        }

        private void LoadLanguageBranches()
        {
            LanguageBranches = _languageBranchRepository.ListEnabled().Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Culture.Name
            }).ToList();

            LanguageBranches.Insert(0, new SelectListItem
            {
                Text = "*",
                Value = ""
            });
        }

        public IActionResult OnPostNew()
        {
            CreateMenuIsVisible = true;
            EditIndex = -1;
            InsertItemPosition = InsertItemPosition.LastItem;

            LoadLanguageBranches();
            BindSitemapDataList();

            PopulateHostListControl();

            return Page();
        }

        public IActionResult OnPostCreate()
        {
            var sitemap = new SitemapData();
            MapDtoToEntity(sitemap);

            _sitemapRepository.Save(sitemap);

            CloseInsert();
            BindSitemapDataList();
            EmptyDto();

            return RedirectToPage();
        }

        private void MapDtoToEntity(SitemapData sitemap)
        {
            var host = sitemap.Host.IsNullOrEmpty()
                ? SitemapViewModel.Host + SitemapHostPostfix
                : SitemapViewModel.Host;

            sitemap.SiteUrl = SitemapViewModel.SiteUrl;
            sitemap.Host = host;
            sitemap.Language = SitemapViewModel.LanguageBranch;
            sitemap.EnableLanguageFallback = SitemapViewModel.EnableLanguageFallback;
            sitemap.IncludeAlternateLanguagePages = SitemapViewModel.IncludeAlternateLanguagePages;
            sitemap.EnableSimpleAddressSupport = SitemapViewModel.EnableSimpleAddressSupport;
            sitemap.PathsToAvoid = GetList(SitemapViewModel.PathsToAvoid);
            sitemap.PathsToInclude = GetList(SitemapViewModel.PathsToAvoid);
            sitemap.IncludeDebugInfo = SitemapViewModel.IncludeDebugInfo;
            sitemap.SitemapFormat = GetSitemapFormat(SitemapViewModel.SitemapFormFormat);
            sitemap.RootPageId = TryParse(SitemapViewModel.RootPageId);
        }

        private void EmptyDto()
        {
            SitemapViewModel = new SitemapViewModel();
        }

        public IActionResult OnPostEdit(string id)
        {
            EditItemId = id;
            var sitemapData = _sitemapRepository.GetSitemapData(Identity.Parse(id));
            MapDataToModel(sitemapData);
            LoadLanguageBranches();
            BindSitemapDataList();
            PopulateHostListControl();
            return Page();
        }

        public IActionResult OnPostUpdate(string id)
        {
            var sitemap = _sitemapRepository.GetSitemapData(Identity.Parse(id));

            if (sitemap == null)
            {
                return NotFound();
            }

            MapDtoToEntity(sitemap);

            _sitemapRepository.Save(sitemap);

            EditIndex = -1;
            BindSitemapDataList();
            EmptyDto();
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(string id)
        {
            _sitemapRepository.Delete(Identity.Parse(id));
            BindSitemapDataList();

            return RedirectToPage();
        }

        private void MapDataToModel(SitemapData data)
        {
            SitemapViewModel.Host = data.Host;
            SitemapViewModel.EnableLanguageFallback = data.EnableLanguageFallback;
            SitemapViewModel.IncludeAlternateLanguagePages = data.IncludeAlternateLanguagePages;
            SitemapViewModel.EnableSimpleAddressSupport = data.EnableSimpleAddressSupport;
            SitemapViewModel.PathsToAvoid = data.PathsToAvoid != null ? string.Join("; ", data.PathsToAvoid) : string.Empty;
            SitemapViewModel.PathsToInclude = data.PathsToInclude != null ? string.Join("; ", data.PathsToInclude) : string.Empty;
            SitemapViewModel.IncludeDebugInfo = data.IncludeDebugInfo;
            SitemapViewModel.RootPageId = data.RootPageId.ToString();
            SitemapViewModel.SitemapFormFormat = data.SitemapFormat.ToString();
        }

        private void PopulateHostListControl()
        {
            if (SiteHosts.Any())
            {
                ShowHostsDropDown = true;

            }
            else
            {
                ShowHostsLabel = true;
            }

        }

        private void BindSitemapDataList()
        {
            SitemapDataList = _sitemapRepository.GetAllSitemapData();
        }

        private void CloseInsert()
        {
            InsertItemPosition = InsertItemPosition.None;
        }

        private int TryParse(string id)
        {
            int rootId;
            int.TryParse(id, out rootId);

            return rootId;
        }

        private SitemapFormat GetSitemapFormat(string format)
        {
            if (format == SitemapFormat.Mobile.ToString())
            {
                return SitemapFormat.Mobile;
            }

            if (format == SitemapFormat.Commerce.ToString())
            {
                return SitemapFormat.Commerce;
            }

            if (format == SitemapFormat.StandardAndCommerce.ToString())
            {
                return SitemapFormat.StandardAndCommerce;
            }

            return SitemapFormat.Standard;
        }

        private IList<string> GetList(string input)
        {
            if (input == null)
            {
                return null;
            }

            var strValue = input.Trim();

            if (string.IsNullOrEmpty(strValue))
            {
                return null;
            }

            return new List<string>(strValue.Split(';'));
        }

        public IActionResult OnPostCancel(string id)
        {
            EditItemId = string.Empty;
            return RedirectToPage();
        }

        public IActionResult OnPostCancelCreate()
        {
            CreateMenuIsVisible = false;
            return RedirectToPage();
        }

        private void GetSiteHosts()
        {
            var hosts = _siteDefinitionRepository.List().ToList();

            var siteUrls = new List<SelectListItem>(hosts.Count);

            foreach (var siteInformation in hosts)
            {
                siteUrls.Add(new SelectListItem
                {
                    Text = siteInformation.SiteUrl.ToString(),
                    Value = siteInformation.SiteUrl.ToString()
                });

                foreach (var host in siteInformation.Hosts)
                {
                    if (ShouldAddToSiteHosts(host, siteInformation))
                    {
                        var hostUri = host.GetUri();
                        siteUrls.Add(new SelectListItem
                        {
                            Text = hostUri.ToString(),
                            Value = hostUri.ToString()
                        });
                    }
                }
            }

            SiteHosts = siteUrls;
        }

        private static bool ShouldAddToSiteHosts(HostDefinition host, SiteDefinition siteInformation)
        {
            if (host.Name == "*") return false;
            return !UriComparer.SchemeAndServerEquals(host.GetUri(), siteInformation.SiteUrl);
        }

        private bool ShouldShowLanguageDropDown()
        {
            return new SitemapOptions().EnableLanguageDropDownInAdmin;
        }
    }
}
