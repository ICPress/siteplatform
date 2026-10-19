(() => {
    const form = document.getElementById('email-verification-form');
    if (!form) return;
    // Consume only through the antiforgery-protected POST, never the landing GET.
    history.replaceState(null, '', '/Account/email-verification');
    form.requestSubmit();
})();
