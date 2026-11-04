// Offline cache for the installed app (own code, no framework template).
// - install: fetch every file of the assets manifest, checked against its SHA-256, into a versioned cache.
//   One failing file fails the install, so a half-filled cache never goes live.
// - fetch: cache first; page navigations get the cached index.html, so deep links like /editor work offline.
// - activate: delete the caches of older versions.
// There is no skipWaiting and no clients.claim: a new version takes over only after every tab and window of the
// app is closed (FR24). All paths are relative to the scope, so the same file works under /HeroPath/.
importScripts('./service-worker-assets.js');

const cachePrefix = 'heropath-cache-';
// The SDK computes the version before finish-site.ps1 changes index.html, so its final hash is part of the name.
const indexAsset = self.assetsManifest.assets.find(asset => asset.url === 'index.html');
if (!indexAsset || !indexAsset.hash) {
    throw new Error('HeroPath service worker: index.html is missing from service-worker-assets.js, so the offline cache cannot be named or filled. Publish the site again.');
}

const cacheName = cachePrefix + self.assetsManifest.version + '-' + indexAsset.hash;
const scope = self.registration.scope;

const skipped = /^(service-worker.*\.js|404\.html|\.nojekyll)$/;
const assets = self.assetsManifest.assets.filter(asset => !skipped.test(asset.url));

function urlOf(path) {
    return new URL(path, scope).href;
}

async function install() {
    const cache = await caches.open(cacheName);
    await Promise.all(assets.map(async asset => {
        try {
            const response = await fetch(new Request(urlOf(asset.url), { integrity: asset.hash, cache: 'no-cache' }));
            if (!response.ok) {
                throw new Error('Could not fetch ' + asset.url + ': ' + response.status);
            }
            await cache.put(urlOf(asset.url), response);
        } catch (error) {
            // Not swallowed: the install fails (the old version keeps running) and the console names the file.
            console.error('HeroPath service worker: offline install failed at ' + asset.url, error);
            throw error;
        }
    }));
}

async function removeOldCaches() {
    const names = await caches.keys();
    await Promise.all(names
        .filter(name => name.startsWith(cachePrefix) && name !== cacheName)
        .map(name => caches.delete(name)));
}

async function respond(request) {
    const cache = await caches.open(cacheName);
    const cached = request.mode === 'navigate'
        ? await cache.match(urlOf('index.html'))
        : await cache.match(request);
    return cached ?? fetch(request);
}

self.addEventListener('install', event => event.waitUntil(install()));
self.addEventListener('activate', event => event.waitUntil(removeOldCaches()));
self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET' || !request.url.startsWith(scope)) {
        return;
    }

    event.respondWith(respond(request));
});
