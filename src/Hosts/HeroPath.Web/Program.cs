using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Levels.UI;
using HeroPath.Shared.Application.Contracts;
using HeroPath.Shared.UI;
using HeroPath.Shared.UI.Localization;
using HeroPath.Web;
using HeroPath.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddSharedUI()
    .AddLevelsModule()
    .AddGameModule();
builder.Services.AddScoped<IAppStorage, LocalStorageAppStorage>();
builder.Services.AddScoped<IDocumentLanguage, BrowserDocumentLanguage>();
builder.Services.AddScoped<IFileDownload, BrowserFileDownload>();
builder.Services.AddScoped<ISoundPlayer, BrowserSoundPlayer>();
builder.Services.AddScoped<IKeyboardShortcuts, BrowserKeyboardShortcuts>();

var host = builder.Build();
await host.Services.ApplyStoredCultureAsync();
await host.Services.ApplyStoredSoundAsync();
await host.Services.LoadCustomLevelsAsync();
await host.RunAsync();
