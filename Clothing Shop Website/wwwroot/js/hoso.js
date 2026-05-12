function toast(m, t = 'ok') { const el = document.getElementById('toast'); el.textContent = m; el.className = 'toast ' + t; el.style.display = 'block'; setTimeout(() => el.style.display = 'none', 2600) }
function tp(id) { const i = document.getElementById(id); i.type = i.type === 'password' ? 'text' : 'password' }
function sw(n, el) {
    document.querySelectorAll('.tab-content').forEach(x => x.classList.remove('active'));
    document.querySelectorAll('.smenu-item').forEach(x => x.classList.remove('active'));
    document.getElementById(n + 'Tab').classList.add('active');
    if (el) el.classList.add('active');
}
let ed = false;
function toggleEdit() { ed = true; document.getElementById('infoView').style.display = 'none'; document.getElementById('infoEdit').style.display = 'block'; document.getElementById('editBtn').style.display = 'none' }
function cancelEdit() { ed = false; document.getElementById('infoView').style.display = 'block'; document.getElementById('infoEdit').style.display = 'none'; document.getElementById('editBtn').style.display = '' }
function saveProfile() {
    const n = document.getElementById('eName').value.trim(), p = document.getElementById('ePhone').value.trim();
    if (!n || !p) { toast('Vui lòng điền đầy đủ!', 'err'); return }
    document.getElementById('vName').textContent = n; document.getElementById('vPhone').textContent = p;
    const dob = document.getElementById('eDob').value; if (dob) { const [y, m, d] = dob.split('-'); document.getElementById('vDob').textContent = `${d}/${m}/${y}` }
    document.getElementById('vGender').textContent = document.getElementById('eGender').value;
    document.querySelector('.p-name').textContent = n;
    cancelEdit(); toast('✦ Cập nhật thông tin thành công!', 'info');
}
let addresses = [
    { id: 1, name: 'Nguyễn Thị B', phone: '0901 234 567', city: 'Đà Nẵng', district: 'Hải Châu', detail: '123 Trần Phú, Hải Châu 1', isDefault: true },
    { id: 2, name: 'Nguyễn Thị B', phone: '0901 234 567', city: 'Đà Nẵng', district: 'Thanh Khê', detail: '456 Nguyễn Văn Linh, Thạc Gián', isDefault: false },
];
let editId = -1;
function renderAddr() {
    const el = document.getElementById('addrList');
    if (!addresses.length) { el.innerHTML = '<p style="font-size:11px;color:rgba(250,248,242,.22);padding:12px 0">Chưa có địa chỉ nào.</p>'; return }
    el.innerHTML = addresses.map(a => `<div class="addr-card ${a.isDefault ? 'def' : ''}">${a.isDefault ? '<div class="addr-badge">Mặc định</div>' : ''}<div class="addr-name">${a.name} · ${a.phone}</div><div class="addr-text">${a.detail}, ${a.district}, ${a.city}</div><div class="addr-acts"><span class="addr-act" onclick="openAddrMo(${a.id})">Chỉnh sửa</span>${!a.isDefault ? `<span class="addr-act" onclick="setDef(${a.id})">Đặt mặc định</span>` : ''}<span class="addr-act red" onclick="delAddr(${a.id})">Xóa</span></div></div>`).join('');
}
function openAddrMo(id) { editId = id; document.getElementById('addrMoT').textContent = id === -1 ? 'Thêm địa chỉ mới' : 'Chỉnh sửa địa chỉ'; if (id !== -1) { const a = addresses.find(x => x.id === id); document.getElementById('adN').value = a.name; document.getElementById('adP').value = a.phone; document.getElementById('adC').value = a.city; document.getElementById('adD').value = a.district; document.getElementById('adA').value = a.detail; document.getElementById('adDef').checked = a.isDefault } else { ['adN', 'adP', 'adD', 'adA'].forEach(i => document.getElementById(i).value = ''); document.getElementById('adDef').checked = false } document.getElementById('addrMo').classList.add('open') }
function closeAddrMo() { document.getElementById('addrMo').classList.remove('open') }
function saveAddr() { const n = document.getElementById('adN').value.trim(), p = document.getElementById('adP').value.trim(), city = document.getElementById('adC').value, d = document.getElementById('adD').value.trim(), a = document.getElementById('adA').value.trim(), def = document.getElementById('adDef').checked; if (!n || !p || !d || !a) { toast('Vui lòng điền đầy đủ!', 'err'); return } if (def) addresses.forEach(x => x.isDefault = false); if (editId === -1) { addresses.push({ id: Date.now(), name: n, phone: p, city, district: d, detail: a, isDefault: def }); toast('✦ Đã thêm địa chỉ!', 'info') } else { const i = addresses.findIndex(x => x.id === editId); addresses[i] = { ...addresses[i], name: n, phone: p, city, district: d, detail: a, isDefault: def }; toast('✦ Đã cập nhật!', 'info') } if (!addresses.some(x => x.isDefault) && addresses.length) addresses[0].isDefault = true; closeAddrMo(); renderAddr() }
function setDef(id) { addresses.forEach(x => x.isDefault = x.id === id); renderAddr(); toast('✦ Đã đặt mặc định!', 'info') }
function delAddr(id) { if (addresses.find(x => x.id === id)?.isDefault && addresses.length > 1) { toast('Không thể xóa địa chỉ mặc định!', 'err'); return } addresses = addresses.filter(x => x.id !== id); if (addresses.length && !addresses.some(x => x.isDefault)) addresses[0].isDefault = true; renderAddr(); toast('Đã xóa địa chỉ.') }
function updPass() { const cur = document.getElementById('curP').value, nw = document.getElementById('newP').value, cf = document.getElementById('cfP').value; if (!cur || !nw || !cf) { toast('Vui lòng điền đầy đủ!', 'err'); return } if (cur !== '123456') { toast('Mật khẩu hiện tại không đúng!', 'err'); return } if (nw.length < 8) { toast('Mật khẩu mới phải ít nhất 8 ký tự!', 'err'); return } if (nw !== cf) { toast('Mật khẩu xác nhận không khớp!', 'err'); return } ['curP', 'newP', 'cfP'].forEach(i => document.getElementById(i).value = ''); toast('✦ Cập nhật mật khẩu thành công!', 'info') }
function openConf() { document.getElementById('confMo').classList.add('open') }
function closeConf() { document.getElementById('confMo').classList.remove('open') }
renderAddr();