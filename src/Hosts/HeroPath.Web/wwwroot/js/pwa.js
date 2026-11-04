// Registers the service worker, but only when index.html carries <meta name="heropath-pwa" content="on">.
// deploy/finish-site.ps1 -Pwa adds that meta tag for the GitHub Pages build. The ZIP, local development and
// every other build leave it out and never get a service worker (FR24). A failed registration must never
// break the app, so every error is swallowed.
(function () {
    try {
        const flag = document.querySelector('meta[name="heropath-pwa"]');
        if (!flag || flag.getAttribute('content') !== 'on' || !('serviceWorker' in navigator)) {
            return;
        }

        window.addEventListener('load', function () {
            try {
                navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' }).catch(function () { });
            } catch (error) {
                // registration is optional
            }
        });
    } catch (error) {
        // registration is optional
    }
})();
