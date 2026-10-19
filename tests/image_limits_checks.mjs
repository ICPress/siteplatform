import {Window} from 'happy-dom';
import { fileURLToPath } from 'node:url';
import fs from 'node:fs';
import assert from 'node:assert/strict';
const base=fileURLToPath(new URL('../',import.meta.url)),checks=[];
const wait=ms=>new Promise(r=>setTimeout(r,ms));
const source=fs.readFileSync(base+'wwwroot/assets/js/profile-images.js','utf8');
async function fixture(kind='avatar',width=3000,height=2000,onboarding=false) {
 const w=new Window({url:'https://example.test/profile/edit',settings:{disableJavaScriptFileLoading:true,enableJavaScriptEvaluation:true,suppressInsecureJavaScriptEnvironmentWarning:true}}),d=w.document;
 d.body.innerHTML=`<form ${onboarding?'data-onboarding-form':''}><input name="__RequestVerificationToken" value="csrf"><div data-profile-image="${kind}" data-prepare-url="${onboarding?'/Account/profile-image/prepare':`/profile/image/${kind}/prepare`}" data-complete-url="/profile/image/${kind}/complete"><input type="file"><img id="${kind}-preview" hidden><button type="button" data-upload-retry hidden>Retry</button><p class="upload-status"></p>${onboarding?'<input name="AvatarTicket"><button type="button" data-image-clear>Skip photo</button>':''}</div><button type="submit">Continue</button></form>`+fs.readFileSync(base+'Views/Shared/_ProfileCropper.cshtml','utf8');
 let selected,calls=[],draws=[],renders=[],fail='',prepareNumber=0;
 const input=d.querySelector('input[type=file]');Object.defineProperty(input,'files',{get:()=>selected?[selected]:[]});
 w.URL.createObjectURL=()=> 'blob:preview';w.URL.revokeObjectURL=()=>{};w.Image=class{naturalWidth=width;naturalHeight=height;async decode(){}};
 w.HTMLCanvasElement.prototype.getContext=function(){const el=this;return{fillRect(){},clearRect(){},drawImage(...args){draws.push({width:el.width,height:el.height,args:args.slice(1)});}}};
 w.HTMLCanvasElement.prototype.toBlob=function(fn){renders.push({width:this.width,height:this.height});const blob=new w.Blob(['jpeg'],{type:'image/jpeg'});blob.dimensions={width:this.width,height:this.height};fn(blob);};
 const canvas=d.getElementById('profile-crop-canvas');canvas.getBoundingClientRect=()=>({width:400,height:300});canvas.setPointerCapture=()=>{};
 const dialog=d.getElementById('profile-crop-dialog');dialog.showModal=()=>dialog.open=true;dialog.close=()=>dialog.open=false;
 w.fetch=async(url,options)=>{
  calls.push({url,options});
  if(options.method==='PUT')return new w.Response('',{status:(fail==='small'&&url.includes('/small/'))||(fail==='large'&&url.includes('/large/'))?503:201});
  if(url.endsWith('/prepare')){
   prepareNumber++;const width=Number(options.body.get('width')),height=Number(options.body.get('height'));
   return new w.Response(JSON.stringify({uploadUrl:`https://swift.test/large/p${prepareNumber}.jpg`,previewUploadUrl:width>576||height>576?`https://swift.test/small/p${prepareNumber}.jpg`:null,authToken:'storage-token',ticket:`receipt-${prepareNumber}`}),{status:fail==='route'?502:fail==='session'?401:200,headers:{'Content-Type':'application/json'}});
  }
  return new w.Response(JSON.stringify({url:'https://cdn.test/profile.jpg'}),{headers:{'Content-Type':'application/json'}});
 };
 w.eval(source);
 const choose=async(type='image/jpeg')=>{selected=new w.File(['input'],'photo',{type});input.dispatchEvent(new w.Event('change'));await wait(5);};
 const apply=async()=>{d.getElementById('crop-apply').click();await wait(20);};
 return {w,d,choose,apply,calls,draws,renders,dialog,setFail:value=>fail=value,close:async()=>{await w.happyDOM.abort();w.close();}};
}
for(const [kind,w,h,large,small] of [
 ['avatar',3000,2000,[1024,1024],[576,576]],
 ['avatar',700,700,[700,700],[576,576]],
 ['avatar',576,576,[576,576],null],
 ['avatar',577,577,[577,577],[576,576]],
 ['avatar',200,100,[100,100],null],
 ['avatar',2000,3000,[1024,1024],[576,576]],
 ['cover',3000,2000,[1024,320],[576,180]],
 ['cover',500,300,[500,156],null],
 ['cover',600,1200,[600,188],[576,180]],
 ['cover',1600,500,[1024,320],[576,180]]]) {
 const f=await fixture(kind,w,h);await f.choose();assert(f.dialog.open);assert.equal(f.calls.length,0);await f.apply();
 assert.deepEqual(f.renders.map(x=>[x.width,x.height]),small?[large,small]:[large]);
 assert.equal(Number(f.calls[0].options.body.get('width')),large[0]);assert.equal(Number(f.calls[0].options.body.get('height')),large[1]);
 assert.equal(f.calls[0].options.body.get('__RequestVerificationToken'),'csrf');
 const puts=f.calls.filter(x=>x.options.method==='PUT');assert.equal(puts.length,small?2:1);assert(puts[0].url.includes('/large/'));if(small){assert(puts[1].url.includes('/small/'));assert.equal(puts[0].url.split('/').at(-1),puts[1].url.split('/').at(-1));}
 for(const request of puts){assert.equal(request.options.credentials,'omit');assert.equal(request.options.headers['X-Auth-Token'],'storage-token');assert.equal(request.options.headers['Content-Type'],'image/jpeg');assert.equal(request.options.body.type,'image/jpeg');assert(!('Authorization' in request.options.headers));}
 assert(f.calls.at(-1).url.endsWith('/complete'));assert.equal(f.calls.at(-1).options.body.get('ticket'),'receipt-1');
 checks.push(`${kind} ${w}×${h}: large ${large.join('×')}, ${small?'preview '+small.join('×'):'no preview or upscaling'}`);await f.close();
}
const position=await fixture('avatar',1200,800);await position.choose();const zoom=position.d.getElementById('crop-zoom');zoom.value='2';zoom.dispatchEvent(new position.w.Event('input'));
const canvas=position.d.getElementById('profile-crop-canvas');canvas.dispatchEvent(new position.w.KeyboardEvent('keydown',{key:'ArrowRight'}));canvas.dispatchEvent(new position.w.PointerEvent('pointerdown',{pointerId:1,clientX:100,clientY:100}));canvas.dispatchEvent(new position.w.PointerEvent('pointermove',{pointerId:1,clientX:110,clientY:105}));await position.apply();
assert.deepEqual(position.draws.find(x=>x.args.length===8).args,[370,190,400,400,0,0,400,400]);checks.push('circle-focused crop follows zoom, keyboard and pointer movement before downsampling');await position.close();
for(const fail of ['large','small']) {
 const f=await fixture();f.setFail(fail);await f.choose();await f.apply();assert(!f.calls.some(x=>x.url.endsWith('/complete')));assert(!f.d.querySelector('[data-upload-retry]').hidden);assert(f.d.querySelector('.upload-status').textContent.includes('could not be saved'));
 f.setFail('');f.d.querySelector('[data-upload-retry]').click();await wait(20);assert.equal(f.calls.at(-1).options.body.get('ticket'),'receipt-2');assert(f.d.querySelector('.upload-status').textContent.includes('Photo saved'));checks.push(`${fail} PUT failure prevents metadata commit; retry uploads both with a fresh receipt`);await f.close();
}
const n=await fixture('avatar',3000,2000,true);n.setFail('small');await n.choose();assert(n.d.querySelector('button[type=submit]').disabled);await n.apply();let submit=new n.w.Event('submit',{cancelable:true});n.d.querySelector('form').dispatchEvent(submit);assert(submit.defaultPrevented);assert.equal(n.d.querySelector('[name=AvatarTicket]').value,'');n.setFail('');n.d.querySelector('[data-upload-retry]').click();await wait(20);assert.equal(n.d.querySelector('[name=AvatarTicket]').value,'receipt-2');assert(!n.calls.some(x=>x.url.endsWith('/complete')));checks.push('registration receives a receipt only after both uploads, then submits it with account creation');n.d.querySelector('[data-image-clear]').click();submit=new n.w.Event('submit',{cancelable:true});n.d.querySelector('form').dispatchEvent(submit);assert(!submit.defaultPrevented);assert.equal(n.d.querySelector('[name=AvatarTicket]').value,'');checks.push('registration can continue without a photo after upload failure or removal');await n.close();
for(const fail of ['route','session']){const f=await fixture();f.setFail(fail);await f.choose();await f.apply();assert.equal(f.calls.length,1);assert(!f.d.querySelector('[data-upload-retry]').hidden);assert(f.d.querySelector('.upload-status').textContent.includes(fail==='route'?'temporarily unavailable':'sign-in has expired'));checks.push(`${fail} failure stops before Swift PUT and shows actionable status`);await f.close();}
const cancel=await fixture();await cancel.choose('image/svg+xml');assert.equal(cancel.calls.length,0);assert(!cancel.dialog.open);await cancel.choose();cancel.d.getElementById('crop-cancel').click();assert(!cancel.dialog.open);assert.equal(cancel.calls.length,0);checks.push('unsupported file and canceled crop never fetch credentials or upload');await cancel.close();
const controller=fs.readFileSync(base+'Controllers/CommunityController.cs','utf8');const view=fs.readFileSync(base+'Views/Community/Edit.cshtml','utf8');
for(const route of ['ProfileImagePrepare','ProfileImageComplete']){assert(controller.includes(`Name="${route}"`));assert(view.includes(`Url.RouteUrl("${route}", new { kind })`));}
assert(controller.includes('/profile/image/{kind}/prepare'));assert(controller.includes('"Community/profile/image/"+kind+"/prepare",new {width,height},Token'));assert(fs.readFileSync(base+'Program.cs','utf8').includes('app.MapControllers();'));assert(controller.includes('StatusCode(502'));checks.push('view uses named prepare/complete attribute routes; upstream missing API is distinct from website 404');
const report={status:'passed',scope:'JavaScript DOM/canvas/network stubs and route source contracts; not live Swift or compiled ASP.NET route tests.',checks};fs.writeFileSync(new URL('./image-limits-results.json',import.meta.url),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));
