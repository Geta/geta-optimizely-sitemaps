using Geta.Optimizely.Sitemaps.Web;

Host.CreateDefaultBuilder(args)
    .ConfigureCmsDefaults()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
        webBuilder.UseContentRoot(Path.GetFullPath("../../sandbox/geta-packages-foundation-sandbox/src/Foundation"));
    })
    .Build()
    .Run();
