// NEVA Admin — phiếu nhập hàng (nhiều sản phẩm)
(function () {
    let productCatalog = [];
    let lineSeq = 0;

    function token() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }

    function fmtMoney(n) {
        return Number(n || 0).toLocaleString('vi-VN') + 'đ';
    }

    function calcModalTotal() {
        let total = 0;
        document.querySelectorAll('.imp-line-row').forEach(row => {
            const price = parseFloat(row.querySelector('.imp-line-price')?.value) || 0;
            const s = parseInt(row.querySelector('.imp-line-s')?.value, 10) || 0;
            const m = parseInt(row.querySelector('.imp-line-m')?.value, 10) || 0;
            const l = parseInt(row.querySelector('.imp-line-l')?.value, 10) || 0;
            total += price * (s + m + l);
        });
        const el = document.getElementById('impTotalAmount');
        if (el) el.textContent = fmtMoney(total);
        return total;
    }

    function buildProductOptions(selectedId) {
        return productCatalog.map(p =>
            `<option value="${p.id}" ${String(p.id) === String(selectedId) ? 'selected' : ''}>${escapeHtml(p.name)}</option>`
        ).join('');
    }

    function escapeHtml(s) {
        return String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }

    function addLineRow(data) {
        const wrap = document.getElementById('impLinesWrap');
        if (!wrap) return;
        const id = ++lineSeq;
        const row = document.createElement('div');
        row.className = 'imp-line-row';
        row.dataset.lineId = String(id);
        row.innerHTML = `
            <div class="imp-line-head">
                <select class="modal-inp imp-line-product" required>
                    <option value="">— Chọn sản phẩm —</option>
                    ${buildProductOptions(data?.productId || '')}
                </select>
                <button type="button" class="ia-btn del imp-line-remove" title="Xóa dòng">×</button>
            </div>
            <div class="imp-line-grid">
                <div>
                    <span class="imp-line-lbl">Giá gốc (đ)</span>
                    <input type="number" class="modal-inp imp-line-price" step="0.01" min="0" value="${data?.importPrice ?? ''}" required />
                </div>
                <div>
                    <span class="imp-line-lbl">Size S</span>
                    <input type="number" class="modal-inp imp-line-s" min="0" step="1" value="${data?.stockS ?? 0}" />
                </div>
                <div>
                    <span class="imp-line-lbl">Size M</span>
                    <input type="number" class="modal-inp imp-line-m" min="0" step="1" value="${data?.stockM ?? 0}" />
                </div>
                <div>
                    <span class="imp-line-lbl">Size L</span>
                    <input type="number" class="modal-inp imp-line-l" min="0" step="1" value="${data?.stockL ?? 0}" />
                </div>
            </div>`;
        wrap.appendChild(row);
        row.querySelector('.imp-line-remove')?.addEventListener('click', () => {
            row.remove();
            calcModalTotal();
        });
        row.querySelectorAll('input, select').forEach(el => el.addEventListener('input', calcModalTotal));
        calcModalTotal();
    }

    async function loadProducts() {
        const r = await fetch('/Admin/GetAllProducts', { credentials: 'same-origin' });
        const data = await r.json();
        productCatalog = (data.data || []).map(p => ({ id: p.id, name: p.name }));
    }

    async function openImportModal() {
        const mo = document.getElementById('importMo');
        if (!mo) return;
        if (productCatalog.length === 0) await loadProducts();
        document.getElementById('impSupplierSelect').value = '';
        const wrap = document.getElementById('impLinesWrap');
        if (wrap) wrap.innerHTML = '';
        addLineRow({});
        calcModalTotal();
        mo.classList.add('open');
    }

    function closeImportModal() {
        document.getElementById('importMo')?.classList.remove('open');
    }

    function collectLines() {
        const lines = [];
        document.querySelectorAll('.imp-line-row').forEach(row => {
            const productId = parseInt(row.querySelector('.imp-line-product')?.value, 10);
            if (!productId) return;
            lines.push({
                productId,
                importPrice: parseFloat(row.querySelector('.imp-line-price')?.value) || 0,
                stockS: parseInt(row.querySelector('.imp-line-s')?.value, 10) || 0,
                stockM: parseInt(row.querySelector('.imp-line-m')?.value, 10) || 0,
                stockL: parseInt(row.querySelector('.imp-line-l')?.value, 10) || 0
            });
        });
        return lines;
    }

    async function submitReceipt(exportXlsx) {
        const supplierId = parseInt(document.getElementById('impSupplierSelect')?.value, 10);
        if (!supplierId) {
            window.showToast?.('Chọn nhà cung cấp.', 'err');
            return;
        }
        const lines = collectLines();
        if (lines.length < 1) {
            window.showToast?.('Thêm ít nhất một sản phẩm.', 'err');
            return;
        }
        for (const ln of lines) {
            if (ln.stockS + ln.stockM + ln.stockL < 1) {
                window.showToast?.('Mỗi sản phẩm cần nhập ít nhất 1 size.', 'err');
                return;
            }
        }

        const btn = document.getElementById('impSubmitBtn');
        if (btn) { btn.disabled = true; btn.style.opacity = '0.6'; }

        const fd = new FormData();
        fd.append('__RequestVerificationToken', token());
        fd.append('supplierId', String(supplierId));
        fd.append('linesJson', JSON.stringify(lines));

        try {
            const r = await fetch('/Admin/CreateImportReceipt', { method: 'POST', body: fd, credentials: 'same-origin' });
            const data = await r.json();
            if (!data.success) {
                window.showToast?.(data.message || 'Lỗi tạo phiếu.', 'err');
                return;
            }
            closeImportModal();
            window.showToast?.('✓ ' + (data.message || 'Đã tạo phiếu.'), 'ok');
            if (exportXlsx && data.receiptId) {
                window.location.href = '/Admin/ExportReceipt?receiptId=' + encodeURIComponent(data.receiptId);
            } else {
                window.location.href = '/Admin/Products#imports';
            }
        } catch {
            window.showToast?.('Lỗi kết nối.', 'err');
        } finally {
            if (btn) { btn.disabled = false; btn.style.opacity = '1'; }
        }
    }

    async function viewReceipt(id) {
        const mo = document.getElementById('receiptViewMo');
        if (!mo) return;
        try {
            const r = await fetch('/Admin/GetImportReceipt?id=' + encodeURIComponent(id), { credentials: 'same-origin' });
            const data = await r.json();
            if (!data.success || !data.receipt) {
                window.showToast?.(data.message || 'Không tải được phiếu.', 'err');
                return;
            }
            const rc = data.receipt;
            document.getElementById('rvTitle').textContent = 'Phiếu #' + String(rc.id).padStart(6, '0');
            document.getElementById('rvMeta').innerHTML =
                `<div><span>Ngày nhập</span><strong>${escapeHtml(rc.importDate)}</strong></div>` +
                `<div><span>Nhà cung cấp</span><strong>${escapeHtml(rc.supplierName)}</strong></div>` +
                `<div><span>Size S / M / L</span><strong>${rc.sizeS} / ${rc.sizeM} / ${rc.sizeL}</strong></div>` +
                `<div><span>Tổng tiền</span><strong style="color:var(--gold)">${fmtMoney(rc.totalAmount)}</strong></div>`;
            const tbody = document.getElementById('rvBody');
            tbody.innerHTML = (rc.lines || []).map(ln =>
                `<tr><td>${escapeHtml(ln.productName)}</td><td style="text-align:center">${escapeHtml(ln.sizeName)}</td>` +
                `<td style="text-align:center">${ln.quantity}</td><td style="text-align:right">${Number(ln.importPrice).toLocaleString('vi-VN')}đ</td>` +
                `<td style="text-align:right;color:var(--gold)">${Number(ln.lineTotal).toLocaleString('vi-VN')}đ</td></tr>`
            ).join('');
            mo.classList.add('open');
        } catch {
            window.showToast?.('Lỗi kết nối.', 'err');
        }
    }

    async function deleteReceipt(id) {
        if (!confirm('Xóa phiếu nhập #' + String(id).padStart(6, '0') + '? Tồn kho sẽ được trừ tương ứng.')) return;
        const fd = new FormData();
        fd.append('__RequestVerificationToken', token());
        fd.append('receiptId', String(id));
        try {
            const r = await fetch('/Admin/DeleteImportReceipt', { method: 'POST', body: fd, credentials: 'same-origin' });
            const data = await r.json();
            if (!data.success) {
                window.showToast?.(data.message || 'Không xóa được.', 'err');
                return;
            }
            window.showToast?.('✓ ' + data.message, 'ok');
            window.location.reload();
        } catch {
            window.showToast?.('Lỗi kết nối.', 'err');
        }
    }

    function openSupplierModal(mode, supplierId) {
        const mo = document.getElementById('supplierMo');
        const form = document.getElementById('supplierForm');
        if (!mo || !form) return;
        document.getElementById('supplierMoTitle').textContent = mode === 'edit' ? 'Sửa nhà cung cấp' : 'Thêm nhà cung cấp';
        document.getElementById('supEditId').value = mode === 'edit' ? String(supplierId || '') : '';
        form.action = mode === 'edit' ? '/Admin/EditSupplier' : '/Admin/AddSupplier';
        if (mode === 'add') {
            form.reset();
            document.getElementById('supEditId').value = '';
        }
        mo.classList.add('open');
    }

    async function editSupplier(id) {
        try {
            const r = await fetch('/Admin/GetSupplier?id=' + encodeURIComponent(id), { credentials: 'same-origin' });
            const data = await r.json();
            if (!data.success || !data.supplier) {
                window.showToast?.(data.message || 'Không tải được NCC.', 'err');
                return;
            }
            const s = data.supplier;
            openSupplierModal('edit', s.id);
            document.getElementById('supName').value = s.name || '';
            document.getElementById('supPhone').value = s.phone || '';
            document.getElementById('supCity').value = s.city || '';
            document.getElementById('supCountry').value = s.country || '';
        } catch {
            window.showToast?.('Lỗi kết nối.', 'err');
        }
    }

    async function deleteSupplier(id, name) {
        if (!confirm('Xóa nhà cung cấp "' + name + '"?')) return;
        const fd = new FormData();
        fd.append('__RequestVerificationToken', token());
        fd.append('supplierId', String(id));
        try {
            const r = await fetch('/Admin/DeleteSupplier', { method: 'POST', body: fd, credentials: 'same-origin' });
            const data = await r.json();
            if (!data.success) {
                window.showToast?.(data.message || 'Không xóa được.', 'err');
                return;
            }
            window.showToast?.('✓ ' + data.message, 'ok');
            window.location.reload();
        } catch {
            window.showToast?.('Lỗi kết nối.', 'err');
        }
    }

    function init() {
        document.getElementById('btnOpenImportReceipt')?.addEventListener('click', openImportModal);
        document.getElementById('importClose')?.addEventListener('click', closeImportModal);
        document.getElementById('importCancel')?.addEventListener('click', closeImportModal);
        document.getElementById('importMo')?.addEventListener('click', e => { if (e.target.id === 'importMo') closeImportModal(); });

        document.getElementById('btnAddImpLine')?.addEventListener('click', () => addLineRow({}));
        document.getElementById('impSubmitBtn')?.addEventListener('click', e => { e.preventDefault(); submitReceipt(true); });
        document.getElementById('impSaveBtn')?.addEventListener('click', e => { e.preventDefault(); submitReceipt(false); });

        document.getElementById('btnOpenAddSupplier')?.addEventListener('click', () => openSupplierModal('add'));
        document.getElementById('supplierClose')?.addEventListener('click', () => document.getElementById('supplierMo')?.classList.remove('open'));
        document.getElementById('supplierCancel')?.addEventListener('click', () => document.getElementById('supplierMo')?.classList.remove('open'));
        document.getElementById('supplierMo')?.addEventListener('click', e => { if (e.target.id === 'supplierMo') e.target.classList.remove('open'); });

        document.querySelectorAll('.btn-view-receipt').forEach(btn => {
            btn.addEventListener('click', () => viewReceipt(btn.getAttribute('data-id')));
        });
        document.querySelectorAll('.btn-del-receipt').forEach(btn => {
            btn.addEventListener('click', () => deleteReceipt(btn.getAttribute('data-id')));
        });
        document.querySelectorAll('.btn-edit-supplier').forEach(btn => {
            btn.addEventListener('click', () => editSupplier(btn.getAttribute('data-id')));
        });
        document.querySelectorAll('.btn-del-supplier').forEach(btn => {
            btn.addEventListener('click', () => deleteSupplier(btn.getAttribute('data-id'), btn.getAttribute('data-name') || ''));
        });

        document.getElementById('receiptViewClose')?.addEventListener('click', () => document.getElementById('receiptViewMo')?.classList.remove('open'));
        document.getElementById('receiptViewMo')?.addEventListener('click', e => { if (e.target.id === 'receiptViewMo') e.target.classList.remove('open'); });

        if (window.location.hash === '#imports') {
            const tabBtn = document.querySelector('.atab[onclick*="imports"]');
            if (tabBtn) switchTab('imports', tabBtn);
        }
    }

    window.NevaImportReceipt = { init, openImportModal, viewReceipt };
})();
