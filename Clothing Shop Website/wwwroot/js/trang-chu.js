// ── Slideshow ──
let curSlide = 0;
let slides = [];
let dots = [];
let slideTimer = null;

function refreshSlideRefs() {
    slides = Array.from(document.querySelectorAll('.hs'));
    dots = Array.from(document.querySelectorAll('.sd'));
    curSlide = slides.findIndex(s => s.classList.contains('on'));
    if (curSlide < 0) curSlide = 0;
}

function goSlide(n) {
    refreshSlideRefs();
    if (!slides.length) return;
    slides[curSlide]?.classList.remove('on');
    dots[curSlide]?.classList.remove('on');
    curSlide = (n + slides.length) % slides.length;
    slides[curSlide]?.classList.add('on');
    dots[curSlide]?.classList.add('on');
}

function nextSlide() { goSlide(curSlide + 1); }
function prevSlide() { goSlide(curSlide - 1); }

function initHeroSlideshow() {
    refreshSlideRefs();
    if (slideTimer) clearInterval(slideTimer);
    slideTimer = null;

    dots.forEach((d, i) => {
        d.replaceWith(d.cloneNode(true));
    });
    refreshSlideRefs();

    dots.forEach((d, i) => {
        d.addEventListener('click', () => {
            if (slideTimer) clearInterval(slideTimer);
            goSlide(i);
            if (slides.length > 1) slideTimer = setInterval(nextSlide, 5000);
        });
    });

    if (slides.length > 1) {
        slideTimer = setInterval(nextSlide, 5000);
    }
}

function initHomeAdPopup() {
    const popup = document.getElementById('homeAdPopup');
    if (!popup) return;

    const adId = popup.getAttribute('data-ad-id');
    const key = 'neva_ad_popup_' + adId;
    if (sessionStorage.getItem(key) === '1') return;

    const close = () => {
        popup.hidden = true;
        sessionStorage.setItem(key, '1');
    };

    popup.hidden = false;
    document.getElementById('homeAdPopupClose')?.addEventListener('click', close);
    document.getElementById('homeAdPopupBackdrop')?.addEventListener('click', close);
}

document.addEventListener('DOMContentLoaded', () => {
    initHeroSlideshow();
    initHomeAdPopup();
});
