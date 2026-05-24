// NEVA Staff — Inventory & Excel Import JS
(function () {

    // ══════════════════════════════════
    //   MỞ / ĐÓNG POPUP IMPORT EXCEL
    // ══════════════════════════════════
    document.addEventListener('click', function (e) {
        if (e.target.id === 'btnImportExcel') {
            openExcelPopup();
        }
        if (e.target.id === 'excelMoClose' || e.target.id === 'excelMoCancel') {
            closeExcelPopup();
        }
        if (e.target.id === 'excelMo') closeExcelPopup();
    });

    function openExcelPopup() {
        const mo = document.getElementById('excelMo');
        if (mo) { mo.style.display = 'flex'; requestAnimationFrame(() => mo.classList.add('open')); }
        resetPopup();
    }

    function closeExcelPopup() {
        const mo = document.getElementById('excelMo');
        if (mo) { mo.classList.remove('open'); setTimeout(() => { if (!mo.classList.contains('open')) mo.style.display = 'none'; }, 300); }
    }

    function resetPopup() {
        const dz = document.getElementById('excelDropzone');
        const preview = document.getElementById('excelPreview');
        const fileInput = document.getElementById('excelFileInput');
        if (dz) dz.style.display = 'block';
        if (preview) preview.style.display = 'none';
        if (fileInput) fileInput.value = '';
        document.getElementById('btnConfirmImport') && (document.getElementById('btnConfirmImport').disabled = true);
    }

    // ══════════════════════════════════
    //   DROPZONE + FILE CHỌN
    // ══════════════════════════════════
    document.addEventListener('DOMContentLoaded', function () {
        const dz = document.getElementById('excelDropzone');
        const fi = document.getElementById('excelFileInput');

        if (!dz || !fi) return;

        dz.addEventListener('click', () => fi.click());

        dz.addEventListener('dragover', e => {
            e.preventDefault();
            dz.classList.add('dz-over');
        });

        dz.addEventListener('dragleave', () => dz.classList.remove('dz-over'));

        dz.addEventListener('drop', e => {
            e.preventDefault();
            dz.classList.remove('dz-over');
            const file = e.dataTransfer.files?.[0];
            if (file) processExcelFile(file);
        });

        fi.addEventListener('change', () => {
            const file = fi.files?.[0];
            if (file) processExcelFile(file);
        });
    });

    // ══════════════════════════════════
    //   PARSE EXCEL (SheetJS)
    // ══════════════════════════════════
    function processExcelFile(file) {
        // Kiểm tra định dạng
        const validExts = ['.xlsx', '.xls', '.csv'];
        const ext = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
        if (!validExts.includes(ext)) {
            showToast('Chỉ hỗ trợ file .xlsx, .xls, .csv!', 'err');
            return;
        }

        const reader = new FileReader();
        reader.onload = function (e) {
            try {
                const data = new Uint8Array(e.target.result);
                const workbook = XLSX.read(data, { type: 'array' });
                const sheet = workbook.Sheets[workbook.SheetNames[0]];
                const rows = XLSX.utils.sheet_to_json(sheet, { header: 1, defval: '' });

                if (!rows || rows.length < 2) {
                    showToast('File không có dữ liệu!', 'err');
                    return;
                }

                const parsed = detectAndParse(rows);
                showPreview(parsed, file.name);
            } catch (err) {
                showToast('Lỗi đọc file: ' + err.message, 'err');
            }
        };
        reader.readAsArrayBuffer(file);
    }

    // Tự động nhận diện cột (tên sản phẩm, size, số lượng)
    function detectAndParse(rows) {
        const header = rows[0].map(h => String(h).toLowerCase().trim());

        // Tìm chỉ số cột theo từ khóa
        function findCol(...keywords) {
            for (const kw of keywords) {
                const idx = header.findIndex(h => h.includes(kw));
                if (idx >= 0) return idx;
            }
            return -1;
        }

        const colName = findCol('tên', 'name', 'sản phẩm', 'product', 'sp');
        const colSize = findCol('size', 'cỡ', 'kích', 'loại');
        const colQty  = findCol('số lượng', 'quantity', 'qty', 'số lượng nhập', 'nhập', 'sl');

        const results = [];
        for (let i = 1; i < rows.length; i++) {
            const row = rows[i];
            if (!row || row.every(c => c === '')) continue;

            const name = colName >= 0 ? String(row[colName] || '').trim() : '';
            const size = colSize >= 0 ? String(row[colSize] || '').trim() : '';
            const qty  = colQty  >= 0 ? parseInt(row[colQty], 10) || 0 : 0;

            if (!name && !size && qty === 0) continue;

            results.push({
                rowIndex: i,
                productName: name || '(không rõ)',
                size: size || '-',
                qty: qty,
                matched: false, // sẽ kiểm tra với DB sau
                sizeId: null
            });
        }

        return {
            rows: results,
            colName,
            colSize,
            colQty,
            headerLabels: [
                colName >= 0 ? header[colName] : '?',
                colSize >= 0 ? header[colSize] : '?',
                colQty  >= 0 ? header[colQty]  : '?'
            ]
        };
    }

    let parsedData = null;

    function showPreview(parsed, fileName) {
        parsedData = parsed;

        const dz = document.getElementById('excelDropzone');
        const preview = document.getElementById('excelPreview');
        if (dz) dz.style.display = 'none';
        if (!preview) return;

        preview.style.display = 'block';

        const filenameEl = document.getElementById('previewFileName');
        if (filenameEl) filenameEl.textContent = fileName + ' — ' + parsed.rows.length + ' dòng nhận diện';

        const detectedEl = document.getElementById('previewDetected');
        if (detectedEl) {
            detectedEl.textContent = `Nhận diện cột: Tên SP → "${parsed.headerLabels[0]}" · Size → "${parsed.headerLabels[1]}" · SL → "${parsed.headerLabels[2]}"`;
        }

        const tbody = document.getElementById('previewTableBody');
        if (!tbody) return;

        tbody.innerHTML = '';
        parsed.rows.forEach((r, idx) => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td style="padding:7px 12px;border-bottom:1px solid rgba(201,168,76,.05);font-size:11px;color:rgba(250,248,242,.8)">${r.productName}</td>
                <td style="padding:7px 12px;border-bottom:1px solid rgba(201,168,76,.05);font-size:11px;color:rgba(250,248,242,.8)">${r.size}</td>
                <td style="padding:7px 12px;border-bottom:1px solid rgba(201,168,76,.05);font-size:11px">
                    <input type="number" min="0" value="${r.qty}"
                        style="width:80px;padding:4px 8px;border:1px solid var(--border);background:var(--dark);color:var(--white);font-size:11px;font-family:var(--font-b);outline:none"
                        onchange="window._updatePreviewQty(${idx}, this.value)" />
                </td>
            `;
            tbody.appendChild(tr);
        });

        const btn = document.getElementById('btnConfirmImport');
        if (btn) btn.disabled = false;
    }

    window._updatePreviewQty = function (idx, val) {
        if (parsedData && parsedData.rows[idx]) {
            parsedData.rows[idx].qty = parseInt(val, 10) || 0;
        }
    };

    // ── Xác nhận import → submit form ──
    document.addEventListener('click', function (e) {
        if (e.target.id === 'btnConfirmImport') {
            if (!parsedData || !parsedData.rows.length) return;
            submitImport(parsedData.rows);
        }
    });

    function submitImport(rows) {
        const form = document.getElementById('importStockForm');
        if (!form) { alert('Không tìm thấy form cập nhật!'); return; }

        // Xóa rows cũ
        form.querySelectorAll('.dynamic-row').forEach(el => el.remove());

        rows.forEach((r, idx) => {
            // Tạo hidden inputs
            const addHidden = (name, value) => {
                const inp = document.createElement('input');
                inp.type = 'hidden';
                inp.name = name;
                inp.value = value;
                inp.className = 'dynamic-row';
                form.appendChild(inp);
            };
            addHidden(`rows[${idx}].SizeId`, r.sizeId || 0);
            addHidden(`rows[${idx}].NewQty`, r.qty);
            addHidden(`rows[${idx}].ProductName`, r.productName);
            addHidden(`rows[${idx}].SizeName`, r.size);
        });

        closeExcelPopup();

        showToast(`Đang cập nhật ${rows.length} dòng tồn kho...`, 'info');
        form.submit();
    }

    // ── Toast helper ──
    function showToast(msg, type) {
        const toast = document.getElementById('toast');
        if (!toast) return;
        toast.textContent = msg;
        toast.className = 'toast ' + (type || 'info') + ' active';
        setTimeout(() => toast.classList.remove('active'), 3000);
    }

})();
