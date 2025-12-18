using MONITOR.SERVICE.VISUALAB;
using MONITOR.SERVICE.VISUALAB.Repositories;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Servicio de monitoreo";
});

//builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7168/") });
//builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5298/") });
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri("http://172.19.50.142:8091/") });
builder.Services.AddSystemd();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IRepository, Repository>();

var host = builder.Build();
host.Run();
