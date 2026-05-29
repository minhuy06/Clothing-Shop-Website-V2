// Trang bộ sưu tập — xem nhanh & thêm giỏ (dữ liệu từ ALL_PRODUCTS do Razor inject)
(function () {
    let currentProductId = null;

    const PLACEHOLDER_IMG = '/images/placeholder-product.svg';

    function imgFallback(url, productId) {
        if (url && String(url).trim() && !/^https?:\/\//i.test(url)) {
            return url;
        }
        if (productId) {
            return '/images/prod/' + productId + '.jpg';
        }
        return PLACEHOLDER_IMG;
    }

    window.openModal = function (id) {
        const p = (window.ALL_PRODUCTS || []).find(x => x.id === id);
        if (!p) return;

        currentProductId = id;
        document.getElementById('mImg').src = imgFallback(p.img, p.id);
        document.getElementById('mImg').alt = p.name;
        document.getElementById('mCat').textContent = (p.cat || '') + (p.season ? ' · ' + p.season : '');
        document.getElementById('mName').textContent = p.name;
        document.getElementById('mPrice').textContent = (p.price || 0).toLocaleString('vi-VN') + 'đ';
        document.getElementById('mOld').textContent = p.old > p.price ? p.old.toLocaleString('vi-VN') + 'đ' : '';
        document.getElementById('mDesc').textContent = p.desc || '';

        const sizes = p.sizes || [];
        const sizesEl = document.getElementById('mSizes');
        const stockHint = document.getElementById('mStockHint');
        if (!sizes.length) {
            sizesEl.innerHTML = '<span class="m-no-size">Chưa có size</span>';
            if (stockHint) stockHint.textContent = '';
        } else {
            sizesEl.innerHTML = sizes.map(s => {
                const stock = Number(s.stock) || 0;
                const out = s.inStock === false || stock <= 0;
                const cls = out ? 'msz out-of-stock' : 'msz';
                return `<button type="button" class="${cls}" data-size-id="${s.id}" data-stock="${stock}" onclick="selSize(this)"${out ? ' disabled aria-disabled="true"' : ''}>${s.name}</button>`;
            }).join('');
            const first = sizesEl.querySelector('.msz:not([disabled])');
            if (first) {
                first.classList.add('on');
                updateQtyForSize(first);
            } else if (stockHint) {
                stockHint.textContent = 'Tất cả size đã hết hàng';
            }
        }

        document.getElementById('qtyIn').value = '1';
        const link = document.getElementById('mDetailLink');
        if (link) link.href = '/Product?highlight=' + id;

        const btnCart = document.querySelector('.btn-cart');
        const anyInStock = sizes.some(s => s.inStock !== false && (Number(s.stock) || 0) > 0);
        if (btnCart) {
            btnCart.disabled = !anyInStock;
            btnCart.style.opacity = anyInStock ? '' : '0.45';
            btnCart.style.pointerEvents = anyInStock ? '' : 'none';
        }

        document.getElementById('mo').classList.add('open');
        document.body.style.overflow = 'hidden';
    };

    window.closeMo = function () {
        document.getElementById('mo').classList.remove('open');
        document.body.style.overflow = '';
        currentProductId = null;
    };

    function updateQtyForSize(el) {
        const stock = parseInt(el.getAttribute('data-stock'), 10) || 0;
        const qtyIn = document.getElementById('qtyIn');
        const hint = document.getElementById('mStockHint');
        qtyIn.max = stock > 0 ? stock : 1;
        const cur = parseInt(qtyIn.value, 10) || 1;
        if (cur > stock) qtyIn.value = Math.max(1, stock);
        if (hint) {
            hint.textContent = stock > 0 ? `Còn ${stock} sản phẩm size này` : '';
        }
    }

    window.selSize = function (el) {
        if (el.disabled || el.classList.contains('out-of-stock')) return;
        document.querySelectorAll('.msz').forEach(s => s.classList.remove('on'));
        el.classList.add('on');
        updateQtyForSize(el);
    };

    window.chQty = function (d) {
        const i = document.getElementById('qtyIn');
        const max = parseInt(i.max, 10) || 999;
        const next = Math.max(1, (parseInt(i.value, 10) || 1) + (d || 0));
        i.value = Math.min(next, max);
    };

    window.addCart = async function () {
        const sz = document.querySelector('.msz.on');
        if (!sz) {
            if (typeof nevaToast === 'function') nevaToast('Vui lòng chọn size!', 'err');
            else alert('Vui lòng chọn size!');
            return;
        }

        const sizeId = parseInt(sz.getAttribute('data-size-id'), 10);
        const qty = parseInt(document.getElementById('qtyIn').value, 10) || 1;
        const stock = parseInt(sz.getAttribute('data-stock'), 10) || 0;
        if (qty > stock) {
            if (typeof nevaToast === 'function') nevaToast('Chỉ còn ' + stock + ' sản phẩm trong kho!', 'err');
            else alert('Không đủ tồn kho!');
            return;
        }

        try {
            const body = new URLSearchParams();
            body.append('sizeId', String(sizeId));
            body.append('quantity', String(qty));

            const res = await fetch('/Cart/Add', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
                body: body.toString()
            });
            const data = await res.json();

            if (!data.success) {
                if (data.message && data.message.includes('đăng nhập')) {
                    if (confirm(data.message + '\n\nChuyển đến trang đăng nhập?')) {
                        window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                    }
                    return;
                }
                if (typeof nevaToast === 'function') nevaToast(data.message || 'Không thêm được', 'err');
                else alert(data.message || 'Lỗi');
                return;
            }

            closeMo();
            if (typeof nevaToast === 'function') nevaToast(data.message || 'Đã thêm vào giỏ hàng!', 'ok');
            else alert('Đã thêm vào giỏ hàng!');
        } catch (e) {
            if (typeof nevaToast === 'function') nevaToast('Lỗi kết nối máy chủ', 'err');
        }
    };

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.pcard[data-product-id]').forEach(card => {
            const id = parseInt(card.getAttribute('data-product-id'), 10);
            card.addEventListener('click', function (e) {
                if (e.target.closest('.pquick')) return;
                openModal(id);
            });
            card.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    openModal(id);
                }
            });
        });

        document.querySelectorAll('.pquick').forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.stopPropagation();
                const id = parseInt(this.getAttribute('data-id'), 10);
                openModal(id);
            });
        });
    });
})();
