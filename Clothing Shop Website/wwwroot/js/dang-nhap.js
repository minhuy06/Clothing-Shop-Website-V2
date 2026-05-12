// ── Switch tabs ──
function switchTab(tab) {
    document.querySelectorAll('.auth-tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));
    document.querySelector(`.auth-tab[data-tab="${tab}"]`).classList.add('active');
    document.getElementById(tab + 'Tab').classList.add('active');
}

// ── Toggle password visibility ──
function togglePass(id) {
    const input = document.getElementById(id);
    input.type = input.type === 'password' ? 'text' : 'password';
}

// ── Login ──
function doLogin() {
    const phone = document.getElementById('loginPhone').value.trim();
    const pass = document.getElementById('loginPass').value;

    clearErrors('login');

    let ok = true;
    if (!phone) { showErr('loginPhoneErr', 'Vui lòng nhập số điện thoại!'); ok = false; }
    if (!pass) { showErr('loginPassErr', 'Vui lòng nhập mật khẩu!'); ok = false; }
    if (!ok) return;

    // Giả lập đăng nhập thành công
    nevaToast('✦ Đăng nhập thành công!', 'info');
    setTimeout(() => window.location.href = '/Account/Profile', 800);
}

// ── Register ──
function doRegister() {
    const name = document.getElementById('regName').value.trim();
    const phone = document.getElementById('regPhone').value.trim();
    const pass = document.getElementById('regPass').value;
    const confirm = document.getElementById('regConfirm').value;

    clearErrors('reg');

    let ok = true;
    if (!name) { showErr('regNameErr', 'Vui lòng nhập họ và tên!'); ok = false; }
    if (!phone) { showErr('regPhoneErr', 'Vui lòng nhập số điện thoại!'); ok = false; }
    if (!pass || pass.length < 6) { showErr('regPassErr', 'Mật khẩu phải ít nhất 6 ký tự!'); ok = false; }
    if (pass !== confirm) { showErr('regConfirmErr', 'Mật khẩu xác nhận không khớp!'); ok = false; }
    if (!ok) return;

    nevaToast('✦ Tạo tài khoản thành công!', 'info');
    setTimeout(() => window.location.href = '/Account/Profile', 800);
}

function showErr(id, msg) {
    const el = document.getElementById(id);
    if (el) { el.textContent = msg; el.classList.add('show'); }
}

function clearErrors(prefix) {
    document.querySelectorAll('.f-err').forEach(e => e.classList.remove('show'));
}

// ── Enter key ──
document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('loginPass')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') doLogin();
    });
    document.getElementById('regConfirm')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') doRegister();
    });
});