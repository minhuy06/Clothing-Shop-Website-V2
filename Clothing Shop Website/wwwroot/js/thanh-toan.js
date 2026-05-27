// Thanh toán — xác thực form trước khi đặt hàng
document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('orderForm');
    if (!form) return;

    form.addEventListener('submit', function (e) {
        const name = document.getElementById('rcvName')?.value.trim();
        const phone = document.getElementById('rcvPhone')?.value.trim();
        const addr = document.getElementById('rcvAddr')?.value.trim();
        const prov = document.getElementById('rcvProvince')?.value.trim();

        if (!name) {
            e.preventDefault();
            if (typeof nevaToast === 'function') nevaToast('Vui lòng nhập họ tên người nhận!', 'err');
            else alert('Vui lòng nhập họ tên người nhận!');
            return;
        }
        if (!phone) {
            e.preventDefault();
            if (typeof nevaToast === 'function') nevaToast('Vui lòng nhập số điện thoại!', 'err');
            return;
        }
        if (!prov) {
            e.preventDefault();
            if (typeof nevaToast === 'function') nevaToast('Vui lòng chọn tỉnh/thành phố!', 'err');
            return;
        }
        if (!addr) {
            e.preventDefault();
            if (typeof nevaToast === 'function') nevaToast('Vui lòng nhập địa chỉ chi tiết!', 'err');
            return;
        }
    });

    document.querySelectorAll('.finput').forEach(input => {
        input.addEventListener('keydown', ev => { if (ev.key === 'Enter') ev.preventDefault(); });
    });
});

function fillAddr(sel) {
    if (!sel.value) return;
    const parts = sel.value.split('|');
    const nameEl = document.getElementById('rcvName');
    const phoneEl = document.getElementById('rcvPhone');
    const addrEl = document.getElementById('rcvAddr');
    const prov = document.getElementById('rcvProvince');
    if (nameEl) nameEl.value = parts[0] || '';
    if (phoneEl) phoneEl.value = parts[1] || '';
    if (addrEl) addrEl.value = parts[2] || '';
    if (prov) {
        for (let i = 0; i < prov.options.length; i++) {
            if (prov.options[i].value === parts[3] || prov.options[i].text === parts[3]) {
                prov.selectedIndex = i;
                break;
            }
        }
    }
}
