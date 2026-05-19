const FREE_SHIP = 500000;
const TOTAL_POINTS = 1250;
const POINTS_RATE = 100; // 100 điểm = 10.000đ
const MAX_POINTS_PCT = 0.3;

let cartItems = [
    { id: 1, name: 'Áo blouse lụa cổ V', meta: 'Kem · Size M', price: 980000, qty: 2, img: 'https://images.unsplash.com/photo-1485462537746-965f33f7f6a7?w=200&q=80', selected: true },
    { id: 2, name: 'Quần wide-leg linen', meta: 'Đen · Size S', price: 1350000, qty: 1, img: 'https://images.unsplash.com/photo-1506629082955-511b1aa562c8?w=200&q=80', selected: true },
    { id: 3, name: 'Váy midi dáng A-line', meta: 'Kem · Size M', price: 1650000, qty: 1, img: 'https://images.unsplash.com/photo-1515372039744-b8f02a3ae446?w=200&q=80', selected: false },
];

let discount = 0;
let pointsDiscount = 0;

function fmt(n) { return n.toLocaleString('vi-VN') + 'đ'; }

// ── Tính subtotal ──
function calcSubtotal() {
    return cartItems.filter(i => i.selected).reduce((s, i) => s + i.price * i.qty, 0);
}

// ── Update summary ──
function updateSummary() {
    const sub = calcSubtotal();
    const ship = sub >= FREE_SHIP ? 0 : 30000;
    const total = sub + ship - discount - pointsDiscount;
    const remain = FREE_SHIP - sub;

    document.getElementById('sumSubtotal').textContent = fmt(sub);
    document.getElementById('sumShip').textContent = ship === 0 ? 'Miễn phí' : fmt(ship);
    document.getElementById('sumDiscount').textContent = '— ' + fmt(discount);
    document.getElementById('sumPoints').textContent = '— ' + fmt(pointsDiscount);
    document.getElementById('sumTotal').textContent = fmt(Math.max(0, total));
    document.getElementById('shipRemain').textContent = remain > 0 ? fmt(remain) : 'đã đủ!';
    if (remain <= 0) document.getElementById('shipRemain').style.color = '#7aba7a';

    const selectedCount = cartItems.filter(i => i.selected).length;
    document.getElementById('cartSubtitle').textContent = selectedCount + ' sản phẩm được chọn · ' + cartItems.length + ' sản phẩm trong giỏ';

    const allSel = cartItems.length > 0 && cartItems.every(i => i.selected);
    const cbAll = document.getElementById('cbAll');
    if (allSel) cbAll.classList.add('on'); else cbAll.classList.remove('on');

    // Update slider max
    const maxPts = Math.min(TOTAL_POINTS, Math.floor(sub * MAX_POINTS_PCT / (10000 / POINTS_RATE)));
    document.getElementById('pointsSlider').max = maxPts;
}

// ── Render cart ──
function renderCart() {
    const rows = document.getElementById('cartRows');
    if (!cartItems.length) {
        rows.innerHTML = `<div class="empty-cart">
            <svg viewBox="0 0 24 24"><path d="M6 2L3 6v14a2 2 0 002 2h14a2 2 0 002-2V6l-3-4z"/><line x1="3" y1="6" x2="21" y2="6"/><path d="M16 10a4 4 0 01-8 0"/></svg>
            <p>Giỏ hàng của bạn đang trống</p>
            <a href="/Product" style="display:inline-block;padding:12px 28px;background:linear-gradient(135deg,var(--gold),var(--gold2));color:var(--black);font-size:10px;letter-spacing:2.5px;text-transform:uppercase;font-family:var(--font-b)">Khám phá sản phẩm</a>
        </div>`;
        updateSummary();
        return;
    }

    rows.innerHTML = cartItems.map((item, i) => `
        <div class="cart-row">
            <div class="cb ${item.selected ? 'on' : ''}" onclick="toggleItem(${i})"></div>
            <div class="cart-product">
                <div class="cart-thumb">
                    <img src="${item.img}" alt="${item.name}" loading="lazy"
                        onerror="this.parentElement.style.background='#14100a';this.style.display='none'">
                </div>
                <div>
                    <div class="cart-pname">${item.name}</div>
                    <div class="cart-pmeta">${item.meta}</div>
                </div>
            </div>
            <div class="cart-price">${fmt(item.price)}</div>
            <div class="qty-row">
                <button class="qbtn" onclick="changeQty(${i},-1)">−</button>
                <input class="qval" type="number" value="${item.qty}" min="1" onchange="setQty(${i},this.value)">
                <button class="qbtn" onclick="changeQty(${i},1)">+</button>
            </div>
            <div class="cart-total">${fmt(item.price * item.qty)}</div>
            <button class="cart-remove" onclick="removeItem(${i})">
                <svg viewBox="0 0 24 24"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/></svg>
            </button>
        </div>`).join('');

    updateSummary();
}

