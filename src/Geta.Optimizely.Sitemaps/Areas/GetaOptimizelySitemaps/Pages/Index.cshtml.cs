using System.Collections.Generic;
using System.Linq;
using EPiServer.Applications;
using EPiServer.Data;
using EPiServer.DataAbstraction;
using Geta.Mapping;
using Geta.Optimizely.Sitemaps.Entities;
using Geta.Optimizely.Sitemaps.Models;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Geta.Optimizely.Sitemaps.Pages.Geta.Optimizely.Sitemaps;

[Authorize(Constants.PolicyName)]
public class IndexModel : PageModel
{
    private readonly ISitemapRepository _sitemapRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly ILanguageBranchRepository _languageBranchRepository;
    private readonly IMapper<SitemapViewModel, SitemapData> _modelToEntityMapper;
    private readonly ICreateFrom<SitemapData, SitemapViewModel> _entityToModelCreator;

    public IndexModel(
        ISitemapRepository sitemapRepository,
        IApplicationRepository applicationRepository,
        ILanguageBranchRepository languageBranchRepository,
        IMapper<SitemapViewModel, SitemapData> modelToEntityMapper,
        ICreateFrom<SitemapData, SitemapViewModel> entityToModelCreator)
    {
        _sitemapRepository = sitemapRepository;
        _applicationRepository = applicationRepository;
        _languageBranchRepository = languageBranchRepository;
        _modelToEntityMapper = modelToEntityMapper;
        _entityToModelCreator = entityToModelCreator;
    }

    public bool CreateMenuIsVisible { get; set; }
    private string EditItemId { get; set; }
    [BindProperty] public IList<SelectListItem> SiteHosts { get; set; }
    public bool ShowHostsDropDown { get; set; }
    public string HostLabel { get; set; }
    [BindProperty] public IList<SelectListItem> LanguageBranches { get; set; }
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

        LoadLanguageBranches();
        BindSitemapDataList();
        PopulateHostListControl();

        return Page();
    }

    public IActionResult OnGetView(string id)
    {
        var sitemap = _sitemapRepository.GetSitemapData(Identity.Parse(id));

        if (sitemap == null)
        {
            return NotFound();
        }

        return File(sitemap.Data, "text/xml; charset=utf-8");
    }

    public IActionResult OnPostCreate()
    {
        var sitemap = new SitemapData();
        _modelToEntityMapper.Map(SitemapViewModel, sitemap);
        _sitemapRepository.Save(sitemap);

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
        var sitemapData = _sitemapRepository.GetSitemapData(Identity.Parse(id));
        SitemapViewModel = _entityToModelCreator.Create(sitemapData);
        EditItemId = id;
        LoadLanguageBranches();
        BindSitemapDataList();
        PopulateHostListControl(sitemapData.SiteUrl);
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
        var sites = _applicationRepository
            .List()
            .OfType<IRoutableApplication>()
            .ToList();

        var urls = new List<SelectListItem>();
        foreach (var site in sites)
        {
            if (site.Url != null)
            {
                var port = site.Url.IsDefaultPort ? string.Empty : $":{site.Url.Port}";
                urls.Add(new SelectListItem
                {
                    Text = site.Url.Host,
                    Value = $"{site.Url.Scheme}://{site.Url.Host}{port}/"
                });
            }

            foreach (var host in site.Hosts.Where(x => x.Url != null))
            {
                if (UriComparer.SchemeAndServerEquals(site.Url, host.Url))
                {
                    continue;
                }

                urls.Add(new SelectListItem
                {
                    Text = host.Url!.ToString(),
                    Value = host.Url!.ToString()
                });
            }
        }
        SiteHosts = urls;
    }

    private void PopulateHostListControl(string selected = null)
    {
        if (SiteHosts.Count > 1)
        {
            ShowHostsDropDown = true;
        }
        else
        {
            HostLabel = selected ?? SiteHosts.FirstOrDefault()?.Value;
        }
    }

    private void EmptyDto()
    {
        SitemapViewModel = new();
    }

    public bool IsEditing(string id)
    {
        return id == EditItemId;
    }

    public bool IsEditing()
    {
        return !string.IsNullOrEmpty(EditItemId);
    }
}
