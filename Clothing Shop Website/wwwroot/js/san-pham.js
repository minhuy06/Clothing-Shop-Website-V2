const PER_PAGE = 9;
let filtered = [...ALL_PRODUCTS];
let currentPage = 1;
let selected = new Set();
let currentModalId = null;

// ── Render grid ──
function fmt(n) { return n.toLocaleString('vi-VN') + 'đ'; }

function renderGrid() {
    const start = (currentPage - 1) * PER_PAGE;
    const page = filtered.slice(start, start + PER_PAGE);
    document.getElementById('countDisplay').textContent = filtered.length;
    const grid = document.getElementById('productGrid');

    if (!page.length) {
        grid.innerHTML = `<div class="empty-state">
            <svg viewBox="0 0 24 24"><circle cx="11" cy="11" r="7"/><line x1="16.5" y1="16.5" x2="22" y2="22"/></svg>
            <p>Không tìm thấy sản phẩm phù hợp</p>
        </div>`;
        document.getElementById('pagination').innerHTML = '';
        return;
    }

    grid.innerHTML = page.map(p => `
        <div class="pcard">
            <div class="pthumb">
                <div class="pimg-inner">
                    <img src="${p.img}" alt="${p.name}" loading="lazy"
                        onerror="this.parentElement.style.background='#14100a';this.style.display='none'">
                </div>
                <div class="pcheck-wrap ${selected.has(p.id) ? 'checked' : ''}"
                    onclick="event.stopPropagation();toggleSelect(${p.id})"></div>
                ${p.badge ? `<span class="pbadge badge-${p.badge}">${p.badge === 'new' ? 'Mới' : 'Sale'}</span>` : ''}
                <div class="pquick" onclick="openModal(${p.id})">
                    <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="3"/><path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7z"/></svg>
                    Xem nhanh
                </div>
            </div>
            <div class="pinfo">
                <div class="pcat">${p.cat} · ${p.season}</div>
                <div class="pname">${p.name}</div>
                <div class="price-row">
                    <span class="price">${fmt(p.price)}</span>
                    ${p.old ? `<span class="price-old">${fmt(p.old)}</span>` : ''}
                </div>
                <div class="szrow">${p.sizes.slice(0, 4).map(s => `<span class="s">${s}</span>`).join('')}</div>
            </div>
        </div>`).join('');

    renderPagination();
}

function renderPagination() {
    const total = Math.ceil(filtered.length / PER_PAGE);
    if (total <= 1) { document.getElementById('pagination').innerHTML = ''; return; }
    let h = '';
    if (currentPage > 1) h += `<div class="ppage" onclick="goPage(${currentPage - 1})">‹</div>`;
    for (let i = 1; i <= total; i++) h += `<div class="ppage ${i === currentPage ? 'on' : ''}" onclick="goPage(${i})">${i}</div>`;
    if (currentPage < total) h += `<div class="ppage" onclick="goPage(${currentPage + 1})">›</div>`;
    document.getElementById('pagination').innerHTML = h;
}

function goPage(n) { currentPage = n; renderGrid(); window.scrollTo({ top: 0, behavior: 'smooth' }); }

// ── Filters ──
function applyFilters() {
    const cats = [...document.querySelectorAll('.fc-item .fc-box.on')].map(b => b.parentElement.dataset.cat).filter(Boolean);
    const seasons = [...document.querySelectorAll('.season-btn.on')].map(b => b.dataset.season);
    const sizes = [...document.querySelectorAll('.sz-btn.on')].map(b => b.dataset.size);
    const minP = parseInt(document.getElementById('priceMin').value) || 0;
    const maxP = parseInt(document.getElementById('priceMax').value) || 9999999;
    const search = document.getElementById('searchInput').value.toLowerCase().trim();

    filtered = ALL_PRODUCTS.filter(p => {
        if (cats.length && !cats.some(cat => p.cat.includes(cat))) return false;
        if (seasons.length && !seasons.includes(p.season)) return false;
        if (p.price < minP || p.price > maxP) return false;
        if (sizes.length && !sizes.some(s => p.sizes.includes(s))) return false;
        if (search && !p.name.toLowerCase().includes(search) && !p.cat.toLowerCase().includes(search)) return false;
        return true;
    });

    applySort(false);
    currentPage = 1;
    renderGrid();
    if (search || cats.length || seasons.length || sizes.length)
        nevaToast(`✦ Tìm thấy ${filtered.length} sản phẩm`, 'info');
}

