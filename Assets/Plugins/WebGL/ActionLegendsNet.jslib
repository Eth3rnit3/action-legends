// Bridge between Unity and window.ALNet (WebGLTemplates/ActionLegends/net.js).
mergeInto(LibraryManager.library, {
  ALNetInit: function (objectName) {
    var config = window.ALNet ? window.ALNet.unityInit(UTF8ToString(objectName)) : '{"mode":"local"}';
    var size = lengthBytesUTF8(config) + 1;
    var buffer = _malloc(size);
    stringToUTF8(config, buffer, size);
    return buffer;
  },
  ALNetSend: function (target, message) {
    if (window.ALNet) window.ALNet.send(UTF8ToString(target), UTF8ToString(message));
  },
  ALNetStatus: function (json) {
    if (window.ALNet) window.ALNet.status(UTF8ToString(json));
  }
});
