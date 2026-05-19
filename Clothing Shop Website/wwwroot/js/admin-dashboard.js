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

    let chartPie, chartBar;
    function renderPie(rows) {
        const ctx = document.getElementById('catPie');
        if (!ctx) return;
        const labels = (rows || []).map(r => r.label);
        const data = (rows || []).map(r => Number(r.amount) || 0);
        if (chartPie) chartPie.destroy();
        chartPie = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{
                    data,
                    backgroundColor: ['#c9a84c', '#8b7355', '#5c4d3a', '#3d3428', '#7a6a4a', '#a08b5a'],
                    borderColor: '#0a0908',
                    borderWidth: 2
                }]
            },
            options: {
                plugins: { legend: { labels: { color: text, font: { size: 10 } } } },
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
    // --- LOGIC XỬ LÝ AI PREDICTION (TIME SERIES) ---
    // ==========================================

    function fetchAIPrediction() {
        const btnDetail = document.getElementById('btnShowAiDetail');
        const resultText = document.getElementById('aiPredictionResult');
        const status = document.getElementById('aiStatus');

        if (!status || !resultText) return;

        status.innerText = "AI đang phân tích...";

        fetch(`/Admin/GetAIPrediction`)
            .then(res => res.json())
            .then(data => {
                status.innerText = "Hoàn tất";
                if (data.success) {
                    resultText.innerHTML = `<strong style="font-size:16px; color:#fff">${data.message}</strong>`;

                    if (btnDetail) {
                        btnDetail.style.display = 'block';
                        btnDetail.onclick = function () {
                            showAiProductDetail(data.categoryName, data.predictedQty);
                        };
                    }
                } else {
                    resultText.innerHTML = `<span style="color:var(--gold)">${data.message}</span>`;
                }
            })
            .catch(err => {
                status.innerText = "Lỗi kết nối";
                resultText.innerHTML = `<span style="color:#ff6b6b">Không thể lấy dữ liệu dự báo từ SSAS.</span>`;
            });
    }

    function showAiProductDetail(categoryName, predictedQty) {
        const modal = document.getElementById('aiDetailMo');
        const list = document.getElementById('aiProductList');
        const summary = document.getElementById('aiDetailSummary');

        // KIỂM TRA: Nếu thiếu HTML, bật cảnh báo ngay lập tức
        if (!modal || !list || !summary) {
            alert("Lỗi: Không tìm thấy HTML của bảng Modal AI! Vui lòng kiểm tra lại file Dashboard.cshtml");
            return;
        }

        summary.innerText = `Danh mục ${categoryName}: AI đề xuất nhập tổng ${predictedQty} sản phẩm dựa trên tỉ trọng 3 tháng qua.`;
        list.innerHTML = '<div style="text-align:center; padding:20px; color: var(--gold);">Đang tìm sản phẩm gánh team...</div>';

        // Bật hiển thị Modal
        modal.classList.add('active');

        // Gọi API lên C#
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
                list.innerHTML = `<div style="text-align:center; padding:20px; color: #ff6b6b;">Lỗi khi lấy dữ liệu sản phẩm từ Cube MDX. Vui lòng nhấn F12 để xem chi tiết.</div>`;
                console.error("Lỗi API GetImportSuggestionsForCategory:", err);
            });
    }

    // Đóng Modal AI
    document.addEventListener('click', function (e) {
        if (e.target.id === 'aiDetailClose' || e.target.id === 'aiDetailCancel') {
            const mod = document.getElementById('aiDetailMo');
            if (mod) mod.classList.remove('active');
        }
    });

    // Sự kiện khi bấm "Chọn" từ bảng gợi ý của AI (Dùng Vanilla JS để tránh lỗi $)
    document.addEventListener('click', function (e) {
        if (e.target && e.target.classList.contains('btn-fill-import')) {
            const btn = e.target;
            const pid = btn.getAttribute('data-pid');
            const pname = btn.getAttribute('data-pname');
            const qty = btn.getAttribute('data-qty');

            // Đóng modal AI
            const detailMo = document.getElementById('aiDetailMo');
            if (detailMo) detailMo.classList.remove('active');

            // Mở modal Nhập hàng gốc 
            const importModal = document.getElementById('importMo');
            if (importModal) importModal.classList.add('active');

            // Điền dữ liệu
            const impPid = document.getElementById('impProductId');
            const impPname = document.getElementById('impProductName');
            const impQty = document.getElementById('impQty');

            if (impPid) impPid.value = pid;
            if (impPname) impPname.value = pname;
            if (impQty) impQty.value = qty;

            // Bật Toast thông báo
            const toast = document.getElementById('toast');
            if (toast) {
                toast.innerText = "AI đã tự động điền số lượng dự đoán!";
                toast.classList.add('active');
                setTimeout(() => toast.classList.remove('active'), 3000);
            }
        }
    });

    // ==========================================
    // --- GỘP CHUNG VÀO 1 SỰ KIỆN KHỞI TẠO TRANG ---
    // ==========================================
    document.addEventListener('DOMContentLoaded', function () {
        const p = readPayload();
        renderBars6m(p.revenue6m || []);
        renderPie(p.catPie || []);
        renderAgeBar(p.ageRev || []);
        wireFilters();

        // Chạy AI ngay khi trang vừa load xong
        fetchAIPrediction();

        if (window.NevaAdminImportModal) window.NevaAdminImportModal.init({ bindTopSellList: true });
    });

})();