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

function openAddrMo() {
    const mo = document.getElementById('addrMo');
    if (!mo) return;
    const form = mo.querySelector('form');
    if (form) form.reset();
    mo.classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeAddrMo() {
    const mo = document.getElementById('addrMo');
    if (!mo) return;
    mo.classList.remove('open');
    document.body.style.overflow = '';
}
