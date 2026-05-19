function toast(m, t = 'ok') {
    const el = document.getElementById('toast');
    el.textContent = m;
    el.className = 'toast ' + t;
    el.style.display = 'block';
    setTimeout(() => el.style.display = 'none', 2500);
}

function fmt(n) { return n.toLocaleString('vi-VN') + 'đ'; }

// Thứ tự flow trạng thái
const STATUS_FLOW = ['pending', 'confirmed', 'shipping', 'done'];
const STATUS_LABELS = {
    pending: 'Chờ duyệt', confirmed: 'Đã xác nhận', shipping: 'Đang giao hàng',
    returning: 'Đang hoàn chuyển', done: 'Đã giao hàng', cancel: 'Đã hủy'
};
const STATUS_CSS = {
    pending: 't-p', confirmed: 't-con', shipping: 't-s', returning: 't-r', done: 't-d', cancel: 't-c'
};

let orders = [
    {
        id: 'NV-20250507-0042', date: '07/05/2025 14:23', status: 'shipping', customer: 'Nguyễn Thị B', phone: '0901 234 567', addr: '123 Trần Phú, Hải Châu, Đà Nẵng',
        items: [{ img: 'https://images.unsplash.com/photo-1485462537746-965f33f7f6a7?w=100&q=80', name: 'Áo blouse lụa cổ V', meta: 'Kem · M', qty: 2, price: 980000 }, { img: 'https://images.unsplash.com/photo-1506629082955-511b1aa562c8?w=100&q=80', name: 'Quần wide-leg linen', meta: 'Đen · S', qty: 1, price: 1350000 }, { img: 'https://images.unsplash.com/photo-1502716119720-b23a93e5fe1b?w=100&q=80', name: 'Đầm cocktail vai trần', meta: 'Đen · S', qty: 1, price: 1890000 }],
        sub: '5.200.000đ', ship: 'Miễn phí', disc: '0đ', pts: '50.000đ', total: '5.150.000đ'
    },
    {
        id: 'NV-20250506-0041', date: '06/05/2025 10:14', status: 'pending', customer: 'Trần Minh K', phone: '0912 345 678', addr: '45 Lê Lợi, Hải Châu, Đà Nẵng',
        items: [{ img: 'https://images.unsplash.com/photo-1601924994987-69e26d50dc26?w=100&q=80', name: 'Đầm dạ hội ren thêu', meta: 'Đen · M', qty: 1, price: 3850000 }],
        sub: '3.850.000đ', ship: '30.000đ', disc: '0đ', pts: '0đ', total: '3.880.000đ'
    },
    {
        id: 'NV-20250505-0040', date: '05/05/2025 08:55', status: 'done', customer: 'Lê Thị H', phone: '0933 456 789', addr: '78 Nguyễn Văn Linh, Thanh Khê, Đà Nẵng',
        items: [{ img: 'https://images.unsplash.com/photo-1591047139829-d91aecb6caea?w=100&q=80', name: 'Áo khoác blazer linen', meta: 'Nâu · M', qty: 1, price: 2250000 }, { img: 'https://images.unsplash.com/photo-1515372039744-b8f02a3ae446?w=100&q=80', name: 'Váy midi A-line', meta: 'Kem · S', qty: 1, price: 1650000 }],
        sub: '3.900.000đ', ship: 'Miễn phí', disc: '0đ', pts: '0đ', total: '3.900.000đ'
    },
    {
        id: 'NV-20250504-0039', date: '04/05/2025 16:30', status: 'cancel', customer: 'Phạm Văn D', phone: '0944 567 890', addr: '22 Trần Cao Vân, Thanh Khê, Đà Nẵng',
        items: [{ img: 'https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=100&q=80', name: 'Đầm xẻ tà tối giản', meta: 'Đen · XS', qty: 1, price: 2100000 }],
        sub: '2.100.000đ', ship: '30.000đ', disc: '0đ', pts: '0đ', total: '2.130.000đ'
    },
    {
        id: 'NV-20250503-0038', date: '03/05/2025 12:08', status: 'returning', customer: 'Hoàng Thị M', phone: '0955 678 901', addr: '99 Điện Biên Phủ, Hải Châu, Đà Nẵng',
        items: [{ img: 'https://images.unsplash.com/photo-1632149877166-f75d49000351?w=100&q=80', name: 'Bộ suit vest nữ tính', meta: 'Kem · L', qty: 1, price: 4500000 }],
        sub: '4.500.000đ', ship: 'Miễn phí', disc: '450.000đ', pts: '0đ', total: '4.050.000đ'
    },
];

let filtered = [...orders];
let curId = null;

function renderOrders() {
    const q = document.getElementById('searchInput').value.toLowerCase();
    let list = filtered.filter(o => !q || (o.id.toLowerCase().includes(q) || o.customer.toLowerCase().includes(q)));
    document.getElementById('countDisplay').textContent = list.length;
    document.getElementById('tbody').innerHTML = list.map(o => {
        const sc = STATUS_CSS[o.status] || 't-p';
        const sl = STATUS_LABELS[o.status] || o.status;
        const totalQty = o.items.reduce((s, i) => s + i.qty, 0);
        return `<tr>
      <td style="color:var(--gold);font-size:11px;letter-spacing:1px">#${o.id}</td>
      <td>${o.customer}</td>
      <td style="color:rgba(250,248,242,.55)">${o.date}</td>
      <td>${totalQty} sản phẩm</td>
      <td>${o.total}</td>
      <td><span class="otag ${sc}"><span class="otag-d"></span>${sl}</span></td>
      <td><div class="act-row"><button class="ia-btn" onclick="openDetail('${o.id}')" title="Chi tiết"><svg viewBox="0 0 24 24"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg></button></div></td>
    </tr>`;
    }).join('');
}