function resetFilters() {
    document.querySelectorAll('.fc-box').forEach(b => b.classList.remove('on'));
    document.querySelectorAll('.sz-btn').forEach(b => b.classList.remove('on'));
    document.querySelectorAll('.season-btn').forEach(b => b.classList.remove('on'));
    document.getElementById('priceMin').value = 0;
    document.getElementById('priceMax').value = 5000000;
    document.getElementById('searchInput').value = '';
    filtered = [...ALL_PRODUCTS];
    currentPage = 1;
    renderGrid();
    nevaToast('Đã xóa bộ lọc');
}

function applySort(rerender = true) {
    const v = document.getElementById('sortSel').value;
    if (v === 'price_asc') filtered.sort((a, b) => a.price - b.price);
    else if (v === 'price_desc') filtered.sort((a, b) => b.price - a.price);
    else if (v === 'popular') filtered.sort((a, b) => b.pop - a.pop);
    else filtered.sort((a, b) => b.id - a.id);
    if (rerender) { currentPage = 1; renderGrid(); }
}

// ── Checkbox select ──
function toggleSelect(id) {
    if (selected.has(id)) selected.delete(id);
    else selected.add(id);
    updateBulkBar();
    renderGrid();
}

function updateBulkBar() {
    const bar = document.getElementById('bulkBar');
    const info = document.getElementById('bulkInfo');
    if (selected.size > 0) {
        bar.classList.add('show');
        info.textContent = selected.size + ' sản phẩm được chọn';
    } else {
        bar.classList.remove('show');
    }
}

function addSelectedToCart() {
    nevaToast(`✦ Đã thêm ${selected.size} sản phẩm vào giỏ hàng!`, 'info');
    selected.clear();
    updateBulkBar();
    renderGrid();
}

// ── Modal ──
function openModal(id) {
    const p = ALL_PRODUCTS.find(x => x.id === id);
    currentModalId = id;
    document.getElementById('mImg').src = p.img;
    document.getElementById('mCat').textContent = p.cat + ' · ' + p.season;
    document.getElementById('mName').textContent = p.name;
    document.getElementById('mPrice').textContent = fmt(p.price);
    document.getElementById('mOld').textContent = p.old ? fmt(p.old) : '';
    document.getElementById('mDesc').textContent = p.desc;
    document.getElementById('mColors').innerHTML = p.colors.map((cl, i) => `
        <div class="mc-item ${i === 0 ? 'on' : ''}" onclick="selColor(this)">
            <div class="mc-circle" style="background:${cl.c};${cl.c === '#f5f0e8' ? 'box-shadow:inset 0 0 0 1px rgba(0,0,0,.12)' : ''}"></div>
            <span class="mc-name">${cl.n}</span>
        </div>`).join('');
    document.getElementById('mSizes').innerHTML = p.sizes.map((s, i) => `
        <div class="msz ${i === 1 ? 'on' : ''}" onclick="selSize(this)">${s}</div>`).join('');
    document.getElementById('mo').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeMo() {
    document.getElementById('mo').classList.remove('open');
    document.body.style.overflow = '';
}

function selSize(el) { document.querySelectorAll('.msz').forEach(s => s.classList.remove('on')); el.classList.add('on'); }
function selColor(el) { document.querySelectorAll('.mc-item').forEach(s => s.classList.remove('on')); el.classList.add('on'); }
function chQty(d) { const i = document.getElementById('qtyIn'); let v = parseInt(i.value) + d; if (v < 1) v = 1; i.value = v; }

function addCart() {
    const sz = document.querySelector('.msz.on');
    if (!sz) { nevaToast('Vui lòng chọn size!', 'info'); return; }
    const qty = parseInt(document.getElementById('qtyIn').value) || 1;
    closeMo();
    nevaToast(`✦ Đã thêm ${qty} sản phẩm vào giỏ hàng!`, 'ok');
}

// ── Event listeners ──
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.fc-item').forEach(item => {
        item.addEventListener('click', () => item.querySelector('.fc-box').classList.toggle('on'));
    });
    document.querySelectorAll('.sz-btn').forEach(b => b.addEventListener('click', () => b.classList.toggle('on')));
    document.querySelectorAll('.season-btn').forEach(b => b.addEventListener('click', () => b.classList.toggle('on')));
    renderGrid();
});