// Online rooms for Action Legends. Trystero only finds the other browsers;
// the game protocol lives in Unity (PanameOnline.cs). This file is a pipe.
import {joinRoom, selfId} from './trystero.js';

const APP_ID = 'action-legends-online-v1';
const CODE_ALPHABET = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';

const net = {
  mode: 'local', code: '', name: '', selfId,
  room: null, action: null, target: null, queue: [], unity: null, objectName: '',
  onChange: null,
};

function emit(kind, peer, data) {
  net.queue.push(kind + '\n' + (peer || '') + '\n' + (data || ''));
  flush();
}

// Unity must never be re-entered from inside a jslib call: deliveries go
// through a task so they always run outside the Unity frame.
let flushScheduled = false;
function flush() {
  if (flushScheduled || !net.unity || !net.objectName) return;
  flushScheduled = true;
  setTimeout(() => {
    flushScheduled = false;
    if (!net.unity) return;
    const pending = net.queue.splice(0);
    for (const event of pending) net.unity.SendMessage(net.objectName, 'OnNetEvent', event);
  }, 0);
}

net.newCode = () => {
  const bytes = crypto.getRandomValues(new Uint8Array(6));
  return Array.from(bytes, b => CODE_ALPHABET[b % CODE_ALPHABET.length]).join('');
};

net.inviteUrl = () => {
  const url = new URL(location.href);
  url.hash = 'partie=' + net.code;
  return url.href;
};

net.start = (mode, code, name) => {
  net.mode = mode; net.code = code; net.name = name;
  net.room = joinRoom({appId: APP_ID, password: code}, code, {
    onJoinError: details => emit('error', details.peerId, String(details.error || 'connexion impossible')),
  });
  net.action = net.room.makeAction('msg');
  net.action.onMessage = (data, {peerId}) => emit('msg', peerId, typeof data === 'string' ? data : '');
  net.room.onPeerJoin = peerId => { emit('join', peerId); net.onChange?.(); };
  net.room.onPeerLeave = peerId => { emit('leave', peerId); net.onChange?.(); };
};

net.peerCount = () => (net.room ? Object.keys(net.room.getPeers()).length : 0);

// Called by the jslib bridge.
net.unityInit = objectName => {
  net.objectName = objectName;
  flush();
  return JSON.stringify({mode: net.mode, code: net.code, name: net.name, self: net.selfId});
};
net.send = (target, message) => {
  if (!net.action) return;
  net.action.send(message, target ? {target} : undefined).catch(() => {});
};
net.attach = instance => { net.unity = instance; flush(); };
net.status = json => { try { net.state = JSON.parse(json); } catch { net.state = null; } net.onChange?.(); };

window.ALNet = net;
window.dispatchEvent(new Event('alnet-ready'));
