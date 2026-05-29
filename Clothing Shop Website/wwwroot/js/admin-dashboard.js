(function () {
    const gold = '#c9a84c';
    const goldDim = 'rgba(201,168,76,0.35)';
    const text = 'rgba(250,248,242,0.75)';

    function readPayload() {
        const el = document.getElementById('dashPayload');
        if (!el) return {};
        try { return JSON.parse(el.textContent); } catch { return {}; }
    }

    function renderBars6m(points) {
        const wrap = document.getElementById('chart6m');
        if (!wrap) return;
        wrap.innerHTML = '';
        if (!points || !points.length) return;
        const max = Math.max(...points.map(p => Number(p.revenue) || 0), 1);
        points.forEach(p => {
            const v = Number(p.revenue) || 0;
            const h = Math.max(4, (v / max) * 118);
            const d = document.createElement('div');
            d.className = 'bg';
            d.innerHTML = '<div class="bar" style="height:' + h + 'px"><div class="bar-tip">' + v.toLocaleString('vi-VN') + 'đ</div></div><div class="blbl">' + (p.label || '') + '</div>';
            wrap.appendChild(d);
        });
    }

    const pieColors = ['#c9a84c', '#8b7355', '#5c4d3a', '#3d3428', '#7a6a4a', '#a08b5a', '#c4b896', '#6b5c48', '#9a8468', '#4a3f32'];

    function renderPieLegend(rows, colors) {
        const el = document.getElementById('catPieLegend');
        if (!el) return;
        const total = (rows || []).reduce((s, r) => s + (Number(r.amount) || 0), 0);
        if (!rows || !rows.length || total <= 0) {
            el.innerHTML = '<div class="cat-pie-legend-empty">Chưa có dữ liệu danh mục.</div>';
            return;
        }
        el.innerHTML = rows.map((r, i) => {
            const amt = Number(r.amount) || 0;
            const pct = total > 0 ? (amt / total * 100).toFixed(1) : '0';
            const color = colors[i % colors.length];
            const label = String(r.label || '—').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            return '<div class="cat-pie-legend-item">' +
                '<span class="cat-pie-legend-swatch" style="background:' + color + '"></span>' +
                '<div class="cat-pie-legend-body">' +
                '<div class="cat-pie-legend-label">' + label + '</div>' +
                '<div class="cat-pie-legend-amt">' + amt.toLocaleString('vi-VN') + 'đ · ' + pct + '%</div>' +
                '</div></div>';
        }).join('');
    }

    let chartPie, chartBar;
    function renderPie(rows) {
        const ctx = document.getElementById('catPie');
        if (!ctx) return;
        const labels = (rows || []).map(r => r.label);
        const data = (rows || []).map(r => Number(r.amount) || 0);
        const colors = data.map((_, i) => pieColors[i % pieColors.length]);
        renderPieLegend(rows, colors);
        if (chartPie) chartPie.destroy();
        chartPie = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{
                    data,
                    backgroundColor: colors,
                    borderColor: '#0a0908',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: { legend: { display: false } },
                cutout: '58%'
            }
        });
    }

    function renderAgeBar(rows) {
        const ctx = document.getElementById('ageBar');
        if (!ctx) return;
        const labels = (rows || []).map(r => r.label);
        const data = (rows || []).map(r => Number(r.amount) || 0);
        if (chartBar) chartBar.destroy();
        chartBar = new Chart(ctx, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: 'Doanh thu',
                    data,
                    backgroundColor: goldDim,
                    borderColor: gold,
                    borderWidth: 1
                }]
            },
            options: {
                scales: {
                    x: { ticks: { color: text, maxRotation: 45 }, grid: { color: 'rgba(255,255,255,.06)' } },
                    y: { ticks: { color: text }, grid: { color: 'rgba(255,255,255,.06)' } }
                },
                plugins: { legend: { display: false } }
            }
        });
    }

    function applyFiltersNav() {
        const s = document.getElementById('fltSeason');
        const a = document.getElementById('fltAge');
        if (!s || !a) return;
        const qs = new URLSearchParams();
        if (s.value) qs.set('season', s.value);
        if (a.value) qs.set('ageGroup', a.value);
        const q = qs.toString();
        window.location.href = q ? ('?' + q) : window.location.pathname;
    }

    function wireFilters() {
        const p = readPayload();
        const s = document.getElementById('fltSeason');
        const a = document.getElementById('fltAge');
        if (s && p.season) s.value = p.season;
        if (a && p.age) a.value = p.age;
        if (s) s.addEventListener('change', applyFiltersNav);
        if (a) a.addEventListener('change', applyFiltersNav);
    }

    // ==========================================
    // --- AI PREDICTION với chọn số tháng ---
    // ==========================================

    function getSelectedMonths() {
        const sel = document.getElementById('aiMonthsSelect');
        return sel ? parseInt(sel.value, 10) || 3 : 3;
    }

    function fetchAIPrediction() {
        const btnDetail = document.getElementById('btnShowAiDetail');
        const resultText = document.getElementById('aiPredictionResult');
        const status = document.getElementById('aiStatus');
        if (!status || !resultText) return;

        const months = getSelectedMonths();
        status.innerText = 'AI đang phân tích...';

        fetch('/Admin/GetAIPrediction?months=' + months)
            .then(res => res.json())
            .then(data => {
                status.innerText = 'Hoàn tất';
                if (data.success) {
                    resultText.innerHTML = `<strong style="font-size:16px; color:#fff">${data.message}</strong>`;
                    if (btnDetail) {
                        btnDetail.style.display = 'block';
                        btnDetail.onclick = function () {
                            showAiProductDetail(data.categoryName, data.predictedQty, data.months || months);
                        };
                    }
                } else {
                    resultText.innerHTML = `<span style="color:var(--gold)">${data.message}</span>`;
                    if (btnDetail) btnDetail.style.display = 'none';
                }
            })
            .catch(() => {
                status.innerText = 'Lỗi kết nối';
                resultText.innerHTML = '<span style="color:#ff6b6b">Không thể lấy dữ liệu dự báo từ SSAS.</span>';
            });
    }

    function showAiProductDetail(categoryName, predictedQty, months) {
        const modal = document.getElementById('aiDetailMo');
        const list = document.getElementById('aiProductList');
        const summary = document.getElementById('aiDetailSummary');

        if (!modal || !list || !summary) {
            alert('Lỗi: Không tìm thấy HTML Modal AI!');
            return;
        }

        summary.innerText = `Danh mục ${categoryName}: AI đề xuất nhập tổng ${predictedQty} sản phẩm trong ${months || 3} tháng tới, phân bổ theo tỉ trọng bán hàng lịch sử.`;
        list.innerHTML = '<div style="text-align:center; padding:20px; color: var(--gold);">Đang tìm sản phẩm gánh team...</div>';
        modal.classList.add('open');

        fetch(`/Admin/GetImportSuggestionsForCategory?categoryName=${encodeURIComponent(categoryName)}&aiPredictedQuantity=${predictedQty}`)
            .then(res => res.json())
            .then(res => {
                if (!res.success) {
                    list.innerHTML = `<div style="text-align:center; padding:20px; color: #ff6b6b;">${res.message}</div>`;
                    return;
                }
                list.innerHTML = '';
                res.data.forEach(p => {
                    const item = document.createElement('div');
                    item.className = 'prod-item';
                    item.innerHTML = `
                        <div class="prod-thumb"><img src="${p.imageUrl || '/images/placeholder-product.svg'}" /></div>
                        <div style="flex:1">
                            <div class="prod-name">${p.productName}</div>
                            <div class="prod-cat">Đã bán: ${p.historySold} · <span style="color:var(--gold)">Gợi ý nhập: ${p.suggestImport}</span></div>
                        </div>
                        <button type="button" class="act-btn btn-fill-import" data-pid="${p.productId}" data-pname="${p.productName}" data-qty="${p.suggestImport}">Chọn</button>
                    `;
                    list.appendChild(item);
                });
            })
            .catch(err => {
                list.innerHTML = '<div style="text-align:center; padding:20px; color: #ff6b6b;">Lỗi khi lấy dữ liệu từ Cube MDX.</div>';
                console.error('Lỗi API GetImportSuggestionsForCategory:', err);
            });
    }

    document.addEventListener('click', function (e) {
        if (e.target.id === 'aiDetailClose' || e.target.id === 'aiDetailCancel') {
            const mod = document.getElementById('aiDetailMo');
            if (mod) mod.classList.remove('open');
        }
    });

    document.addEventListener('click', function (e) {
        if (e.target && e.target.classList.contains('btn-fill-import')) {
            const btn = e.target;
            const pid = btn.getAttribute('data-pid');
            const pname = btn.getAttribute('data-pname');
            const qty = btn.getAttribute('data-qty');

            const detailMo = document.getElementById('aiDetailMo');
            if (detailMo) detailMo.classList.remove('open');

            const importModal = document.getElementById('importMo');
            if (importModal) importModal.classList.add('open');

            const impPid = document.getElementById('impProductId');
            const impPname = document.getElementById('impProductName');
            const impQty = document.getElementById('impQty');

            if (impPid) impPid.value = pid;
            if (impPname) impPname.value = pname;
            if (impQty) impQty.value = qty;

            const toast = document.getElementById('toast');
            if (toast) {
                toast.innerText = 'AI đã tự động điền số lượng dự đoán!';
                toast.classList.add('active');
                setTimeout(() => toast.classList.remove('active'), 3000);
            }
        }
    });

    // Re-fetch khi thay đổi số tháng dự đoán
    document.addEventListener('change', function (e) {
        if (e.target && e.target.id === 'aiMonthsSelect') {
            const btnDetail = document.getElementById('btnShowAiDetail');
            if (btnDetail) btnDetail.style.display = 'none';
            fetchAIPrediction();
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        const p = readPayload();
        renderBars6m(p.revenue6m || []);
        renderPie(p.catPie || []);
        renderAgeBar(p.ageRev || []);
        wireFilters();
        fetchAIPrediction();
        if (window.NevaAdminImportModal) window.NevaAdminImportModal.init({ bindTopSellList: true });
    });

})();
