// Blazor WebAssembly only loads the satellite assemblies (resx) of the culture active at startup. The app switches
// language at runtime without a reload, so all of them are loaded up front. loadAllSatelliteResources is read by the
// .NET runtime loader but is not part of its public type definitions (dotnet.d.ts); LanguageTests (E2E) guards it.
// This is an own file (not an inline script) so a Content-Security-Policy does not need a hash for it.
Blazor.start({
    configureRuntime: dotnet => dotnet.withConfig({ loadAllSatelliteResources: true })
});
