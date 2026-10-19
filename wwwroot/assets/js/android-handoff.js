(() => {
    const panel = document.getElementById('android-handoff');
    const link = document.getElementById('android-handoff-link');
    if (!panel || !link || !/Android/i.test(navigator.userAgent)) return;
    panel.hidden = false;
    document.getElementById('android-handoff-dismiss')?.addEventListener('click', () => { panel.hidden = true; });
    // The browser is already on the destination page. Try the app once; the real
    // anchor remains available when a browser requires another user gesture.
    // A token-free browser_fallback_url returns here if no app can handle the intent.
    // TempData is consumed server-side so the fallback cannot start a redirect loop.
    if (link.href.startsWith('intent://')) {
        try { window.location.assign(link.href); } catch (_) { /* Keep the manual link available. */ }
    }
})();