// ── Actions ──
function toggleItem(i) { cartItems[i].selected = !cartItems[i].selected; renderCart(); }

function toggleSelectAll() {
    const allSel = cartItems.every(i => i.selected);
    cartItems.forEach(i => i.selected = !allSel);
    renderCart();
}

function deleteSelected() {
    const count = cartItems.filter(i => i.selected).length;
    if (!count) { nevaToast('Chưa chọn sản phẩm nào!', 'err'); return; }
    cartItems = cartItems.filter(i => !i.selected);
    renderCart();
    nevaToast(`Đã xóa ${count} sản phẩm`);
}

function removeItem(i) { cartItems.splice(i, 1); renderCart(); nevaToast('Đã xóa sản phẩm'); }
function changeQty(i, d) { cartItems[i].qty = Math.max(1, cartItems[i].qty + d); renderCart(); }
function setQty(i, v) { cartItems[i].qty = Math.max(1, parseInt(v) || 1); renderCart(); }

// ── Coupon ──
function applyCoupon() {
    const code = document.getElementById('couponInput').value.trim().toUpperCase();
    if (code === 'NEVA20') { discount = calcSubtotal() * 0.2; renderCart(); nevaToast('✦ Giảm 20% đã được áp dụng!', 'info'); }
    else if (code === 'SALE50K') { discount = 50000; renderCart(); nevaToast('✦ Giảm 50.000đ đã được áp dụng!', 'info'); }
    else nevaToast('Mã giảm giá không hợp lệ!', 'err');
}

// ── Points ──
function syncSlider() {
    const v = parseInt(document.getElementById('pointsInput').value) || 0;
    document.getElementById('pointsSlider').value = v;
    updatePtsSaving(v);
}

function syncInput() {
    const v = parseInt(document.getElementById('pointsSlider').value) || 0;
    document.getElementById('pointsInput').value = v;
    updatePtsSaving(v);
}

function updatePtsSaving(pts) {
    const saving = Math.floor(pts / POINTS_RATE) * 10000;
    document.getElementById('ptsSaving').textContent = 'Giảm: ' + fmt(saving);
}

function applyPoints() {
    const sub = calcSubtotal();
    let pts = parseInt(document.getElementById('pointsInput').value) || 0;
    if (pts <= 0) { nevaToast('Vui lòng nhập số điểm muốn dùng!', 'err'); return; }
    if (pts > TOTAL_POINTS) { nevaToast('Bạn không đủ điểm!', 'err'); return; }
    const maxDisc = sub * MAX_POINTS_PCT;
    const saving = Math.floor(pts / POINTS_RATE) * 10000;
    if (saving > maxDisc) {
        pts = Math.floor(maxDisc / 10000) * POINTS_RATE;
        nevaToast('Điều chỉnh xuống mức tối đa cho phép (30% đơn hàng)', 'info');
    }
    pointsDiscount = Math.floor(pts / POINTS_RATE) * 10000;
    document.getElementById('pointsApplied').classList.add('show');
    document.getElementById('pointsAppliedText').textContent = `Đã dùng ${pts.toLocaleString('vi-VN')} điểm · Giảm ${fmt(pointsDiscount)}`;
    document.getElementById('pointsDisplay').textContent = (TOTAL_POINTS - pts).toLocaleString('vi-VN') + ' điểm';
    renderCart();
    nevaToast('✦ Đã áp dụng điểm thưởng!', 'info');
}

function cancelPoints() {
    pointsDiscount = 0;
    document.getElementById('pointsInput').value = '';
    document.getElementById('pointsSlider').value = 0;
    document.getElementById('pointsApplied').classList.remove('show');
    document.getElementById('ptsSaving').textContent = 'Giảm: 0đ';
    document.getElementById('pointsDisplay').textContent = TOTAL_POINTS.toLocaleString('vi-VN') + ' điểm';
    renderCart();
    nevaToast('Đã hủy sử dụng điểm');
}

function checkout() {
    const sel = cartItems.filter(i => i.selected);
    if (!sel.length) { nevaToast('Vui lòng chọn ít nhất 1 sản phẩm!', 'err'); return; }
    window.location.href = '/Order/Checkout';
}

// ── Init ──
document.addEventListener('DOMContentLoaded', renderCart);