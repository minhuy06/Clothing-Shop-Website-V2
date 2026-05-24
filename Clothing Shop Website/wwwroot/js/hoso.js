function tp(id) {
    const i = document.getElementById(id);
    if (i) i.type = i.type === 'password' ? 'text' : 'password';
}

function sw(n, el) {
    document.querySelectorAll('.tab-content').forEach(x => x.classList.remove('active'));
    document.querySelectorAll('.smenu-item').forEach(x => x.classList.remove('active'));
    const tab = document.getElementById(n + 'Tab');
    if (tab) tab.classList.add('active');
    if (el) el.classList.add('active');
}

function toggleEdit() {
    document.getElementById('infoView').style.display = 'none';
    document.getElementById('infoEdit').style.display = 'block';
    const b = document.getElementById('editBtn');
    if (b) b.style.display = 'none';
}

function cancelEdit() {
    document.getElementById('infoView').style.display = 'block';
    document.getElementById('infoEdit').style.display = 'none';
    const b = document.getElementById('editBtn');
    if (b) b.style.display = '';
}

function setAddrMoMode(mode, data) {
    const form = document.getElementById('addrForm');
    const title = document.getElementById('addrMoTitle');
    const submitBtn = document.getElementById('addrSubmitBtn');
    const idField = document.getElementById('addrIdField');
    if (!form || !title || !submitBtn || !idField) return;

    const addUrl = form.dataset.addUrl || '/Account/AddAddress';
    const updateUrl = form.dataset.updateUrl || '/Account/UpdateAddress';

    if (mode === 'edit' && data) {
        form.action = updateUrl;
        title.textContent = 'Chỉnh sửa địa chỉ';
        submitBtn.textContent = 'Lưu thay đổi';
        idField.value = data.id || '';
        document.getElementById('addrFullName').value = data.name || '';
        document.getElementById('addrPhone').value = data.phone || '';
        document.getElementById('addrDetail').value = data.detail || '';
        const prov = document.getElementById('addrProvince');
        if (prov && data.province) prov.value = data.province;
    } else {
        form.action = addUrl;
        title.textContent = 'Thêm địa chỉ mới';
        submitBtn.textContent = 'Lưu địa chỉ';
        idField.value = '';
        form.reset();
    }
}

function openAddrMo() {
    const mo = document.getElementById('addrMo');
    if (!mo) return;
    setAddrMoMode('add');
    mo.classList.add('open');
    document.body.style.overflow = 'hidden';
}

function openEditAddrMo(btn) {
    const mo = document.getElementById('addrMo');
    if (!mo || !btn) return;
    setAddrMoMode('edit', {
        id: btn.dataset.id,
        name: btn.dataset.name,
        phone: btn.dataset.phone,
        province: btn.dataset.province,
        detail: btn.dataset.detail
    });
    mo.classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeAddrMo() {
    const mo = document.getElementById('addrMo');
    if (!mo) return;
    mo.classList.remove('open');
    document.body.style.overflow = '';
    setAddrMoMode('add');
}

document.addEventListener('DOMContentLoaded', () => {
    const tab = document.body.dataset.profileTab;
    if (tab) {
        const item = document.querySelector(`.smenu-item[onclick*="'${tab}'"]`);
        sw(tab, item);
    }
});
