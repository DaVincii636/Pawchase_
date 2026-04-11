/* order.js */
document.addEventListener('DOMContentLoaded', function() {
    var refInput = document.querySelector('input[name="referenceNumber"]');
    if (refInput) {
        refInput.addEventListener('input', function() {
            var pos = this.selectionStart;
            this.value = this.value.toUpperCase();
            this.setSelectionRange(pos, pos);
        });
    }
    // Timeline animation
    document.querySelectorAll('.timeline-step').forEach(function(step, i) {
        step.style.opacity = '0';
        step.style.transform = 'translateY(14px)';
        setTimeout(function() {
            step.style.transition = 'opacity .4s, transform .4s';
            step.style.opacity = '1';
            step.style.transform = 'translateY(0)';
        }, 120 + i * 110);
    });
    // Confirmation card
    var card = document.querySelector('.confirmation-card');
    if (card) {
        card.style.opacity = '0'; card.style.transform = 'scale(.96)';
        card.style.transition = 'opacity .5s, transform .5s';
        setTimeout(function() { card.style.opacity = '1'; card.style.transform = 'scale(1)'; }, 100);
    }
    // Click to copy ref
    var refNum = document.getElementById('refNum');
    if (refNum) {
        var orig = refNum.textContent;
        refNum.addEventListener('click', function() {
            navigator.clipboard && navigator.clipboard.writeText(orig.trim()).then(function() {
                refNum.textContent = '✓ Copied!'; refNum.style.color = '#1e8449';
                setTimeout(function() { refNum.textContent = orig; refNum.style.color = ''; }, 1600);
            });
        });
    }
});
