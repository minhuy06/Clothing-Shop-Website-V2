// NEVA Admin — Dashboard (cube MDX + Chart.js)
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

    document.addEventListener('DOMContentLoaded', function () {
        const p = readPayload();
        renderBars6m(p.revenue6m || []);
        renderPie(p.catPie || []);
        renderAgeBar(p.ageRev || []);
        wireFilters();
        if (window.NevaAdminImportModal) window.NevaAdminImportModal.init({ bindTopSellList: true });
    });
})();
