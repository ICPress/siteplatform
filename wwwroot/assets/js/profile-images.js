(() => {
    'use strict';
    const FULL_LIMIT = 1024, PREVIEW_LIMIT = 576;
    const dialog = document.getElementById('profile-crop-dialog');
    if (!dialog) return;
    const canvas = document.getElementById('profile-crop-canvas'), ctx = canvas.getContext('2d');
    const slider = document.getElementById('crop-zoom'), zoomLabel = document.getElementById('crop-zoom-value');
    const apply = document.getElementById('crop-apply'), error = document.getElementById('crop-error');
    const states = [];
    let active, image, sourceUrl, previousFocus, opening = false, offsetX = 0, offsetY = 0, baseScale = 1, dragging, cropWidth = 200, cropHeight = 200;
    function fit(width, height, limit) {
        const scale = Math.min(1, limit / width, limit / height);
        return {width:Math.max(1, Math.round(width * scale)), height:Math.max(1, Math.round(height * scale))};
    }
    function busy(state, value) {
        state.busy = value; state.input.disabled = value; state.retry.disabled = value;
        const form = state.root.closest('[data-onboarding-form]');
        if (form) form.querySelector('button[type=submit]').disabled = value;
    }
    function draw() {
        const scale = baseScale * Number(slider.value);
        const width = image.naturalWidth * scale, height = image.naturalHeight * scale;
        const maxX = Math.max(0, (width - cropWidth) / 2), maxY = Math.max(0, (height - cropHeight) / 2);
        offsetX = Math.max(-maxX, Math.min(maxX, offsetX)); offsetY = Math.max(-maxY, Math.min(maxY, offsetY));
        ctx.clearRect(0, 0, 400, 300); ctx.drawImage(image, (400 - width) / 2 + offsetX, (300 - height) / 2 + offsetY, width, height);
        zoomLabel.textContent = Math.round(Number(slider.value) * 100) + '%';
    }
    function closeCrop() {
        dialog.close(); if (sourceUrl) URL.revokeObjectURL(sourceUrl);
        sourceUrl = null; image = null; active = null; dragging = null; previousFocus?.focus();
    }
    function cancelCrop() {
        if (apply.disabled || !active) return;
        const state = active; state.input.value = ''; busy(state, false); closeCrop();
        state.status.textContent = state.saved ? 'Your saved photo is unchanged.' : 'No new photo selected.';
    }
    dialog.addEventListener('cancel', event => { event.preventDefault(); cancelCrop(); });
    document.getElementById('crop-cancel').addEventListener('click', cancelCrop);
    slider.addEventListener('input', () => { if (image) draw(); });
    canvas.addEventListener('pointerdown', event => {
        if (!image) return; dragging = {id:event.pointerId, x:event.clientX, y:event.clientY}; canvas.setPointerCapture(event.pointerId);
    });
    canvas.addEventListener('pointermove', event => {
        if (!dragging || event.pointerId !== dragging.id) return;
        const rect = canvas.getBoundingClientRect();
        offsetX += (event.clientX - dragging.x) * 400 / rect.width; offsetY += (event.clientY - dragging.y) * 300 / rect.height;
        dragging.x = event.clientX; dragging.y = event.clientY; draw();
    });
    for (const name of ['pointerup', 'pointercancel', 'lostpointercapture']) canvas.addEventListener(name, () => { dragging = null; });
    canvas.addEventListener('keydown', event => {
        if (!image || !['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(event.key)) return;
        event.preventDefault(); const delta = event.shiftKey ? 20 : 5;
        if (event.key === 'ArrowLeft') offsetX -= delta;
        if (event.key === 'ArrowRight') offsetX += delta;
        if (event.key === 'ArrowUp') offsetY -= delta;
        if (event.key === 'ArrowDown') offsetY += delta;
        draw();
    });
    function csrf(state) { return state.root.querySelector('[name=__RequestVerificationToken]')?.value || state.root.closest('form')?.querySelector('[name=__RequestVerificationToken]')?.value; }
    async function post(state, url, fields = {}) {
        if (!url) throw new Error('route');
        const data = new FormData(); data.set('__RequestVerificationToken', csrf(state));
        for (const [key, value] of Object.entries(fields)) data.set(key, value);
        const response = await fetch(url, {method:'POST', body:data, headers:{Accept:'application/json','X-Requested-With':'XMLHttpRequest'}});
        if (!response.ok) throw new Error(response.status === 401 ? 'session' : response.status === 404 || response.status === 502 ? 'route' : 'save');
        return response.json();
    }
    async function put(url, token, blob) {
        const endpoint = new URL(url);
        if (!['https:', 'http:'].includes(endpoint.protocol) || !token) throw new Error('storage');
        const response = await fetch(endpoint.href, {method:'PUT', mode:'cors', credentials:'omit', redirect:'error', body:blob, headers:{'X-Auth-Token':token,'Content-Type':'image/jpeg'}});
        if (!response.ok) throw new Error('storage');
    }
    async function upload(state) {
        if (!state.large || state.busy) return;
        busy(state, true); state.retry.hidden = true; state.status.textContent = 'Saving photo…';
        try {
            const prepared = await post(state, state.root.dataset.prepareUrl, state.dimensions);
            if (!prepared.ticket || Boolean(prepared.previewUploadUrl) !== Boolean(state.small)) throw new Error('storage');
            await put(prepared.uploadUrl, prepared.authToken, state.large);
            if (state.small) await put(prepared.previewUploadUrl, prepared.authToken, state.small);
            // Commit only after every required object has been uploaded. The API verifies both dimensions.
            if (state.ticket) state.ticket.value = prepared.ticket;
            else { const result = await post(state, state.root.dataset.completeUrl, {ticket:prepared.ticket}); state.preview.src = result.url; }
            state.saved = true; state.status.textContent = state.ticket ? 'Photo uploaded. Continue to finish creating your account.' : 'Photo saved.';
        } catch (failure) {
            state.status.textContent = failure.message === 'session' ? 'Your sign-in has expired. Sign in again to upload a photo.' : failure.message === 'route' ? 'Image uploads are temporarily unavailable. Please try again after the site is updated.' : 'The photo could not be saved. Retry the upload or choose another photo.';
            state.retry.hidden = false;
        } finally { busy(state, false); }
    }
    const jpeg = surface => new Promise((resolve, reject) => surface.toBlob(blob => blob ? resolve(blob) : reject(new Error('crop')), 'image/jpeg', .9));
    function surface(width, height) {
        const out = document.createElement('canvas'); out.width = width; out.height = height;
        const context = out.getContext('2d'); context.imageSmoothingEnabled = true; context.imageSmoothingQuality = 'high';
        context.fillStyle = '#ffffff'; context.fillRect(0,0,width,height); return {out, context};
    }
    apply.addEventListener('click', async () => {
        if (!active || !image || apply.disabled) return;
        const state = active; apply.disabled = true; error.textContent = '';
        try {
            const scale = baseScale * Number(slider.value);
            const left = (400 - image.naturalWidth * scale) / 2 + offsetX, top = (300 - image.naturalHeight * scale) / 2 + offsetY;
            const sourceWidth = cropWidth / scale, sourceHeight = cropHeight / scale;
            const dimensions = fit(sourceWidth, sourceHeight, FULL_LIMIT);
            const {out, context} = surface(dimensions.width, dimensions.height);
            context.drawImage(image, ((400 - cropWidth) / 2 - left) / scale, ((300 - cropHeight) / 2 - top) / scale, sourceWidth, sourceHeight, 0,0,dimensions.width,dimensions.height);
            const large = await jpeg(out); let small = null;
            if (dimensions.width > PREVIEW_LIMIT || dimensions.height > PREVIEW_LIMIT) {
                const previewSize = fit(dimensions.width, dimensions.height, PREVIEW_LIMIT);
                const preview = surface(previewSize.width, previewSize.height);
                preview.context.drawImage(out, 0,0,dimensions.width,dimensions.height,0,0,previewSize.width,previewSize.height);
                small = await jpeg(preview.out);
            }
            state.large = large; state.small = small; state.dimensions = dimensions;
            if (state.previewUrl) URL.revokeObjectURL(state.previewUrl);
            state.previewUrl = URL.createObjectURL(large); state.preview.src = state.previewUrl; state.preview.hidden = false;
            state.preview.parentElement.querySelector('i')?.setAttribute('hidden', '');
            if (state.ticket) state.ticket.value = '';
            closeCrop(); busy(state, false); apply.disabled = false; await upload(state);
        } catch { error.textContent = 'The photo could not be cropped. Please try another image.'; }
        finally { apply.disabled = false; }
    });
    for (const root of document.querySelectorAll('[data-profile-image]')) {
        const kind = root.dataset.profileImage;
        const state = {root, kind, input:root.querySelector('input[type=file]'), status:root.querySelector('.upload-status'), retry:root.querySelector('[data-upload-retry]'), ticket:root.querySelector('[name=AvatarTicket]'), preview:document.getElementById(kind + '-preview'), busy:false};
        states.push(state); state.saved = !!state.ticket?.value || !!state.preview.getAttribute('src');
        if (state.ticket?.value) state.status.textContent = 'Your uploaded photo is ready. Choose it again if the upload expires.';
        state.retry.addEventListener('click', () => upload(state));
        root.querySelector('[data-image-clear]')?.addEventListener('click', () => {
            if (state.busy) return;
            state.ticket.value = ''; state.large = null; state.small = null; state.saved = false; state.input.value = ''; state.preview.hidden = true; state.preview.removeAttribute('src'); state.retry.hidden = true; state.status.textContent = 'You can continue without a photo.';
        });
        state.input.addEventListener('change', async () => {
            const file = state.input.files[0]; if (!file || opening || active) return;
            if (!['image/jpeg', 'image/png'].includes(file.type) || file.size > 5 * 1024 * 1024) { state.status.textContent = 'Choose a JPG or PNG up to 5 MB.'; state.input.value = ''; return; }
            opening = true; busy(state, true); state.status.textContent = 'Opening photo…'; const source = URL.createObjectURL(file);
            try {
                const loaded = new Image(); loaded.src = source; await loaded.decode(); if (!loaded.naturalWidth || !loaded.naturalHeight) throw new Error('image');
                active = state; image = loaded; sourceUrl = source; previousFocus = state.input;
                cropWidth = kind === 'avatar' ? 200 : 360; cropHeight = kind === 'avatar' ? 200 : 112.5;
                baseScale = Math.max(cropWidth / image.naturalWidth, cropHeight / image.naturalHeight); offsetX = 0; offsetY = 0; slider.value = '1';
                dialog.dataset.kind = kind; document.getElementById('crop-title').textContent = kind === 'avatar' ? 'Crop your profile picture' : 'Crop your cover photo';
                apply.textContent = state.ticket ? 'Use photo' : 'Save photo'; error.textContent = ''; draw(); dialog.showModal(); slider.focus();
            } catch { URL.revokeObjectURL(source); state.status.textContent = 'This image could not be opened. Choose another JPG or PNG.'; busy(state, false); }
            finally { opening = false; }
        });
    }
    document.querySelector('[data-onboarding-form]')?.addEventListener('submit', event => {
        const state = states.find(item => item.ticket);
        if (state && (state.busy || (state.large && !state.ticket.value))) { event.preventDefault(); state.status.textContent = 'Finish uploading your photo, or choose “Continue without a photo”.'; }
    });
})();
