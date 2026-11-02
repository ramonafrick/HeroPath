using HeroPath.Modules.Game.UI;
using HeroPath.Modules.Levels.UI;
using HeroPath.Shared.UI;
using HeroPath.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddSharedUI()
    .AddLevelsModule()
    .AddGameModule();

await builder.Build().RunAsync();
