/* pawchase.js &mdash; Global utilities */

// Alt+Shift+C → Admin Login
document.addEventListener('keydown', function(e) {
    if (e.altKey && e.shiftKey && (e.key === 'C' || e.key === 'c')) {
        e.preventDefault();
        window.location.href = '/Admin/Login';
    }
});

document.addEventListener('DOMContentLoaded', function() {
    // Auto-dismiss admin toast
    var toast = document.querySelector('.admin-toast');
    if (toast) {
        setTimeout(function() {
            toast.style.transition = 'opacity .5s';
            toast.style.opacity = '0';
            setTimeout(function() { if (toast.parentNode) toast.parentNode.removeChild(toast); }, 500);
        }, 3500);
    }

    // Pause announcement bar on hover
    var track = document.querySelector('.announcement-track');
    if (track) {
        track.addEventListener('mouseenter', function() { track.style.animationPlayState = 'paused'; });
        track.addEventListener('mouseleave', function() { track.style.animationPlayState = 'running'; });
    }

    // Toast show
    var toasts = document.querySelectorAll('.toast');
    toasts.forEach(function(t) { setTimeout(function() { t.classList.add('show'); }, 10); setTimeout(function() { t.classList.remove('show'); }, 3500); });

    // Mobile nav
    buildMobileNav();
});

/* ── MOBILE NAV ───────────────────────────── */
function buildMobileNav() {
    if (window.innerWidth > 768) return;

    var path = window.location.pathname.toLowerCase();

    // Top bar
    var topBar = document.createElement('div');
    topBar.className = 'mobile-top-bar';
    topBar.innerHTML = '<a href="/Home/Index"><img src="/Content/images/Pawchase_logo.png" alt="PawChase" onerror="this.style.display=\'none\';this.nextElementSibling.style.display=\'flex\'"/><span class="logo-fallback" style="display:none"><i class="fa-solid fa-paw" style="color:var(--blue)"></i> PawChase</span></a>';

    var annBar = document.querySelector('.announcement-bar');
    var navbar = document.querySelector('.navbar');
    var insertBefore = annBar || document.querySelector('main') || document.body.firstChild;
    if (annBar) { document.body.insertBefore(topBar, annBar); }
    else { document.body.insertBefore(topBar, document.body.firstChild); }

    // Account sheet
    var sheet = document.createElement('div');
    sheet.className = 'mob-account-sheet';
    sheet.id = 'mob-sheet';

    var userName = document.querySelector('.nav-account span');
    var isLoggedIn = userName && userName.textContent.trim().length > 0;

    if (isLoggedIn) {
        sheet.innerHTML = '<div class="mob-user-info"><strong>' + userName.textContent + '</strong><span>Logged in</span></div>' +
            '<a href="/Account/Orders"><i class="fa-solid fa-list-check"></i> My Orders</a>' +
            '<a href="/Reviews/Index"><i class="fa-solid fa-paw"></i> Reviews</a>' +
            '<a href="/Account/Logout" class="logout-link"><i class="fa-solid fa-right-from-bracket"></i> Logout</a>';
    } else {
        sheet.innerHTML = '<a href="/Account/Login"><i class="fa-solid fa-right-to-bracket"></i> Sign In</a>' +
            '<a href="/Account/Register"><i class="fa-solid fa-user-plus"></i> Create Account</a>';
    }
    document.body.appendChild(sheet);

    document.addEventListener('click', function(e) {
        if (!sheet.contains(e.target) && !e.target.closest('.mob-account-btn')) {
            sheet.classList.remove('open');
        }
    });

    // Bottom nav
    var nav = document.createElement('nav');
    nav.className = 'mobile-nav';

    var isHome     = (path === '/' || path.includes('/home/index') || path.endsWith('/')) ? 'active' : '';
    var isProducts = path.includes('/product') ? 'active' : '';
    var isReviews  = path.includes('/review') ? 'active' : '';
    var isCart     = path.includes('/cart') ? 'active' : '';
    var isAccount  = path.includes('/account') || path.includes('/login') || path.includes('/register') ? 'active' : '';

    nav.innerHTML =
        '<a href="/Home/Index" class="mob-nav-item ' + isHome + '"><i class="fa-solid fa-house"></i>Home</a>' +
        '<a href="/Product/Index" class="mob-nav-item ' + isProducts + '"><i class="fa-solid fa-bag-shopping"></i>Shop</a>' +
        '<a href="/Reviews/Index" class="mob-nav-item ' + isReviews + '"><i class="fa-solid fa-paw"></i>Reviews</a>' +
        '<a href="/Cart/Index" class="mob-nav-item ' + isCart + '"><i class="fa-solid fa-cart-shopping"></i>Cart<span class="mob-cart-badge" id="mob-cart-badge"></span></a>' +
        '<button class="mob-nav-item mob-account-btn ' + isAccount + '" onclick="document.getElementById(\'mob-sheet\').classList.toggle(\'open\')"><i class="fa-regular fa-user"></i>Account</button>';

    document.body.appendChild(nav);
}

function showToast(msg, type) {
    var existing = document.querySelector('.toast');
    if (existing) existing.remove();
    var t = document.createElement('div');
    t.className = 'toast' + (type === 'error' ? ' toast-error' : '');
    t.innerHTML = '<i class="fa-solid fa-' + (type === 'error' ? 'circle-exclamation' : 'circle-check') + '"></i> ' + msg;
    document.body.appendChild(t);
    setTimeout(function() { t.classList.add('show'); }, 10);
    setTimeout(function() { t.classList.remove('show'); setTimeout(function() { t.remove(); }, 300); }, 3000);
}


function openLogoutModal() {
    document.getElementById("logoutModal").style.display = "flex";
}

function closeLogoutModal() {
    document.getElementById("logoutModal").style.display = "none";
}