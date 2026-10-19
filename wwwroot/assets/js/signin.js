(() => {
    const host = document.getElementById('google-signin');
    if (!host) return;
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.onload = () => {
        google.accounts.id.initialize({client_id: host.dataset.clientId, auto_select: false, callback: response => {
            document.getElementById('google-credential').value = response.credential;
            document.getElementById('google-form').submit();
        }});
        google.accounts.id.renderButton(host, {theme: 'outline', size: 'large', width: Math.min(360, host.clientWidth), text: 'continue_with'});
        document.getElementById('google-status').hidden = true;
    };
    script.onerror = () => { document.getElementById('google-status').textContent = 'Google sign-in is unavailable. Please use email.'; };
    document.head.append(script);
})();
