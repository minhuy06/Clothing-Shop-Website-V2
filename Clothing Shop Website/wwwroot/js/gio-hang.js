// Giỏ hàng — quy đổi điểm & mã giảm giá (dữ liệu từ #cartConfig)
(function () {
    const FREE_SHIP = 500000;
    const POINTS_PER_10K = 100; // 100 điểm = 10.000đ
    const MAX_POINTS_PCT = 0.3;

    let subtotal = 0;
    let shipping = 0;
    let totalPoints = 0;
    let discountAmount = 0;
    let pointsDiscount = 0;
    let appliedCoupon = '';
    let appliedPoints = 0;

    function fmt(n) {
        return Math.max(0, Math.round(n)).toLocaleString('vi-VN') + 'đ';
    }

    function calcPointsSaving(pts) {
        return Math.floor(pts / POINTS_PER_10K) * 10000;
    }

    function maxUsablePoints() {
        const maxByOrder = Math.floor(subtotal * MAX_POINTS_PCT / 10000) * POINTS_PER_10K;
        return Math.min(totalPoints, maxByOrder);
    }

    function updateSummary() {
        const total = Math.max(0, subtotal + shipping - discountAmount - pointsDiscount);

        const elSub = document.getElementById('sumSubtotal');
        const elShip = document.getElementById('sumShip');
        const elTotal = document.getElementById('sumTotal');
        const rowDisc = document.getElementById('rowDiscount');
        const rowPts = document.getElementById('rowPoints');

        if (elSub) elSub.textContent = fmt(subtotal);
        if (elShip) elShip.textContent = shipping === 0 ? 'Miễn phí' : fmt(shipping);
        if (elTotal) elTotal.textContent = fmt(total);

        if (rowDisc) {
            rowDisc.hidden = discountAmount <= 0;
            const el = document.getElementById('sumDiscount');
            if (el) el.textContent = '— ' + fmt(discountAmount);
        }
        if (rowPts) {
            rowPts.hidden = pointsDiscount <= 0;
            const el = document.getElementById('sumPoints');
            if (el) el.textContent = '— ' + fmt(pointsDiscount);
        }
    }

    function setCouponMsg(text, ok) {
        const el = document.getElementById('couponMsg');
        if (!el) return;
        el.textContent = text || '';
        el.className = 'promo-msg' + (text ? (ok ? ' ok' : ' err') : '');
    }

    async function applyCoupon() {
        const input = document.getElementById('couponInput');
        const code = (input?.value || '').trim();
        if (!code) {
            discountAmount = 0;
            appliedCoupon = '';
            setCouponMsg('Vui lòng nhập mã giảm giá.', false);
            updateSummary();
            return;
        }

        try {
            const res = await fetch('/Cart/ValidateCoupon?code=' + encodeURIComponent(code));
            const data = await res.json();
            if (!data.success) {
                discountAmount = 0;
                appliedCoupon = '';
                setCouponMsg(data.message || 'Mã không hợp lệ.', false);
                updateSummary();
                return;
            }
            discountAmount = data.discount;
            appliedCoupon = data.code || code.toUpperCase();
            if (input) input.value = appliedCoupon;
            setCouponMsg('✓ ' + (data.label || 'Đã áp dụng mã'), true);
            updateSummary();
            if (typeof nevaToast === 'function') nevaToast('Đã áp dụng mã giảm giá', 'ok');
        } catch {
            setCouponMsg('Lỗi kết nối máy chủ.', false);
        }
    }

    function syncSliderFromInput() {
        const inp = document.getElementById('pointsInput');
        const slider = document.getElementById('pointsSlider');
        if (!inp || !slider) return;
        let v = parseInt(inp.value, 10) || 0;
        v = Math.min(Math.max(0, v), maxUsablePoints());
        slider.value = v;
        const saving = document.getElementById('ptsSaving');
        if (saving) saving.textContent = 'Giảm: ' + fmt(calcPointsSaving(v));
    }

    function syncInputFromSlider() {
        const inp = document.getElementById('pointsInput');
        const slider = document.getElementById('pointsSlider');
        if (!inp || !slider) return;
        inp.value = slider.value;
        syncSliderFromInput();
    }

    function applyPoints() {
        const inp = document.getElementById('pointsInput');
        let pts = parseInt(inp?.value, 10) || 0;
        if (pts <= 0) {
            if (typeof nevaToast === 'function') nevaToast('Vui lòng nhập số điểm muốn dùng!', 'err');
            return;
        }
        const maxPts = maxUsablePoints();
        if (pts > maxPts) {
            pts = maxPts;
            if (inp) inp.value = pts;
            if (typeof nevaToast === 'function') nevaToast('Đã điều chỉnh theo mức tối đa (30% đơn hàng)', 'info');
        }
        if (pts > totalPoints) {
            if (typeof nevaToast === 'function') nevaToast('Bạn không đủ điểm!', 'err');
            return;
        }

        appliedPoints = pts;
        pointsDiscount = calcPointsSaving(pts);

        const applied = document.getElementById('pointsApplied');
        const appliedText = document.getElementById('pointsAppliedText');
        if (applied) applied.classList.add('show');
        if (appliedText) {
            appliedText.textContent = `Đã dùng ${pts.toLocaleString('vi-VN')} điểm · Giảm ${fmt(pointsDiscount)}`;
        }
        updateSummary();
        if (typeof nevaToast === 'function') nevaToast('Đã áp dụng điểm thưởng', 'info');
    }

    function cancelPoints() {
        appliedPoints = 0;
        pointsDiscount = 0;
        const inp = document.getElementById('pointsInput');
        const slider = document.getElementById('pointsSlider');
        if (inp) inp.value = '';
        if (slider) slider.value = 0;
        document.getElementById('pointsApplied')?.classList.remove('show');
        const saving = document.getElementById('ptsSaving');
        if (saving) saving.textContent = 'Giảm: 0đ';
        updateSummary();
    }

    async function goCheckout() {
        const body = new URLSearchParams();
        body.append('coupon', appliedCoupon);
        body.append('usePoints', String(appliedPoints));
        try {
            await fetch('/Cart/SetCheckoutPromo', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: body.toString()
            });
        } catch { /* vẫn chuyển trang */ }
        window.location.href = '/Order/Checkout';
    }

    function restoreSavedPromo(cfg) {
        if (cfg.dataset.savedCoupon) {
            applyCoupon();
        }
        const savedPts = parseInt(cfg.dataset.savedUsePoints, 10) || 0;
        if (savedPts > 0) {
            const inp = document.getElementById('pointsInput');
            const slider = document.getElementById('pointsSlider');
            if (inp) inp.value = savedPts;
            if (slider) slider.value = savedPts;
            applyPoints();
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        const cfg = document.getElementById('cartConfig');
        if (!cfg) return;

        subtotal = parseFloat(cfg.dataset.subtotal) || 0;
        shipping = parseFloat(cfg.dataset.shipping) || 0;
        totalPoints = parseInt(cfg.dataset.points, 10) || 0;

        const slider = document.getElementById('pointsSlider');
        if (slider) slider.max = maxUsablePoints();

        document.getElementById('btnApplyCoupon')?.addEventListener('click', applyCoupon);
        document.getElementById('couponInput')?.addEventListener('keydown', e => {
            if (e.key === 'Enter') { e.preventDefault(); applyCoupon(); }
        });
        document.getElementById('btnApplyPoints')?.addEventListener('click', applyPoints);
        document.getElementById('btnCancelPoints')?.addEventListener('click', cancelPoints);
        document.getElementById('pointsInput')?.addEventListener('input', syncSliderFromInput);
        document.getElementById('pointsSlider')?.addEventListener('input', syncInputFromSlider);
        document.getElementById('btnCheckout')?.addEventListener('click', goCheckout);

        updateSummary();
        restoreSavedPromo(cfg);
    });
})();
