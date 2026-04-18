/* cart.js — Checkboxes + summary */
document.addEventListener('DOMContentLoaded', function () {
    updateSummary();

    // Payment options toggle
    document.querySelectorAll('.payment-opt:not(.disabled)').forEach(function (opt) {
        opt.addEventListener('click', function () {
            document.querySelectorAll('.payment-opt').forEach(function (o) { o.classList.remove('active'); });
            opt.classList.add('active');
        });
    });
});

function toggleAll(cb) {
    document.querySelectorAll('.item-check').forEach(function (c) { c.checked = cb.checked; });
    updateSummary();
}

function updateSummary() {
    var checks = document.querySelectorAll('.item-check:checked');
    var subtotal = 0;
    var count = 0;
    checks.forEach(function (cb) {
        // Strip the peso sign (₱), commas, and whitespace before parsing
        var raw = (cb.dataset.subtotal || '0').replace(/[₱,\s]/g, '');
        subtotal += parseFloat(raw) || 0;
        count += parseInt(cb.dataset.qty || 0) || 0;
    });
    var shipping = subtotal >= 500 ? 0 : (subtotal > 0 ? 80 : 0);
    var total = subtotal + shipping;

    var el = function (id) { return document.getElementById(id); };

    // Use innerHTML (not textContent) so the ₱ peso entity renders correctly
    if (el('sum-subtotal')) el('sum-subtotal').innerHTML = '&#8369;' + subtotal.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    if (el('sum-shipping')) el('sum-shipping').innerHTML = shipping === 0 && subtotal > 0
        ? '<span class="free-ship"><i class="fa-solid fa-truck"></i> FREE</span>'
        : '&#8369;80.00';
    if (el('sum-total')) el('sum-total').innerHTML = '&#8369;' + total.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    if (el('sum-count')) el('sum-count').textContent = count + ' item' + (count !== 1 ? 's' : '');

    var tip = el('sum-tip');
    if (tip) tip.style.display = subtotal > 0 && subtotal < 500 ? 'flex' : 'none';
    if (tip && subtotal > 0) {
        var amountNeeded = Math.max(0, 500 - subtotal);
        var tipSpan = tip.querySelector('span');
        if (tipSpan) tipSpan.innerHTML = 'Add &#8369;' + amountNeeded.toFixed(2) + ' more for FREE shipping!';

        // Update progress bar
        var progressFill = tip.querySelector('.ship-progress-fill');
        var progressText = tip.querySelector('.ship-progress-text');
        if (progressFill && progressText) {
            var percent = Math.min(100, Math.round((subtotal / 500) * 100));
            progressFill.style.width = percent + '%';
            progressText.textContent = percent + '% towards free shipping';
        }
    }
}

function confirmRemove() { return confirm('Remove this item from your cart?'); }
