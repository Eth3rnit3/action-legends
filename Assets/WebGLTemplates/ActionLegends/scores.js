// Global leaderboard for Action Legends, without a game server. Scores are signed Nostr events
// (NIP-78 app data, kind 30078) on public relays: one per browser key and per board, replaced
// when the player beats it. Each event carries the run's inputs; Unity replays them and only
// shows the scores it reproduces (PanameScores.cs). This file only signs, sends and fetches.
(() => {
  'use strict';
  const T = window.NostrTools;
  const KIND = 30078, TIMEOUT = 7000, MAX_CONTENT = 200000;
  // ?relais=ws://127.0.0.1:7777 replaces the public relays, for local tests.
  const custom = new URLSearchParams(location.search).get('relais');
  const RELAYS = custom ? custom.split(',') : ['wss://relay.damus.io', 'wss://nos.lol', 'wss://relay.primal.net', 'wss://nostr.mom'];
  const store = {
    get(key) { try { return localStorage.getItem(key); } catch { return null; } },
    set(key, value) { try { localStorage.setItem(key, value); } catch {} },
  };
  let pool = null;
  const relays = () => pool || (pool = new T.SimplePool());
  const timeout = (promise, ms) => Promise.race([promise, new Promise((_, reject) => setTimeout(() => reject(new Error('délai dépassé')), ms))]);

  function secretKey() {
    let hex = store.get('al-nostr-key');
    if (!hex || !/^[0-9a-f]{64}$/.test(hex)) { hex = T.utils.bytesToHex(T.generateSecretKey()); store.set('al-nostr-key', hex); }
    return T.utils.hexToBytes(hex);
  }
  const verdictKey = (id, version) => 'al-verdict:' + version + ':' + id;
  const bestKey = (board, version) => 'al-best:' + version + ':' + board;

  // Unity cannot be re-entered from inside a jslib call: answers always arrive in a later task.
  function deliver(payload) {
    setTimeout(() => {
      const net = window.ALNet;
      if (net && net.unity && net.objectName) net.unity.SendMessage(net.objectName, 'OnScoreEvent', JSON.stringify(payload));
    }, 0);
  }

  async function pack(text) {
    if (!window.CompressionStream) return {enc: 'plain', data: text};
    const stream = new Blob([text]).stream().pipeThrough(new CompressionStream('deflate-raw'));
    const bytes = new Uint8Array(await new Response(stream).arrayBuffer());
    let binary = '';
    for (let i = 0; i < bytes.length; i += 0x8000) binary += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
    return {enc: 'deflate', data: btoa(binary)};
  }
  async function unpack(packed) {
    if (packed.enc === 'plain') return String(packed.data);
    if (packed.enc !== 'deflate' || !window.DecompressionStream) throw new Error('format inconnu');
    const bytes = Uint8Array.from(atob(packed.data), c => c.charCodeAt(0));
    const stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream('deflate-raw'));
    return new Response(stream).text();
  }

  function team(record) {
    const pseudo = (store.get('al-name') || 'Joueur').trim().slice(0, 12) || 'Joueur';
    return record.heroes.map((hero, i) => hero < 0 ? null : (record.names && record.names[i]) || (i === 0 ? pseudo : 'J' + (i + 1))).filter(Boolean);
  }

  async function publish(json) {
    try {
      const request = JSON.parse(json), record = request.record;
      const best = Number(store.get(bestKey(request.board, record.version)) ?? -1);
      if (record.score <= best) { deliver({type: 'published', ok: 0, message: 'better'}); return; }
      const names = team(record), replay = Object.assign({}, record);
      delete replay.runs;
      const content = JSON.stringify({game: 'action-legends', name: names.join(' & ').slice(0, 40), players: names.length,
        score: record.score, time: record.time, won: record.won, replay, runs: await pack(record.runs)});
      if (content.length > MAX_CONTENT) { deliver({type: 'published', ok: 0, message: 'trop long'}); return; }
      const event = T.finalizeEvent({kind: KIND, created_at: Math.floor(Date.now() / 1000), content,
        tags: [['d', request.board], ['t', 'action-legends'], ['alt', 'Score Action Legends : ' + record.score + ' points']]}, secretKey());
      const results = await Promise.allSettled(relays().publish(RELAYS, event).map(sent => timeout(sent, TIMEOUT)));
      const ok = results.filter(result => result.status === 'fulfilled').length;
      if (ok) { store.set(bestKey(request.board, record.version), String(record.score)); store.set(verdictKey(event.id, record.version), 'ok'); }
      deliver({type: 'published', ok, message: ok ? '' : 'relais'});
    } catch (error) {
      deliver({type: 'published', ok: 0, message: String(error && error.message || error)});
    }
  }

  async function fetchBoard(json) {
    const request = JSON.parse(json);
    try {
      const mine = T.getPublicKey(secretKey());
      const events = await timeout(relays().querySync(RELAYS, {kinds: [KIND], '#d': [request.board], limit: 500}, {maxWait: TIMEOUT}), TIMEOUT + 1500);
      // Relays may still hold an author's older event: keep the newest one per key.
      const latest = new Map();
      for (const event of events) {
        if (event.content.length > MAX_CONTENT || !T.verifyEvent(event)) continue;
        const known = latest.get(event.pubkey);
        if (!known || event.created_at > known.created_at) latest.set(event.pubkey, event);
      }
      const scored = [];
      for (const event of latest.values()) {
        try {
          const content = JSON.parse(event.content);
          // One board per game version: an older run is not replayed by this simulation (PanameScores.cs).
          if (content.game === 'action-legends' && content.replay && content.runs && Number.isInteger(content.score) &&
            content.replay.version === request.version) scored.push({event, content});
        } catch {}
      }
      scored.sort((a, b) => b.content.score - a.content.score);
      const entries = [];
      for (const {event, content} of scored.slice(0, 40)) {
        try {
          const replay = Object.assign({}, content.replay, {runs: await unpack(content.runs)});
          entries.push({id: event.id, name: String(content.name || '?').slice(0, 40), players: content.players | 0, score: content.score,
            time: +content.time || 0, won: !!content.won, mine: event.pubkey === mine, verdict: store.get(verdictKey(event.id, request.version)) || '', replay});
        } catch {}
      }
      deliver({type: 'board', board: request.board, entries, message: ''});
    } catch (error) {
      deliver({type: 'board', board: request.board, entries: [], message: String(error && error.message || error)});
    }
  }

  window.ALScores = {
    publish,
    fetch: fetchBoard,
    verdict: (id, ok, version) => store.set(verdictKey(id, version), ok ? 'ok' : 'ko'),
  };
})();
