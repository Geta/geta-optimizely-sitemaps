using System.Collections.Generic;
using System.Linq;
using EPiServer.Applications;
using EPiServer.Data;
using EPiServer.DataAbstraction;
using EPiServer.Scheduler;
using Geta.Mapping;
using Geta.Optimizely.Sitemaps.Entities;
using Geta.Optimizely.Sitemaps.Models;
using Geta.Optimizely.Sitemaps.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;

namespace Geta.Optimizely.Sitemaps.Pages.Geta.Optimizely.Sitemaps;

[Authorize(Constants.PolicyName)]
public class IndexModel : PageModel
{
    // The scheduler's own limit before it treats a running job as dead.
    private const int MissedPingsBeforeJobIsDead = 4;

    private readonly ISitemapRepository _sitemapRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly ILanguageBranchRepository _languageBranchRepository;
    private readonly IMapper<SitemapViewModel, SitemapData> _modelToEntityMapper;
    private readonly ICreateFrom<SitemapData, SitemapViewModel> _entityToModelCreator;
    private readonly IScheduledJobRepository _scheduledJobRepository;
    private readonly IScheduledJobExecutor _scheduledJobExecutor;
    private readonly SchedulerOptions _schedulerOptions;

    public IndexModel(
        ISitemapRepository sitemapRepository,
        IApplicationRepository applicationRepository,
        ILanguageBranchRepository languageBranchRepository,
        IMapper<SitemapViewModel, SitemapData> modelToEntityMapper,
        ICreateFrom<SitemapData, SitemapViewModel> entityToModelCreator,
        IScheduledJobRepository scheduledJobRepository,
        IScheduledJobExecutor scheduledJobExecutor,
        IOptions<SchedulerOptions> schedulerOptions)
    {
        _sitemapRepository = sitemapRepository;
        _applicationRepository = applicationRepository;
        _languageBranchRepository = languageBranchRepository;
        _modelToEntityMapper = modelToEntityMapper;
        _entityToModelCreator = entityToModelCreator;
        _scheduledJobRepository = scheduledJobRepository;
        _scheduledJobExecutor = scheduledJobExecutor;
        _schedulerOptions = schedulerOptions.Value;
    }

    public bool CreateMenuIsVisible { get; set; }
    private string EditItemId { get; set; }
    [BindProperty] public IList<SelectListItem> SiteHosts { get; set; }
    public bool ShowHostsDropDown { get; set; }
    public string HostLabel { get; set; }
    [TempData] public string StatusMessage { get; set; }
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

    public IActionResult OnPostRunJob()
    {
        StatusMessage = StartJob();
        return RedirectToPage();
    }

    private string StartJob()
    {
        var job = _scheduledJobRepository.Get<SitemapCreateJob>();

        if (job == null)
        {
            return "The sitemap job isn't registered as a scheduled job.";
        }

        if (IsRunning(job))
        {
            return "The sitemap job is already running.";
        }

        // Not awaited: a large site can take longer to generate than a request should last. The job logs its own result.
        var execution = _scheduledJobExecutor.StartAsync(job, new JobExecutionOptions { Trigger = ScheduledJobTrigger.User });

        return execution.IsCompletedSuccessfully && execution.Result.Status == ScheduledJobExecutionStatus.UnableToStart
            ? execution.Result.Message
            : "The sitemap job has started. Its result will be in the scheduled job's history.";
    }

    /// <summary>
    /// The stored flag stays set if the process dies during a run, but a live run on any server keeps pinging.
    /// </summary>
    private bool IsRunning(ScheduledJob job)
    {
        return job.IsRunning
               && job.SecondsAfterLastPing < MissedPingsBeforeJobIsDead * _schedulerOptions.PingTime.TotalSeconds;
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
        SiteHosts = _applicationRepository
            .List()
            .OfType<IRoutableApplication>()
            .SelectMany(GetSitemapUrls)
            .Distinct()
            .Select(url => new SelectListItem { Text = url, Value = url })
            .ToList();
    }

    private static IEnumerable<string> GetSitemapUrls(IRoutableApplication site)
    {
        // Edit, preview, media and redirect hosts don't serve the site's pages, so a sitemap can't live on them.
        var hostUrls = site.Hosts
            .Where(x => x.Type is ApplicationHostType.Primary or ApplicationHostType.Default)
            .Select(x => x.Url);

        return hostUrls.Prepend(site.Url).Where(url => url != null).Select(url => url.ToString());
    }

    private void PopulateHostListControl(string selected = null)
    {
        // A dropdown without the saved URL shows its first option, and saving would silently change the sitemap's host.
        if (!string.IsNullOrEmpty(selected) && SiteHosts.All(x => x.Value != selected))
        {
            SiteHosts.Insert(0, new SelectListItem { Text = selected, Value = selected });
        }

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
