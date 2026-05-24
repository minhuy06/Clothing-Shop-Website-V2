// NEVA Staff — Statistics JS
(function () {
    const gold = '#c9a84c';
    const goldDim = 'rgba(201,168,76,0.35)';
    const text = 'rgba(250,248,242,0.75)';

    let chartRev, chartCat;

    function readPayload() {
        const el = document.getElementById('statsPayload');
        if (!el) return {};
        try { return JSON.parse(el.textContent); } catch { return {}; }
    }

    function renderRevenue(points) {
        const ctx = document.getElementById('revChart');
        if (!ctx) return;
        const labels = (points || []).map(p => p.label);
        const data = (points || []).map(p => Number(p.revenue) || 0);
        if (chartRev) chartRev.destroy();
        chartRev = new Chart(ctx, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: 'Doanh thu (đ)',
                    data,
                    backgroundColor: goldDim,
                    borderColor: gold,
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                scales: {
                    x: { ticks: { color: text }, grid: { color: 'rgba(255,255,255,.06)' } },
                    y: { ticks: { color: text, callback: v => v.toLocaleString('vi-VN') + 'đ' }, grid: { color: 'rgba(255,255,255,.06)' } }
                },
                plugins: { legend: { display: false } }
            }
        });
    }

    function renderCatPie(rows) {
        const ctx = document.getElementById('catChart');
        if (!ctx) return;
        const labels = (rows || []).map(r => r.label);
        const data = (rows || []).map(r => Number(r.amount) || 0);
        if (chartCat) chartCat.destroy();
        chartCat = new Chart(ctx, {
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
                cutout: '55%'
            }
        });
    }

    // Export to CSV via server
    document.addEventListener('click', function (e) {
        if (e.target.id === 'btnExportReport' || e.target.closest('#btnExportReport')) {
            window.location.href = '/Staff/ExportReport';
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        const p = readPayload();
        renderRevenue(p.revenue6m || []);
        renderCatPie(p.catPie || []);
    });

})();
