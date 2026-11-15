const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const html = fs.readFileSync(path.join(__dirname, '../XRProcedualCopilot/Assets/Role3_TutorialData/youtube_player.html'), 'utf8');
const source = html.match(/<script>([\s\S]*?)<\/script>/)[1];
let tick, events, time = 0, state = 2, pauses = 0, seeks = [], notices = [];
const fake = {
  pauseVideo() { pauses++; state = 2; },
  playVideo() { state = 1; },
  seekTo(value) { seeks.push(value); },
  getCurrentTime() { return time; },
  getPlayerState() { return state; }
};
const window = { tlab: { unitySendMessage(go, method, message) { notices.push(message); } } };
const context = vm.createContext({
  window, console: { log() {} },
  document: { createElement() { return {}; }, head: { appendChild() {} } },
  setInterval(fn) { tick = fn; },
  YT: { Player: function(id, config) { events = config.events; return fake; } }
});
vm.runInContext(source, context);
window.onYouTubeIframeAPIReady();
events.onReady();
assert.ok(notices.includes('ready'));
window.copilot.segment(8, 15, true);
time = 40; tick(); // Stale pre-seek time must not finish the new segment.
assert.equal(state, 1);
time = 8; tick();
time = 15; tick();
assert.equal(state, 2);
assert.ok(notices.includes('segment ended'));
window.copilot.resume(); // A completed segment waits for an explicit repeat/next.
assert.equal(state, 2);
window.copilot.segment(8, 15, true); // Repeat.
time = 8; tick();
assert.equal(state, 1);
window.copilot.pause();
assert.equal(state, 2);
window.copilot.resume();
assert.equal(state, 1);
window.copilot.segment(15, 22, true); // Adjacent next segment.
time = 15; tick();
time = 22; tick();
assert.equal(state, 2);
assert.deepEqual(seeks, [8, 8, 15]);
events.onError({ data: 153 });
assert.ok(notices.includes('YouTube error 153'));
console.log('PASS: seek, boundary pause, repeat, adjacent next, pause/resume, and error reporting');
