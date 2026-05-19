function toast(m, t = 'info') {
    const el = document.getElementById('toast');
    el.textContent = m;
    el.className = 'toast ' + t;
    el.style.display = 'block';
    setTimeout(() => el.style.display = 'none', 2600);
}

const ORDERS = [
    {
        id: 'NV-20250507-0042', date: '07/05/2025 14:23', status: 'shipping', method: 'COD', addr: '123 Trần Phú, Hải Châu, Đà Nẵng',
        items: [{ img: 'https://images.unsplash.com/photo-1485462537746-965f33f7f6a7?w=200&q=80', name: 'Áo blouse lụa cổ V x2', meta: 'Kem · Size M', price: '1.960.000đ' }, { img: 'https://images.unsplash.com/photo-1506629082955-511b1aa562c8?w=200&q=80', name: 'Quần wide-leg linen x1', meta: 'Đen · Size S', price: '1.350.000đ' }, { img: 'https://images.unsplash.com/photo-1502716119720-b23a93e5fe1b?w=200&q=80', name: 'Đầm cocktail vai trần x1', meta: 'Đen · Size S', price: '1.890.000đ' }],
        sub: '5.200.000đ', ship: 'Miễn phí', disc: '0đ', pts: '50.000đ', total: '5.150.000đ',
        tl: [{ l: 'Đặt hàng', s: 'done' }, { l: 'Xác nhận', s: 'done' }, { l: 'Đang giao', s: 'current' }, { l: 'Đã giao', s: 'wait' }]
    },
    {
        id: 'NV-20250420-0031', date: '20/04/2025 09:15', status: 'done', method: 'COD', addr: '123 Trần Phú, Hải Châu, Đà Nẵng',
        items: [{ img: 'https://images.unsplash.com/photo-1591047139829-d91aecb6caea?w=200&q=80', name: 'Áo khoác blazer linen x1', meta: 'Nâu · Size M', price: '2.250.000đ' }],
        sub: '2.250.000đ', ship: '30.000đ', disc: '0đ', pts: '0đ', total: '2.280.000đ',
        tl: [{ l: 'Đặt hàng', s: 'done' }, { l: 'Xác nhận', s: 'done' }, { l: 'Đang giao', s: 'done' }, { l: 'Đã giao', s: 'done' }]
    },
    {
        id: 'NV-20250310-0018', date: '10/03/2025 16:40', status: 'cancel', method: 'COD', addr: '456 Nguyễn Văn Linh, Thanh Khê, Đà Nẵng',
        items: [{ img: 'https://images.unsplash.com/photo-1515372039744-b8f02a3ae446?w=200&q=80', name: 'Váy midi dáng A-line x1', meta: 'Kem · Size S', price: '1.650.000đ' }],
        sub: '1.650.000đ', ship: '30.000đ', disc: '0đ', pts: '0đ', total: '1.680.000đ', tl: []
    },
    {
        id: 'NV-20250210-0009', date: '10/02/2025 11:00', status: 'pending', method: 'COD', addr: '123 Trần Phú, Hải Châu, Đà Nẵng',
        items: [{ img: 'https://images.unsplash.com/photo-1601924994987-69e26d50dc26?w=200&q=80', name: 'Đầm dạ hội ren thêu x1', meta: 'Đen · Size M', price: '3.850.000đ' }],
        sub: '3.850.000đ', ship: '30.000đ', disc: '0đ', pts: '0đ', total: '3.880.000đ',
        tl: [{ l: 'Đặt hàng', s: 'done' }, { l: 'Xác nhận', s: 'current' }, { l: 'Đang giao', s: 'wait' }, { l: 'Đã giao', s: 'wait' }]
    },
];

const ST = {
    pending: { label: 'Chờ duyệt', cls: 'sb-pending' },
    shipping: { label: 'Đang giao hàng', cls: 'sb-shipping' },
    done: { label: 'Đã giao hàng', cls: 'sb-done' },
    cancel: { label: 'Đã hủy', cls: 'sb-cancel' }
};

let filtered = [...ORDERS], curI = null;

function tl(steps) {
    if (!steps || !steps.length) return '';
    let h = '<div class="order-timeline"><div class="timeline">';
    steps.forEach((s, i) => {
        h += `<div class="tl-step tl-${s.s}"><div class="tl-dot">${s.s === 'done' ? '✓' : s.s === 'current' ? '◆' : (i + 1)}</div><div class="tl-lbl">${s.l}</div></div>`;
        if (i < steps.length - 1) h += `<div class="tl-conn ${s.s === 'done' ? 'done' : 'wait'}"></div>`;
    });
    return h + '</div></div>';
}

