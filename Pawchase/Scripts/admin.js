/* admin.js */
document.addEventListener('DOMContentLoaded', function () {

    // ── Discount preview (AddProduct) ────────────────────────────────────────
    var priceInput = document.getElementById('f-price');
    var origInput = document.getElementById('f-orig');
    if (priceInput && origInput) {
        function calcDiscount() {
            var price = parseFloat(priceInput.value);
            var orig = parseFloat(origInput.value);
            var preview = document.getElementById('discount-preview');
            var text = document.getElementById('discount-text');
            if (preview && text) {
                if (price > 0 && orig > price) {
                    var pct = Math.round((orig - price) / orig * 100);
                    preview.style.display = 'block';
                    text.innerHTML = pct + '% OFF &mdash; SALE badge will appear on product';
                } else if (price > 0 && orig > 0 && orig <= price) {
                    // FIX: warn if original <= sale price
                    preview.style.display = 'block';
                    preview.style.background = 'rgba(224,85,85,.1)';
                    preview.style.borderColor = 'rgba(224,85,85,.3)';
                    preview.style.color = '#c94040';
                    text.innerHTML = '<i class="fa-solid fa-triangle-exclamation"></i> Original Price must be higher than Sale Price';
                } else {
                    preview.style.display = 'none';
                    preview.style.background = '';
                    preview.style.borderColor = '';
                    preview.style.color = '';
                }
            }
        }
        priceInput.addEventListener('input', calcDiscount);
        origInput.addEventListener('input', calcDiscount);
    }

    // ── Image preview upload ──────────────────────────────────────────────────
    var imgFile = document.getElementById('imgFile');
    if (imgFile) {
        imgFile.addEventListener('change', function () {
            if (this.files && this.files[0]) {
                var reader = new FileReader();
                reader.onload = function (e) {
                    var preview = document.getElementById('imgPreview');
                    var icon = document.getElementById('imgIcon');
                    var fname = document.getElementById('imgPath');
                    if (preview) { preview.src = e.target.result; preview.style.display = 'block'; }
                    if (icon) icon.style.display = 'none';
                    if (fname) fname.value = '/Content/images/products/' + imgFile.files[0].name;
                };
                reader.readAsDataURL(this.files[0]);
            }
        });
    }

    // ── Auto-dismiss toasts (success + error) ────────────────────────────────
    function dismissToast(id, delay) {
        var toast = document.getElementById(id);
        if (!toast) return;
        setTimeout(function () {
            toast.style.transition = 'opacity .4s ease, transform .4s ease';
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(-6px)';
            setTimeout(function () { if (toast.parentNode) toast.parentNode.removeChild(toast); }, 400);
        }, delay);
    }

    dismissToast('adminToast', 3500);
    dismissToast('adminToastError', 5000); // errors stay a bit longer
});
