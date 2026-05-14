// NEVA Admin — modal nhập hàng / thêm sản phẩm (dùng chung Dashboard + Products)
(function () {
    let lastProd = [], lastSup = [];

    function escapeHtml(s) {
        return String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }

    function openImport(row) {
        const mo = document.getElementById('importMo');
        const form = document.getElementById('importForm');
        if (!mo || !form) return;
        document.getElementById('impProductId').value = row.pid || '';
        document.getElementById('impProductName').value = row.pname || '';
        const catEl = document.getElementById('impCategoryId');
        if (catEl) catEl.value = row.catId || catEl.value;
        document.getElementById('impSession').value = row.seasonNum || '1';
        document.getElementById('impSalePrice').value = row.price || '';
        document.getElementById('impOriginalPrice').value = row.orig || '';
        document.getElementById('impQty').value = '1';
        document.getElementById('impSize').value = '';
        document.getElementById('impFile').value = '';
        document.getElementById('impSupplierId').value = '';
        document.getElementById('impSupplierName').value = '';
        const dz = document.getElementById('dropzone');
        const fi = document.getElementById('impFile');
        if (dz && fi && !fi.files?.length) {
            const sp = dz.querySelector('span');
            if (sp) sp.textContent = 'Kéo thả hoặc bấm để chọn';
        }
        mo.classList.add('open');
    }

    function closeImport() {
        const mo = document.getElementById('importMo');
        if (mo) mo.classList.remove('open');
    }

    async function suggestProducts(q) {
        const dl = document.getElementById('prodSuggest');
        if (!dl) return;
        const r = await fetch('/Admin/SearchProducts?q=' + encodeURIComponent(q));
        lastProd = await r.json();
        dl.innerHTML = lastProd.map(p => '<option value="' + escapeHtml(p.name) + '">').join('');
    }

    async function suggestSuppliers(q) {
        const dl = document.getElementById('supSuggest');
        if (!dl) return;
        const r = await fetch('/Admin/SearchSuppliers?q=' + encodeURIComponent(q));
        lastSup = await r.json();
        dl.innerHTML = lastSup.map(s => '<option value="' + escapeHtml(s.name) + '">').join('');
    }

    /**
     * @param {{ bindTopSellList?: boolean, openEmptyButtonId?: string }} opts
     */
    function init(opts) {
        opts = opts || {};
        const mo = document.getElementById('importMo');
        const form = document.getElementById('importForm');
        if (!mo || !form) return;

        if (opts.bindTopSellList !== false) {
            document.querySelectorAll('#topSellList .btn-import').forEach(btn => {
                btn.addEventListener('click', () => {
                    const row = btn.closest('.prod-item');
                    if (!row) return;
                    const seasonMap = { 'Xuân': '1', 'Hạ': '2', 'Thu': '3', 'Đông': '4' };
                    const sn = seasonMap[(row.dataset.season || '').trim()] || '1';
                    openImport({
                        pid: row.dataset.pid,
                        pname: row.dataset.pname,
                        catId: row.dataset.catid,
                        seasonNum: sn,
                        price: row.dataset.price,
                        orig: row.dataset.orig
                    });
                });
            });
        }

        if (opts.openEmptyButtonId) {
            document.getElementById(opts.openEmptyButtonId)?.addEventListener('click', () => {
                openImport({ pid: '', pname: '', catId: '', seasonNum: '1', price: '', orig: '' });
            });
        }

        document.getElementById('importClose')?.addEventListener('click', closeImport);
        document.getElementById('importCancel')?.addEventListener('click', closeImport);
        mo.addEventListener('click', e => { if (e.target === mo) closeImport(); });

        let tmr;
        document.getElementById('impProductName')?.addEventListener('input', e => {
            clearTimeout(tmr);
            const v = e.target.value.trim();
            tmr = setTimeout(() => { if (v.length >= 1) suggestProducts(v); }, 220);
        });
        let tmr2;
        document.getElementById('impSupplierName')?.addEventListener('input', e => {
            clearTimeout(tmr2);
            const v = e.target.value.trim();
            tmr2 = setTimeout(() => { if (v.length >= 1) suggestSuppliers(v); }, 220);
        });

        form.addEventListener('submit', (e) => {
            const pn = document.getElementById('impProductName').value.trim();
            const m = lastProd.find(p => p.name === pn);
            document.getElementById('impProductId').value = m ? m.id : '';
            const sn = document.getElementById('impSupplierName').value.trim();
            const ms = lastSup.find(s => s.name === sn);
            document.getElementById('impSupplierId').value = ms ? ms.id : '';
            if (!document.getElementById('impSupplierId').value) {
                alert('Chọn nhà cung cấp hợp lệ từ gợi ý (gõ tên và chọn khớp).');
                e.preventDefault();
            }
        });

        const dz = document.getElementById('dropzone');
        const fi = document.getElementById('impFile');
        dz?.addEventListener('click', () => fi?.click());
        dz?.addEventListener('dragover', e => { e.preventDefault(); dz.classList.add('dz-over'); });
        dz?.addEventListener('dragleave', () => dz.classList.remove('dz-over'));
        dz?.addEventListener('drop', e => {
            e.preventDefault();
            dz.classList.remove('dz-over');
            if (e.dataTransfer.files?.[0]) {
                fi.files = e.dataTransfer.files;
                dz.querySelector('span').textContent = fi.files[0].name;
            }
        });
        fi?.addEventListener('change', () => {
            if (fi.files?.[0] && dz) dz.querySelector('span').textContent = fi.files[0].name;
        });
    }

    window.NevaAdminImportModal = { init, openImport, closeImport };
})();
