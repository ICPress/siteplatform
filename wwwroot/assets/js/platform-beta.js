(() => {
    'use strict';
    const byId = id => document.getElementById(id);
    const text = (tag, value, className) => { const el = document.createElement(tag); el.textContent = value; if (className) el.className = className; return el; };
    const flag = country => {
        const el = text('span', '', 'instrument-flag');
        const code = (country || '').toUpperCase();
        el.setAttribute('role', 'img'); el.setAttribute('aria-label', code || 'Country unavailable');
        el.textContent = /^[A-Z]{2}$/.test(code) ? [...code].map(c => String.fromCodePoint(127397 + c.charCodeAt(0))).join('') : '🌐';
        return el;
    };
    const url = item => '/instrument/' + encodeURIComponent(item.id);

    // Accessible, debounced autocomplete. Sequence IDs discard every stale response, including during debounce.
    const input = byId('instrument-query'), results = byId('instrument-results'), status = byId('instrument-search-status');
    if (input && results) {
        let timer, controller, sequence = 0, items = [], active = -1;
        const close = () => { results.hidden = true; input.setAttribute('aria-expanded', 'false'); input.removeAttribute('aria-activedescendant'); active = -1; };
        const open = () => { results.hidden = false; input.setAttribute('aria-expanded', 'true'); };
        const message = value => { results.replaceChildren(text('li', value, 'instrument-empty')); status.textContent = value; open(); };
        const choose = i => { if (items[i]) window.location.assign(url(items[i])); };
        const setActive = i => {
            active = i;
            [...results.querySelectorAll('[role="option"]')].forEach((el, n) => el.setAttribute('aria-selected', String(n === i)));
            const el = byId('instrument-option-' + i);
            if (el) { input.setAttribute('aria-activedescendant', el.id); el.scrollIntoView({block: 'nearest'}); }
        };
        input.addEventListener('input', () => {
            clearTimeout(timer); controller?.abort(); const request = ++sequence;
            items = []; active = -1; close();
            const q = input.value.trim();
            if (!q.length) { status.textContent = 'Type an instrument name or ticker.'; return; }
            timer = setTimeout(async () => {
                controller = new AbortController(); message('Searching…');
                try {
                    const response = await fetch('/instrument/search?q=' + encodeURIComponent(q), {signal:controller.signal});
                    if (!response.ok) throw new Error('search');
                    const data = await response.json();
                    if (request !== sequence) return;
                    items = data; results.replaceChildren();
                    if (!items.length) { message('No instruments found. Try a name or ticker.'); return; }
                    items.forEach((item, i) => {
                        const row = document.createElement('li'); row.id = 'instrument-option-' + i;
                        row.setAttribute('role', 'option'); row.setAttribute('aria-selected', 'false');
                        const copy = text('span', '', 'instrument-result-text');
                        copy.append(text('strong', item.name), text('small', `${item.symbol} · ${item.exchange} · ${item.type}`));
                        row.append(flag(item.countryCode), copy); row.addEventListener('mousedown', e => e.preventDefault());
                        row.addEventListener('click', () => choose(i)); results.append(row);
                    });
                    status.textContent = `${items.length} instruments found. Use arrow keys to choose.`; open();
                } catch (error) { if (error.name !== 'AbortError' && request === sequence) message('Search unavailable. Please try again.'); }
            }, 220);
        });
        input.addEventListener('keydown', event => {
            if (event.key === 'Escape') { ++sequence; clearTimeout(timer); controller?.abort(); close(); return; }
            if (event.key === 'Tab') { close(); return; }
            if (!items.length) return;
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                event.preventDefault(); open();
                setActive(event.key === 'ArrowDown' ? (active + 1) % items.length : (active <= 0 ? items.length - 1 : active - 1));
            } else if (event.key === 'Enter' && !results.hidden) { event.preventDefault(); choose(active >= 0 ? active : 0); }
        });
        document.addEventListener('click', e => { if (!input.parentElement.contains(e.target)) { ++sequence; clearTimeout(timer); controller?.abort(); close(); } });
        input.addEventListener('blur', () => { ++sequence; clearTimeout(timer); controller?.abort(); close(); });
    }

    const mobileToggle = byId('mobileMenuToggle'), mainNav = byId('mainNav');
    const closeMobile = () => { mainNav?.classList.remove('active'); mobileToggle?.setAttribute('aria-expanded', 'false'); };
    document.addEventListener('click', e => { if (mainNav && mobileToggle && !mainNav.contains(e.target) && !mobileToggle.contains(e.target)) closeMobile(); });
    document.addEventListener('keydown', e => { if (e.key === 'Escape' && mainNav?.classList.contains('active')) { closeMobile(); mobileToggle?.focus(); } });

    // Existing account menu styling, with server-derived counts. A failed refresh never invents a zero.
    const menuButton = byId('userMenuButton'), menu = byId('userDropdown');
    if (menuButton && menu) {
        const close = () => { menu.classList.remove('active'); menuButton.setAttribute('aria-expanded', 'false'); };
        menuButton.addEventListener('click', () => { const open = menu.classList.toggle('active'); menuButton.setAttribute('aria-expanded', String(open)); if (open) refreshCounts(); });
        document.addEventListener('click', e => { if (!byId('userMenuContainer').contains(e.target)) close(); });
        document.addEventListener('keydown', e => { if (e.key === 'Escape' && menu.classList.contains('active')) { close(); menuButton.focus(); } });
        byId('userMenuContainer').addEventListener('focusout', e => { if (!byId('userMenuContainer').contains(e.relatedTarget)) close(); });
        let loading = false;
        async function refreshCounts() {
            if (loading || document.hidden) return; loading = true;
            try {
                const response = await fetch('/Account/session', {cache:'no-store'});
                if (response.status === 401) { window.location.reload(); return; }
                if (!response.ok) throw new Error('counts');
                const account = await response.json();
                for (const [id, count] of [['notificationBadge', account.unreadNotifications], ['messageBadge', account.unreadMessages], ['userNavBadge', account.unreadNotifications + account.unreadMessages]]) {
                    const badge = byId(id); badge.textContent = count > 99 ? '99+' : String(count); badge.hidden = count === 0;
                    badge.setAttribute('aria-label', `${count} unread items`);
                }
                byId('account-count-status').textContent = ''; byId('account-count-status').hidden = true;
            } catch { byId('account-count-status').hidden = false; byId('account-count-status').textContent = 'Could not refresh counts. Showing the last available counts.'; }
            finally { loading = false; }
        }
        document.addEventListener('account-counts-changed', refreshCounts);
        setInterval(refreshCounts, 60000);
        document.addEventListener('visibilitychange', () => { if (!document.hidden) refreshCounts(); });
    }

    // TradingView stays outside the inert, blurred preview. Symbols are resolved on the server.
    const charts = new Map();
    function renderChart(host, symbol, compact = false) {
        charts.set(host, {symbol, compact}); host.replaceChildren();
        if (!symbol || !/^[A-Za-z0-9_:-]+(?:\.[A-Za-z0-9_-]+)*$/.test(symbol)) {
            host.append(text('p', 'A live chart is not available for this listing.', 'chart-fallback')); return;
        }
        const container = text('div', '', 'tradingview-widget-container'); container.style.height = '100%'; container.style.width = '100%';
        const widget = text('div', '', 'tradingview-widget-container__widget'); widget.style.height = '100%'; widget.style.width = '100%';
        const script = document.createElement('script'); script.src = 'https://s3.tradingview.com/external-embedding/embed-widget-advanced-chart.js'; script.async = true;
        script.textContent = JSON.stringify({autosize:true, symbol, interval:compact ? '60' : 'D', ...(compact ? {range:'1D', hide_legend:true, hide_volume:true} : {}), timezone:'Etc/UTC', theme:document.documentElement.dataset.theme === 'dark' ? 'dark' : 'light', style:'1', locale:'en', allow_symbol_change:false, hide_top_toolbar:compact, hide_side_toolbar:true, calendar:false, support_host:'https://www.tradingview.com'});
        script.onerror = () => { widget.replaceChildren(text('p', 'The chart could not load. Please try again later.', 'chart-fallback')); };
        container.append(widget, script); host.append(container);
    }
    new MutationObserver(() => { for (const [host, config] of charts) renderChart(host, config.symbol, config.compact); }).observe(document.documentElement, {attributes:true, attributeFilter:['data-theme']});
    const instrumentChart = byId('instrument-chart');
    if (instrumentChart) renderChart(instrumentChart, instrumentChart.dataset.symbol);
    const modeToggle = byId('chartModeToggle'), expandToggle = byId('marketExpandToggle');
    modeToggle?.addEventListener('click', () => {
        const technical = modeToggle.getAttribute('aria-pressed') !== 'true';
        modeToggle.setAttribute('aria-pressed', String(technical));
        modeToggle.querySelector('span').textContent = technical ? 'Live chart' : 'Technical chart';
        instrumentChart.hidden = technical; byId('technical-chart').hidden = !technical;
    });
    const expandChart = expanded => {
        byId('marketExpandRegion')?.classList.toggle('market-expanded', expanded);
        expandToggle?.setAttribute('aria-expanded', String(expanded));
        if (expandToggle) expandToggle.querySelector('span').textContent = expanded ? 'Restore' : 'Expand';
    };
    expandToggle?.addEventListener('click', () => expandChart(expandToggle.getAttribute('aria-expanded') !== 'true'));
    document.addEventListener('keydown', event => { if (event.key === 'Escape') expandChart(false); });
    const preview = byId('instrument-preview-frame');
    if (preview) {
        const sync = () => { preview.src = '/assets/instrument-preview.html#' + (document.documentElement.dataset.theme || 'light'); };
        sync(); new MutationObserver(sync).observe(document.documentElement, {attributes:true, attributeFilter:['data-theme']});
    }
    const list = byId('etf-list');
    if (list) {
        const select = row => {
            const host = byId('overview-chart');
            if (!host || charts.get(host)?.symbol === row.dataset.symbol) return;
            byId('overview-symbol').textContent = row.dataset.label;
            for (const item of list.querySelectorAll('.etf-item')) { item.classList.toggle('active', item === row); item.setAttribute('aria-pressed', String(item === row)); }
            renderChart(host, row.dataset.symbol, true);
        };
        for (const row of list.querySelectorAll('.etf-item')) row.addEventListener('click', event => { event.preventDefault(); select(row); });
        const first = list.querySelector('.etf-item');
        if (first) select(first);
    }
})();
