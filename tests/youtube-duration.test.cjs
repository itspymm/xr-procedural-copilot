const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
function fixture() {
  const timers = new Map(); let next=0, config, destroyed=0, seconds=0;
  const context={URL,location:{origin:'http://127.0.0.1:8765'},
    setInterval(fn){timers.set(++next,fn);return next;},setTimeout(fn){timers.set(++next,fn);return next;},
    clearInterval(id){timers.delete(id);},clearTimeout(id){timers.delete(id);}};
  vm.createContext(context);vm.runInContext(fs.readFileSync('role3_ai/web/duration.js','utf8'),context);
  const player={getDuration:()=>seconds,destroy(){destroyed++;}};
  return {api:context.CopilotDuration,timers,player,setSeconds:v=>seconds=v,
    create:(_,options)=>{config=options;return player;},get config(){return config;},get destroyed(){return destroyed;}};
}
const f=fixture();
for (const url of ['https://youtu.be/1PVA2nPU60U','https://www.youtube.com/watch?v=1PVA2nPU60U&t=5','https://youtube.com/shorts/1PVA2nPU60U','https://youtube.com/embed/1PVA2nPU60U']) assert.equal(f.api.videoId(url),'1PVA2nPU60U');
for(const url of ['http://youtu.be/1PVA2nPU60U','https://youtube.com.evil.example/watch?v=1PVA2nPU60U','https://youtu.be/invalid']) assert.throws(()=>f.api.videoId(url));
let result, error;
const cancel=f.api.watch(f.create,{},'1PVA2nPU60U',v=>result=v,e=>error=e);
f.config.events.onReady({target:f.player});assert.equal(result,undefined);
f.setSeconds(89.92);f.config.events.onStateChange({target:f.player});assert.equal(result,89);assert.equal(f.timers.size,0);
cancel();assert.equal(f.destroyed,1);
const old=fixture();let stale=false;const stop=old.api.watch(old.create,{},'1PVA2nPU60U',()=>stale=true,()=>stale=true);
stop();old.setSeconds(123);old.config.events.onReady({target:old.player});assert.equal(stale,false);assert.equal(old.timers.size,0);
const failed=fixture();failed.api.watch(failed.create,{},'1PVA2nPU60U',()=>assert.fail('Unexpected duration'),e=>error=e);
failed.config.events.onError({data:101});assert.match(error,/manually/);assert.equal(failed.timers.size,0);
const timed=fixture();timed.api.watch(timed.create,{},'1PVA2nPU60U',()=>assert.fail('Unexpected duration'),e=>error=e);
[...timed.timers.values()][1]();assert.match(error,/retry/);assert.equal(timed.timers.size,0);
console.log('PASS: URL formats, delayed metadata, integer duration, cancellation, errors, and manual fallback timeout');
