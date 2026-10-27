(function(){
"use strict";
const $=s=>document.querySelector(s);
const esc=s=>String(s??"").replace(/[&<>"']/g,c=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c]));
const z=n=>String(n).padStart(2,"0");
const ymd=d=>d.getFullYear()+"-"+z(d.getMonth()+1)+"-"+z(d.getDate());
const parse=s=>{const[y,m,d]=s.split("-").map(Number);return new Date(y,m-1,d)};
const hoy=()=>ymd(new Date());
const DIAS=["L","M","X","J","V","S","D"];
const DIAS_L=["lunes","martes","miércoles","jueves","viernes","sábado","domingo"];
const MES=["ene","feb","mar","abr","may","jun","jul","ago","sep","oct","nov","dic"];
const dow=d=>(d.getDay()+6)%7;
const lunes=d=>{const x=new Date(d);x.setHours(0,0,0,0);x.setDate(x.getDate()-dow(x));return x};
const corta=s=>{const d=parse(s);return d.getDate()+" "+MES[d.getMonth()]};
const larga=s=>{const d=parse(s);return DIAS_L[dow(d)]+" "+d.getDate()+" "+MES[d.getMonth()]};
const n1=(n,dec=1)=>Number(n).toLocaleString("es",{maximumFractionDigits:dec});
const toNum=v=>{const x=parseFloat(String(v??"").replace(",","."));return isFinite(x)?x:NaN};
const uid=()=>{try{return crypto.randomUUID()}catch(e){return"10000000-1000-4000-8000-100000000000".replace(/[018]/g,c=>(c^Math.random()*16>>c/4).toString(16))}};
const e1rm=(p,r)=>r<=1?p:p*(1+r/30);

/* ---------- Almacenamiento: Supabase (cuenta de cada usuario) + copia local para usarla sin señal ---------- */
const CFG=window.DIARIO_GYM_CONFIG||{};
const configurado=!!(CFG.supabaseUrl&&CFG.supabaseAnonKey&&!/TU-PROYECTO|TU-ANON-KEY/.test(CFG.supabaseUrl+CFG.supabaseAnonKey));
const sb=configurado&&window.supabase?window.supabase.createClient(CFG.supabaseUrl,CFG.supabaseAnonKey):null;
const COLS=["asistencias","rutinas","registros","peso","ajustes"];
const S={mode:null,user:null,data:{},cola:[],subiendo:false,auth:{vista:"entrar",msg:"",err:""}};
const vaciar=()=>COLS.forEach(c=>S.data[c]={});vaciar();
// Cada colección de la app se guarda en una tabla de supabase/schema.sql
const T={
  asistencias:{tabla:"asistencias",clave:"user_id,fecha",col:"fecha",
    fila:(id,o)=>({fecha:id,rutina_id:o.rutinaId||null,ts:o.ts||null}),
    doc:r=>[r.fecha,{fecha:r.fecha,rutinaId:r.rutina_id,ts:Number(r.ts)||0}]},
  rutinas:{tabla:"rutinas",clave:"id",col:"id",
    fila:(id,o)=>({id,nombre:o.nombre,dias:o.dias||[],ejercicios:o.ejercicios||[],orden:o.orden||0}),
    doc:r=>[r.id,{nombre:r.nombre,dias:r.dias||[],ejercicios:r.ejercicios||[],orden:r.orden}]},
  registros:{tabla:"registros",clave:"id",col:"id",
    fila:(id,o)=>({id,fecha:o.fecha,ejercicio:o.ejercicio,peso:o.peso,reps:o.reps,ts:o.ts||null,rutina_id:o.rutinaId||null}),
    doc:r=>[r.id,{fecha:r.fecha,ejercicio:r.ejercicio,peso:Number(r.peso),reps:r.reps,ts:Number(r.ts)||0,rutinaId:r.rutina_id}]},
  peso:{tabla:"pesos",clave:"user_id,fecha",col:"fecha",
    fila:(id,o)=>({fecha:id,kg:o.kg}),
    doc:r=>[r.fecha,{fecha:r.fecha,kg:Number(r.kg)}]},
  ajustes:{tabla:"perfiles",clave:"user_id",col:null,
    fila:(id,o)=>({meta_semanal:o.metaSemanal||4,peso_meta:o.pesoMeta??null,altura:o.altura??null,sexo:o.sexo||null,nacimiento:o.nacimiento||null,actualizado:new Date().toISOString()}),
    doc:r=>["general",{metaSemanal:r.meta_semanal,pesoMeta:r.peso_meta==null?null:Number(r.peso_meta),altura:r.altura==null?null:Number(r.altura),sexo:r.sexo,nacimiento:r.nacimiento}]}
};
const lsKey=()=>"diario-gym-pwa:"+S.user.id;
function lsSave(){try{localStorage.setItem(lsKey(),JSON.stringify({data:S.data,cola:S.cola}))}catch(e){}}
function lsLoad(){try{const j=JSON.parse(localStorage.getItem(lsKey())||"null");if(j){COLS.forEach(c=>S.data[c]=(j.data&&j.data[c])||{});S.cola=j.cola||[]}}catch(e){}}
function aplicar(op){const o={...S.data[op.col]};if(op.t==="put")o[op.id]=op.obj;else delete o[op.id];S.data[op.col]=o}
const esRed=e=>!navigator.onLine||!e||e.status===0||/fetch|network|load failed/i.test(e.message||"");
async function ejecutar(op){
  const t=T[op.col];
  if(op.t==="put"){
    const fila={...t.fila(op.id,op.obj),user_id:S.user.id};
    let r=await sb.from(t.tabla).upsert(fila,{onConflict:t.clave});
    // La rutina referida se borró en otro dispositivo: guardar sin rutina
    if(r.error&&r.error.code==="23503"&&fila.rutina_id){fila.rutina_id=null;r=await sb.from(t.tabla).upsert(fila,{onConflict:t.clave})}
    return r;
  }
  return sb.from(t.tabla).delete().eq("user_id",S.user.id).eq(t.col,op.id);
}
async function subir(){
  if(S.subiendo||!S.user)return;S.subiendo=true;
  try{
    while(S.cola.length){
      const r=await ejecutar(S.cola[0]);
      if(r.error){if(esRed(r.error))break;console.error(r.error);toast("Un cambio no se pudo guardar: "+(r.error.message||"error"))}
      S.cola.shift();lsSave();
    }
  }catch(e){}finally{S.subiendo=false;schedule()}
}
async function leerTodo(){
  const o={};
  for(const c of COLS){
    o[c]={};
    for(let i=0;;i+=1000){
      const {data,error}=await sb.from(T[c].tabla).select("*").range(i,i+999);
      if(error)throw error;
      data.forEach(r=>{const[id,d]=T[c].doc(r);o[c][id]=d});
      if(data.length<1000)break;
    }
  }
  return o;
}
let sincronizando=false;
async function sincronizar(){
  if(!S.user||!navigator.onLine||sincronizando)return;
  sincronizando=true;const u=S.user;
  await subir();
  try{if(S.user!==u)return;const o=await leerTodo();if(S.user!==u)return;COLS.forEach(c=>S.data[c]=o[c]);S.cola.forEach(aplicar);lsSave();S.mode="nube"}
  catch(e){if(!esRed(e))toast("No se pudieron leer tus datos. Inténtalo más tarde.")}
  finally{sincronizando=false}
  schedule();
}
async function iniciarSesion(user){
  if(S.user&&S.user.id===user.id)return;
  S.user=user;vaciar();S.cola=[];lsLoad();S.mode="local";E=null;confirmar=null;schedule();
  sincronizar();
}
function cerrarSesionLocal(){S.user=null;S.mode="auth";vaciar();S.cola=[];schedule()}
function initStore(){
  if(!sb){S.mode="config";schedule();return}
  sb.auth.onAuthStateChange((ev,ses)=>{
    if(ev==="PASSWORD_RECOVERY"){S.auth={vista:"nueva",msg:"",err:""}}
    if(ses&&ses.user){setTimeout(()=>iniciarSesion(ses.user),0)}else if(ev!=="TOKEN_REFRESHED")cerrarSesionLocal();
    schedule();
  });
  addEventListener("online",sincronizar);addEventListener("offline",schedule);
  document.addEventListener("visibilitychange",()=>{if(document.visibilityState==="visible")sincronizar()});
}
function put(col,id,obj){
  id=id||uid();const op={t:"put",col,id,obj};aplicar(op);S.cola.push(op);lsSave();schedule();subir();
  return id;
}
function del(col,id){
  const op={t:"del",col,id};aplicar(op);S.cola.push(op);lsSave();schedule();subir();
}
const aj=()=>S.data.ajustes.general||{};
const metaSem=()=>aj().metaSemanal||4;
function setAj(p){put("ajustes","general",{...aj(),...p})}
const fechaEnt=()=>{const f=D.fEnt;return f&&/^\d{4}-\d{2}-\d{2}$/.test(f)&&f<=hoy()?f:hoy()};
/* Composición corporal: IMC y % de grasa estimado (Deurenberg 1991) */
function edadEn(f){const n=aj().nacimiento;if(!n)return null;const a=parse(n),d=parse(f);let e=d.getFullYear()-a.getFullYear();if(d.getMonth()<a.getMonth()||(d.getMonth()===a.getMonth()&&d.getDate()<a.getDate()))e--;return e>0?e:null}
function imcDe(kg){const h=aj().altura;return h?kg/((h/100)**2):null}
function grasaDe(kg,f){const imc=imcDe(kg),e=edadEn(f),sx=aj().sexo;if(imc==null||e==null||!sx)return null;return 1.2*imc+0.23*e-10.8*(sx==="h"?1:0)-5.4}
function catImc(i){return i<18.5?"bajo peso":i<25?"normal":i<30?"sobrepeso":"obesidad"}

/* ---------- Estado de UI ---------- */
const D={};           // borradores de inputs
let E=null;           // rutina en edición
let confirmar=null;   // id pendiente de confirmar borrado
const TABS={hoy:"Hoy",rutinas:"Rutinas",evolucion:"Evolución",peso:"Peso"};
let tab=(location.hash||"").slice(1);if(!TABS[tab])tab="hoy";
const val=(k,def)=>D[k]!==undefined?D[k]:(def??"");

let pend=false;
function schedule(){if(pend)return;pend=true;requestAnimationFrame(()=>{pend=false;render()})}
function render(){
  if(!S.mode)return;
  if(arr){arr.pendiente=true;return}
  const a=document.activeElement,fid=a&&a.id;let sel=null;try{sel=a.selectionStart}catch(e){}
  const dentro=!!S.user&&S.auth.vista!=="nueva";
  $("#tabs").hidden=!dentro;
  $("#titulo").textContent=dentro?TABS[tab]:"Bienvenido";
  $("#modo").innerHTML=dentro?vEstado():(instalar?'<button class="link" data-act="instalar">Instalar app</button>':"");
  document.querySelectorAll("[data-tab]").forEach(b=>{b.classList.toggle("on",b.dataset.tab===tab);b.setAttribute("aria-current",b.dataset.tab===tab?"page":"false")});
  $("#view").innerHTML=dentro?({hoy:vHoy,rutinas:vRutinas,evolucion:vEvol,peso:vPeso})[tab]():vAuth();
  if(fid){const n=document.getElementById(fid);if(n){n.focus({preventScroll:true});if(sel!=null)try{n.setSelectionRange(sel,sel)}catch(e){}}}
}
function vEstado(){
  const n=S.cola.length,ok=S.mode==="nube"&&!n&&navigator.onLine;
  const pend=`${n} ${n===1?"cambio":"cambios"} por subir`;
  const txt=ok?"Sincronizado":!navigator.onLine?(n?"Sin conexión · "+pend:"Sin conexión"):n?"Subiendo "+pend+"…":"Sincronizando…";
  return `<span><i class="sync-dot ${ok?"":"off"}"></i>${txt}</span>
    ${instalar?'<button class="link" data-act="instalar">Instalar app</button>':""}
    <button class="link" data-act="salir">Salir · ${esc(S.user.email||"")}</button>`;
}

/* ---------- Vista: Cuenta (entrar, crear cuenta, recuperar contraseña) ---------- */
function vAuth(){
  if(S.mode==="config")return `<section class="card auth"><h2>Falta conectar Supabase</h2>
    <p class="muted" style="margin:0">Edita <b>config.js</b> con la URL y la anon key de tu proyecto de Supabase y vuelve a publicar.</p></section>`;
  const v=S.auth.vista,m=S.auth.msg?`<p class="ok">${esc(S.auth.msg)}</p>`:"",e=S.auth.err?`<p class="err">${esc(S.auth.err)}</p>`:"";
  const email=`<label class="f">Correo<input id="aEmail" data-d="aEmail" type="email" autocomplete="email" inputmode="email" required value="${esc(val("aEmail"))}"></label>`;
  const clave=(nueva)=>`<label class="f">${nueva?"Contraseña nueva (mínimo 6 caracteres)":"Contraseña"}<input id="aClave" data-d="aClave" type="password" minlength="6" required autocomplete="${nueva?"new-password":"current-password"}" value="${esc(val("aClave"))}"></label>`;
  const enviando=S.auth.enviando?" disabled":"";
  if(v==="nueva")return `<section class="card auth"><h2>Nueva contraseña</h2><form data-form="nueva">${clave(true)}${e}${m}
    <button class="btn block"${enviando}>Guardar contraseña</button></form></section>`;
  if(v==="recuperar")return `<section class="card auth"><h2>Recuperar contraseña</h2>
    <p class="muted" style="margin:0">Te enviaremos un enlace a tu correo para elegir una contraseña nueva.</p>
    <form data-form="recuperar">${email}${e}${m}<button class="btn block"${enviando}>Enviar enlace</button></form>
    <button class="link" data-act="auth" data-v="entrar">Volver</button></section>`;
  const reg=v==="registro";
  return `<section class="card auth"><h2>${reg?"Crear cuenta":"Entrar"}</h2>
    <p class="muted" style="margin:0">Tus asistencias, rutinas, series y peso se guardan en tu cuenta y solo tú puedes verlos.</p>
    <form data-form="${reg?"registro":"entrar"}">${email}${clave(reg)}${e}${m}
    <button class="btn block"${enviando}>${reg?"Crear cuenta":"Entrar"}</button></form>
    ${reg?'<button class="link" data-act="auth" data-v="entrar">Ya tengo cuenta</button>'
      :'<div class="row between"><button class="link" data-act="auth" data-v="registro">Crear cuenta nueva</button><button class="link" data-act="auth" data-v="recuperar">Olvidé mi contraseña</button></div>'}
  </section>${tipInstalar()}`;
}
const urlApp=()=>location.origin+location.pathname;
function traducir(err){
  const m=(err&&err.message)||"";
  if(/invalid login credentials/i.test(m))return "Correo o contraseña incorrectos.";
  if(/email not confirmed/i.test(m))return "Primero confirma tu correo con el enlace que te enviamos.";
  if(/already registered|already been registered/i.test(m))return "Ese correo ya tiene cuenta. Entra o recupera tu contraseña.";
  if(/at least 6|password should be/i.test(m))return "La contraseña debe tener al menos 6 caracteres.";
  if(/rate limit|too many/i.test(m))return "Demasiados intentos. Espera unos minutos.";
  if(esRed(err))return "Sin conexión. Revisa tu internet.";
  return m||"Algo salió mal. Inténtalo de nuevo.";
}
async function enviarAuth(form){
  const email=(D.aEmail||"").trim(),clave=D.aClave||"";
  S.auth={...S.auth,err:"",msg:"",enviando:true};render();
  let r,msg="";
  if(form==="entrar")r=await sb.auth.signInWithPassword({email,password:clave});
  else if(form==="registro"){r=await sb.auth.signUp({email,password:clave,options:{emailRedirectTo:urlApp()}});
    if(!r.error&&!r.data.session)msg="Listo. Revisa tu correo y toca el enlace para confirmar tu cuenta.";}
  else if(form==="recuperar"){r=await sb.auth.resetPasswordForEmail(email,{redirectTo:urlApp()});if(!r.error)msg="Te enviamos el enlace. Revisa tu correo (y la carpeta de spam).";}
  else if(form==="nueva"){r=await sb.auth.updateUser({password:clave});if(!r.error){S.auth={vista:"entrar",msg:"",err:""};D.aClave="";toast("Contraseña actualizada");render();return}}
  S.auth={...S.auth,enviando:false,err:r.error?traducir(r.error):"",msg};
  if(!r.error)D.aClave="";
  render();
}
document.addEventListener("submit",ev=>{const f=ev.target.closest("[data-form]");if(!f)return;ev.preventDefault();enviarAuth(f.dataset.form)});

/* ---------- Instalación en el teléfono ---------- */
let instalar=null;
addEventListener("beforeinstallprompt",ev=>{ev.preventDefault();instalar=ev;schedule()});
addEventListener("appinstalled",()=>{instalar=null;schedule()});
const standalone=()=>matchMedia("(display-mode: standalone)").matches||navigator.standalone===true;
const esIOS=()=>/iphone|ipad|ipod/i.test(navigator.userAgent)||(navigator.platform==="MacIntel"&&navigator.maxTouchPoints>1);
function tipInstalar(){
  let oculto=false;try{oculto=localStorage.getItem("diario-gym-tip")==="1"}catch(e){}
  if(standalone()||oculto||!esIOS())return "";
  return `<section class="card tip"><b>Instálala en tu iPhone</b>
    <p style="margin:0">En Safari toca <b>Compartir</b> (el cuadrado con la flecha) y luego <b>Añadir a pantalla de inicio</b>.</p>
    <button class="link" data-act="tip-ok" style="align-self:flex-start">Entendido</button></section>`;
}
if("serviceWorker" in navigator&&location.protocol==="https:")navigator.serviceWorker.register("sw.js").catch(()=>{});
/* ---------- Ordenar arrastrando (rutinas y ejercicios de una rutina) ---------- */
// El bloque sigue al dedo saltando de hueco en hueco; al soltar se guarda el nuevo orden.
let arr=null;
const itemsDe=lista=>[...lista.children].filter(n=>n.dataset.item!==undefined);
const ordenDe=lista=>itemsDe(lista).map(n=>n.dataset.item);
function moverSegunY(y){
  const {item,lista}=arr;
  for(const o of itemsDe(lista)){
    if(o===item)continue;
    const r=o.getBoundingClientRect(),mid=r.top+r.height/2,antes=item.compareDocumentPosition(o)&Node.DOCUMENT_POSITION_PRECEDING;
    if(antes&&y<mid){lista.insertBefore(item,o);return}
    if(!antes&&y>mid){lista.insertBefore(item,o.nextSibling);return}
  }
}
function autoScroll(){
  if(!arr)return;
  const m=90,y=arr.y,abajo=innerHeight-m-60,v=y<m?-(m-y)/5:y>abajo?(y-abajo)/5:0;
  if(v){scrollBy(0,v);moverSegunY(y)}
  arr.raf=requestAnimationFrame(autoScroll);
}
function aplicarOrden(lista,orden){
  if(lista.dataset.lista==="ejercicios"&&E)E.ejercicios=orden.map(i=>E.ejercicios[Number(i)]);
  else if(lista.dataset.lista==="rutinas")orden.forEach((id,i)=>{const r=S.data.rutinas[id];if(r&&r.orden!==i)put("rutinas",id,{...r,orden:i})});
}
function soltar(){
  if(!arr)return;
  const {item,lista,inicio}=arr,orden=ordenDe(lista);
  cancelAnimationFrame(arr.raf);item.classList.remove("arrastrando");document.body.classList.remove("arrastrando");
  arr=null;
  if(orden.join()!==inicio)aplicarOrden(lista,orden);
  render();
}
document.addEventListener("pointerdown",ev=>{
  const h=ev.target.closest("[data-drag]");if(!h||arr||ev.button>0)return;
  const item=h.closest("[data-item]"),lista=item&&item.parentElement;if(!lista||!lista.dataset.lista)return;
  ev.preventDefault();try{h.setPointerCapture(ev.pointerId)}catch(e){}
  arr={item,lista,y:ev.clientY,inicio:ordenDe(lista).join()};
  item.classList.add("arrastrando");document.body.classList.add("arrastrando");
  try{navigator.vibrate&&navigator.vibrate(10)}catch(e){}
  arr.raf=requestAnimationFrame(autoScroll);
});
document.addEventListener("pointermove",ev=>{if(!arr)return;arr.y=ev.clientY;moverSegunY(ev.clientY)});
document.addEventListener("pointerup",soltar);
document.addEventListener("pointercancel",soltar);
// Con teclado: flechas arriba/abajo sobre el asa
document.addEventListener("keydown",ev=>{
  const h=ev.target.closest&&ev.target.closest("[data-drag]");if(!h||(ev.key!=="ArrowUp"&&ev.key!=="ArrowDown"))return;
  const item=h.closest("[data-item]"),lista=item.parentElement,its=itemsDe(lista),i=its.indexOf(item),j=ev.key==="ArrowUp"?i-1:i+1;
  if(j<0||j>=its.length)return;ev.preventDefault();
  lista.insertBefore(item,ev.key==="ArrowUp"?its[j]:its[j].nextSibling);
  const tipo=lista.dataset.lista;aplicarOrden(lista,ordenDe(lista));render();
  const nueva=document.querySelector(`[data-lista="${tipo}"]`),n=nueva&&itemsDe(nueva)[j];n&&n.querySelector("[data-drag]").focus();
});

let tt;
function toast(m){const t=$("#toast");t.textContent=m;t.hidden=false;clearTimeout(tt);tt=setTimeout(()=>t.hidden=true,2600)}

/* ---------- Helpers de datos ---------- */
const rutinas=()=>Object.entries(S.data.rutinas).map(([id,r])=>({id,...r})).sort((a,b)=>(a.orden||0)-(b.orden||0)||String(a.nombre).localeCompare(b.nombre));
const registros=()=>Object.entries(S.data.registros).map(([id,r])=>({id,...r}));
function ultimoPeso(ej){let best=null;registros().forEach(r=>{if(r.ejercicio===ej&&(!best||r.fecha>best.fecha||(r.fecha===best.fecha&&(r.ts||0)>(best.ts||0))))best=r});return best}
function semanasCumplidas(){
  const meta=metaSem(),cnt={};
  Object.keys(S.data.asistencias).forEach(f=>{const k=ymd(lunes(parse(f)));cnt[k]=(cnt[k]||0)+1});
  const d=lunes(new Date());if((cnt[ymd(d)]||0)<meta)d.setDate(d.getDate()-7);
  let r=0;while((cnt[ymd(d)]||0)>=meta){r++;d.setDate(d.getDate()-7)}return r;
}

/* ---------- Gráfica SVG ---------- */
function chart(pts,{unit="kg",meta=null,dec=1}={}){
  if(pts.length<2)return '<p class="empty">Con dos registros o más verás aquí la gráfica.</p>';
  const W=340,H=190,pl=40,pr=14,pt=22,pb=26;
  const t0=parse(pts[0].x).getTime();let t1=parse(pts[pts.length-1].x).getTime();if(t1===t0)t1=t0+864e5;
  const vs=pts.map(p=>p.y);if(meta!=null)vs.push(meta);
  let lo=Math.min(...vs),hi=Math.max(...vs);const pad=(hi-lo)*.12||1;lo-=pad;hi+=pad;
  const X=t=>pl+(t-t0)/(t1-t0)*(W-pl-pr),Y=v=>pt+(hi-v)/(hi-lo)*(H-pt-pb);
  let g="";
  for(let i=0;i<4;i++){const v=lo+(hi-lo)*i/3,y=Y(v);g+=`<line class="g-grid" x1="${pl}" x2="${W-pr}" y1="${y}" y2="${y}"/><text class="g-txt" x="${pl-6}" y="${y+4}" text-anchor="end">${n1(v,hi-lo<10?1:0)}</text>`}
  if(meta!=null){const y=Y(meta);g+=`<line class="g-meta" x1="${pl}" x2="${W-pr}" y1="${y}" y2="${y}"/><text class="g-txt" x="${W-pr}" y="${y-5}" text-anchor="end">meta ${n1(meta)}</text>`}
  const xy=pts.map(p=>[X(parse(p.x).getTime()),Y(p.y)]);
  const line=xy.map((p,i)=>(i?"L":"M")+p[0].toFixed(1)+" "+p[1].toFixed(1)).join(" ");
  const area=line+` L${xy[xy.length-1][0].toFixed(1)} ${H-pb} L${xy[0][0].toFixed(1)} ${H-pb} Z`;
  const dots=xy.length<=40?xy.slice(0,-1).map(p=>`<circle class="g-dot" cx="${p[0]}" cy="${p[1]}" r="2.5"/>`).join(""):"";
  const L=xy[xy.length-1],last=pts[pts.length-1];
  const lx=Math.min(L[0],W-pr-4);
  return `<div class="chart"><svg viewBox="0 0 ${W} ${H}" role="img" aria-label="Gráfica de ${esc(unit)}">${g}
  <path class="g-area" d="${area}"/><path class="g-line" d="${line}"/>${dots}
  <circle class="g-end" cx="${L[0]}" cy="${L[1]}" r="5"/>
  <text class="g-val" x="${lx}" y="${Math.max(L[1]-10,13)}" text-anchor="end">${n1(last.y,dec)} ${esc(unit)}</text>
  <text class="g-txt" x="${pl}" y="${H-8}">${corta(pts[0].x)}</text>
  <text class="g-txt" x="${W-pr}" y="${H-8}" text-anchor="end">${corta(last.x)}</text></svg></div>`;
}

/* ---------- Vista: Hoy / Asistencia ---------- */
function vHoy(){
  const t=hoy(),hd=new Date(),A=S.data.asistencias,vino=!!A[t],meta=metaSem();
  const lun=ymd(lunes(hd));const semana=Object.keys(A).filter(f=>f>=lun&&f<=t).length;
  const mesPref=t.slice(0,7);const mes=Object.keys(A).filter(f=>f.startsWith(mesPref)).length;
  const racha=semanasCumplidas();
  const rs=rutinas();
  const fe=fechaEnt(),esHoy=fe===t,feVino=!!A[fe];
  const sugerida=(A[fe]&&A[fe].rutinaId)||(rs.find(r=>(r.dias||[]).includes(dow(parse(fe))))||{}).id||"";
  const rid=val("rut-"+fe,sugerida);
  const rut=rs.find(r=>r.id===rid);
  const hoyRegs=registros().filter(r=>r.fecha===fe).sort((a,b)=>(a.ts||0)-(b.ts||0));

  // mapa de calor: 16 semanas
  const ini=lunes(hd);ini.setDate(ini.getDate()-15*7);let cells="";
  for(let i=0;i<16*7;i++){const d=new Date(ini);d.setDate(ini.getDate()+i);const f=ymd(d);
    const fut=f>t;cells+=`<button class="${A[f]?"on":""} ${f===t?"today":""} ${f===fe&&!esHoy?"sel":""} ${fut?"fut":""}" data-act="dia-ent" data-f="${f}" aria-label="${larga(f)}${A[f]?", asististe":""}" ${fut?'tabindex="-1" disabled':""}></button>`}

  const setsDe=ej=>hoyRegs.filter(r=>r.ejercicio===ej).map(r=>`<span class="chip">${n1(r.peso)}×${r.reps}<button data-act="delreg" data-id="${r.id}" aria-label="Borrar serie">×</button></span>`).join("");
  let exHtml="";
  if(rut){(rut.ejercicios||[]).forEach((ej,i)=>{
    const u=ultimoPeso(ej.nombre),k=rid+"-"+i;
    exHtml+=`<div class="ex"><div class="ex-h"><b>${esc(ej.nombre)}</b><span class="muted num">${ej.series||"?"}×${ej.reps||"?"}${ej.peso?" · "+n1(ej.peso)+" kg":""}${u?" · última "+n1(u.peso)+"×"+u.reps:""}</span></div>
      <div class="chips">${setsDe(ej.nombre)}</div>
      <div class="ex-in"><label class="f">kg<input id="p-${k}" data-d="p-${k}" type="number" inputmode="decimal" step="0.5" value="${esc(val("p-"+k,u?u.peso:(ej.peso||"")))}"></label>
      <label class="f">reps<input id="r-${k}" data-d="r-${k}" type="number" inputmode="numeric" value="${esc(val("r-"+k,ej.reps||""))}"></label>
      <button class="btn" data-act="serie" data-k="${k}" data-i="${i}">+ Serie</button></div></div>`})}
  const enRut=new Set(rut?(rut.ejercicios||[]).map(e=>e.nombre):[]);
  const otros=[...new Set(hoyRegs.map(r=>r.ejercicio).filter(n=>!enRut.has(n)))];
  otros.forEach(n=>{exHtml+=`<div class="ex"><div class="ex-h"><b>${esc(n)}</b></div><div class="chips">${setsDe(n)}</div></div>`});

  return tipInstalar()+`
  <section class="card">
    <div class="row between"><div><div class="eyebrow">${t===hoy()?"Hoy":""}</div><div class="hero-date">${DIAS_L[dow(hd)]} ${hd.getDate()}</div><div class="muted">${MES[hd.getMonth()]} ${hd.getFullYear()}</div></div></div>
    <button class="btn big block ${vino?"done":""}" data-act="toggle" data-f="${t}">${vino?"✓ Asistencia registrada":"Registrar asistencia"}</button>
    ${vino?'<p class="muted" style="margin:0;text-align:center;font-size:.85rem">Toca de nuevo para quitarla.</p>':""}
  </section>

  <section class="card">
    <div class="stats">
      <div class="stat"><b class="num">${semana}<small class="muted" style="font-size:1rem">/${meta}</small></b><span>esta semana</span></div>
      <div class="stat"><b class="num">${mes}</b><span>este mes</span></div>
      <div class="stat"><b class="num hot">${racha}</b><span>semanas seguidas cumpliendo la meta</span></div>
    </div>
    <div class="meter" aria-hidden="true"><i style="width:${Math.min(100,semana/meta*100)}%"></i></div>
    <div class="row between"><span class="muted" style="font-size:.9rem">Meta semanal</span>
      <span class="stepper"><button data-act="meta" data-v="-1" aria-label="Bajar meta">−</button><b class="num">${meta}</b><button data-act="meta" data-v="1" aria-label="Subir meta">+</button></span></div>
  </section>

  <section class="card">
    <div class="row between"><h2>Últimas 16 semanas</h2><span class="muted num">${Object.keys(A).length} ${Object.keys(A).length===1?"día":"días"} en total</span></div>
    <div class="heat-wrap"><div class="heat">${cells}</div></div>
    <div class="heat-legend"><span>Toca un día para ver o completar ese entreno</span><span>L → D</span></div>
  </section>

  <section class="card" id="ent">
    <div class="row between"><h2>${esHoy?"Entreno de hoy":"Entreno del "+corta(fe)}</h2>${esHoy?"":'<button class="link" data-act="ent-hoy">Volver a hoy</button>'}</div>
    <label class="f">Fecha del entreno<input id="fEnt" data-d="fEnt" type="date" max="${t}" value="${fe}"></label>
    ${esHoy?"":`<button class="btn ${feVino?"done":"ghost"}" data-act="toggle" data-f="${fe}">${feVino?"✓ Asististe el "+larga(fe):"Marcar asistencia del "+larga(fe)}</button>`}
    ${rs.length?`<label class="f">Rutina<select id="rut-${fe}" data-d="rut-${fe}"><option value="">Sin rutina</option>${rs.map(r=>`<option value="${r.id}" ${r.id===rid?"selected":""}>${esc(r.nombre)}</option>`).join("")}</select></label>`
      :`<p class="muted" style="margin:0">Aún no tienes rutinas. <button class="link" data-act="go" data-t="rutinas">Crea la primera</button> para registrar tus series más rápido.</p>`}
    <div>${exHtml||'<p class="empty">Elige una rutina o anota un ejercicio suelto abajo.</p>'}</div>
    <div class="ex" style="border-top:1px solid var(--line);padding-top:12px">
      <b>Otro ejercicio</b>
      <label class="f">Ejercicio<input id="oN" data-d="oN" list="ejs" value="${esc(val("oN"))}" placeholder="Ej. Dominadas"></label>
      <datalist id="ejs">${[...new Set(registros().map(r=>r.ejercicio))].map(n=>`<option value="${esc(n)}">`).join("")}</datalist>
      <div class="ex-in"><label class="f">kg<input id="oP" data-d="oP" type="number" inputmode="decimal" step="0.5" value="${esc(val("oP"))}"></label>
      <label class="f">reps<input id="oR" data-d="oR" type="number" inputmode="numeric" value="${esc(val("oR"))}"></label>
      <button class="btn" data-act="serie-otro">+ Serie</button></div>
    </div>
  </section>`;
}

/* ---------- Vista: Rutinas ---------- */
const DRAG='<button type="button" class="drag" data-drag aria-label="Arrastra para cambiar el orden"><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="9" cy="6" r="1.6"/><circle cx="15" cy="6" r="1.6"/><circle cx="9" cy="12" r="1.6"/><circle cx="15" cy="12" r="1.6"/><circle cx="9" cy="18" r="1.6"/><circle cx="15" cy="18" r="1.6"/></svg></button>';
function vRutinas(){
  if(E)return vEditor();
  const rs=rutinas();
  const cards=rs.map(r=>`<section class="card" data-item="${r.id}">
    <div class="row between"><div class="row" style="gap:4px">${rs.length>1?DRAG:""}<h3>${esc(r.nombre)}</h3></div><div class="days">${DIAS.map((d,i)=>`<span class="day mini ${(r.dias||[]).includes(i)?"on":""}" aria-hidden="true">${d}</span>`).join("")}</div></div>
    <ul class="ej-list">${(r.ejercicios||[]).map(e=>`<li><span>${esc(e.nombre)}</span><span>${e.series||"?"}×${e.reps||"?"}${e.peso?" · "+n1(e.peso)+" kg":""}</span></li>`).join("")||'<li class="muted">Sin ejercicios</li>'}</ul>
    ${confirmar===r.id?`<div class="confirm"><span>¿Eliminar “${esc(r.nombre)}”? Tus series registradas se conservan.</span><button class="btn danger sm" data-act="delrut-si" data-id="${r.id}">Eliminar</button><button class="btn ghost sm" data-act="cancel">Cancelar</button></div>`
     :`<div class="row"><button class="btn ghost sm" data-act="editrut" data-id="${r.id}">Editar</button><button class="btn ghost sm" data-act="delrut" data-id="${r.id}">Eliminar</button></div>`}
  </section>`).join("");
  return `<button class="btn block" data-act="newrut">+ Nueva rutina</button>${rs.length>1?'<p class="muted" style="margin:0;font-size:.85rem;text-align:center">Arrastra ⠿ para ordenar tus rutinas.</p>':""}<div class="stack" data-lista="rutinas">${cards}</div>${cards?"":'<section class="card"><p class="empty">Crea tu primera rutina: ponle nombre, elige los días y añade ejercicios con series, repeticiones y peso objetivo.</p></section>'}`;
}
function vEditor(){
  const rows=E.ejercicios.map((e,i)=>`<div class="ej-edit" data-item="${i}">${DRAG}
    <input class="ej-name" id="en${i}" data-e="${i}.nombre" value="${esc(e.nombre)}" placeholder="Ejercicio" aria-label="Ejercicio">
    <input id="es${i}" data-e="${i}.series" type="number" inputmode="numeric" value="${esc(e.series)}" placeholder="Series" aria-label="Series">
    <input id="er${i}" data-e="${i}.reps" type="number" inputmode="numeric" value="${esc(e.reps)}" placeholder="Reps" aria-label="Repeticiones">
    <input id="ep${i}" data-e="${i}.peso" type="number" inputmode="decimal" step="0.5" value="${esc(e.peso)}" placeholder="kg" aria-label="Peso objetivo en kg">
    <button class="btn ghost sm" data-act="ej-del" data-i="${i}" aria-label="Quitar ejercicio">×</button></div>`).join("");
  return `<section class="card">
    <h2>${E.id?"Editar rutina":"Nueva rutina"}</h2>
    <label class="f">Nombre<input id="eNombre" data-e="nombre" value="${esc(E.nombre)}" placeholder="Ej. Pierna"></label>
    <div class="f" style="font-size:.8rem;color:var(--muted);font-weight:600">Días<div class="days" style="margin-top:4px">${DIAS.map((d,i)=>`<button class="day ${E.dias.includes(i)?"on":""}" data-act="dia" data-i="${i}" aria-pressed="${E.dias.includes(i)}" aria-label="${DIAS_L[i]}">${d}</button>`).join("")}</div></div>
    <div class="stack" style="gap:8px">
      <div class="ej-edit ej-head"><span></span><span>Ejercicio</span><span>Series</span><span>Reps</span><span>kg</span><span></span></div>
      <div class="stack" style="gap:8px" data-lista="ejercicios">${rows}</div>
      <button class="btn ghost" data-act="ej-add">+ Añadir ejercicio</button>
    </div>
    <div class="row"><button class="btn" data-act="saverut">Guardar rutina</button><button class="btn ghost" data-act="cancelrut">Cancelar</button></div>
  </section>`;
}

/* ---------- Vista: Evolución ---------- */
function vEvol(){
  const regs=registros();
  const ult={};regs.forEach(r=>{if(!ult[r.ejercicio]||r.fecha>ult[r.ejercicio])ult[r.ejercicio]=r.fecha});
  const ejs=Object.keys(ult).sort((a,b)=>ult[b].localeCompare(ult[a])||a.localeCompare(b));
  if(!ejs.length)return '<section class="card"><p class="empty">Cuando registres series en la pestaña Asistencia verás aquí cómo progresas en cada ejercicio: peso máximo, 1RM estimado y volumen.</p><button class="btn" data-act="go" data-t="hoy">Registrar mi primera serie</button></section>';
  const ej=ejs.includes(D.ej)?D.ej:ejs[0];const met=D.met||"max";
  const porDia={};regs.filter(r=>r.ejercicio===ej).forEach(r=>{(porDia[r.fecha]=porDia[r.fecha]||[]).push(r)});
  const fechas=Object.keys(porDia).sort();
  const agg=f=>{const s=porDia[f];return{max:Math.max(...s.map(r=>r.peso)),rm:Math.max(...s.map(r=>e1rm(r.peso,r.reps))),vol:s.reduce((a,r)=>a+r.peso*r.reps,0)}};
  const pts=fechas.map(f=>({x:f,y:agg(f)[met==="max"?"max":met==="1rm"?"rm":"vol"]}));
  const first=agg(fechas[0]),lastA=agg(fechas[fechas.length-1]);
  const mejor=Math.max(...fechas.map(f=>agg(f).max)),mejorRM=Math.max(...fechas.map(f=>agg(f).rm));
  const dif=lastA.max-first.max;
  const hist=fechas.slice().reverse().map(f=>`<div class="hist-row"><div><b>${larga(f)}</b><div class="chips" style="margin-top:6px">${porDia[f].sort((a,b)=>(a.ts||0)-(b.ts||0)).map(r=>`<span class="chip">${n1(r.peso)}×${r.reps}<button data-act="delreg" data-id="${r.id}" aria-label="Borrar serie">×</button></span>`).join("")}</div></div><span class="num muted">${n1(agg(f).vol,0)} kg vol.</span></div>`).join("");
  return `<section class="card">
    <label class="f">Ejercicio<select id="selEj" data-d="ej">${ejs.map(n=>`<option ${n===ej?"selected":""}>${esc(n)}</option>`).join("")}</select></label>
    <div class="stats">
      <div class="stat"><b class="num">${n1(mejor)}</b><span>kg máximo</span></div>
      <div class="stat"><b class="num">${n1(mejorRM,0)}</b><span>kg 1RM estimado</span></div>
      <div class="stat"><b class="num ${dif>0?"delta-good":""}">${dif>0?"+":""}${n1(dif)}</b><span>kg desde la 1.ª sesión</span></div>
    </div>
    <div class="seg" role="group" aria-label="Métrica">
      <button class="${met==="max"?"on":""}" data-act="met" data-v="max">Peso máx.</button>
      <button class="${met==="1rm"?"on":""}" data-act="met" data-v="1rm">1RM est.</button>
      <button class="${met==="vol"?"on":""}" data-act="met" data-v="vol">Volumen</button>
    </div>
    ${chart(pts,{unit:"kg",dec:met==="max"?1:0})}
    <p class="muted" style="margin:0;font-size:.8rem">1RM estimado con la fórmula de Epley: peso × (1 + reps/30). Volumen = suma de kg × reps del día.</p>
  </section>
  <section class="card"><h2>Historial · ${fechas.length} sesiones</h2><div class="hist">${hist}</div></section>`;
}

/* ---------- Vista: Peso corporal ---------- */
function vPeso(){
  const P=Object.values(S.data.peso).filter(p=>p&&p.fecha).sort((a,b)=>a.fecha.localeCompare(b.fecha));
  const meta=aj().pesoMeta;
  const act=P[P.length-1],ini=P[0];
  let hace30=null;if(act){const lim=new Date(parse(act.fecha));lim.setDate(lim.getDate()-30);const l=ymd(lim);hace30=[...P].reverse().find(p=>p.fecha<=l)||ini}
  const d30=act&&hace30&&hace30!==act?act.kg-hace30.kg:null;
  const fmtD=d=>d==null?"—":(d>0?"+":"")+n1(d);
  const g=aj(),perfilOk=!!(g.altura&&g.sexo&&g.nacimiento);
  const imcAct=act?imcDe(act.kg):null,grAct=act?grasaDe(act.kg,act.fecha):null;
  const mg=D.mg||"kg";
  const ptsG=P.map(p=>({x:p.fecha,y:grasaDe(p.kg,p.fecha)})).filter(p=>p.y!=null);
  const lista=P.slice().reverse().map((p,i,arr)=>{const prev=arr[i+1];const d=prev?p.kg-prev.kg:null;const gi=grasaDe(p.kg,p.fecha),ii=imcDe(p.kg);
    const extra=[ii?"IMC "+n1(ii):"",gi!=null?n1(gi)+" % grasa":""].filter(Boolean).join(" · ");
    return `<div class="hist-row"><div><b>${larga(p.fecha)}</b>${extra?`<div class="muted num" style="font-size:.85rem">${extra}</div>`:""}</div>
    <div class="row" style="gap:6px"><span class="num" style="font-size:1.25rem">${n1(p.kg)} kg</span>${d!=null&&d!==0?`<span class="num ${d>0?"delta-up":"delta-down"}" style="font-size:.9rem">${fmtD(d)}</span>`:""}
    <button class="btn ghost sm" data-act="delpeso" data-id="${p.fecha}" aria-label="Borrar registro">×</button></div></div>`}).join("");
  return `<section class="card">
    <h2>Registrar peso</h2>
    <div class="fields">
      <label class="f">Fecha<input id="pf" data-d="pf" type="date" max="${hoy()}" value="${esc(val("pf",hoy()))}"></label>
      <label class="f">Peso (kg)<input id="pk" data-d="pk" type="number" inputmode="decimal" step="0.1" value="${esc(val("pk",act?act.kg:""))}"></label>
    </div>
    ${perfilOk?"":'<p class="muted" style="margin:0;font-size:.85rem">Completa tu perfil abajo (altura, sexo y fecha de nacimiento) para calcular tu IMC y % de grasa.</p>'}
    <button class="btn" data-act="savepeso">Guardar peso</button>
  </section>
  <section class="card">
    <div class="stats">
      <div class="stat"><b class="num">${act?n1(act.kg):"—"}</b><span>kg actual</span></div>
      <div class="stat"><b class="num">${fmtD(d30)}</b><span>kg en 30 días</span></div>
      <div class="stat"><b class="num">${act&&ini&&act!==ini?fmtD(act.kg-ini.kg):"—"}</b><span>kg desde el inicio</span></div>
    </div>
    <div class="stats">
      <div class="stat"><b class="num">${imcAct?n1(imcAct):"—"}</b><span>IMC${imcAct?" · "+catImc(imcAct):""}</span></div>
      <div class="stat"><b class="num">${grAct!=null?n1(grAct):"—"}</b><span>% grasa estimado</span></div>
      <div class="stat"><b class="num">${grAct!=null?n1(act.kg*(1-grAct/100)):"—"}</b><span>kg masa magra</span></div>
    </div>
    <div class="seg" role="group" aria-label="Gráfica">
      <button class="${mg==="kg"?"on":""}" data-act="mg" data-v="kg">Peso</button>
      <button class="${mg==="gr"?"on":""}" data-act="mg" data-v="gr">% grasa</button>
    </div>
    ${mg==="kg"?chart(P.map(p=>({x:p.fecha,y:p.kg})),{unit:"kg",meta:meta||null}):(perfilOk?chart(ptsG,{unit:"%"}):'<p class="empty">Completa tu perfil para ver la evolución de tu % de grasa.</p>')}
    <div class="row between"><label class="f" style="max-width:170px">Peso meta (kg)<input id="pm" data-d="pm" type="number" inputmode="decimal" step="0.5" value="${esc(val("pm",meta||""))}"></label>
      <button class="btn ghost sm" data-act="savemeta" style="align-self:flex-end">Guardar meta</button></div>
    ${meta&&act?`<p class="muted" style="margin:0">Te faltan <b class="num">${n1(Math.abs(act.kg-meta))} kg</b> para tu meta de ${n1(meta)} kg.</p>`:""}
  </section>
  <section class="card">
    <h2>Mi perfil</h2>
    <div class="fields">
      <label class="f">Altura (cm)<input id="ha" data-d="ha" type="number" inputmode="decimal" step="0.5" placeholder="175" value="${esc(val("ha",g.altura||""))}"></label>
      <label class="f">Sexo<select id="hs" data-d="hs"><option value="">Elegir</option><option value="h" ${val("hs",g.sexo)==="h"?"selected":""}>Hombre</option><option value="m" ${val("hs",g.sexo)==="m"?"selected":""}>Mujer</option></select></label>
      <label class="f">Nacimiento<input id="hn" data-d="hn" type="date" max="${hoy()}" value="${esc(val("hn",g.nacimiento||""))}"></label>
    </div>
    <button class="btn ghost" data-act="saveperfil">Guardar perfil</button>
    <p class="muted" style="margin:0;font-size:.8rem">El % de grasa se estima con la fórmula de Deurenberg: 1,2 × IMC + 0,23 × edad − 10,8 × sexo (1 hombre, 0 mujer) − 5,4. Es una aproximación: si tienes mucha masa muscular, el IMC sobreestima la grasa.</p>
  </section>
  <section class="card"><h2>Historial</h2><div class="hist">${lista||'<p class="empty">Aún no hay registros. Anota tu peso de hoy arriba.</p>'}</div></section>`;
}

/* ---------- Acciones ---------- */
function marcarAsistencia(f,rid){const a=S.data.asistencias[f];if(!a)put("asistencias",f,{fecha:f,rutinaId:rid||null,ts:Date.now()});else if(rid&&a.rutinaId!==rid)put("asistencias",f,{...a,rutinaId:rid})}
function guardarSerie(ej,pk,rk,rid){
  const p=toNum(D[pk]),r=parseInt(D[rk],10);
  if(!ej){toast("Escribe el nombre del ejercicio.");return}
  if(!(p>=0)||!(r>0)){toast("Pon el peso en kg y las repeticiones.");return}
  const f=fechaEnt();
  put("registros",null,{fecha:f,ejercicio:ej,peso:p,reps:r,ts:Date.now(),rutinaId:rid||null});
  marcarAsistencia(f,rid);toast(`${ej}: ${n1(p)} kg × ${r}${f===hoy()?"":" · "+corta(f)}`);
}
document.addEventListener("click",ev=>{
  const tb=ev.target.closest("[data-tab]");if(tb){tab=tb.dataset.tab;E=null;confirmar=null;try{history.replaceState(null,"","#"+tab)}catch(e){}window.scrollTo(0,0);render();return}
  const b=ev.target.closest("[data-act]");if(!b)return;const a=b.dataset.act;
  if(a==="auth"){S.auth={vista:b.dataset.v,msg:"",err:""};render()}
  else if(a==="salir"){
    if(S.cola.length){toast(`Tienes ${S.cola.length} cambios sin subir. Conéctate a internet antes de salir.`);return}
    try{localStorage.removeItem(lsKey())}catch(e){}
    sb.auth.signOut().catch(()=>{});cerrarSesionLocal()}
  else if(a==="instalar"){if(instalar){instalar.prompt();instalar.userChoice.finally(()=>{instalar=null;schedule()})}}
  else if(a==="tip-ok"){try{localStorage.setItem("diario-gym-tip","1")}catch(e){}render()}
  else if(a==="go"){tab=b.dataset.t;render();window.scrollTo(0,0)}
  else if(a==="toggle"){const f=b.dataset.f;if(S.data.asistencias[f]){del("asistencias",f);toast("Asistencia del "+corta(f)+" quitada")}else{put("asistencias",f,{fecha:f,rutinaId:D["rut-"+f]||null,ts:Date.now()});toast("Asistencia del "+corta(f)+" registrada")}}
  else if(a==="dia-ent"){D.fEnt=b.dataset.f;render();const c=document.getElementById("ent");c&&c.scrollIntoView({behavior:"smooth",block:"start"})}
  else if(a==="ent-hoy"){D.fEnt=hoy();render()}
  else if(a==="meta"){setAj({metaSemanal:Math.max(1,Math.min(7,metaSem()+Number(b.dataset.v)))})}
  else if(a==="serie"){const rs=rutinas();const k=b.dataset.k;const rid=k.slice(0,k.lastIndexOf("-"));const r=rs.find(x=>x.id===rid);const ej=r&&r.ejercicios[Number(b.dataset.i)];
    if(D["p-"+k]===undefined)D["p-"+k]=document.getElementById("p-"+k).value;if(D["r-"+k]===undefined)D["r-"+k]=document.getElementById("r-"+k).value;
    guardarSerie(ej&&ej.nombre,"p-"+k,"r-"+k,rid)}
  else if(a==="serie-otro"){guardarSerie((D.oN||"").trim(),"oP","oR",null)}
  else if(a==="delreg"){del("registros",b.dataset.id)}
  else if(a==="newrut"){E={id:null,nombre:"",dias:[],ejercicios:[{nombre:"",series:"",reps:"",peso:""}]};render()}
  else if(a==="editrut"){const r=S.data.rutinas[b.dataset.id];E=JSON.parse(JSON.stringify({id:b.dataset.id,nombre:r.nombre||"",dias:r.dias||[],ejercicios:r.ejercicios||[],orden:r.orden}));if(!E.ejercicios.length)E.ejercicios.push({nombre:"",series:"",reps:"",peso:""});render()}
  else if(a==="dia"){const i=Number(b.dataset.i);E.dias=E.dias.includes(i)?E.dias.filter(x=>x!==i):[...E.dias,i].sort();render()}
  else if(a==="ej-add"){E.ejercicios.push({nombre:"",series:"",reps:"",peso:""});render();const n=document.getElementById("en"+(E.ejercicios.length-1));n&&n.focus()}
  else if(a==="ej-del"){E.ejercicios.splice(Number(b.dataset.i),1);render()}
  else if(a==="cancelrut"){E=null;render()}
  else if(a==="saverut"){
    const nombre=E.nombre.trim();if(!nombre){toast("Ponle un nombre a la rutina.");return}
    const ejercicios=E.ejercicios.filter(e=>String(e.nombre).trim()).map(e=>({nombre:String(e.nombre).trim(),series:parseInt(e.series,10)||null,reps:parseInt(e.reps,10)||null,peso:isFinite(toNum(e.peso))?toNum(e.peso):null}));
    put("rutinas",E.id,{nombre,dias:E.dias,ejercicios,orden:E.orden??Object.keys(S.data.rutinas).length});
    toast("Rutina guardada");E=null;render()}
  else if(a==="delrut"){confirmar=b.dataset.id;render()}
  else if(a==="delrut-si"){del("rutinas",b.dataset.id);confirmar=null;toast("Rutina eliminada")}
  else if(a==="cancel"){confirmar=null;render()}
  else if(a==="met"){D.met=b.dataset.v;render()}
  else if(a==="savepeso"){
    const f=val("pf",hoy()),kg=toNum(val("pk",document.getElementById("pk").value));
    if(!/^\d{4}-\d{2}-\d{2}$/.test(f)){toast("Elige una fecha.");return}
    if(!(kg>20&&kg<400)){toast("Escribe tu peso en kg, por ejemplo 78,5.");return}
    const o={fecha:f,kg:Math.round(kg*10)/10};put("peso",f,o);toast("Peso del "+corta(f)+" guardado")}
  else if(a==="mg"){D.mg=b.dataset.v;render()}
  else if(a==="saveperfil"){
    const g=aj(),h=toNum(val("ha",g.altura||"")),sx=val("hs",g.sexo||""),n=val("hn",g.nacimiento||"");
    if(!(h>=100&&h<=250)){toast("Escribe tu altura en centímetros, por ejemplo 175.");return}
    if(n&&!/^\d{4}-\d{2}-\d{2}$/.test(n)){toast("Elige tu fecha de nacimiento.");return}
    setAj({altura:Math.round(h*10)/10,sexo:sx||null,nacimiento:n||null});toast("Perfil guardado")}
  else if(a==="delpeso"){del("peso",b.dataset.id)}
  else if(a==="savemeta"){const m=toNum(D.pm);if(D.pm===""){setAj({pesoMeta:null});toast("Meta quitada");return}if(!(m>20&&m<400)){toast("Escribe una meta en kg.");return}setAj({pesoMeta:m});toast("Meta guardada")}
});
document.addEventListener("input",ev=>{
  const t=ev.target;
  if(t.dataset.d){const k=t.dataset.d;D[k]=t.value;
    if(k.startsWith("rut-")&&t.value){const f=k.slice(4),as=S.data.asistencias[f];if(as&&as.rutinaId!==t.value)put("asistencias",f,{...as,rutinaId:t.value})}
    if(t.tagName==="SELECT"||k==="fEnt")render()}
  else if(t.dataset.e&&E){const p=t.dataset.e.split(".");if(p.length===1)E[p[0]]=t.value;else E.ejercicios[Number(p[0])][p[1]]=t.value}
});

render();initStore();
})();
