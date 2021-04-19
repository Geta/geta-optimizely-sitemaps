using EPiServer.Data;
using EPiServer.DataAbstraction;
using EPiServer.Web;
using Geta.Mapping;
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
        private readonly ICreateFrom<SitemapData, SitemapViewModel> _entityToModelCreator;

        public IndexModel(
            ISitemapRepository sitemapRepository,
            ISiteDefinitionRepository siteDefinitionRepository,
            ILanguageBranchRepository languageBranchRepository,
            IMapper<SitemapViewModel, SitemapData> modelToEntityMapper,
            ICreateFrom<SitemapData, SitemapViewModel> entityToModelCreator)
        {
            _sitemapRepository = sitemapRepository;
            _siteDefinitionRepository = siteDefinitionRepository;
            _languageBranchRepository = languageBranchRepository;
            _modelToEntityMapper = modelToEntityMapper;
            _entityToModelCreator = entityToModelCreator;
        }

        public bool CreateMenuIsVisible { get; set; }
        public string EditItemId { get; set; }
        [BindProperty] public IList<SelectListItem> SiteHosts { get; set; }
        public bool ShowHostsDropDown { get; set; }
        public string HostLabel { get; set; }
        public bool ShowHostsLabel { get; set; }
        [BindProperty] public IList<SelectListItem> LanguageBranches { get; set; }
        protected int EditIndex { get; set; }
        protected InsertItemPosition InsertItemPosition { get; set; }
        [BindProperty] public SitemapViewModel SitemapViewModel { get; set; }
        [BindProperty] public IList<SitemapViewModel> SitemapViewModels { get; set; }

        public void OnGet()
        {
            BindSitemapDataList();
        }

        public IActionResult OnPostNew()
        {
            LoadSiteHosts();

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

        public IActionResult OnPostCancelCreate()
        {
            CreateMenuIsVisible = false;
            return RedirectToPage();
        }

        public IActionResult OnPostEdit(string id)
        {
            LoadSiteHosts();
            EditItemId = id;
            var sitemapData = _sitemapRepository.GetSitemapData(Identity.Parse(id));
            SitemapViewModel = _entityToModelCreator.Create(sitemapData);
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

        public IActionResult OnPostCancel(string id)
        {
            EditItemId = string.Empty;
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(string id)
        {
            _sitemapRepository.Delete(Identity.Parse(id));
            BindSitemapDataList();

            return RedirectToPage();
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

        private void BindSitemapDataList()
        {
            var sitemapsData = _sitemapRepository.GetAllSitemapData();
            SitemapViewModels = sitemapsData.Select(entity => _entityToModelCreator.Create(entity)).ToList();
        }

        private void LoadSiteHosts()
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

        private void PopulateHostListControl()
        {
            if (SiteHosts.Count > 1)
            {
                ShowHostsDropDown = true;
            }
            else
            {
                HostLabel = SiteHosts.ElementAt(0).Value;
                ShowHostsLabel = true;
            }
        }

        private void CloseInsert()
        {
            InsertItemPosition = InsertItemPosition.None;
        }

        private void EmptyDto()
        {
            SitemapViewModel = new SitemapViewModel();
        }


        public bool IsEditing(string id)
        {
            return id == EditItemId;
        }
    }
}