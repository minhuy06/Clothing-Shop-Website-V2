function fmt(n) { return n.toLocaleString('vi-VN') + 'đ'; }

function placeOrder() {
    const name = document.getElementById('rcvName').value.trim();
    const phone = document.getElementById('rcvPhone').value.trim();
    const addr = document.getElementById('rcvAddr').value.trim();

    if (!name) { nevaToast('Vui lòng nhập họ tên người nhận!', 'err'); return; }
    if (!phone) { nevaToast('Vui lòng nhập số điện thoại!', 'err'); return; }
    if (!addr) { nevaToast('Vui lòng nhập địa chỉ chi tiết!', 'err'); return; }

    // Tạo mã đơn
    const code = 'NV-' + new Date().toISOString().slice(0, 10).replace(/-/g, '') + '-' + Math.floor(Math.random() * 9000 + 1000);
    document.getElementById('orderCode').textContent = '# ' + code;
    document.getElementById('successScreen').classList.add('show');
}

document.addEventListener('DOMContentLoaded', function () {
    // Chặn enter submit
    document.querySelectorAll('.finput').forEach(input => {
        input.addEventListener('keydown', e => { if (e.key === 'Enter') e.preventDefault(); });
    });
});