function filterOrders(type, btn) {
    document.querySelectorAll('.ftab').forEach(t => t.classList.remove('active'));
    btn.classList.add('active');
    filtered = type === 'all' ? [...orders] : orders.filter(o => o.status === type);
    renderOrders();
}

function buildStatusFlow(currentStatus) {
    const mainFlow = ['pending', 'confirmed', 'shipping', 'done'];
    const idx = mainFlow.indexOf(currentStatus);
    let html = '<div style="display:flex;align-items:center;gap:0;flex-wrap:wrap">';
    mainFlow.forEach((s, i) => {
        let cls = 'wait';
        if (i < idx) cls = 'done';
        else if (i === idx) cls = 'current';
        const dot = cls === 'done' ? '✓' : (i + 1);
        html += `<div class="sf-step ${cls}"><div class="sf-dot">${dot}</div><div class="sf-lbl">${STATUS_LABELS[s]}</div></div>`;
        if (i < mainFlow.length - 1) html += `<div style="width:24px;height:2px;margin-top:-28px;background:${cls === 'done' ? 'rgba(201,168,76,.4)' : 'rgba(250,248,242,.08)'}"></div>`;
    });
    // Nhánh đặc biệt
    if (currentStatus === 'returning' || currentStatus === 'cancel') {
        html += `<div style="width:16px"></div>`;
        html += `<div class="sf-step current"><div class="sf-dot" style="border-color:${currentStatus === 'returning' ? '#7ab8e8' : '#d47a76'};color:${currentStatus === 'returning' ? '#7ab8e8' : '#d47a76'}">!</div><div class="sf-lbl" style="color:${currentStatus === 'returning' ? '#7ab8e8' : '#d47a76'}">${STATUS_LABELS[currentStatus]}</div></div>`;
    }
    html += '</div>';
    return html;
}

function buildStatusOptions(currentStatus) {
    const mainFlow = ['pending', 'confirmed', 'shipping', 'done'];
    const curIdx = mainFlow.indexOf(currentStatus);
    const all = ['pending', 'confirmed', 'shipping', 'returning', 'done', 'cancel'];
    return all.map(s => {
        const sIdx = mainFlow.indexOf(s);
        const isPrev = sIdx !== -1 && sIdx < curIdx;
        const isSpecial = s === 'returning' || s === 'cancel';
        const disabled = isPrev && !isSpecial;
        return `<option value="${s}" ${disabled ? 'disabled' : ''} ${currentStatus === s ? 'selected' : ''} style="${disabled ? 'color:rgba(250,248,242,.2)' : ''}">${STATUS_LABELS[s]}</option>`;
    }).join('');
}

function openDetail(id) {
    const o = orders.find(x => x.id === id);
    curId = id;
    document.getElementById('mhdrS').textContent = '# ' + o.id;
    document.getElementById('mId').textContent = '#' + o.id;
    document.getElementById('mDate').textContent = o.date;
    document.getElementById('mCust').textContent = o.customer;
    document.getElementById('mPhone').textContent = o.phone;
    document.getElementById('mAddr').textContent = o.addr;
    document.getElementById('mItems').innerHTML = o.items.map(it => `
    <div class="mitem">
      <div class="mitem-thumb"><img src="${it.img}" alt="" loading="lazy"></div>
      <div class="mitem-info"><div class="mitem-name">${it.name}</div><div class="mitem-meta">${it.meta}</div></div>
      <div class="mitem-qty">x${it.qty}</div>
      <div class="mitem-price">${fmt(it.price * it.qty)}</div>
    </div>`).join('');
    document.getElementById('mTotals').innerHTML = `
    <div class="mtrow"><span class="mtlbl">Tạm tính</span><span>${o.sub}</span></div>
    <div class="mtrow"><span class="mtlbl">Vận chuyển</span><span>${o.ship}</span></div>
    <div class="mtrow"><span class="mtlbl">Giảm giá</span><span style="color:#7aba7a">— ${o.disc}</span></div>
    <div class="mtrow"><span class="mtlbl">Điểm thưởng</span><span style="color:#7aba7a">— ${o.pts}</span></div>
    <div class="mtrow grand"><span>Tổng cộng</span><span>${o.total}</span></div>`;
    document.getElementById('statusFlow').innerHTML = buildStatusFlow(o.status);
    document.getElementById('statusSel').innerHTML = buildStatusOptions(o.status);
    document.getElementById('mo').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeMo() {
    document.getElementById('mo').classList.remove('open');
    document.body.style.overflow = '';
}

function updateStatus() {
    const newStatus = document.getElementById('statusSel').value;
    const idx = orders.findIndex(x => x.id === curId);
    orders[idx].status = newStatus;
    const fi = filtered.findIndex(x => x.id === curId);
    if (fi !== -1) filtered[fi].status = newStatus;
    document.getElementById('statusFlow').innerHTML = buildStatusFlow(newStatus);
    document.getElementById('statusSel').innerHTML = buildStatusOptions(newStatus);
    renderOrders();
    toast('✦ Cập nhật trạng thái thành công!', 'info');
}

renderOrders();