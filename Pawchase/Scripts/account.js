/* account.js */
function togglePw(inputId, iconId) {
    var inp = document.getElementById(inputId);
    var ic  = document.getElementById(iconId);
    if (!inp) return;
    inp.type = inp.type === 'password' ? 'text' : 'password';
    if (ic) ic.className = inp.type === 'password' ? 'fa-regular fa-eye' : 'fa-regular fa-eye-slash';
}
document.addEventListener('DOMContentLoaded', function() {
    var pw  = document.getElementById('pw');
    var cpw = document.getElementById('cpw');
    var msg = document.getElementById('pw-match');
    var btn = document.getElementById('reg-btn');
    if (!pw || !cpw) return;
    function check() {
        if (!cpw.value) { if (msg) msg.style.display = 'none'; if (btn) btn.disabled = false; return; }
        var ok = pw.value === cpw.value;
        if (msg) msg.style.display = ok ? 'none' : 'block';
        if (btn) btn.disabled = !ok;
        cpw.style.borderColor = ok ? '' : '#e05555';
    }
    pw.addEventListener('input', check);
    cpw.addEventListener('input', check);
});

function toggleReview(ref) {
    var el = document.getElementById('review-' + ref);
    if (el) el.style.display = el.style.display === 'none' ? 'block' : 'none';
}
function setStars(el, val, inputId) {
    el.parentElement.querySelectorAll('i').forEach(function(s, i) {
        s.className = i < val ? 'fa-solid fa-star' : 'fa-regular fa-star';
    });
    document.getElementById(inputId).value = val;
}
