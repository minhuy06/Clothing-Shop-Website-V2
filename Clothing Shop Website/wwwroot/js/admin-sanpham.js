function toast(m, t = 'ok') {
    const el = document.getElementById('toast');
    if (!el) return;
    el.textContent = m;
    el.className = 'toast ' + t;
    el.style.display = 'block';
    setTimeout(() => { el.style.display = 'none'; }, 2500);
}

function fmt(n) { return n.toLocaleString('vi-VN') + 'đ'; }

// TABS
function switchTab(tab, btn) {
    document.querySelectorAll('.atab').forEach(t => t.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById('productsTab').style.display = tab === 'products' ? 'block' : 'none';
    document.getElementById('discountsTab').style.display = tab === 'discounts' ? 'block' : 'none';
    if (tab === 'discounts') renderDiscounts();
}

// PRODUCTS — dữ liệu lấy từ server (không dùng mảng mẫu client-side)
let products = [];
let editProdId = -1;
let previewDataUrl = '';

function renderTable() {
    const tbody = document.getElementById('tbody');
    const countEl = document.getElementById('countDisplay');
    if (!tbody) return;
    const q = (document.getElementById('searchInput')?.value || '').toLowerCase();
    const cat = document.getElementById('catFilter')?.value || '';
    const season = document.getElementById('seasonFilter')?.value || '';
    let list = products.filter(p => {
        if (q && !p.name.toLowerCase().includes(q) && !(p.cat || '').toLowerCase().includes(q)) return false;
        if (cat && p.cat !== cat) return false;
        if (season && p.season !== season) return false;
        return true;
    });
    if (countEl) countEl.textContent = list.length;
    if (!list.length) {
        tbody.innerHTML = '<tr><td colspan="7" style="text-align:center;padding:24px;color:rgba(250,248,242,.35);font-size:11px">Chưa có sản phẩm. Thêm sản phẩm mới hoặc nhập hàng từ Dashboard.</td></tr>';
        return;
    }
    tbody.innerHTML = list.map(p => `
    <tr>
      <td><div class="prow-thumb"><img src="${p.img}" alt="" loading="lazy" onerror="this.style.display='none'"></div></td>
      <td><div class="prow-info"><div><div class="prow-name">${p.name}</div><div class="prow-cat">${(p.desc || '').substring(0, 38)}${(p.desc || '').length > 38 ? '...' : ''}</div></div>${p.badge === 'new' ? '<span class="badge-new">Mới</span>' : p.badge === 'sale' ? '<span class="badge-sale">Sale</span>' : ''}</div></td>
      <td>${p.cat || ''}</td>
      <td>${p.season || ''}</td>
      <td style="white-space:nowrap">${fmt(p.price)}${p.old ? `<br><span style="font-size:11px;color:rgba(250,248,242,.35);text-decoration:line-through">${fmt(p.old)}</span>` : ''}</td>
      <td><span class="pstock ${p.stock <= 5 ? 'low' : 'ok'}">${p.stock} cái</span></td>
      <td><div class="act-row">
        <button class="ia-btn" onclick="openProdMo(${p.id})" title="Sửa"><svg viewBox="0 0 24 24"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg></button>
        <button class="ia-btn del" onclick="delProd(${p.id})" title="Xóa"><svg viewBox="0 0 24 24"><polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 01-2 2H8a2 2 0 01-2-2L5 6"/></svg></button>
      </div></td>
    </tr>`).join('');
}

function openProdMo(id) {
    editProdId = id;
    previewDataUrl = '';
    const title = document.getElementById('prodMoTitle');
    if (title) title.textContent = id === -1 ? 'Thêm sản phẩm mới' : 'Chỉnh sửa sản phẩm';
    if (id !== -1) {
        const p = products.find(x => x.id === id);
        if (!p) return;
        document.getElementById('fName').value = p.name;
        document.getElementById('fCat').value = p.cat;
        document.getElementById('fSeason').value = p.season;
        document.getElementById('fPrice').value = p.price;
        document.getElementById('fOldPrice').value = p.old;
        document.getElementById('fStock').value = p.stock;
        document.getElementById('fBadge').value = p.badge;
        document.getElementById('fImg').value = p.img;
        document.getElementById('fDesc').value = p.desc;
        document.getElementById('previewImg').src = p.img;
        document.getElementById('imgPreview').style.display = 'block';
        document.getElementById('uploadArea').style.display = 'none';
    } else {
        ['fName', 'fPrice', 'fOldPrice', 'fStock', 'fImg', 'fDesc'].forEach(i => {
            const el = document.getElementById(i);
            if (el) el.value = '';
        });
        clearImage();
    }
    document.getElementById('prodMo')?.classList.add('open');
}

function closeProdMo() { document.getElementById('prodMo')?.classList.remove('open'); }

function previewImage(input) {
    if (!input.files || !input.files[0]) return;
    const reader = new FileReader();
    reader.onload = e => {
        previewDataUrl = e.target.result;
        document.getElementById('previewImg').src = previewDataUrl;
        document.getElementById('imgPreview').style.display = 'block';
        document.getElementById('uploadArea').style.opacity = '.5';
    };
    reader.readAsDataURL(input.files[0]);
}

function clearImage() {
    previewDataUrl = '';
    const file = document.getElementById('fImgFile');
    if (file) file.value = '';
    document.getElementById('imgPreview').style.display = 'none';
    const area = document.getElementById('uploadArea');
    if (area) {
        area.style.display = 'block';
        area.style.opacity = '1';
    }
}

function saveProd() {
    const name = document.getElementById('fName')?.value.trim();
    if (!name) { toast('Vui lòng nhập tên sản phẩm!', 'err'); return; }
    toast('Trang này dùng dữ liệu từ server — vui lòng thêm/sửa qua form nhập hàng trên Dashboard hoặc Products.', 'info');
    closeProdMo();
}

function delProd() {
    toast('Xóa sản phẩm qua trang Admin Sản phẩm (server).', 'info');
}

// DISCOUNTS — không dùng mảng mẫu; tab mã giảm giá trên Products.cshtml dùng DB
let discounts = [];
let editDiscId = -1;

function renderDiscounts() {
    const body = document.getElementById('discBody');
    if (!body) return;
    if (!discounts.length) {
        body.innerHTML = '<tr><td colspan="6" style="text-align:center;padding:20px;color:rgba(250,248,242,.35);font-size:11px">Chưa có mã giảm giá.</td></tr>';
        return;
    }
}

function openDiscMo() { document.getElementById('discMo')?.classList.add('open'); }
function closeDiscMo() { document.getElementById('discMo')?.classList.remove('open'); }
function saveDisc() { toast('Lưu mã giảm giá qua form trên trang Admin Sản phẩm.', 'info'); closeDiscMo(); }
function delDisc() { toast('Xóa mã qua form trên trang Admin Sản phẩm.', 'info'); }

if (document.getElementById('tbody')) renderTable();
