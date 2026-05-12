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