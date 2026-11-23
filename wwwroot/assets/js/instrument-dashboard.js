(() => {
    'use strict';
    const root = document.getElementById('instrumentDashboard');
    if (!root) return;
    const byId = id => document.getElementById(id);

    const siteHeader = byId('mainHeader');
    function updateInstrumentHeaderOffset() {
        if (!siteHeader) return;
        const height = siteHeader.getBoundingClientRect().height;
        if (Number.isFinite(height) && height > 0)
            document.documentElement.style.setProperty('--instrument-header-height', `${height}px`);
    }
    updateInstrumentHeaderOffset();
    if (siteHeader && 'ResizeObserver' in window) {
        const headerResizeObserver = new ResizeObserver(updateInstrumentHeaderOffset);
        headerResizeObserver.observe(siteHeader);
    }
    window.addEventListener('resize', updateInstrumentHeaderOffset, { passive: true });
    window.addEventListener('scroll', updateInstrumentHeaderOffset, { passive: true });

    // Keep the prototype's left-hand on-page navigation in sync with scrolling.
    const sectionLinks = [...root.querySelectorAll('.research-section-tabs a[href^="#"]')];
    if (sectionLinks.length) {
        const activate = href => sectionLinks.forEach(link => {
            if (link.getAttribute('href') === href) link.setAttribute('aria-current', 'location');
            else link.removeAttribute('aria-current');
        });
        sectionLinks.forEach(link => link.addEventListener('click', () => activate(link.getAttribute('href'))));
        if ('IntersectionObserver' in window) {
            const observed = sectionLinks.map(link => ({ link, target: document.querySelector(link.getAttribute('href')) })).filter(item => item.target);
            const observer = new IntersectionObserver(() => {
                const offset = window.innerWidth >= 1180 ? 120 : 145;
                const visible = observed.filter(item => item.target.getBoundingClientRect().top <= offset);
                if (visible.length) activate(visible[visible.length - 1].link.getAttribute('href'));
            }, { rootMargin: '-100px 0px -60% 0px', threshold: 0 });
            observed.forEach(item => observer.observe(item.target));
        }
    }
    const authenticated = root.dataset.authenticated === 'true';
    const symbol = root.dataset.symbol;

    // Matomo is initialized globally by _MatomoTracker.local.cshtml. Send each
    // dashboard engagement event once per page view, with the ticker as event name.
    function trackDashboardEvent(action) {
        const queue = window._paq = window._paq || [];
        queue.push(['trackEvent', 'Instrument Dashboard', action, symbol]);
    }
    const scrollEventTargets = [
        { element: byId('overviewSection'), action: 'Scroll to Price Level Section' },
        { element: byId('marketExpandRegion'), action: 'Scroll to Market Chart Section' }
    ].filter(item => item.element);
    const sentScrollEvents = new Set();
    let scrollCheckPending = false;
    function checkDashboardScrollEvents() {
        if (scrollCheckPending || sentScrollEvents.size === scrollEventTargets.length) return;
        scrollCheckPending = true;
        requestAnimationFrame(() => {
            scrollCheckPending = false;
            const viewportHeight = window.innerHeight || document.documentElement.clientHeight;
            scrollEventTargets.forEach(item => {
                if (sentScrollEvents.has(item.action)) return;
                const rect = item.element.getBoundingClientRect();
                if (rect.top < viewportHeight * 0.82 && rect.bottom > viewportHeight * 0.18) {
                    sentScrollEvents.add(item.action);
                    trackDashboardEvent(item.action);
                }
            });
            if (sentScrollEvents.size === scrollEventTargets.length)
                window.removeEventListener('scroll', checkDashboardScrollEvents);
        });
    }
    window.addEventListener('scroll', checkDashboardScrollEvents, { passive: true });
    root.querySelector('.ai-pack-download')?.addEventListener('click', () => {
        trackDashboardEvent('Research Pack Download Click');
    });

    const barsUrl = root.dataset.barsUrl;
    const pineUrl = root.dataset.pineUrl;
    const chartToggle = byId('chartModeToggle');
    const liveChart = byId('instrument-chart');
    const technicalChartHost = byId('technical-chart');
    const technicalGate = byId('technicalChartGate');
    const labelToggle = byId('technicalLabelsToggle');
    const technicalControls = byId('technicalViewControls');
    const timeframeSelect = byId('technicalTimeframeSelect');
    let velaChart = null;
    let pineSources = null;
    let structureIndicator = null;
    let labelIndicator = null;
    let labelsVisible = false;
    let chartPromise = null;
    let technicalMode = false;

    // ROC-based price map adapted from the attached dashboard prototype.
    // Each level is assigned to its FIRST qualifying horizon only.
    const levelMap = byId('priceLevelMap');
    if (levelMap) {
        const payload = byId('researchLevelsData');
        let levels = [];
        try { levels = JSON.parse(payload?.textContent || '[]'); }
        catch (error) { console.warn('Unable to parse price levels', error); }
        const horizons = [{key:'week', label:'Week', days:5}, {key:'month', label:'Month', days:21}, {key:'year', label:'Year', days:252}];
        const close = Number(levelMap.dataset.referenceClose);
        const roc = Number(root.dataset.averageRoc);
        const rocReady = root.dataset.averageRoc !== '' && Number.isFinite(roc) && roc >= 0 && Number.isFinite(close) && close > 0;
        const state = {horizon:'week', category:'all'};
        const money = value => Number(value).toLocaleString('en-US',{style:'currency',currency:'USD',minimumFractionDigits:3,maximumFractionDigits:3});
        const pct = value => `${value > 0 ? '+' : value < 0 ? '−' : ''}${Math.abs(value).toFixed(3)}%`;
        const distance = value => (value / close - 1) * 100;
        const normalized = levels.map((level, index) => ({
            index, name:String(level.name || ''), category:level.category || 'TECHNICAL',
            low: Number(level.start), high:Number(level.end), description:String(level.description || ''),
            formula:String(level.formula || ''), date:String(level.date || '')
        })).filter(level => level.name && Number.isFinite(level.low) && Number.isFinite(level.high) && level.low > 0 && level.high > 0 && Number.isFinite(close) && close > 0);
        function bucket(level) {
            if (!rocReady) return 'week'; // Show levels, but identify that there is no ROC classification.
            const extent = Math.max(Math.abs(distance(level.low)), Math.abs(distance(level.high)));
            return horizons.find(h => extent <= roc * h.days + 1e-9)?.key || null;
        }
        function wordBoundary(description) {
            if (description.length <= 100) return description;
            const previous = description.lastIndexOf(' ',100), next = description.indexOf(' ',100);
            if (previous < 0) return next < 0 ? description.slice(0,100) : description.slice(0,next);
            return next < 0 || (100-previous) <= (next-100) ? description.slice(0,previous) : description.slice(0,next);
        }
        const make = (tag, className, value) => {
            const el = document.createElement(tag);
            if (className) el.className = className;
            if (value != null) el.textContent = value;
            return el;
        };
        function render() {
            const categoryMatches = item => state.category === 'all' || item.category === state.category;
            const counts = Object.fromEntries(horizons.map(h=>[h.key,normalized.filter(item=>bucket(item)===h.key && categoryMatches(item)).length]));
            const active = horizons.find(h=>h.key===state.horizon) || horizons[0];
            const activeIndex = horizons.indexOf(active);
            horizons.forEach(h => {
                const tab = byId(`lm-horizon-${h.key}`);
                tab?.setAttribute('aria-selected',String(h === active));
                if (tab) tab.tabIndex = h === active ? 0 : -1;
                if (byId(`lm-count-${h.key}`)) byId(`lm-count-${h.key}`).textContent = counts[h.key];
            });
            root.querySelectorAll('[data-category]').forEach(button=>button.setAttribute('aria-pressed',String(button.dataset.category===state.category)));
            byId('lm-horizon-panel').setAttribute('aria-labelledby',`lm-horizon-${state.horizon}`);
            byId('lm-reference-close').textContent=money(close);
            byId('lm-reference-date').textContent=(levelMap.dataset.referenceDate || 'Close date unavailable')+' · recorded market close';
            const from = activeIndex ? roc * horizons[activeIndex-1].days : 0;
            const envelope = roc * active.days;
            byId('lm-envelope').textContent = rocReady ? `${activeIndex ? '> '+from.toFixed(3)+'% to ' : '0% to '}${envelope.toFixed(3)}% absolute distance` : 'ROC history unavailable';
            byId('lm-roc-sample').textContent = rocReady ? `${roc.toFixed(3)}% mean absolute daily ROC · ${root.dataset.rocSample || 0} returns` : 'Insufficient closing-price history for ROC';
            byId('lm-horizon-note').textContent = `${active.days} trading days${activeIndex ? ' · excludes '+(activeIndex===1?'Week':'Week & Month') : ''}`;
            const rows = normalized.filter(level=>categoryMatches(level)&&bucket(level)===state.horizon).sort((x,y)=>x.low-y.low);
            const outside = normalized.filter(level=>bucket(level)===null).length;
            byId('lm-filter-status').textContent = rocReady
                ? `${rows.length} levels in ${active.label}. ${activeIndex?'Earlier-horizon levels are excluded.':'Each level appears in its first qualifying horizon.'} ${outside?outside+' levels outside Year are omitted.':''}`
                : `${rows.length} levels shown without horizon classification because historical ROC is unavailable.`;
            const numbers = rows.flatMap(level=>[distance(level.low),distance(level.high)]);
            const minimum = Math.min(0,...numbers), maximum = Math.max(0,...numbers);
            const padding = Math.max(1, (maximum-minimum)*0.08);
            const step = maximum-minimum>100?20:maximum-minimum>40?10:maximum-minimum>15?5:1;
            const lower = Math.max(-100, Math.floor((minimum-padding)/step)*step), upper = Math.ceil((maximum+padding)/step)*step;
            const position = value => Math.min(100,Math.max(0,(value-lower)/(upper-lower || 1)*100));
            const zero = position(0);
            levelMap.style.setProperty('--lm-zero',`${zero}%`);
            byId('lm-range-caption').textContent=`Display scale: ${pct(lower)} to ${pct(upper)} · auto-fit to selected levels`;
            const axis = byId('lm-axis'); axis.replaceChildren();
            const ticks = [lower,lower+(upper-lower)*.25,0,lower+(upper-lower)*.75,upper].sort((x,y)=>x-y);
            [...new Set(ticks.map(value=>Number(value.toFixed(3))))].filter(t=>t===0||Math.abs(position(t)-zero)>10||t===lower||t===upper).forEach(t=>{
                const tick = make('span','lm-axis-tick',`${t>0?'+':''}${t}%`);
                tick.style.left=`${position(t)}%`; axis.append(tick);
            });
            const container=byId('lm-rows'); container.replaceChildren();
            if (!rows.length) {
                const empty=make('div','lm-empty');
                empty.append(make('strong','',`No ${state.category==='all'?'levels':state.category.toLowerCase()+' levels'} in ${active.label}.`));
                const other=horizons.filter(h=>h.key!==state.horizon && counts[h.key]>0);
                empty.append(make('p','',other.length ? `Available in ${other.map(h=>h.label+' ('+counts[h.key]+')').join(' and ')}. Earlier-horizon levels are not repeated.` : 'No stored levels qualify for this filter.'));
                other.forEach(h=>{const jump=make('button','','View '+h.label);jump.type='button';jump.addEventListener('click',()=>{state.horizon=h.key;render();byId(`lm-horizon-${h.key}`)?.focus();});empty.append(jump);});
                container.append(empty); return;
            }
            rows.forEach(level=>{
                const a = distance(level.low), b = distance(level.high), midpoint=(a+b)/2;
                const range = Math.abs(level.high-level.low)>0.000001;
                const priceText=range ? `${money(level.low)}–${money(level.high)}` : money(level.low);
                const deltaText=range ? `${pct(a)} to ${pct(b)}` : pct(a);
                const row=make('div','lm-row');
                const name=make('div','lm-name-column');
                const label=make('div','lm-name',level.name);
                label.append(make('span','lm-tag',level.category));
                name.append(label);
                if (level.description) {
                    const short=wordBoundary(level.description);
                    const truncated=short.length<level.description.length;
                    const desc=make('p','lm-description',truncated?short+'…':short);
                    name.append(desc);
                    if (truncated) {
                        const toggle=make('button','lm-desc-toggle','View more');toggle.type='button';toggle.setAttribute('aria-expanded','false');
                        toggle.addEventListener('click',()=>{
                            const expanded=toggle.getAttribute('aria-expanded')!=='true';
                            toggle.setAttribute('aria-expanded',String(expanded));
                            desc.textContent=expanded?level.description:short+'…';toggle.textContent=expanded?'Show less':'View more';
                        });
                        name.append(toggle);
                    }
                }
                if (level.formula) name.append(make('span','lm-level-formula',`Formula: ${level.formula}`));
                const track=make('div','lm-plot');track.setAttribute('aria-label',`${deltaText} from reference close`);
                const bar=make('span','lm-bar');bar.dataset.group=level.category;
                bar.style.left=`${Math.min(zero,position(midpoint))}%`;
                bar.style.width=`${Math.max(.5,Math.abs(position(midpoint)-zero))}%`;
                track.append(bar);
                if (range) {
                    const zone=make('span','lm-zone');zone.style.left=`${position(Math.min(a,b))}%`;
                    zone.style.width=`${Math.max(.6,position(Math.max(a,b))-position(Math.min(a,b)))}%`;
                    track.append(zone);
                }
                const marker=make('span','lm-point');marker.style.left=`${position(midpoint)}%`;track.append(marker);
                row.append(name,track,make('strong','lm-price',priceText),make('strong','lm-distance',deltaText));
                container.append(row);
            });
        }
        horizons.forEach((h,i)=>{
            const tab=byId(`lm-horizon-${h.key}`);
            tab.addEventListener('click',()=>{state.horizon=h.key;render();});
            tab.addEventListener('keydown',event=>{
                let next=null;
                if (event.key==='ArrowRight') next=(i+1)%horizons.length;
                if (event.key==='ArrowLeft') next=(i+horizons.length-1)%horizons.length;
                if (event.key==='Home') next=0;
                if (event.key==='End') next=horizons.length-1;
                if (next!==null) {event.preventDefault();byId(`lm-horizon-${horizons[next].key}`).click();byId(`lm-horizon-${horizons[next].key}`).focus();}
            });
        });
        root.querySelectorAll('[data-category]').forEach(button=>button.addEventListener('click',()=>{state.category=button.dataset.category;render();}));
        render();
    }

    // The public chart is initialized by platform-beta.js. The Pine chart is
    // loaded lazily, only after a signed-in member selects Technical chart.
    function datePartsInNewYork(ms) {
        const formatter = new Intl.DateTimeFormat('en-CA', {
            timeZone: 'America/New_York', year: 'numeric', month: '2-digit', day: '2-digit'
        });
        const parts = {};
        for (const part of formatter.formatToParts(new Date(ms))) {
            if (part.type !== 'literal') parts[part.type] = part.value;
        }
        return `${parts.year}-${parts.month}-${parts.day}`;
    }

    function nyOpenEpoch(dateText) {
        const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(String(dateText || '').slice(0, 10));
        if (!match) return NaN;
        const target = Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3]), 9, 30);
        let guess = target;
        const formatter = new Intl.DateTimeFormat('en-US', {
            timeZone: 'America/New_York', year: 'numeric', month: '2-digit', day: '2-digit',
            hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23'
        });
        for (let i = 0; i < 4; i++) {
            const parts = {};
            for (const part of formatter.formatToParts(new Date(guess))) {
                if (part.type !== 'literal') parts[part.type] = part.value;
            }
            const actual = Date.UTC(Number(parts.year), Number(parts.month) - 1, Number(parts.day), Number(parts.hour), Number(parts.minute), Number(parts.second));
            const delta = target - actual;
            guess += delta;
            if (!delta) break;
        }
        return guess;
    }

    function epochToBeforeDate(value) {
        const n = Number(value);
        if (!Number.isFinite(n)) return null;
        return datePartsInNewYork(n < 1e12 ? n * 1000 : n);
    }

    async function fetchBars(range, timeframe='1D') {
        const url = new URL(barsUrl, window.location.origin);
        url.searchParams.set('timeframe', '1D');
        url.searchParams.set('limit', String(Math.min(Math.max((Number(range?.limit) || 360) * (timeframe==='1W'?5:1), 1), 1000)));
        const before = epochToBeforeDate(range?.to);
        if (before) {
            const day = new Date(`${before}T00:00:00Z`);
            day.setUTCDate(day.getUTCDate() + 1);
            url.searchParams.set('before', day.toISOString().slice(0, 10));
        }
        const response = await fetch(url, { headers: { Accept: 'application/json' }, credentials: 'same-origin' });
        if (!response.ok) throw new Error(`Market history request failed (${response.status}).`);
        const rows = await response.json();
        if (!Array.isArray(rows)) throw new Error('Market history response was not a JSON array.');
        const from = Number.isFinite(Number(range?.from)) ? Number(range.from) * (Number(range.from) < 1e12 ? 1000 : 1) : null;
        const to = Number.isFinite(Number(range?.to)) ? Number(range.to) * (Number(range.to) < 1e12 ? 1000 : 1) : null;
        const daily = rows.map(row => {
            if (!Array.isArray(row) || row.length < 5) return null;
            const time = nyOpenEpoch(row[0]);
            const open = Number(row[1]), high = Number(row[2]), low = Number(row[3]), close = Number(row[4]);
            const volume = Number(row[5]);
            if (![time, open, high, low, close].every(Number.isFinite)) return null;
            if (from !== null && time < from) return null;
            if (to !== null && time > to) return null;
            const bar = { time, open, high, low, close };
            if (Number.isFinite(volume)) bar.volume = volume;
            return bar;
        }).filter(Boolean).sort((a, b) => a.time - b.time);
        if (timeframe!=='1W') return daily;
        // The bars API stores only daily candles. Aggregate observed daily bars
        // locally into Monday-start exchange weeks for the prototype's 1W mode.
        const weeks = new Map();
        for (const bar of daily) {
            const day = new Date(bar.time);
            const start = Date.UTC(day.getUTCFullYear(),day.getUTCMonth(),day.getUTCDate() - ((day.getUTCDay()+6)%7));
            if (!weeks.has(start)) weeks.set(start,{...bar});
            else {
                const week = weeks.get(start);
                week.high = Math.max(week.high,bar.high);
                week.low = Math.min(week.low,bar.low);
                week.close = bar.close;
                week.volume = (week.volume||0)+(bar.volume||0);
            }
        }
        return [...weeks.values()].sort((a,b)=>a.time-b.time);
    }

    const provider = {
        async getBars(_ticker, timeframe, range) {
            return fetchBars(range || {}, timeframe==='1W'?'1W':'1D');
        },
        async listSymbols() {
            return [{ ticker: symbol, description: `${symbol} historical technical chart`, name: `${symbol} technical chart`, type: 'stock', prefix: root.dataset.market }];
        },
        async getSymbolInfo(ticker) {
            return {
                ticker, tickerid: `${root.dataset.market}:${ticker}`, prefix: root.dataset.market,
                description: `${symbol} historical technical chart`, name: `${symbol} technical chart`,
                type: 'stock', currency: 'USD', timezone: 'America/New_York', session: '0930-1600',
                mintick: 0.01, minTick: 0.01
            };
        }
    };

    async function loadPineSources() {
        if (pineSources) return pineSources;
        const response = await fetch(pineUrl, { headers: { Accept: 'application/json' }, credentials: 'same-origin' });
        if (response.status === 401) throw new Error('Sign in to load the technical chart.');
        if (!response.ok) throw new Error(`The technical chart is temporarily unavailable (${response.status}).`);
        pineSources = await response.json();
        if (!pineSources?.structureScript) throw new Error('The latest price structure analysis is unavailable.');
        return pineSources;
    }

    function keepLabelsOnTop(indicator) {
        if (!velaChart || !indicator?.id || !velaChart.renderer) return;
        try {
            const supported = typeof velaChart.renderer.supports !== 'function' || velaChart.renderer.supports('seriesOrder');
            if (supported && typeof velaChart.renderer.set === 'function')
                velaChart.renderer.set('seriesOrder', { id: indicator.id, to: 'front' });
        } catch (error) { console.warn('Unable to raise technical labels above structure.', error); }
    }

    function addPineIndicator(source, title, isLabel = false) {
        const indicator = velaChart.addIndicator(source, { language: 'pine', title, pane: 'price', overlay: true });
        indicator.on?.('ready', () => {
            if (isLabel) keepLabelsOnTop(indicator);
            if (typeof velaChart?.resize === 'function') velaChart.resize();
        });
        indicator.on?.('error', event => console.error(`${title} failed`, event));
        return indicator;
    }

    async function ensureTechnicalChart() {
        if (velaChart) return velaChart;
        if (chartPromise) return chartPromise;
        chartPromise = (async () => {
            if (!window.Vela?.Vela || !window.VelaPinets?.PineEngine) throw new Error('The technical chart library could not be loaded.');
            const scripts = await loadPineSources();
            technicalChartHost.replaceChildren();
            const chart = new window.Vela.Vela(`#technical-chart`, {
                symbol: `icpress:${symbol}`, timeframe: timeframeSelect?.value || '1D', bars: 360,
                theme: document.documentElement.classList.contains('dark-mode') ? 'dark' : 'light', live: false
            });
            chart.registerEngine('pine', new window.VelaPinets.PineEngine());
            velaChart = chart;
            structureIndicator = addPineIndicator(scripts.structureScript, `${symbol} Price structure`);
            chart.data.registerProvider('icpress', provider);
            await chart.data.ready();
            await chart.setMarket({ timeframe: timeframeSelect?.value || '1D', bars: 360 });
            await chart.ready();
            if (typeof chart.historyComplete === 'function') await chart.historyComplete();
            if (labelsVisible && scripts.labelScript) labelIndicator = addPineIndicator(scripts.labelScript, `${symbol} Price labels`, true);
            return chart;
        })();
        try {
            return await chartPromise;
        } catch (error) {
            chartPromise = null;
            velaChart = null;
            structureIndicator = null;
            throw error;
        }
    }

    async function setLabels(enabled) {
        labelsVisible = !!enabled;
        if (labelToggle) {
            labelToggle.setAttribute('aria-pressed', String(labelsVisible));
            labelToggle.classList.toggle('active', labelsVisible);
        }
        if (!velaChart) return;
        const scripts = await loadPineSources();
        if (labelsVisible && scripts.labelScript) {
            if (labelIndicator?.remove) labelIndicator.remove();
            labelIndicator = addPineIndicator(scripts.labelScript, `${symbol} Price labels`, true);
        } else if (labelIndicator?.remove) {
            labelIndicator.remove();
            labelIndicator = null;
        }
    }

    async function setTechnicalMode(enabled) {
        technicalMode = !!enabled;
        if (chartToggle) {
            chartToggle.setAttribute('aria-pressed', String(technicalMode));
            chartToggle.querySelector('span').textContent = technicalMode ? 'Live chart' : 'Technical chart';
        }
        if (liveChart) liveChart.hidden = technicalMode;
        if (technicalChartHost) technicalChartHost.hidden = !technicalMode;
        if (technicalGate) technicalGate.hidden = !technicalMode || authenticated;
        if (technicalControls) technicalControls.hidden = !technicalMode;
        if (!technicalMode || !authenticated) return;
        try {
            await ensureTechnicalChart();
            if (typeof velaChart?.resize === 'function') velaChart.resize();
        } catch (error) {
            technicalChartHost.replaceChildren(Object.assign(document.createElement('p'), {
                className: 'chart-fallback', textContent: error.message || 'The technical chart could not be loaded.'
            }));
            technicalChartHost.hidden = false;
        }
    }

    chartToggle?.addEventListener('click', () => setTechnicalMode(!technicalMode));
    labelToggle?.addEventListener('click', () => setLabels(!labelsVisible).catch(error => console.error(error)));
    timeframeSelect?.addEventListener('change', async () => {
        try {
            if (velaChart) await velaChart.setMarket({timeframe:timeframeSelect.value, bars:360});
        } catch (error) { console.error('Unable to change technical timeframe', error); }
    });
    if (labelToggle) labelToggle.setAttribute('aria-pressed', 'false');
    // Signed-in users with a saved Pine structure start on the technical chart.
    // Labels are deliberately left disabled until the user opts in.
    if (chartToggle && authenticated && technicalChartHost) void setTechnicalMode(true);

    const themeObserver = new MutationObserver(() => {
        if (velaChart && typeof velaChart.setTheme === 'function') {
            velaChart.setTheme(document.documentElement.classList.contains('dark-mode') ? 'dark' : 'light');
        }
    });
    themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });

    // The available-periods endpoint drives the financial year/quarter selectors.
    // Fundamentals are requested only when the account has beta access.
    if (root.dataset.betaEnabled === 'true') {
        const yearSelect = byId('financialYearSelect');
        const quarterSelect = byId('financialQuarterSelect');
        const output = byId('financialMetricsOutput');
        const periodsNode = byId('availableFinancialPeriods');
        const read = (obj, name) => obj?.[name] ?? obj?.[name.charAt(0).toUpperCase() + name.slice(1)];
        let availablePeriods = { years: [], quarters: [] };
        try { if (periodsNode?.textContent) availablePeriods = JSON.parse(periodsNode.textContent); } catch { /* Keep an empty selector if the payload is malformed. */ }

        function updateQuarterOptions() {
            if (!quarterSelect || !yearSelect) return;
            const group = (read(availablePeriods, 'quarters') || []).find(item => String(read(item, 'year')) === String(yearSelect.value));
            const available = read(group, 'availableQuarters') || [];
            quarterSelect.replaceChildren();
            const all = document.createElement('option'); all.value = ''; all.textContent = 'All periods'; quarterSelect.append(all);
            available.forEach(quarter => {
                const option = document.createElement('option'); option.value = String(quarter); option.textContent = `Q${quarter}`; quarterSelect.append(option);
            });
        }

        function appendCell(row, value, className) {
            const cell = document.createElement('td');
            if (className) cell.className = className;
            cell.textContent = value == null ? '—' : String(value);
            row.append(cell);
        }

        async function loadFinancialMetrics() {
            if (!output || !yearSelect) return;
            output.replaceChildren(Object.assign(document.createElement('p'), { textContent: 'Loading financial metrics…' }));
            const url = new URL(root.dataset.fundamentalsUrl, window.location.origin);
            if (yearSelect.value) url.searchParams.set('year', yearSelect.value);
            if (quarterSelect?.value) url.searchParams.set('quarter', quarterSelect.value);
            try {
                const response = await fetch(url, { headers: { Accept: 'application/json' }, credentials: 'same-origin' });
                if (!response.ok) throw new Error(`Financial metrics request failed (${response.status}).`);
                const payload = await response.json();
                const metrics = read(payload, 'rows') || [];
                output.replaceChildren();
                if (!metrics.length) {
                    output.append(Object.assign(document.createElement('p'), { className: 'financial-empty', textContent: `No financial metrics are available for ${yearSelect.value || 'the trailing 12 months'}.` }));
                    return;
                }
                const groups = new Map();
                for (const metric of metrics) {
                    const key = [metric.periodType,metric.fiscalYear,metric.fiscalQuarter,metric.periodEndDate,metric.accessionNumber].join('|');
                    if (!groups.has(key)) groups.set(key,[]);
                    groups.get(key).push(metric);
                }
                for (const rows of groups.values()) {
                    const first=rows[0];
                    const block = document.createElement('section'); block.className='financial-report-block';
                    const heading=document.createElement('h3');
                    heading.textContent=`${first.periodType} · FY${first.fiscalYear}${first.fiscalQuarter?' · Q'+first.fiscalQuarter:''} · ${first.periodEndDate}`;
                    block.append(heading);
                    const table = document.createElement('table');table.className='financial-metrics-table';
                    const head=document.createElement('thead'), headRow=document.createElement('tr');
                    ['Metric','Value','Unit','GAAP','Verified'].forEach(label=>{const th=document.createElement('th');th.textContent=label;headRow.append(th);});
                    head.append(headRow);table.append(head);
                    const body=document.createElement('tbody');
                    rows.forEach(metric=>{
                        const row=document.createElement('tr');
                        appendCell(row,metric.metricName||metric.metricKey);
                        const value=Number(metric.metricValue);
                        appendCell(row,Number.isFinite(value)?value.toLocaleString(undefined,{maximumFractionDigits:3}):metric.metricValue);
                        appendCell(row,metric.unitCode);
                        appendCell(row,metric.isGaap?'Yes':'No');
                        appendCell(row,metric.verified?'Yes':'No');
                        body.append(row);
                    });
                    table.append(body);block.append(table);output.append(block);
                }
            } catch (error) {
                output.replaceChildren(Object.assign(document.createElement('p'), { className: 'financial-empty', textContent: error.message || 'Financial metrics could not be loaded.' }));
            }
        }
        yearSelect?.addEventListener('change', () => { updateQuarterOptions(); loadFinancialMetrics(); });
        quarterSelect?.addEventListener('change', loadFinancialMetrics);
        updateQuarterOptions();
        loadFinancialMetrics();
    }

    // Joining is an explicit consent action; the button itself records acceptance.
    const waitlistForm = byId('betaWaitlistForm');
    if (waitlistForm) {
        const status = byId('betaWaitlistStatus');
        const buttons = [...root.querySelectorAll('.beta-join-button')];
        buttons.forEach(button => button.addEventListener('click', async () => {
            if (button.disabled) return;
            button.disabled = true;
            button.textContent = 'Joining…';
            if (status) status.textContent = '';
            try {
                const token = waitlistForm.querySelector('input[name="__RequestVerificationToken"]')?.value;
                const response = await fetch(waitlistForm.action, {
                    method: 'POST', credentials: 'same-origin',
                    headers: { 'RequestVerificationToken': token || '', 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' }
                });
                const payload = await response.json().catch(() => ({}));
                if (!response.ok) throw new Error(payload.message || 'The beta waitlist request could not be saved.');
                buttons.forEach(item => { item.textContent = 'Joined beta waitlist'; item.disabled = true; });
                if (status) status.textContent = payload.message || 'You have joined the beta waitlist.';
                root.querySelectorAll('.beta-gate-card').forEach(card => {
                    if (card.querySelector('.beta-join-button')) {
                        card.innerHTML = '<i class="fas fa-check-circle" aria-hidden="true"></i><h3>You’re on the beta waiting list</h3><p>We’ll contact you when beta access is available.</p>';
                    }
                });
            } catch (error) {
                button.disabled = false;
                button.textContent = 'Join beta waitlist';
                if (status) status.textContent = error.message || 'Please try again.';
            }
        }));
    }
})();
