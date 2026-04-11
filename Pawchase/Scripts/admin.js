/* admin.js */
document.addEventListener('DOMContentLoaded', function() {
    // Auto discount preview
    var priceInput = document.getElementById('f-price');
    var origInput  = document.getElementById('f-orig');
    if (priceInput && origInput) {
        function calcDiscount() {
            var price = parseFloat(priceInput.value);
            var orig  = parseFloat(origInput.value);
            var preview = document.getElementById('discount-preview');
            var text    = document.getElementById('discount-text');
            if (preview && text) {
                if (price > 0 && orig > price) {
                    var pct = Math.round((orig - price) / orig * 100);
                    preview.style.display = 'block';
                    text.textContent = pct + '% OFF &mdash; SALE badge will appear on product';
                } else {
                    preview.style.display = 'none';
                }
            }
        }
        priceInput.addEventListener('input', calcDiscount);
        origInput.addEventListener('input', calcDiscount);
    }

    // Image preview upload
    var imgFile = document.getElementById('imgFile');
    if (imgFile) {
        imgFile.addEventListener('change', function() {
            if (this.files && this.files[0]) {
                var reader = new FileReader();
                reader.onload = function(e) {
                    var preview = document.getElementById('imgPreview');
                    var icon    = document.getElementById('imgIcon');
                    var fname   = document.getElementById('imgPath');
                    if (preview) { preview.src = e.target.result; preview.style.display = 'block'; }
                    if (icon)    icon.style.display = 'none';
                    if (fname)   fname.value = '/Content/images/products/' + imgFile.files[0].name;
                };
                reader.readAsDataURL(this.files[0]);
            }
        });
    }

    // Admin toast auto-dismiss
    var toast = document.querySelector('.admin-toast');
    if (toast) {
        setTimeout(function() {
            toast.style.transition = 'opacity .5s'; toast.style.opacity = '0';
            setTimeout(function() { if (toast.parentNode) toast.parentNode.removeChild(toast); }, 500);
        }, 3500);
    }
});
