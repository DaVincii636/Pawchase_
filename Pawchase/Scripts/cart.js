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

    // Intercept the main "Proceed to Checkout" link so we only send selected items
    document.querySelectorAll('a.checkout-btn').forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            postSelectedToCheckout(link);
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

function postSelectedToCheckout(link) {
    // Collect checked rows
    var rows = document.querySelectorAll('.cart-row');
    var selectedIds = [];
    var selectedVariants = [];
    var selectedQtys = [];

    for (var i = 0; i < rows.length; i++) {
        var cb = rows[i].querySelector('.item-check');
        if (!cb) continue;
        if (!cb.checked) continue;

        // Read product id and variant from the existing UpdateQuantity form inputs
        var idInput = rows[i].querySelector('form input[name="id"]');
        // The variantLabel hidden input is inside the UpdateQuantity form (name="variantLabel")
        var variantInput = rows[i].querySelector('form input[name="variantLabel"]');
        // Quantity is shown in a span.qty-count; use that as the current quantity
        var qtyEl = rows[i].querySelector('.qty-count');

        var id = idInput ? idInput.value : null;
        var variant = variantInput ? variantInput.value : '';
        var qty = qtyEl ? qtyEl.textContent.trim() : '1';

        if (id) {
            selectedIds.push(id);
            selectedVariants.push(variant);
            selectedQtys.push(qty || '1');
        }
    }

    if (selectedIds.length === 0) {
        // If none selected, fall back to default behaviour: proceed with all items
        // by navigating to the Checkout page
        window.location.href = link ? link.href : 'Checkout';
        return;
    }

    // Build and submit a temporary form to POST to CheckoutSelected
    var form = document.createElement('form');
    form.method = 'POST';
    // Use a relative action so the app virtual path is respected
    // If the link element is provided, build action from its href
    if (link && link.href) {
        // Replace the final 'Checkout' segment with 'CheckoutSelected' while keeping the app path
        try {
            var href = link.href;
            var newAction = href.replace(/Checkout(\?.*)?$/, 'CheckoutSelected$1');
            form.action = newAction;
        } catch (e) {
            form.action = 'CheckoutSelected';
        }
    } else {
        form.action = 'CheckoutSelected';
    }

    // Add arrays: selectedId, selectedVariantLabel, selectedQty
    selectedIds.forEach(function (v) {
        var inp = document.createElement('input');
        inp.type = 'hidden';
        inp.name = 'selectedId';
        inp.value = v;
        form.appendChild(inp);
    });
    selectedVariants.forEach(function (v) {
        var inp = document.createElement('input');
        inp.type = 'hidden';
        inp.name = 'selectedVariantLabel';
        inp.value = v || '';
        form.appendChild(inp);
    });
    selectedQtys.forEach(function (v) {
        var inp = document.createElement('input');
        inp.type = 'hidden';
        inp.name = 'selectedQty';
        inp.value = v || '1';
        form.appendChild(inp);
    });

    document.body.appendChild(form);
    form.submit();
}

function confirmRemove() { return confirm('Remove this item from your cart?'); }
