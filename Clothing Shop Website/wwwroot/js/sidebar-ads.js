// Khung quảng cáo góc phải — thu gọn / mở lại
(function () {
    const panel = document.getElementById('sidebarAdPanel');
    const tab = document.getElementById('sidebarAdTab');
    const closeBtn = panel?.querySelector('.sidebar-ad-panel-close');
    if (!panel || !tab) return;

    const key = 'neva_sidebar_ads_collapsed';

    function collapse() {
        panel.classList.add('is-collapsed');
        tab.hidden = false;
        sessionStorage.setItem(key, '1');
    }

    function expand() {
        panel.classList.remove('is-collapsed');
        tab.hidden = true;
        sessionStorage.removeItem(key);
    }

    closeBtn?.addEventListener('click', collapse);
    tab.addEventListener('click', expand);

    if (sessionStorage.getItem(key)) collapse();
})();
