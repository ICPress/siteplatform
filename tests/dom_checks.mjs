import { Window } from 'happy-dom';
import { fileURLToPath } from 'node:url';
import fs from 'node:fs';
import assert from 'node:assert/strict';
const base=fileURLToPath(new URL('../',import.meta.url));
const window=new Window({url:'https://example.test/',settings:{disableCSSFileLoading:true,disableJavaScriptFileLoading:true,enableJavaScriptEvaluation:true}});
const doc=window.document;
const wait=ms=>new Promise(resolve=>setTimeout(resolve,ms));
const checks=[];
const items=[{id:'1',symbol:'AVGO',name:'Broadcom Inc.',exchange:'NASDAQ',countryCode:'US',type:'stock',tradingViewSymbol:'NASDAQ:AVGO'}, {id:'2',symbol:'SAP',name:'SAP SE',exchange:'XETR',countryCode:'DE',type:'stock',tradingViewSymbol:'XETR:SAP'}];
let lastSearch='', aborts=0, overviewRequests=0;
window.fetch=async (url,options={})=>{
 const u=new URL(url,'https://example.test');
 if(u.pathname==='/instrument/search'){
  const q=u.searchParams.get('q');lastSearch=q;
  if(q==='slow') await wait(400);
  if(options.signal?.aborted){aborts++;throw new DOMException('Aborted','AbortError')}
  if(q==='fail')return new window.Response('',{status:503});
  return new window.Response(JSON.stringify(q==='zz'?[]:q==='xss'?[{...items[0],name:'<img src=x onerror=alert(1)>'}]:items),{headers:{'Content-Type':'application/json'}});
 }
 if(u.pathname==='/instrument/overview'){overviewRequests++;return new window.Response('',{status:503});}
 if(u.pathname==='/Account/session')return new window.Response(JSON.stringify({unreadNotifications:4,unreadMessages:7}),{headers:{'Content-Type':'application/json'}});
 throw new Error('Unexpected URL '+u);
};
doc.write('<html data-theme="light"><body>'+fs.readFileSync(base+'Views/Shared/_InstrumentSearch.cshtml','utf8')+fs.readFileSync(base+'Views/Shared/_MarketOverview.cshtml','utf8')+`<div id="userMenuContainer"><button id="userMenuButton" aria-expanded="false"><span id="userNavBadge"></span></button><div id="userDropdown"><span id="notificationBadge"></span><span id="messageBadge"></span><span id="account-count-status"></span></div></div><div id="instrument-chart" data-symbol="NASDAQ:AVGO"></div></body></html>`);
window.HTMLElement.prototype.scrollIntoView=function(){};
window.eval(fs.readFileSync(base+'wwwroot/assets/js/platform-beta.js','utf8'));
const input=doc.getElementById('instrument-query');
const results=doc.getElementById('instrument-results');
function search(value){input.value=value;input.dispatchEvent(new window.Event('input',{bubbles:true}));}
function key(value){input.dispatchEvent(new window.KeyboardEvent('keydown',{key:value,bubbles:true,cancelable:true}));}
await wait(20);
search('av');await wait(260);
assert.equal(results.querySelectorAll('[role=option]').length,2);
assert.equal(results.querySelector('.instrument-flag').textContent,'🇺🇸');
assert.equal(results.querySelectorAll('.instrument-flag')[1].textContent,'🇩🇪');checks.push('autocomplete with country flags');
key('ArrowDown');assert.equal(input.getAttribute('aria-activedescendant'),'instrument-option-0');key('ArrowUp');assert.equal(input.getAttribute('aria-activedescendant'),'instrument-option-1');checks.push('keyboard wrap-around and active descendant');
key('Escape');assert(results.hidden);assert.equal(input.getAttribute('aria-expanded'),'false');checks.push('escape closes results');
search('zz');await wait(260);assert(results.textContent.includes('No instruments found'));checks.push('empty results');
search('fail');await wait(260);assert(results.textContent.includes('Search unavailable'));checks.push('search failure');
search('xss');await wait(260);assert.equal(results.querySelectorAll('img').length,0);assert(results.textContent.includes('<img'));checks.push('result text escapes HTML');
search('slow');await wait(250);search('');await wait(450);assert(results.hidden);assert.equal(results.querySelectorAll('[role=option]').length,0);assert(aborts>0);checks.push('stale requests aborted and hidden');
search('T');await wait(260);assert.equal(lastSearch,'T');checks.push('single-letter ticker search');
key('ArrowDown');key('Enter');assert.equal(window.location.pathname,'/instrument/1');checks.push('search navigates by listing ID');
doc.getElementById('userMenuButton').click();await wait(20);assert.equal(doc.getElementById('userNavBadge').textContent,'11');assert.equal(doc.getElementById('notificationBadge').textContent,'4');assert.equal(doc.getElementById('messageBadge').textContent,'7');checks.push('account badges use API counts');
doc.dispatchEvent(new window.KeyboardEvent('keydown',{key:'Escape'}));assert.equal(doc.getElementById('userMenuButton').getAttribute('aria-expanded'),'false');checks.push('account menu escape');
const list=doc.getElementById('etf-list');
const rows=list.querySelectorAll('button.etf-item');assert.equal(rows.length,12);assert.equal(overviewRequests,0);
assert.equal(doc.getElementById('overview-symbol').textContent,'SPY');
assert.equal(JSON.parse(doc.getElementById('overview-chart').querySelector('script').textContent).symbol,'AMEX:SPY');
assert(!doc.getElementById('overview-status'));assert(!list.textContent.includes('Explore ETFs'));checks.push('prototype ETF list and default chart work without overview API or database');
assert(!rows[1].hasAttribute('href'));
const initial = doc.getElementById('overview-chart').querySelector('script');
const locationBefore = window.location.href;
rows[1].dispatchEvent(new window.Event('focus'));
rows[2].dispatchEvent(new window.Event('mouseenter'));
list.dispatchEvent(new window.Event('scroll'));
assert.equal(doc.getElementById('overview-chart').querySelector('script'),initial);checks.push('hover, focus and scrolling do not switch chart');
rows[1].click();assert.equal(window.location.href,locationBefore);assert.equal(doc.getElementById('overview-symbol').textContent,'QQQ');assert.equal(rows[1].getAttribute('aria-pressed'),'true');assert.equal(rows[0].getAttribute('aria-pressed'),'false');assert.equal(JSON.parse(doc.getElementById('overview-chart').querySelector('script').textContent).symbol,'NASDAQ:QQQ');checks.push('ETF button selects chart without navigation and updates accessible selection');
const selectedChart=doc.getElementById('overview-chart').querySelector('script');rows[1].click();assert.equal(doc.getElementById('overview-chart').querySelector('script'),selectedChart);checks.push('selecting same ETF does not reload widget');
const chart=doc.getElementById('instrument-chart');const config=JSON.parse(chart.querySelector('script').textContent);assert.equal(config.symbol,'NASDAQ:AVGO');assert.equal(config.allow_symbol_change,false);checks.push('explicit instrument TradingView mapping');
doc.documentElement.setAttribute('data-theme','dark');await wait(20);assert.equal(JSON.parse(chart.querySelector('script').textContent).theme,'dark');assert.equal(JSON.parse(doc.getElementById('overview-chart').querySelector('script').textContent).symbol,'NASDAQ:QQQ');checks.push('chart theme change preserves selected instrument and ETF');
chart.querySelector('script').onerror();assert(chart.textContent.includes('The chart could not load'));assert.equal(chart.querySelector('a'),null);checks.push('chart failure displays a message without extra provider link');
// Provider callback, fallback and pages without Google use the shipped sign-in script.
const authWindow = new Window({url:'https://example.test/Account/signin',settings:{disableJavaScriptFileLoading:true,enableJavaScriptEvaluation:true}});
const authDoc = authWindow.document;
authDoc.body.innerHTML = `<div id="google-signin" data-client-id="test-client"></div><p id="google-status"></p><form id="google-form" method="post" action="/Account/google"><input name="__RequestVerificationToken" value="csrf"><input id="google-credential" name="credential"><input name="returnUrl" value="/instrument/1"></form><a id="email-choice" href="/Account/signin?method=email">Continue with email</a>`;
Object.defineProperty(authDoc.getElementById('google-signin'),'clientWidth',{value:320});
let providerConfig, buttonConfig, submitted = false;
authWindow.google = {accounts:{id:{initialize:c=>providerConfig=c,renderButton:(_host,c)=>buttonConfig=c}}};
authDoc.getElementById('google-form').submit = () => { submitted = true; };
authWindow.eval(fs.readFileSync(base+'wwwroot/assets/js/signin.js','utf8'));
const providerScript = authDoc.head.querySelector('script');
assert.equal(providerScript.src,'https://accounts.google.com/gsi/client');
providerScript.onload();
assert.equal(providerConfig.client_id,'test-client');assert.equal(providerConfig.auto_select,false);assert.equal(buttonConfig.text,'continue_with');assert(authDoc.getElementById('google-status').hidden);checks.push('Google choice initializes without automatic sign-in');
providerConfig.callback({credential:'verified-by-api-later'});
assert(submitted);assert.equal(authDoc.getElementById('google-credential').value,'verified-by-api-later');assert.equal(authDoc.querySelector('[name=__RequestVerificationToken]').value,'csrf');checks.push('Google callback submits credential through antiforgery form');
authDoc.getElementById('google-status').hidden = false;
providerScript.onerror();assert(authDoc.getElementById('google-status').textContent.includes('Please use email'));assert(authDoc.getElementById('email-choice'));checks.push('Google load failure leaves email choice available');
authDoc.body.innerHTML = '<input type="email" name="email">';authDoc.head.innerHTML = '';
authWindow.eval(fs.readFileSync(base+'wwwroot/assets/js/signin.js','utf8'));assert.equal(authDoc.head.querySelectorAll('script').length,0);checks.push('email step does not load Google');
await authWindow.happyDOM.abort();authWindow.close();
// A browser/app handoff must preserve the homepage and never navigate desktop users.
const handoffScript = fs.readFileSync(base+'wwwroot/assets/js/android-handoff.js','utf8');
async function handoffCase(android, blocked = false) {
 const w = new Window({url:'https://icpress.org/',settings:{disableJavaScriptFileLoading:true,enableJavaScriptEvaluation:true}});
 Object.defineProperty(w.navigator,'userAgent',{value:android ? 'Mozilla/5.0 (Linux; Android 15)' : 'Desktop Browser'});
 const uri = 'intent://icpress.org/account/verify?header.payload.signature#Intent;scheme=https;package=net.crowdventures.storypop;S.browser_fallback_url=https%3A%2F%2Ficpress.org%2F;end';
 w.document.body.innerHTML = `<aside id="android-handoff" hidden><a id="android-handoff-link" href="${uri}">Open Android app</a><button id="android-handoff-dismiss">Dismiss</button></aside>`;
 const navigations = [];
 w.location.assign = url => { navigations.push(url); if (blocked) throw new Error('User gesture required'); };
 w.eval(handoffScript);
 return {w, navigations, uri};
}
let mobile = await handoffCase(true);
assert.deepEqual(mobile.navigations,[mobile.uri]);assert.equal(mobile.w.location.pathname,'/');
assert.equal(mobile.w.document.getElementById('android-handoff').hidden,false);
const intent = mobile.navigations[0];assert(intent.includes('/account/verify?header.payload.signature#Intent;'));assert(intent.includes('package=net.crowdventures.storypop;'));
assert.equal(decodeURIComponent(intent.match(/S.browser_fallback_url=([^;]+)/)[1]),'https://icpress.org/');checks.push('Android attempts existing intent binding once with token-free homepage fallback');
mobile.w.document.getElementById('android-handoff-dismiss').click();assert(mobile.w.document.getElementById('android-handoff').hidden);checks.push('Android handoff can be dismissed on homepage');
await mobile.w.happyDOM.abort();mobile.w.close();
let blocked = await handoffCase(true,true);assert.equal(blocked.navigations.length,1);assert.equal(blocked.w.document.getElementById('android-handoff-link').href,blocked.uri);assert.equal(blocked.w.document.getElementById('android-handoff').hidden,false);checks.push('blocked automatic app launch keeps a real tap-to-open link');await blocked.w.happyDOM.abort();blocked.w.close();
let desktop = await handoffCase(false);assert.equal(desktop.navigations.length,0);assert(desktop.w.document.getElementById('android-handoff').hidden);checks.push('desktop stays on homepage without app launch');await desktop.w.happyDOM.abort();desktop.w.close();
const report={status:'passed',scope:'DOM-level checks of shipped JavaScript with HTTP stubs; no native browser, Razor runtime, live API, database or external widgets.',checks};
fs.writeFileSync(new URL('./dom-results.json',import.meta.url),JSON.stringify(report,null,2));
console.log(JSON.stringify(report,null,2));await window.happyDOM.abort();window.close();
