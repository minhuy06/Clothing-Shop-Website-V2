// ── Toast ──
function nevaToast(message, type = 'ok') {
    let toast = document.getElementById('nevaToast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'nevaToast';
        toast.className = 'neva-toast';
        document.body.appendChild(toast);
    }
    toast.textContent = message;
    toast.className = 'neva-toast ' + type;
    toast.style.display = 'block';
    setTimeout(() => toast.style.display = 'none', 2600);
}

// ── Format tiền ──
function formatVND(amount) {
    return amount.toLocaleString('vi-VN') + 'đ';
}

// ── Active nav link theo URL hiện tại ──
document.addEventListener('DOMContentLoaded', function () {
    const path = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('.nav-link');

    navLinks.forEach(link => {
        const href = link.getAttribute('href')?.toLowerCase() || '';
        if (href !== '/' && path.startsWith(href)) {
            link.classList.add('active');
        }
    });

    // Logo vàng khi ở trang chủ
    if (path === '/' || path === '/home' || path === '/home/index') {
        const logoLink = document.querySelector('.nav-logo a');
        if (logoLink) logoLink.classList.add('home-active');
    }

    // Icon cart vàng khi ở trang giỏ hàng
    if (path.includes('/cart')) {
        const cartIcon = document.querySelector('.nav-icon[data-page="cart"]');
        if (cartIcon) cartIcon.classList.add('active');
    }

    // Icon user vàng khi ở trang tài khoản
    if (path.includes('/account') || path.includes('/order/history')) {
        const userIcon = document.querySelector('.nav-icon[data-page="account"]');
        if (userIcon) userIcon.classList.add('active');
    }
});