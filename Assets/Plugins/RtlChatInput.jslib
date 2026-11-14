// UX-013 follow-up (Batch 5, owner-approved): a native HTML text input placed over the chat InputField in WebGL builds,
// so Hebrew, mixed Hebrew/English, numbers and punctuation are edited with the browser's own bidi, caret,
// selection, paste and IME. The raw value (logical Unicode order, never reversed) is sent to Unity on every edit;
// Unity keeps using the existing InputField text and send pipeline. Used by Assets/Script/RtlChatInputWebGL.cs.
mergeInto(LibraryManager.library, {
    RtlInput_Create: function (objectNamePtr, placeholderPtr) {
        var objectName = UTF8ToString(objectNamePtr);
        var placeholder = UTF8ToString(placeholderPtr);
        var old = window.__fdRtlChatInput;
        if (old && old.dispose) { old.dispose(); }

        if (!document.getElementById('fd-rtl-chat-input-style')) {
            var css = document.createElement('style');
            css.id = 'fd-rtl-chat-input-style';
            css.textContent = '#fd-rtl-chat-input::placeholder{color:rgba(50,50,50,0.5);font-style:italic;}';
            document.head.appendChild(css);
        }

        var el = document.createElement('input');
        el.id = 'fd-rtl-chat-input';
        el.type = 'text';
        el.placeholder = placeholder;
        el.dir = 'rtl';
        el.autocomplete = 'off';
        el.spellcheck = false;
        el.setAttribute('aria-label', placeholder);
        var s = el.style;
        s.position = 'absolute'; s.zIndex = '10'; s.boxSizing = 'border-box'; s.margin = '0';
        s.border = '1px solid #c8c8c8'; s.borderRadius = '3px'; s.background = '#ffffff'; s.color = '#323232';
        s.padding = '0 10px'; s.fontFamily = '"Segoe UI", Arial, sans-serif'; s.textAlign = 'right';
        s.outline = 'none'; s.display = 'none';

        var st = { el: el, rect: null, visible: false };
        var send = function (method, value) {
            try {
                if (typeof SendMessage === 'function') { SendMessage(objectName, method, value); }
                else if (Module && Module.SendMessage) { Module.SendMessage(objectName, method, value); }
            } catch (e) { console.warn('RtlChatInput: ' + e); }
        };
        var updateDir = function () { el.dir = el.value.length ? 'auto' : 'rtl'; };
        var place = function () {
            var canvas = Module['canvas'];
            var fs = document.fullscreenElement || document.webkitFullscreenElement;
            // In browser fullscreen only the canvas is shown: hide the overlay and let the Unity field work as before.
            if (!st.visible || !st.rect || !canvas || (fs && !fs.contains(el))) {
                if (document.activeElement === el) { el.blur(); }
                s.display = 'none';
                return;
            }
            var r = canvas.getBoundingClientRect();
            var sx = r.width / canvas.width, sy = r.height / canvas.height;
            s.left = (r.left + window.pageXOffset + st.rect.x * sx) + 'px';
            s.top = (r.top + window.pageYOffset + st.rect.y * sy) + 'px';
            s.width = (st.rect.w * sx) + 'px';
            s.height = (st.rect.h * sy) + 'px';
            s.fontSize = (st.rect.f * sy) + 'px';
            s.display = 'block';
        };
        var stop = function (e) { e.stopPropagation(); };
        var onKeyDown = function (e) {
            e.stopPropagation();
            if (e.key === 'Enter' && !e.isComposing) { e.preventDefault(); send('OnHtmlSubmit', el.value); }
            else if (e.key === 'Escape') { el.blur(); }
        };
        var onInput = function () { updateDir(); send('OnHtmlInput', el.value); };
        var onFocus = function () { send('OnHtmlFocus', '1'); };
        var onBlur = function () { send('OnHtmlFocus', '0'); };

        el.addEventListener('keydown', onKeyDown);
        el.addEventListener('keyup', stop);
        el.addEventListener('keypress', stop);
        el.addEventListener('input', onInput);
        el.addEventListener('focus', onFocus);
        el.addEventListener('blur', onBlur);
        window.addEventListener('resize', place);
        document.addEventListener('fullscreenchange', place);
        document.addEventListener('webkitfullscreenchange', place);
        document.body.appendChild(el);

        st.place = place;
        st.updateDir = updateDir;
        st.dispose = function () {
            window.removeEventListener('resize', place);
            document.removeEventListener('fullscreenchange', place);
            document.removeEventListener('webkitfullscreenchange', place);
            if (el.parentNode) { el.parentNode.removeChild(el); }
            window.__fdRtlChatInput = null;
        };
        window.__fdRtlChatInput = st;
    },

    RtlInput_SetRect: function (x, y, w, h, fontPx) {
        var st = window.__fdRtlChatInput;
        if (!st) { return; }
        st.rect = { x: x, y: y, w: w, h: h, f: fontPx };
        st.place();
    },

    RtlInput_SetVisible: function (visible) {
        var st = window.__fdRtlChatInput;
        if (!st) { return; }
        st.visible = visible !== 0;
        st.place();
    },

    RtlInput_SetValue: function (valuePtr) {
        var st = window.__fdRtlChatInput;
        if (!st) { return; }
        var v = UTF8ToString(valuePtr);
        if (st.el.value !== v) { st.el.value = v; st.updateDir(); }
    },

    RtlInput_Destroy: function () {
        var st = window.__fdRtlChatInput;
        if (st && st.dispose) { st.dispose(); }
    }
});
