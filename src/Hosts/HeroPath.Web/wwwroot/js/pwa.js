// Registers the service worker, but only when index.html carries <meta name="heropath-pwa" content="on">.
// deploy/finish-site.ps1 -Pwa adds that meta tag for the GitHub Pages build. The ZIP, local development and
// every other build leave it out and never get a service worker (FR24). A failed registration must never
// break the app: it is logged with console.warn and nothing else happens.
(function () {
    try {
        const flag = document.querySelector('meta[name="heropath-pwa"]');
        if (!flag || flag.getAttribute('content') !== 'on' || !('serviceWorker' in navigator)) {
            return;
        }

        window.addEventListener('load', function () {
            try {
                navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' }).catch(function (error) {
                    console.warn('HeroPath: the service worker could not be registered, the app runs online only.', error);
                });
            } catch (error) {
                console.warn('HeroPath: the service worker could not be registered, the app runs online only.', error);
            }
        });
    } catch (error) {
        console.warn('HeroPath: the offline support could not be set up.', error);
    }
})();
