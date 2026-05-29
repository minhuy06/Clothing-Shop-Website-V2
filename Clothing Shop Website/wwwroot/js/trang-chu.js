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

// ── Popup quảng cáo (có thể xếp chồng nhiều QC) ──
(function () {
    const stackRoot = document.getElementById('adPopupStack');
    if (!stackRoot) return;

    const stackEl = stackRoot.querySelector('.ad-popup-stack');
    if (!stackEl) return;

    const storageKey = id => 'neva_popup_dismissed_' + id;

    function getCards() {
        return [...stackEl.querySelectorAll('.ad-popup-card')];
    }

    function visibleCards() {
        return getCards().filter(c => !sessionStorage.getItem(storageKey(c.dataset.adId)));
    }

    function layoutStack() {
        const cards = visibleCards();
        const total = cards.length;
        cards.forEach((card, i) => {
            card.style.setProperty('--stack-index', String(i));
            card.classList.toggle('is-top', i === 0);
            const badge = card.querySelector('.ad-popup-stack-badge');
            if (badge) {
                badge.textContent = (i + 1) + '/' + total;
                badge.hidden = total <= 1;
            }
        });
        const n = total;
        stackEl.dataset.stackCount = String(n);
        if (n <= 1) {
            stackEl.style.padding = '0';
        } else {
            stackEl.style.padding = `0 ${(n - 1) * 14}px ${(n - 1) * 14}px 0`;
        }
    }

    function hideAll() {
        stackRoot.setAttribute('hidden', '');
        document.body.style.overflow = '';
    }

    function showStack() {
        const cards = visibleCards();
        if (!cards.length) {
            hideAll();
            return;
        }
        getCards().forEach(card => {
            const dismissed = sessionStorage.getItem(storageKey(card.dataset.adId));
            card.hidden = !!dismissed;
        });
        layoutStack();
        stackRoot.removeAttribute('hidden');
        document.body.style.overflow = 'hidden';
    }

    function closeTop() {
        const cards = visibleCards();
        const top = cards[0];
        if (!top) {
            hideAll();
            return;
        }
        const adId = top.dataset.adId;
        sessionStorage.setItem(storageKey(adId), '1');
        top.classList.add('is-removing');
        setTimeout(() => {
            top.hidden = true;
            top.classList.remove('is-removing');
            if (visibleCards().length) {
                layoutStack();
            } else {
                hideAll();
            }
        }, 320);
    }

    getCards().forEach(card => {
        card.querySelector('.ad-popup-close')?.addEventListener('click', e => {
            e.stopPropagation();
            closeTop();
        });
    });

    stackRoot.querySelector('.ad-popup-backdrop')?.addEventListener('click', closeTop);

    showStack();
})();
