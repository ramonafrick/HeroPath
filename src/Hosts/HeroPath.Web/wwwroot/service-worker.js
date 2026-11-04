// Development: no caching and no offline mode. The published build ships service-worker.published.js instead
// (see the ServiceWorker item in HeroPath.Web.csproj). The page only registers a service worker when index.html
// carries <meta name="heropath-pwa" content="on">, which deploy/finish-site.ps1 -Pwa adds.
self.addEventListener('fetch', () => { });
