(() => {
    'use strict';
    const form = document.querySelector('[data-mark-read]');
    if (!form) return;
    const allowed = new Set([...form.querySelectorAll('input[name=ids]')].map(x => x.value));
    const pending = new Set(); let timer, running = false;
    const status = document.querySelector('.read-status');
    async function flush() {
        if (running || !pending.size || document.hidden) return;
        running = true; let succeeded = false;
        const ids = [...pending].slice(0, 30); ids.forEach(id => pending.delete(id));
        const data = new FormData(form); data.delete('ids'); ids.forEach(id => data.append('ids', id));
        try {
            const response = await fetch(form.action, {method:'POST', body:data, headers:{'X-Requested-With':'XMLHttpRequest'}});
            if (!response.ok) throw new Error('read');
            for (const row of document.querySelectorAll('[data-read-item]')) if (ids.includes(row.dataset.readItem)) {
                row.classList.remove('unread'); row.querySelector('.unread-dot')?.remove();
            }
            if (status) status.replaceChildren();
            succeeded = true;
            document.dispatchEvent(new Event('account-counts-changed'));
        } catch {
            ids.forEach(id => pending.add(id));
            if (status) {
                status.textContent = 'Read status could not be updated. ';
                const retry = document.createElement('button'); retry.type = 'button'; retry.className = 'text-button'; retry.textContent = 'Retry';
                retry.addEventListener('click', flush); status.append(retry);
            }
        } finally { running = false; if (succeeded && pending.size) timer = setTimeout(flush, 150); }
    }
    const queue = id => { if (!allowed.delete(id)) return; pending.add(id); clearTimeout(timer); timer = setTimeout(flush, 150); };
    if ('IntersectionObserver' in window) {
        const observer = new IntersectionObserver(entries => {
            for (const entry of entries) if (entry.isIntersecting) { queue(entry.target.dataset.readItem); observer.unobserve(entry.target); }
        }, {threshold:.25});
        document.querySelectorAll('[data-read-item]').forEach(row => observer.observe(row));
    } else allowed.forEach(id => queue(id));
    document.addEventListener('visibilitychange', () => { if (!document.hidden) flush(); });
})();