function renderOrders(list) {
    const el = document.getElementById('orderList');
    if (!list.length) {
        el.innerHTML = '<div class="empty"><svg viewBox="0 0 24 24"><path d="M6 2L3 6v14a2 2 0 002 2h14a2 2 0 002-2V6l-3-4z"/><line x1="3" y1="6" x2="21" y2="6"/><path d="M16 10a4 4 0 01-8 0"/></svg><p>Không có đơn hàng nào</p><button class="btn-gold" onclick="window.location=\'/Product\'">Mua sắm ngay</button></div>';
        return;
    }
    el.innerHTML = list.map((o, i) => {
        const st = ST[o.status];
        const show = o.items.slice(0, 2);
        const more = o.items.length > 2 ? `<div class="items-more">+${o.items.length - 2} sản phẩm</div>` : '';
        const acts = o.status === 'done'
            ? `<button class="btn-gold" onclick="buyAgain(${i})">Mua lại</button><button class="btn-outline" onclick="openDetail(${i})">Chi tiết</button>`
            : o.status === 'cancel'
                ? `<button class="btn-gold" onclick="window.location='/Product'">Đặt lại</button><button class="btn-outline" onclick="openDetail(${i})">Chi tiết</button>`
                : `<button class="btn-outline" onclick="openDetail(${i})">Chi tiết</button>`;
        return `<div class="order-card"><div class="order-head"><div><div class="order-id"># ${o.id}</div><div class="order-date">${o.date}</div></div><span class="status-badge ${st.cls}"><span class="sb-dot"></span>${st.label}</span></div><div class="order-body"><div class="order-items">${show.map(it => `<div class="order-item"><div class="item-thumb"><img src="${it.img}" alt="" loading="lazy"></div><div><div class="item-name">${it.name}</div><div class="item-meta">${it.meta} · ${it.price}</div></div></div>`).join('')}${more}</div><div class="order-footer"><div><div class="order-total-lbl">Tổng đơn hàng</div><div class="order-total">${o.total}</div></div><div class="order-acts">${acts}</div></div></div>${tl(o.tl)}</div>`;
    }).join('');
}

function filterOrders(type, btn) {
    document.querySelectorAll('.ftab').forEach(t => t.classList.remove('active'));
    btn.classList.add('active');
    filtered = type === 'all' ? [...ORDERS] : ORDERS.filter(o => o.status === type);
    renderOrders(filtered);
}

function openDetail(idx) {
    const o = filtered[idx];
    curI = idx;
    const st = ST[o.status];
    document.getElementById('mhdrS').textContent = '# ' + o.id + ' · ' + o.date;
    document.getElementById('mId').textContent = '#' + o.id;
    document.getElementById('mDate').textContent = o.date;
    document.getElementById('mStatus').innerHTML = `<span class="status-badge ${st.cls}" style="font-size:7px;padding:2px 8px"><span class="sb-dot"></span>${st.label}</span>`;
    document.getElementById('mMethod').textContent = o.method;
    document.getElementById('mAddr').textContent = o.addr;
    document.getElementById('mItems').innerHTML = o.items.map(it => `
    <div class="mitem-row">
      <div class="mitem-thumb"><img src="${it.img}" alt="" loading="lazy"></div>
      <div style="flex:1"><div class="mitem-name">${it.name}</div><div class="mitem-meta">${it.meta}</div></div>
      <div class="mitem-price">${it.price}</div>
    </div>`).join('');
    document.getElementById('mTotals').innerHTML = `
    <div class="mtotal-row"><span class="mtlabel">Tạm tính</span><span>${o.sub}</span></div>
    <div class="mtotal-row"><span class="mtlabel">Vận chuyển</span><span>${o.ship}</span></div>
    <div class="mtotal-row"><span class="mtlabel">Giảm giá</span><span style="color:#7aba7a">— ${o.disc}</span></div>
    <div class="mtotal-row"><span class="mtlabel">Điểm thưởng</span><span style="color:#7aba7a">— ${o.pts}</span></div>
    <div class="mtotal-row grand"><span>Tổng cộng</span><span>${o.total}</span></div>`;
    const ab = document.getElementById('mActBtn');
    if (o.status === 'done') { ab.style.display = ''; ab.textContent = 'Mua lại'; }
    else if (o.status === 'cancel') { ab.style.display = ''; ab.textContent = 'Đặt lại'; }
    else ab.style.display = 'none';
    document.getElementById('detMo').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeMo() {
    document.getElementById('detMo').classList.remove('open');
    document.body.style.overflow = '';
}

function mAct() {
    closeMo();
    if (curI !== null && filtered[curI]?.status === 'done') buyAgain(curI);
    else window.location = '/Product';
}

function buyAgain(i) {
    toast('✦ Đã thêm vào giỏ hàng!');
}

renderOrders(ORDERS);