import { Window } from 'happy-dom';
import { fileURLToPath } from 'node:url';
import fs from 'node:fs';
import assert from 'node:assert/strict';
const base=fileURLToPath(new URL('../',import.meta.url));
const source=fs.readFileSync(base+'wwwroot/assets/js/community.js','utf8');
const checks=[];const wait=ms=>new Promise(r=>setTimeout(r,ms));
function windowFor(path='/notifications'){return new Window({url:'https://example.test'+path,settings:{disableJavaScriptFileLoading:true,enableJavaScriptEvaluation:true,suppressInsecureJavaScriptEnvironmentWarning:true}});}
async function close(w){await w.happyDOM.abort();w.close();}
const w=windowFor(),d=w.document;
d.body.innerHTML='<article data-read-item="12" class="unread"><span class="unread-dot"></span></article><article data-read-item="13" class="unread"></article><article data-read-item="99"></article><form data-mark-read action="/notifications/read"><input name="__RequestVerificationToken" value="csrf"><input name="ids" value="12"><input name="ids" value="13"></form><p class="read-status"></p>';
let observe,observed=[],requests=[],bad=false,events=0;
w.IntersectionObserver=class{constructor(fn){observe=fn;}observe(el){observed.push(el);}unobserve(){}};
w.fetch=async(url,options)=>{requests.push({url,ids:options.body.getAll('ids'),csrf:options.body.get('__RequestVerificationToken'),method:options.method,headers:options.headers});return new w.Response('',{status:bad?503:204});};
d.addEventListener('account-counts-changed',()=>events++);w.eval(source);await wait(180);assert.equal(requests.length,0);checks.push('read state stays untouched before items enter the viewport');
observe([{isIntersecting:true,target:observed[0]},{isIntersecting:true,target:observed[2]}]);await wait(190);
assert.deepEqual(requests[0].ids,['12']);assert.equal(requests[0].csrf,'csrf');assert.equal(requests[0].method,'POST');assert.equal(requests[0].headers['X-Requested-With'],'XMLHttpRequest');assert.equal(events,1);assert(!observed[0].classList.contains('unread'));assert(observed[1].classList.contains('unread'));checks.push('only visible eligible IDs are posted with antiforgery token and counts refreshed');
bad=true;observe([{isIntersecting:true,target:observed[1]}]);await wait(190);assert(observed[1].classList.contains('unread'));assert.equal(events,1);assert(d.querySelector('.read-status button'));checks.push('failed read update retains unread status and offers retry');
bad=false;d.querySelector('.read-status button').click();await wait(30);assert.equal(events,2);assert(!observed[1].classList.contains('unread'));checks.push('read update retry succeeds without losing pending IDs');await close(w);
const email=windowFor('/Account/email-verification?authToken=secret');email.document.body.innerHTML='<form id="email-verification-form" method="post" action="/Account/verify"><input name="authToken" value="secret"><input name="__RequestVerificationToken" value="csrf"></form>';
let submitted=0;email.document.querySelector('form').requestSubmit=()=>submitted++;email.eval(fs.readFileSync(base+'wwwroot/assets/js/email-verification.js','utf8'));
assert.equal(submitted,1);assert.equal(email.location.search,'');assert.equal(email.document.querySelector('[name=authToken]').value,'secret');checks.push('email landing auto-submits the proof once and removes it from browser URL');await close(email);
const mobile=windowFor();mobile.document.body.innerHTML='<button id="mobileMenuToggle" aria-expanded="true"></button><nav id="mainNav" class="active"></nav>';mobile.eval(fs.readFileSync(base+'wwwroot/assets/js/platform-beta.js','utf8'));
mobile.document.dispatchEvent(new mobile.KeyboardEvent('keydown',{key:'Escape'}));assert.equal(mobile.document.getElementById('mobileMenuToggle').getAttribute('aria-expanded'),'false');assert(!mobile.document.getElementById('mainNav').classList.contains('active'));checks.push('mobile visitor menu closes on Escape and updates expanded state');await close(mobile);
const report={status:'passed',scope:'DOM-level script checks with stubbed network, canvas and viewport; not native visual or server integration tests.',checks};fs.writeFileSync(new URL('./community-dom-results.json',import.meta.url),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));
