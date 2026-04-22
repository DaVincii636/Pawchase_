/* product.js — Filter toggle + live search + auto-open panel */

var filterOpen = false;

function toggleFilter() {
    var panel = document.getElementById('filterPanel');
    var btn = document.getElementById('filterToggleBtn');
    var ov = document.getElementById('filterOverlay');
    filterOpen = !filterOpen;
    if (panel) panel.classList.toggle('open', filterOpen);
    if (btn) btn.classList.toggle('active', filterOpen);
    if (ov) ov.classList.toggle('show', filterOpen);
}

/* ── LIVE SEARCH ───────────────────────────────────────────────────────────
   Filters already-rendered cards by their data-search attribute.
   No page reload needed. Falls back to form submit on Enter.
   ───────────────────────────────────────────────────────────────────────── */
var liveTimer = null;

function initLiveSearch() {
    var input = document.getElementById('toolbarSearchInput');
    if (!input) return;

    input.addEventListener('input', function () {
        clearTimeout(liveTimer);
        liveTimer = setTimeout(function () {
            filterCards(input.value.trim().toLowerCase());
        }, 200);
    });

    // Still allow pressing Enter to do a full server-side search
    input.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            clearTimeout(liveTimer);
            var form = input.closest('form') || document.querySelector('.filter-form');
            if (form) form.submit();
        }
    });
}

function filterCards(query) {
    var grid = document.getElementById('productsGrid');
    var noResult = document.getElementById('noProducts');
    if (!grid) return;

    var cards = grid.querySelectorAll('.product-card');
    var visible = 0;

    cards.forEach(function (card) {
        var text = (card.getAttribute('data-search') || '').toLowerCase();
        var show = query === '' || text.indexOf(query) !== -1;
        card.style.display = show ? '' : 'none';
        if (show) visible++;
    });

    // Update result count in toolbar
    var num = document.getElementById('resultNum');
    if (num) num.textContent = visible;

    // Show/hide empty state
    if (noResult) noResult.style.display = visible === 0 ? 'flex' : 'none';
    if (grid) grid.style.display = visible === 0 ? 'none' : '';
}

/* ── AUTO-OPEN FILTER PANEL ────────────────────────────────────────────────
   Only auto-opens when panelOpen=1 is explicitly present in the URL.
   This is set by setFilter, applyPriceFilter, and resetAllFilters — but
   NOT by applySort, so sorting never causes the panel to open or stay open.
   ───────────────────────────────────────────────────────────────────────── */
function shouldAutoOpenPanel() {
    var qs = new URLSearchParams(window.location.search);
    return qs.get('panelOpen') === '1';
}

document.addEventListener('DOMContentLoaded', function () {

    // Auto-open filter panel only when panelOpen=1 is in the URL
    if (shouldAutoOpenPanel()) {
        toggleFilter();
    }

    initLiveSearch();

    // Staggered card entrance animation
    var cards = document.querySelectorAll('.product-card');
    cards.forEach(function (card, i) {
        card.style.opacity = '0';
        card.style.transform = 'translateY(16px)';
        card.style.transition = 'opacity .35s ease ' + (i * 0.05) + 's, transform .35s ease ' + (i * 0.05) + 's';
    });

    var obs = new IntersectionObserver(function (entries) {
        entries.forEach(function (e) {
            if (e.isIntersecting) {
                e.target.style.opacity = '1';
                e.target.style.transform = 'translateY(0)';
            }
        });
    }, { threshold: 0.08 });

    cards.forEach(function (c) { obs.observe(c); });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && filterOpen) toggleFilter();
    });
});
