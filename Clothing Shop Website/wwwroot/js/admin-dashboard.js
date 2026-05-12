// ==========================================
//   NEVA Admin — Dashboard JS
// ==========================================

document.addEventListener('DOMContentLoaded', function () {
    // Render biểu đồ doanh thu
    const data = [
        { m: 'T12', v: 52 }, { m: 'T1', v: 48 }, { m: 'T2', v: 61 },
        { m: 'T3', v: 70 }, { m: 'T4', v: 65 }, { m: 'T5', v: 84 }
    ];
    const max = Math.max(...data.map(d => d.v));
    const chart = document.getElementById('chart');
    if (chart) {
        chart.innerHTML = data.map(d => `
            <div class="bg">
                <div class="bar" style="height:${(d.v / max) * 118}px">
                    <div class="bar-tip">${d.v}M đ</div>
                </div>
                <div class="blbl">${d.m}</div>
            </div>`).join('');
    }
});