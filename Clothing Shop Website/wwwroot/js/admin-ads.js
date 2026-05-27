// NEVA Admin — modal & form tạo quảng cáo
(function () {
    const overlay = document.getElementById('addAdMo');
    const openBtn = document.getElementById('btnOpenAddAd');
    const closeBtns = document.querySelectorAll('[data-close-ad-modal]');
    const posHidden = document.getElementById('adPosition');
    const discVal = document.getElementById('adDiscountValue');
    const discSuffix = document.getElementById('adDiscountSuffix');
    const form = document.getElementById('addAdForm');
    const startDate = document.getElementById('adStartDate');
    const endDate = document.getElementById('adEndDate');

    function openModal() {
        if (overlay) {
            overlay.classList.add('open');
            document.body.style.overflow = 'hidden';
        }
        const search = document.getElementById('adProductSearch');
        if (search) setTimeout(() => search.focus(), 200);
    }

    function closeModal() {
        if (overlay) {
            overlay.classList.remove('open');
            document.body.style.overflow = '';
        }
    }

    openBtn?.addEventListener('click', openModal);
    closeBtns.forEach(btn => btn.addEventListener('click', closeModal));
    overlay?.addEventListener('click', e => {
        if (e.target === overlay) closeModal();
    });
    document.addEventListener('keydown', e => {
        if (e.key === 'Escape' && overlay?.classList.contains('open')) closeModal();
    });

    if (posHidden) {
        function syncPositionFromUi() {
            const checked = document.querySelector('.ads-pos-card input[type=radio]:checked');
            const card = checked?.closest('.ads-pos-card');
            const pos = card?.getAttribute('data-pos') || 'popup';
            posHidden.value = pos;
            posHidden.dispatchEvent(new Event('change'));
            document.querySelectorAll('.ads-pos-card').forEach(c => {
                c.classList.toggle('is-selected', c === card);
            });
        }

        document.querySelectorAll('.ads-pos-card input[type=radio]').forEach(r => {
            r.addEventListener('change', syncPositionFromUi);
        });
        syncPositionFromUi();
    }

    function updateDiscSuffix() {
        if (!discSuffix || !discVal) return;
        const pct = document.querySelector('input[name="discountType"]:checked')?.value === '1';
        discSuffix.textContent = pct ? '%' : 'đ';
        discVal.placeholder = pct ? 'VD: 20' : 'VD: 50000';
    }

    document.querySelectorAll('input[name="discountType"]').forEach(r => {
        r.addEventListener('change', updateDiscSuffix);
    });
    updateDiscSuffix();

    initAdDatePickers(startDate, endDate);

    form?.addEventListener('submit', e => {
        const pid = document.getElementById('adProductId');
        const file = document.getElementById('adImageFile');
        if (!pid?.value) {
            e.preventDefault();
            alert('Vui lòng chọn sản phẩm được quảng cáo.');
            return;
        }
        if (!file?.files?.length) {
            e.preventDefault();
            alert('Vui lòng chọn và cắt ảnh quảng cáo.');
            return;
        }
        if (startDate?.value && endDate?.value && startDate.value > endDate.value) {
            e.preventDefault();
            alert('Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.');
        }
    });
})();

function initAdDatePickers(startEl, endEl) {
    if (!startEl || !endEl) return;

    function openPicker(input) {
        if (!input) return;
        input.focus();
        if (typeof input.showPicker === 'function') {
            try {
                input.showPicker();
            } catch (_) {
                input.click();
            }
        } else {
            input.click();
        }
    }

    function syncEndMin() {
        if (startEl.value) {
            endEl.min = startEl.value;
            if (endEl.value && endEl.value < startEl.value) {
                endEl.value = startEl.value;
            }
        } else {
            endEl.removeAttribute('min');
        }
    }

    startEl.addEventListener('change', syncEndMin);
    syncEndMin();

    document.querySelectorAll('.ads-date-btn').forEach(btn => {
        btn.addEventListener('click', e => {
            e.preventDefault();
            e.stopPropagation();
            const id = btn.getAttribute('data-date-for');
            openPicker(document.getElementById(id));
        });
    });

    document.querySelectorAll('.ads-date-wrap').forEach(wrap => {
        const input = wrap.querySelector('input[type="date"]');
        if (!input) return;
        wrap.addEventListener('click', e => {
            if (e.target.closest('.ads-date-btn')) return;
            if (e.target === input) return;
            openPicker(input);
        });
    });
}
