/* home.js */
document.addEventListener('DOMContentLoaded', function() {

    // Staggered card entrance animations
    var cards = document.querySelectorAll('.product-card, .breed-card, .category-card, .review-card, .promo-card');
    cards.forEach(function(card, i) {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        card.style.transition = 'opacity .4s ease ' + (i * 0.07) + 's, transform .4s ease ' + (i * 0.07) + 's';
    });

    var obs = new IntersectionObserver(function(entries) {
        entries.forEach(function(e) {
            if (e.isIntersecting) {
                e.target.style.opacity = '1';
                e.target.style.transform = 'translateY(0)';
            }
        });
    }, { threshold: 0.1 });

    cards.forEach(function(c) { obs.observe(c); });

    // Hero stat counter animation
    document.querySelectorAll('.stat strong').forEach(function(el) {
        el.style.opacity = '0';
        el.style.transform = 'translateY(8px)';
        el.style.transition = 'opacity .5s, transform .5s';
        setTimeout(function() {
            el.style.opacity = '1';
            el.style.transform = 'translateY(0)';
        }, 600);
    });
});
