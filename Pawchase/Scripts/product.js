/* product.js &mdash; Toggle filter + live search */

var filterOpen = false;

function toggleFilter() {
    var panel = document.getElementById('filterPanel');
    var btn   = document.getElementById('filterToggleBtn');
    var ov    = document.getElementById('filterOverlay');
    filterOpen = !filterOpen;
    if (panel) panel.classList.toggle('open', filterOpen);
    if (btn)   btn.classList.toggle('active', filterOpen);
    if (ov)    ov.classList.toggle('show', filterOpen);
}

// Live search
var liveTimer = null;
function liveSearch(input) {
    clearTimeout(liveTimer);
    liveTimer = setTimeout(function() {
        var form = input.closest('form') || document.querySelector('.filter-form');
        if (form) form.submit();
    }, 350);
}

document.addEventListener('DOMContentLoaded', function() {
    // Auto open if filter active
    var hasFilter = document.getElementById('filter-dot') && document.getElementById('filter-dot').style.display !== 'none';
    if (hasFilter) toggleFilter();

    // Staggered product cards
    var cards = document.querySelectorAll('.product-card');
    cards.forEach(function(card, i) {
        card.style.opacity = '0';
        card.style.transform = 'translateY(16px)';
        card.style.transition = 'opacity .35s ease ' + (i * 0.05) + 's, transform .35s ease ' + (i * 0.05) + 's';
    });
    var obs = new IntersectionObserver(function(entries) {
        entries.forEach(function(e) {
            if (e.isIntersecting) { e.target.style.opacity = '1'; e.target.style.transform = 'translateY(0)'; }
        });
    }, { threshold: 0.08 });
    cards.forEach(function(c) { obs.observe(c); });

    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && filterOpen) toggleFilter();
    });
});
