(function () {
    let modal = null;
    let resolveFn = null;

    function ensureModal() {
        if (modal) return modal;

        modal = document.createElement('div');
        modal.id = 'nevaConfirmMo';
        modal.className = 'modal-overlay';
        modal.innerHTML =
            '<div class="modal-box" style="max-width:480px">' +
            '<div class="modal-hdr"><span id="nevaConfirmTitle">Xác nhận</span>' +
            '<button type="button" class="modal-x" id="nevaConfirmClose" aria-label="Đóng">×</button></div>' +
            '<p id="nevaConfirmText" class="confirm-modal-text"></p>' +
            '<div class="modal-actions">' +
            '<button type="button" class="act-btn" id="nevaConfirmCancel">Hủy</button>' +
            '<button type="button" class="act-btn" id="nevaConfirmOk">Xác nhận</button>' +
            '</div></div>';

        document.body.appendChild(modal);

        const close = (result) => {
            modal.classList.remove('open');
            const fn = resolveFn;
            resolveFn = null;
            if (fn) fn(result);
        };

        document.getElementById('nevaConfirmClose')?.addEventListener('click', () => close(false));
        document.getElementById('nevaConfirmCancel')?.addEventListener('click', () => close(false));
        document.getElementById('nevaConfirmOk')?.addEventListener('click', () => close(true));
        modal.addEventListener('click', (e) => { if (e.target === modal) close(false); });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && modal.classList.contains('open')) close(false);
        });

        return modal;
    }

    function nevaConfirm(opts) {
        const o = typeof opts === 'string' ? { message: opts } : (opts || {});
        ensureModal();

        const titleEl = document.getElementById('nevaConfirmTitle');
        const textEl = document.getElementById('nevaConfirmText');
        const okBtn = document.getElementById('nevaConfirmOk');
        const cancelBtn = document.getElementById('nevaConfirmCancel');

        if (titleEl) titleEl.textContent = o.title || 'Xác nhận';
        if (textEl) {
            if (o.html) textEl.innerHTML = o.html;
            else textEl.textContent = o.message || 'Bạn có chắc chắn?';
        }
        if (okBtn) {
            okBtn.textContent = o.okText || 'Xác nhận';
            okBtn.style.borderColor = o.danger ? '#d47a76' : 'var(--gold)';
            okBtn.style.color = o.danger ? '#d47a76' : 'var(--gold)';
        }
        if (cancelBtn) cancelBtn.textContent = o.cancelText || 'Hủy';

        modal.classList.add('open');

        return new Promise((resolve) => {
            resolveFn = resolve;
        });
    }

    function bindConfirmForm(form) {
        if (!form || form.dataset.nevaConfirmBound === '1') return;
        form.dataset.nevaConfirmBound = '1';
        form.addEventListener('submit', async (e) => {
            if (form.dataset.nevaConfirmSkip === '1') {
                delete form.dataset.nevaConfirmSkip;
                return;
            }
            e.preventDefault();
            const ok = await nevaConfirm({
                title: form.getAttribute('data-neva-confirm-title') || 'Xác nhận xóa',
                message: form.getAttribute('data-neva-confirm') || 'Bạn có chắc chắn?',
                okText: form.getAttribute('data-neva-confirm-ok') || 'Xóa',
                danger: form.hasAttribute('data-neva-confirm-danger')
            });
            if (ok) {
                form.dataset.nevaConfirmSkip = '1';
                if (typeof form.requestSubmit === 'function') form.requestSubmit();
                else form.submit();
            }
        });
    }

    function initNevaConfirmForms(root) {
        (root || document).querySelectorAll('form[data-neva-confirm]').forEach(bindConfirmForm);
    }

    window.nevaConfirm = nevaConfirm;
    window.initNevaConfirmForms = initNevaConfirmForms;
})();
