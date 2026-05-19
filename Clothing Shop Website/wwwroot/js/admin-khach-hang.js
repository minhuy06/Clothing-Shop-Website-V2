// ==========================================
//   NEVA Admin — Khách hàng JS
// ==========================================

function toast(m, t = 'ok') {
    const el = document.getElementById('toast');
    el.textContent = m;
    el.className = 'toast ' + t;
    el.style.display = 'block';
    setTimeout(() => el.style.display = 'none', 2500);
}

let customers = [
    { id: 1, name: 'Nguyễn Thị B', phone: '0901 234 567', points: 1250, orders: 8, cancels: 0, locked: false },
    { id: 2, name: 'Trần Minh K', phone: '0912 345 678', points: 480, orders: 3, cancels: 1, locked: false },
    { id: 3, name: 'Lê Thị H', phone: '0933 456 789', points: 920, orders: 6, cancels: 0, locked: false },
    { id: 4, name: 'Phạm Văn D', phone: '0944 567 890', points: 0, orders: 4, cancels: 3, locked: true },
    { id: 5, name: 'Hoàng Thị M', phone: '0955 678 901', points: 2100, orders: 15, cancels: 1, locked: false },
    { id: 6, name: 'Võ Minh T', phone: '0966 789 012', points: 50, orders: 1, cancels: 0, locked: false },
];

function renderCustomers() {
    const q = document.getElementById('custSearch').value.toLowerCase();
    const list = customers.filter(cu =>
        !q || cu.name.toLowerCase().includes(q) || cu.phone.includes(q)
    );
    document.getElementById('custCount').textContent = list.length;
    document.getElementById('custBody').innerHTML = list.map(cu => `
        <tr>
            <td style="font-family:'EB Garamond',serif;font-style:italic;font-size:14px;color:var(--white)">${cu.name}</td>
            <td>${cu.phone}</td>
            <td><span class="pts-badge">✦ ${cu.points.toLocaleString('vi-VN')}</span></td>
            <td>${cu.orders} đơn</td>
            <td><span class="${cu.cancels >= 3 ? 'boom-warn' : ''}">${cu.cancels} lần${cu.cancels >= 3 ? ' ⚠️' : ''}</span></td>
            <td><span class="cust-status ${cu.locked ? 'cs-locked' : 'cs-active'}">${cu.locked ? 'Đã khóa' : 'Hoạt động'}</span></td>
            <td>
                <button class="lock-btn ${cu.locked ? 'locked' : ''}"
                    onclick="toggleLock(${cu.id})"
                    title="${cu.locked ? 'Mở khóa' : 'Khóa tài khoản'}">
                    <svg viewBox="0 0 24 24">
                        ${cu.locked
            ? '<rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 9.9-1"/>'
            : '<rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>'
        }
                    </svg>
                </button>
            </td>
        </tr>`).join('');
}

function toggleLock(id) {
    const cu = customers.find(x => x.id === id);
    cu.locked = !cu.locked;
    renderCustomers();
    toast(
        cu.locked ? `Đã khóa tài khoản ${cu.name}` : `Đã mở khóa tài khoản ${cu.name}`,
        cu.locked ? 'err' : 'ok'
    );
}

document.addEventListener('DOMContentLoaded', renderCustomers);