// Tìm & chọn sản phẩm khi thêm quảng cáo (ProductID)
(function () {
    const search = document.getElementById('adProductSearch');
    const hidden = document.getElementById('adProductId');
    const results = document.getElementById('adProductResults');
    const selected = document.getElementById('adProductSelected');
    const selectedText = document.getElementById('adProductSelectedText');
    const clearBtn = document.getElementById('adProductClear');
    if (!search || !hidden || !results) return;

    let timer = null;

    function fmtPrice(n) {
        return Number(n).toLocaleString('vi-VN') + 'đ';
    }

    function hideResults() {
        results.hidden = true;
        results.innerHTML = '';
    }

    function selectProduct(item) {
        hidden.value = item.id;
        search.value = '';
        search.style.display = 'none';
        selected.hidden = false;
        const statusNote = item.status !== 1 ? ' · chưa hiển thị' : '';
        selectedText.textContent = `#${item.id} · ${item.name} · ${fmtPrice(item.price)}${statusNote}`;
        hideResults();
    }

    function clearSelection() {
        hidden.value = '';
        search.value = '';
        search.style.display = '';
        selected.hidden = true;
        hideResults();
    }

    function renderResults(items) {
        if (!items.length) {
            results.innerHTML = '<div class="ad-product-empty">Không tìm thấy sản phẩm</div>';
            results.hidden = false;
            return;
        }
        results.innerHTML = items.map(p => `
            <button type="button" class="ad-product-opt" data-id="${p.id}">
                <span class="ad-product-opt-name">#${p.id} · ${escapeHtml(p.name)}</span>
                <span class="ad-product-opt-meta">${escapeHtml(p.category || '')} · ${fmtPrice(p.price)}${p.status !== 1 ? ' · ẩn' : ''}</span>
            </button>`).join('');
        results.hidden = false;
        results.querySelectorAll('.ad-product-opt').forEach(btn => {
            btn.addEventListener('click', () => {
                const id = parseInt(btn.getAttribute('data-id'), 10);
                const item = items.find(x => x.id === id);
                if (item) selectProduct(item);
            });
        });
    }

    function escapeHtml(s) {
        return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
    }

    async function doSearch(q) {
        const url = '/Admin/SearchProductsForAd?q=' + encodeURIComponent(q || '');
        try {
            const res = await fetch(url, { headers: { Accept: 'application/json' } });
            if (!res.ok) return;
            const data = await res.json();
            renderResults(data);
        } catch (_) { /* ignore */ }
    }

    search.addEventListener('input', () => {
        clearTimeout(timer);
        timer = setTimeout(() => doSearch(search.value.trim()), 280);
    });

    search.addEventListener('focus', () => {
        if (!results.innerHTML) doSearch(search.value.trim());
        else results.hidden = false;
    });

    document.addEventListener('click', e => {
        if (!e.target.closest('.ad-product-picker')) hideResults();
    });

    clearBtn?.addEventListener('click', clearSelection);
})();
