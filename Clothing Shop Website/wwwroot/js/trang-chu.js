// ── Slideshow ──
let curSlide = 0;
const slides = document.querySelectorAll('.hs');
const dots = document.querySelectorAll('.sd');

function goSlide(n) {
    if (!slides.length) return;
    slides[curSlide].classList.remove('on');
    dots[curSlide].classList.remove('on');
    curSlide = (n + slides.length) % slides.length;
    slides[curSlide].classList.add('on');
    dots[curSlide].classList.add('on');
}

function nextSlide() { goSlide(curSlide + 1); }
function prevSlide() { goSlide(curSlide - 1); }

// Auto play
let slideTimer = setInterval(nextSlide, 5000);

// Dots click
dots.forEach((d, i) => {
    d.addEventListener('click', () => {
        clearInterval(slideTimer);
        goSlide(i);
        slideTimer = setInterval(nextSlide, 5000);
    });
});

// ── Popup quảng cáo (lần đầu mở trang / quảng cáo mới) ──
(function () {
    const popup = document.getElementById('adPopup');
    if (!popup) return;
    const adId = popup.getAttribute('data-ad-id');
    const key = 'neva_popup_dismissed_' + adId;
    if (sessionStorage.getItem(key)) return;

    popup.removeAttribute('hidden');

    function closePopup() {
        popup.setAttribute('hidden', '');
        sessionStorage.setItem(key, '1');
    }

    popup.querySelector('.ad-popup-close')?.addEventListener('click', closePopup);
    popup.querySelector('.ad-popup-backdrop')?.addEventListener('click', closePopup);
})();