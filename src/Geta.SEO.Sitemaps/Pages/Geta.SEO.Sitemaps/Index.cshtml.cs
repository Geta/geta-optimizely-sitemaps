using EPiServer.Data;
using EPiServer.DataAbstraction;
using EPiServer.Web;
using Geta.Mapping;
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

namespace Geta.SEO.Sitemaps.Pages.Geta.SEO.Sitemaps
{
    public class IndexModel : PageModel
    {
        private readonly ISitemapRepository _sitemapRepository;
        private readonly ISiteDefinitionRepository _siteDefinitionRepository;
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly IMapper<SitemapViewModel, SitemapData> _modelToEntityMapper;

        public IndexModel(
            ISiteDefinitionRepository siteDefinitionRepository,
            ILanguageBranchRepository languageBranchRepository,
            ISitemapRepository sitemapRepository,
            IMapper<SitemapViewModel, SitemapData> modelToEntityMapper)
        {
            _siteDefinitionRepository = siteDefinitionRepository;
            _languageBranchRepository = languageBranchRepository;
            _sitemapRepository = sitemapRepository;
            _modelToEntityMapper = modelToEntityMapper;
        }

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

        [BindProperty] public SitemapViewModel SitemapViewModel { get; set; }

        [BindProperty]
        public IList<SitemapData> SitemapDataList { get; set; }

        public void OnGet()
        {
            GetSiteHosts();
            ShowLanguageDropDown = ShouldShowLanguageDropDown();

            LoadLanguageBranches();

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
            _modelToEntityMapper.Map(SitemapViewModel, sitemap);
            _sitemapRepository.Save(sitemap);

            CloseInsert();
            BindSitemapDataList();
            EmptyDto();

            return RedirectToPage();
        }

        private void EmptyDto()
        {
            SitemapViewModel = new SitemapViewModel();
        }

        public IActionResult OnPostEdit(string id)
        {
            EditItemId = id;
            var sitemapData = _sitemapRepository.GetSitemapData(Identity.Parse(id));
            SitemapViewModel.MapToViewModel(sitemapData);
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

            _modelToEntityMapper.Map(SitemapViewModel, sitemap);
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

        public bool IsEditing(string id)
        {
            return id == EditItemId;
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
