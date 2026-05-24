// NEVA Admin — upload + crop ảnh sản phẩm (modal sửa)
(function () {
    const instances = {};

    function init(opts) {
        const prefix = opts.prefix || 'edit';
        const fileEl = document.getElementById(prefix + 'File');
        const zone = document.getElementById(prefix + 'ImgZone');
        const cropMo = document.getElementById(prefix + 'CropMo');
        const cropImg = document.getElementById(prefix + 'CropImg');
        const cropApply = document.getElementById(prefix + 'CropApply');
        const cropCancel = document.getElementById(prefix + 'CropCancel');
        if (!fileEl || !zone) return null;

        let cropper = null;

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

        function showEmpty() {
            zone.classList.remove('has-image');
            zone.innerHTML =
                '<div class="img-upload-icon">' +
                '<svg viewBox="0 0 24 24"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/></svg>' +
                '</div>' +
                '<div class="img-upload-text">Chưa có ảnh mới</div>' +
                '<div class="img-upload-hint">Kéo thả hoặc bấm để chọn · JPG, PNG</div>';
        }

        function showPreview(src, label) {
            zone.classList.add('has-image');
            zone.innerHTML =
                '<div class="img-zone-preview"><img src="' + src + '" alt="" /></div>' +
                '<div class="img-zone-actions">' +
                '<button type="button" class="img-zone-change">Đổi ảnh</button>' +
                (label ? '<span class="img-zone-label">' + label + '</span>' : '') +
                '</div>';
            zone.querySelector('.img-zone-change')?.addEventListener('click', e => {
                e.stopPropagation();
                fileEl.click();
            });
        }

        function setFileFromBlob(blob, name) {
            const f = new File([blob], name || 'product.jpg', { type: blob.type || 'image/jpeg' });
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
            const reader = new FileReader();
            reader.onload = e => {
                cropImg.src = e.target.result;
                cropMo.classList.add('open');
                destroyCropper();
                cropImg.onload = () => {
                    cropper = new Cropper(cropImg, {
                        aspectRatio: opts.aspectRatio || 4 / 5,
                        viewMode: 1,
                        dragMode: 'move',
                        autoCropArea: 0.92,
                        responsive: true,
                        background: false
                    });
                };
            };
            reader.readAsDataURL(file);
        }

        function pickFile(f) {
            if (!f || !f.type.startsWith('image/')) return;
            openCropper(f);
        }

        zone.addEventListener('click', () => {
            if (!zone.classList.contains('has-image')) fileEl.click();
        });
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

        cropApply?.addEventListener('click', () => {
            if (!cropper) return;
            const canvas = cropper.getCroppedCanvas({ width: 800, height: 1000, imageSmoothingQuality: 'high' });
            canvas.toBlob(blob => {
                if (!blob) return;
                const url = URL.createObjectURL(blob);
                showPreview(url, 'Đã cắt ảnh');
                setFileFromBlob(blob, 'product.jpg');
                closeCrop();
            }, 'image/jpeg', 0.92);
        });
        cropCancel?.addEventListener('click', closeCrop);
        cropMo?.addEventListener('click', e => { if (e.target === cropMo) closeCrop(); });

        const api = {
            reset() {
                fileEl.value = '';
                closeCrop();
                showEmpty();
            },
            showCurrent(url) {
                fileEl.value = '';
                closeCrop();
                if (url) {
                    zone.classList.add('has-image');
                    zone.innerHTML =
                        '<div class="img-zone-preview"><img src="' + url + '" alt="" /></div>' +
                        '<div class="img-zone-current">Ảnh hiện tại</div>' +
                        '<div class="img-zone-actions"><button type="button" class="img-zone-change">Tải ảnh mới</button></div>';
                    zone.querySelector('.img-zone-change')?.addEventListener('click', e => {
                        e.stopPropagation();
                        fileEl.click();
                    });
                } else {
                    showEmpty();
                }
            }
        };

        instances[prefix] = api;
        showEmpty();
        return api;
    }

    window.NevaProductImageUpload = {
        init,
        get(prefix) { return instances[prefix || 'edit']; }
    };
})();
