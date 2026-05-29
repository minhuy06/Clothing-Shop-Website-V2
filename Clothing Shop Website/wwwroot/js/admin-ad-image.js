// NEVA Admin — crop ảnh QC: popup + sidebar
(function () {
    const CROP_BY_POSITION = {
        popup: { aspectRatio: 4 / 5, width: 800, height: 1000, label: 'Popup 4:5' },
        sidebar: { aspectRatio: 16 / 10, width: 480, height: 300, label: 'Sidebar 16:10 (khung góc phải)' }
    };

    function getPosition() {
        const sel = document.getElementById('adPosition');
        const v = (sel?.value || 'popup').toLowerCase();
        return CROP_BY_POSITION[v] ? v : 'popup';
    }

    function getCropOpts() {
        return CROP_BY_POSITION[getPosition()] || CROP_BY_POSITION.popup;
    }

    function init() {
        const fileEl = document.getElementById('adImageFile');
        const zone = document.getElementById('adDropzone');
        const cropMo = document.getElementById('adCropMo');
        const cropImg = document.getElementById('adCropImg');
        const cropApply = document.getElementById('adCropApply');
        const cropCancel = document.getElementById('adCropCancel');
        const cropCancel2 = document.getElementById('adCropCancel2');
        const ratioHint = document.getElementById('adCropRatioHint');
        const cropModalHint = document.getElementById('adCropModalHint');
        const posSel = document.getElementById('adPosition');
        if (!fileEl || !zone) return;

        let cropper = null;

        function updateRatioHint() {
            const o = getCropOpts();
            if (ratioHint) ratioHint.textContent = o.label;
            if (cropModalHint) cropModalHint.textContent = o.label;
        }

        function destroyCropper() {
            if (cropper) {
                cropper.destroy();
                cropper = null;
            }
        }

        function closeCrop() {
            destroyCropper();
            if (cropMo) cropMo.classList.remove('open');
            if (cropImg) cropImg.src = '';
        }

        function showPreview(src, label) {
            zone.classList.add('has-file');
            const span = zone.querySelector('span');
            if (span) span.textContent = label || 'Đã chọn ảnh';
            const prev = document.getElementById('adImgPreview');
            const img = document.getElementById('adPreviewImg');
            if (prev && img) {
                img.src = src;
                prev.style.display = 'block';
            } else if (prev) {
                prev.style.display = 'block';
            }
        }

        function setFileFromBlob(blob, name) {
            const f = new File([blob], name || 'ad.jpg', { type: blob.type || 'image/jpeg' });
            const dt = new DataTransfer();
            dt.items.add(f);
            fileEl.files = dt.files;
        }

        function openCropper(file) {
            if (!cropMo || !cropImg || typeof Cropper === 'undefined') {
                const reader = new FileReader();
                reader.onload = e => {
                    showPreview(e.target.result, file.name);
                    setFileFromBlob(file, file.name);
                };
                reader.readAsDataURL(file);
                return;
            }
            const opts = getCropOpts();
            const reader = new FileReader();
            reader.onload = e => {
                cropImg.src = e.target.result;
                cropMo.classList.add('open');
                destroyCropper();
                cropImg.onload = () => {
                    cropImg.style.maxWidth = '100%';
                    cropImg.style.maxHeight = '100%';
                    cropper = new Cropper(cropImg, {
                        aspectRatio: opts.aspectRatio,
                        viewMode: 1,
                        dragMode: 'move',
                        autoCropArea: 0.9,
                        responsive: true,
                        background: false,
                        ready() {
                            cropper?.resize();
                        }
                    });
                };
            };
            reader.readAsDataURL(file);
        }

        function pickFile(f) {
            if (!f || !f.type.startsWith('image/')) return;
            openCropper(f);
        }

        zone.addEventListener('click', () => fileEl.click());
        zone.addEventListener('dragover', e => { e.preventDefault(); zone.classList.add('dz-over'); });
        zone.addEventListener('dragleave', () => zone.classList.remove('dz-over'));
        zone.addEventListener('drop', e => {
            e.preventDefault();
            zone.classList.remove('dz-over');
            if (e.dataTransfer.files?.[0]) pickFile(e.dataTransfer.files[0]);
        });
        fileEl.addEventListener('change', () => {
            if (fileEl.files?.[0]) pickFile(fileEl.files[0]);
        });

        posSel?.addEventListener('change', updateRatioHint);

        function applyCrop() {
            if (!cropper) return;
            const opts = getCropOpts();
            const canvas = cropper.getCroppedCanvas({
                width: opts.width,
                height: opts.height,
                imageSmoothingQuality: 'high'
            });
            canvas.toBlob(blob => {
                if (!blob) return;
                const url = URL.createObjectURL(blob);
                showPreview(url, 'Đã cắt · ' + opts.label);
                setFileFromBlob(blob, 'ad-' + getPosition() + '.jpg');
                closeCrop();
            }, 'image/jpeg', 0.92);
        }

        cropApply?.addEventListener('click', applyCrop);
        cropCancel?.addEventListener('click', closeCrop);
        cropCancel2?.addEventListener('click', closeCrop);
        cropMo?.addEventListener('click', e => { if (e.target === cropMo) closeCrop(); });

        updateRatioHint();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
