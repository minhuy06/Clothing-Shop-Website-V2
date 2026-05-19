function toast(m, t = 'ok') { const el = document.getElementById('toast'); el.textContent = m; el.className = 'toast ' + t; el.style.display = 'block'; setTimeout(() => el.style.display = 'none', 2500) }
function fmt(n) { return n.toLocaleString('vi-VN') + 'đ' }

// TABS
function switchTab(tab, btn) {
    document.querySelectorAll('.atab').forEach(t => t.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById('productsTab').style.display = tab === 'products' ? 'block' : 'none';
    document.getElementById('discountsTab').style.display = tab === 'discounts' ? 'block' : 'none';
    if (tab === 'discounts') renderDiscounts();
}

// PRODUCTS
let products = [
    { id: 1, name: 'Đầm dạ hội ren thêu', cat: 'Đầm', season: 'Đông', price: 3850000, old: 0, stock: 12, badge: 'new', img: 'https://images.unsplash.com/photo-1601924994987-69e26d50dc26?w=100&q=80', desc: 'Đầm dạ hội thêu ren tinh xảo.' },
    { id: 2, name: 'Váy midi dáng A-line', cat: 'Đầm', season: 'Xuân', price: 1650000, old: 0, stock: 24, badge: '', img: 'https://images.unsplash.com/photo-1515372039744-b8f02a3ae446?w=100&q=80', desc: 'Váy midi dáng A-line thanh lịch.' },
    { id: 3, name: 'Áo khoác blazer linen', cat: 'Áo khoác', season: 'Thu', price: 2250000, old: 2800000, stock: 8, badge: 'sale', img: 'https://images.unsplash.com/photo-1591047139829-d91aecb6caea?w=100&q=80', desc: 'Blazer linen cao cấp.' },
    { id: 4, name: 'Áo blouse lụa cổ V', cat: 'Áo blouse', season: 'Hạ', price: 980000, old: 0, stock: 31, badge: 'new', img: 'https://images.unsplash.com/photo-1485462537746-965f33f7f6a7?w=100&q=80', desc: 'Áo blouse lụa mềm mại.' },
    { id: 5, name: 'Quần wide-leg linen', cat: 'Quần', season: 'Hạ', price: 1350000, old: 0, stock: 19, badge: '', img: 'https://images.unsplash.com/photo-1506629082955-511b1aa562c8?w=100&q=80', desc: 'Quần ống rộng chất linen.' },
    { id: 6, name: 'Đầm xẻ tà tối giản', cat: 'Đầm', season: 'Thu', price: 2100000, old: 2600000, stock: 5, badge: 'sale', img: 'https://images.unsplash.com/photo-1469334031218-e382a71b716b?w=100&q=80', desc: 'Đầm xẻ tà dáng maxi.' },
    { id: 7, name: 'Áo khoác dạ tweed', cat: 'Áo khoác', season: 'Đông', price: 3200000, old: 0, stock: 3, badge: 'new', img: 'https://images.unsplash.com/photo-1548624313-0396c75e4b1a?w=100&q=80', desc: 'Áo khoác dạ tweed cao cấp.' },
    { id: 8, name: 'Bộ suit vest nữ tính', cat: 'Bộ suit', season: 'Thu', price: 4500000, old: 0, stock: 7, badge: '', img: 'https://images.unsplash.com/photo-1632149877166-f75d49000351?w=100&q=80', desc: 'Bộ suit vest nữ tính.' },
];
let editProdId = -1;
let previewDataUrl = '';

function renderTable() {
    const q = document.getElementById('searchInput').value.toLowerCase();
    const cat = document.getElementById('catFilter').value;
    const season = document.getElementById('seasonFilter').value;
    let list = products.filter(p => {
        if (q && !p.name.toLowerCase().includes(q) && !p.cat.toLowerCase().includes(q)) return false;
        if (cat && p.cat !== cat) return false;
        if (season && p.season !== season) return false;
        return true;
    });
    document.getElementById('countDisplay').textContent = list.length;
    document.getElementById('tbody').innerHTML = list.map(p => `
    <tr>
      <td><div class="prow-thumb"><img src="${p.img}" alt="" loading="lazy" onerror="this.style.display='none'"></div></td>
      <td><div class="prow-info"><div><div class="prow-name">${p.name}</div><div class="prow-cat">${p.desc.substring(0, 38)}...</div></div>${p.badge === 'new' ? '<span class="badge-new">Mới</span>' : p.badge === 'sale' ? '<span class="badge-sale">Sale</span>' : ''}</div></td>
      <td>${p.cat}</td>
      <td>${p.season}</td>
      <td style="white-space:nowrap">${fmt(p.price)}${p.old ? `<br><span style="font-size:11px;color:rgba(250,248,242,.35);text-decoration:line-through">${fmt(p.old)}</span>` : ''}</td>
      <td><span class="pstock ${p.stock <= 5 ? 'low' : 'ok'}">${p.stock} cái</span></td>
      <td><div class="act-row">
        <button class="ia-btn" onclick="openProdMo(${p.id})" title="Sửa"><svg viewBox="0 0 24 24"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg></button>
        <button class="ia-btn del" onclick="delProd(${p.id})" title="Xóa"><svg viewBox="0 0 24 24"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/></svg></button>
      </div></td>
    </tr>`).join('');
}

function openProdMo(id) {
    editProdId = id; previewDataUrl = '';
    document.getElementById('prodMoTitle').textContent = id === -1 ? 'Thêm sản phẩm mới' : 'Chỉnh sửa sản phẩm';
    if (id !== -1) {
        const p = products.find(x => x.id === id); document.getElementById('fName').value = p.name; document.getElementById('fCat').value = p.cat; document.getElementById('fSeason').value = p.season; document.getElementById('fPrice').value = p.price; document.getElementById('fOldPrice').value = p.old; document.getElementById('fStock').value = p.stock; document.getElementById('fBadge').value = p.badge; document.getElementById('fImg').value = p.img; document.getElementById('fDesc').value = p.desc;
        document.getElementById('previewImg').src = p.img; document.getElementById('imgPreview').style.display = 'block'; document.getElementById('uploadArea').style.display = 'none'
    }
    else { ['fName', 'fPrice', 'fOldPrice', 'fStock', 'fImg', 'fDesc'].forEach(i => document.getElementById(i).value = ''); clearImage() }
    document.getElementById('prodMo').classList.add('open');
}
function closeProdMo() { document.getElementById('prodMo').classList.remove('open') }
function previewImage(input) {
    if (!input.files || !input.files[0]) return;
    const file = input.files[0];
    const reader = new FileReader();
    reader.onload = e => {
        previewDataUrl = e.target.result;
        document.getElementById('previewImg').src = previewDataUrl;
        document.getElementById('imgPreview').style.display = 'block';
        document.getElementById('uploadArea').style.opacity = '.5';
    };
    reader.readAsDataURL(file);
}
function clearImage() {
    previewDataUrl = '';
    document.getElementById('fImgFile').value = '';
    document.getElementById('imgPreview').style.display = 'none';
    document.getElementById('uploadArea').style.display = 'block';
    document.getElementById('uploadArea').style.opacity = '1';
}
function saveProd() {
    const name = document.getElementById('fName').value.trim();
    if (!name) { toast('Vui lòng nhập tên sản phẩm!', 'err'); return }
    const img = previewDataUrl || document.getElementById('fImg').value || 'https://images.unsplash.com/photo-1515372039744-b8f02a3ae446?w=100&q=80';
    const p = { name, cat: document.getElementById('fCat').value, season: document.getElementById('fSeason').value, price: parseInt(document.getElementById('fPrice').value) || 0, old: parseInt(document.getElementById('fOldPrice').value) || 0, stock: parseInt(document.getElementById('fStock').value) || 0, badge: document.getElementById('fBadge').value, img, desc: document.getElementById('fDesc').value };
    if (editProdId === -1) { p.id = Date.now(); products.push(p); toast('✦ Đã thêm sản phẩm!', 'info') }
    else { const i = products.findIndex(x => x.id === editProdId); products[i] = { ...products[i], ...p }; toast('✦ Đã cập nhật sản phẩm!', 'info') }
    closeProdMo(); renderTable();
}
function delProd(id) { if (!confirm('Xóa sản phẩm này?')) return; products = products.filter(x => x.id !== id); renderTable(); toast('Đã xóa sản phẩm.') }

// DISCOUNTS
let discounts = [
    { id: 1, code: 'NEVA20', value: 20, type: 'percent', qty: 100, used: 32, exp: '2025-12-31', desc: 'Giảm 20% toàn đơn' },
    { id: 2, code: 'SALE50K', value: 50000, type: 'fixed', qty: 50, used: 50, exp: '2025-06-30', desc: 'Giảm 50.000đ' },
    { id: 3, code: 'VIP30', value: 30, type: 'percent', qty: 20, used: 5, exp: '2025-08-15', desc: 'Ưu đãi thành viên VIP' },
];
let editDiscId = -1;

function renderDiscounts() {
    const now = new Date();
    let active = 0, expired = 0;
    document.getElementById('totalCodes').textContent = discounts.length;
    document.getElementById('discBody').innerHTML = discounts.map(d => {
        const isExp = new Date(d.exp) < now || d.used >= d.qty;
        if (isExp) expired++; else active++;
        return `<tr>
      <td><span class="code-tag">${d.code}</span></td>
      <td style="color:var(--gold)">${d.type === 'percent' ? d.value + '%' : fmt(d.value)}</td>
      <td>${d.used}/${d.qty} <span style="font-size:10px;color:rgba(250,248,242,.35)">đã dùng</span></td>
      <td><span class="exp-date ${isExp ? 'expired' : ''}">${d.exp}</span></td>
      <td><span style="font-size:10px;letter-spacing:1.5px;padding:3px 9px;border:1px solid;${isExp ? 'border-color:rgba(212,120,116,.4);color:#d47a76' : 'border-color:rgba(80,180,80,.4);color:#7aba7a'}">${isExp ? 'Hết hạn' : 'Hoạt động'}</span></td>
      <td><div class="act-row">
        <button class="ia-btn" onclick="openDiscMo(${d.id})" title="Sửa"><svg viewBox="0 0 24 24"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg></button>
        <button class="ia-btn del" onclick="delDisc(${d.id})" title="Xóa"><svg viewBox="0 0 24 24"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/></svg></button>
      </div></td>
    </tr>`;
    }).join('');
    document.getElementById('activeCodes').textContent = active;
    document.getElementById('expiredCodes').textContent = expired;
}
function openDiscMo(id) {
    editDiscId = id;
    document.getElementById('discMoTitle').textContent = id === -1 ? 'Thêm mã giảm giá' : 'Chỉnh sửa mã';
    if (id !== -1) { const d = discounts.find(x => x.id === id); document.getElementById('dCode').value = d.code; document.getElementById('dValue').value = d.value; document.getElementById('dType').value = d.type; document.getElementById('dQty').value = d.qty; document.getElementById('dExp').value = d.exp; document.getElementById('dDesc').value = d.desc || '' }
    else { ['dCode', 'dValue', 'dQty', 'dExp', 'dDesc'].forEach(i => document.getElementById(i).value = '') }
    document.getElementById('discMo').classList.add('open');
}
function closeDiscMo() { document.getElementById('discMo').classList.remove('open') }
function saveDisc() {
    const code = document.getElementById('dCode').value.trim().toUpperCase();
    if (!code) { toast('Vui lòng nhập mã code!', 'err'); return }
    const d = { code, value: parseFloat(document.getElementById('dValue').value) || 0, type: document.getElementById('dType').value, qty: parseInt(document.getElementById('dQty').value) || 0, used: 0, exp: document.getElementById('dExp').value, desc: document.getElementById('dDesc').value };
    if (editDiscId === -1) { d.id = Date.now(); discounts.push(d); toast('✦ Đã thêm mã giảm giá!', 'info') }
    else { const i = discounts.findIndex(x => x.id === editDiscId); discounts[i] = { ...discounts[i], ...d }; toast('✦ Đã cập nhật!', 'info') }
    closeDiscMo(); renderDiscounts();
}
function delDisc(id) { if (!confirm('Xóa mã này?')) return; discounts = discounts.filter(x => x.id !== id); renderDiscounts(); toast('Đã xóa mã.') }

renderTable();