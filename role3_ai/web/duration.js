'use strict';
(function (root) {
  function videoId(value) {
    let url;
    try { url = new URL(value.trim()); } catch (_) { throw Error('Enter a valid HTTPS YouTube link.'); }
    let id = null;
    if (url.protocol === 'https:') {
      if (url.hostname === 'youtu.be') id = url.pathname.replace(/^\/+|\/+$/g, '');
      else if (['youtube.com', 'www.youtube.com', 'm.youtube.com'].includes(url.hostname)) {
        if (url.pathname === '/watch') id = url.searchParams.get('v');
        else { const parts = url.pathname.split('/').filter(Boolean); if (parts.length === 2 && ['embed', 'shorts'].includes(parts[0])) id = parts[1]; }
      }
    }
    if (!id || !/^[A-Za-z0-9_-]{11}$/.test(id)) throw Error('Enter a valid HTTPS YouTube watch or share link.');
    return id;
  }

  let apiPromise;
  function loadAPI() {
    if (root.YT && root.YT.Player) return Promise.resolve(root.YT);
    if (!apiPromise) apiPromise = new Promise((resolve, reject) => {
      const script = document.createElement('script');
      const timeout = setTimeout(() => fail(), 12000);
      function fail() { clearTimeout(timeout); script.remove(); apiPromise = null; reject(Error('YouTube could not load. Enter the length manually or retry.')); }
      root.onYouTubeIframeAPIReady = () => { clearTimeout(timeout); resolve(root.YT); };
      script.src = 'https://www.youtube.com/iframe_api';
      script.onerror = fail;
      document.head.append(script);
    });
    return apiPromise;
  }

  // Each lookup owns its player and timers. Cancellation prevents a previous
  // video's delayed metadata from overwriting the current video's length.
  function watch(createPlayer, host, id, onDuration, onError) {
    let player, ended = false, cancelled = false;
    function finish(value, error) {
      if (ended || cancelled) return;
      ended = true; clearInterval(poll); clearTimeout(timeout);
      if (error) onError(error); else onDuration(value);
    }
    function check(target) {
      if (ended || cancelled || !target || typeof target.getDuration !== 'function') return;
      const seconds = Number(target.getDuration());
      if (Number.isFinite(seconds) && seconds >= 1) {
        if (seconds > 86400) finish(null, 'This tutorial exceeds the 24-hour limit.');
        else finish(Math.floor(seconds));
      }
    }
    const poll = setInterval(() => check(player), 500);
    const timeout = setTimeout(() => finish(null, 'Could not read the length. Press Play and retry, or enter seconds manually.'), 20000);
    try {
      player = createPlayer(host, { videoId: id, width: '100%', height: '270',
        playerVars: { playsinline: 1, controls: 1, origin: root.location.origin },
        events: { onReady: event => check(event.target), onStateChange: event => check(event.target),
          onError: () => finish(null, 'YouTube could not load this video. Enter its length manually; embedding may also fail on Quest.') } });
    } catch (_) { finish(null, 'Could not open the video preview. Enter the length manually or retry.'); }
    return () => { cancelled = true; clearInterval(poll); clearTimeout(timeout); if (player && player.destroy) player.destroy(); };
  }
  root.CopilotDuration = { videoId, loadAPI, watch };
})(globalThis);
