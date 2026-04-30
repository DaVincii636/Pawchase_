var reviewsConfig = window.reviewsData || {};
var currentFilters = reviewsConfig.currentFilters || { sortBy: '', category: '', photoFilter: '' };
var urls = reviewsConfig.urls || {};
var reviews = reviewsConfig.reviews || [];
var openReviewId = reviewsConfig.openReviewId || 0;
// eunice modified: read login state passed from server
var isLoggedIn = reviewsConfig.isLoggedIn || false;

var activeReview = null;
var pendingReportReviewId = null;

var byId = function (id) { return document.getElementById(id); };
var setBodyLock = function (locked) { document.body.style.overflow = locked ? 'hidden' : ''; };

// eunice modified: redirects to Login page with returnUrl so user comes back after logging in
var _loginUrl = '/Account/Login';
function showLoginRequired() {
    window.location.href = _loginUrl + '?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
}

function renderStars(stars) {
    var html = [];
    for (var s = 1; s <= 5; s++) {
        html.push('<i class="' + (s <= stars ? 'fa-solid' : 'fa-regular') + ' fa-star"></i>');
    }
    return html.join('');
}

function formatRelativeTime(isoDate) {
    if (!isoDate) return '';
    var date = new Date(isoDate);
    var now = new Date();
    var diffMs = now - date;
    var mins = Math.floor(diffMs / 60000);
    var hours = Math.floor(diffMs / 3600000);
    var days = Math.floor(diffMs / 86400000);

    if (mins < 1) return 'Edited just now';
    if (mins < 60) return 'Edited ' + mins + ' minute' + (mins === 1 ? '' : 's') + ' ago';
    if (hours < 24) return 'Edited ' + hours + ' hour' + (hours === 1 ? '' : 's') + ' ago';
    if (days === 1) return 'Edited yesterday';
    return 'Edited ' + days + ' days ago';
}

function refreshEditedIndicators() {
    document.querySelectorAll('[data-edited-at]').forEach(function (el) {
        var editedAt = el.getAttribute('data-edited-at');
        if (editedAt) el.textContent = formatRelativeTime(editedAt);
    });
}

function initWriteReviewCounter() {
    var textarea = byId('write-review-comment');
    var counter = byId('write-review-counter');
    var submitBtn = byId('write-review-submit');
    if (!textarea || !counter || !submitBtn) return;

    var maxChars = 200;
    var update = function () {
        var len = textarea.value.length;
        var exceeded = len > maxChars;

        counter.textContent = len + ' / ' + maxChars + ' characters';
        counter.style.color = exceeded ? '#e85d5d' : 'var(--text-light)';
        submitBtn.disabled = exceeded;
        submitBtn.style.opacity = exceeded ? '0.6' : '1';
        submitBtn.style.cursor = exceeded ? 'not-allowed' : 'pointer';
    };

    textarea.addEventListener('input', update);
    textarea.closest('form')?.addEventListener('submit', function (e) {
        if (textarea.value.length > maxChars) {
            e.preventDefault();
            update();
        }
    });

    update();
}

function applyFilter(changes) {
    if (changes.sortBy === '' || changes.category === '') return;

    var next = {
        sortBy: changes.sortBy || currentFilters.sortBy,
        category: changes.category || currentFilters.category,
        photoFilter: changes.photoFilter || currentFilters.photoFilter
    };

    var params = new URLSearchParams();
    if (next.sortBy) params.set('sortBy', next.sortBy);
    if (next.category) params.set('category', next.category);
    if (next.photoFilter) params.set('photoFilter', next.photoFilter);

    window.location.href = (urls.index || '/Reviews/Index') + '?' + params.toString();
}

function openWriteModal() {
    // eunice modified: require login to write a review
    if (!isLoggedIn) { showLoginRequired(); return; }
    byId('write-modal').classList.add('open');
    setBodyLock(true);
}

function closeWriteModal() {
    byId('write-modal').classList.remove('open');
    setBodyLock(false);
}

function setWriteStar(val) {
    byId('stars-val').value = val;
    document.querySelectorAll('#write-stars i').forEach(function (s, i) {
        s.className = i < val ? 'fa-solid fa-star' : 'fa-regular fa-star';
    });
}

function renderComments(comments) {
    var box = byId('modal-comments-list');
    if (!comments || comments.length === 0) {
        box.innerHTML = '<div style="font-size:11px;color:var(--text-light);padding:8px 0;">No comments yet.</div>';
        return;
    }

    box.innerHTML = comments.map(function (c) {
        var initial = (c.userName || 'U')[0];
        return '<div class="comment-item">'
            + '<div class="comment-avatar">' + initial + '</div>'
            + '<div>'
            + '<div class="comment-name">' + c.userName + '</div>'
            + '<div class="comment-time">' + (c.date || '') + '</div>'
            + '<div class="comment-text">' + c.text + '</div>'
            + '</div>'
            + '</div>';
    }).join('');
}

function openModal(index) {
    var r = reviews[index];
    activeReview = r;

    var modal = byId('review-modal');
    var photo = byId('modal-photo');
    var editedEl = byId('modal-edited');
    var modalLikeBtn = byId('modal-like-btn');
    var modalReportBtn = byId('modal-report-btn');

    modal.classList.toggle('single', !!r.isTextOnly);
    if (!r.isTextOnly) {
        var photoUrl = r.resolvedPhotoUrl || r.photoUrl;
        if (photoUrl) {
            photo.style.background = '#f4f7fb';
            photo.innerHTML = '<img src="' + photoUrl + '" alt="Review photo" style="width:100%;height:100%;object-fit:cover;display:block"><i class="fa-solid ' + r.icon + '" style="font-size:5rem;color:rgba(0,0,0,.15);display:none"></i>';

            var img = photo.querySelector('img');
            var fallbackIcon = photo.querySelector('i');
            if (img && fallbackIcon) {
                img.onerror = function () {
                    this.remove();
                    photo.style.background = r.color;
                    fallbackIcon.style.display = 'inline-flex';
                };
            }
        } else {
            photo.style.background = r.color;
            photo.innerHTML = '<i class="fa-solid ' + r.icon + '" style="font-size:5rem;color:rgba(0,0,0,.15)"></i>';
        }
    }

    byId('modal-avatar').textContent = (r.name || 'A')[0];
    byId('modal-name').textContent = r.name;
    byId('modal-date').textContent = r.date + ' · Verified Buyer';
    byId('modal-caption').textContent = '"' + r.comment + '"';
    var modalProduct = byId('modal-product');
    if (r.productName) {
        modalProduct.style.display = 'inline-flex';
        modalProduct.innerHTML = '<i class="fa-solid fa-bone" style="color:var(--blue)"></i> ' + r.productName;
    } else {
        modalProduct.style.display = 'none';
        modalProduct.innerHTML = '';
    }
    byId('modal-product-link').href = '/Product/Details/' + r.productId;
    byId('modal-likes').textContent = r.likes;
    byId('modal-reports').textContent = r.reportCount;
    byId('modal-comment-count').textContent = r.comments.length;
    byId('comment-review-id').value = r.id;
    byId('modal-stars').innerHTML = renderStars(r.stars);

    if (r.isEdited && r.editedAt) {
        editedEl.textContent = formatRelativeTime(r.editedAt);
        editedEl.style.display = 'block';
        editedEl.setAttribute('data-edited-at', r.editedAt);
    } else {
        editedEl.style.display = 'none';
        editedEl.removeAttribute('data-edited-at');
    }

    var isLiked = !!document.querySelector('[data-review-id="' + r.id + '"]')?.classList.contains('liked');
    modalLikeBtn.setAttribute('data-review-id', r.id);
    modalLikeBtn.classList.toggle('liked', isLiked);
    modalLikeBtn.querySelector('i').className = isLiked ? 'fa-solid fa-heart' : 'fa-regular fa-heart';

    modalReportBtn.setAttribute('data-report-review-id', r.id);
    modalReportBtn.classList.toggle('reported', !!r.isReported);
    modalReportBtn.querySelector('i').className = r.isReported ? 'fa-solid fa-flag' : 'fa-regular fa-flag';

    renderComments(r.comments);

    // ── Verified Purchase badge ──────────────────────────────────────
    var verifiedEl = byId('modal-verified-badge');
    if (verifiedEl) {
        verifiedEl.style.display = r.isVerifiedPurchase ? 'inline-flex' : 'none';
    }

    // ── Seller Reply ─────────────────────────────────────────────────
    var replyWrap = byId('modal-seller-reply');
    if (replyWrap) {
        if (r.sellerReply) {
            byId('modal-seller-reply-text').textContent = r.sellerReply;
            byId('modal-seller-reply-date').textContent = r.sellerReplyDate ? 'Seller · ' + r.sellerReplyDate : 'Seller';
            replyWrap.style.display = 'block';
        } else {
            replyWrap.style.display = 'none';
        }
    }

    byId('review-modal-overlay').classList.add('open');
    setBodyLock(true);
}

function closeModal() {
    byId('review-modal-overlay').classList.remove('open');
    setBodyLock(false);
    activeReview = null;
}

function openModalComments(index) {
    // eunice modified: guests can view comments freely; login only needed when sending
    openModal(index);
    byId('modal-comments-panel').style.display = 'block';
}

function toggleCommentPanel() {
    var panel = byId('modal-comments-panel');
    panel.style.display = panel.style.display === 'none' || !panel.style.display ? 'block' : 'none';
}

function openReportModal(reviewId) {
    // eunice modified: require login to report a review
    if (!isLoggedIn) { showLoginRequired(); return; }
    pendingReportReviewId = reviewId;
    byId('report-error').textContent = '';
    document.querySelectorAll('input[name="report-reason"]').forEach(function (radio) { radio.checked = false; });
    // eunice modified: reset Other input field when modal opens
    var otherInput = byId('report-other-input');
    if (otherInput) { otherInput.value = ''; otherInput.style.display = 'none'; }
    byId('report-modal-overlay').classList.add('open');
}

function closeReportModal() {
    byId('report-modal-overlay').classList.remove('open');
    pendingReportReviewId = null;
}

// eunice modified: show/hide the Other input field based on radio selection
function toggleOtherInput(radio) {
    var otherInput = byId('report-other-input');
    if (!otherInput) return;
    otherInput.style.display = radio.value === 'Other' ? 'block' : 'none';
    if (radio.value !== 'Other') otherInput.value = '';
}

function applyReportedState(reviewId, reportCount) {
    var reviewIndex = reviews.findIndex(function (r) { return r.id === reviewId; });
    if (reviewIndex >= 0) {
        reviews[reviewIndex].reportCount = reportCount;
        reviews[reviewIndex].isReported = true;
        // eunice modified: auto-unlike the review when reported
        reviews[reviewIndex].isLiked = false;
    }

    document.querySelectorAll('[data-report-review-id="' + reviewId + '"]').forEach(function (btn) {
        btn.classList.add('reported');
        var icon = btn.querySelector('i');
        var count = btn.querySelector('span');
        if (icon) icon.className = 'fa-solid fa-flag';
        if (count) count.textContent = reportCount;
    });

    // eunice modified: auto-unlike — update like button UI and likes count
    var likedBtns = document.querySelectorAll('[data-review-id="' + reviewId + '"]');
    likedBtns.forEach(function (btn) {
        if (btn.classList.contains('liked')) {
            btn.classList.remove('liked');
            var icon = btn.querySelector('i');
            var countSpan = btn.querySelector('span');
            if (icon) icon.className = 'fa-regular fa-heart';
            if (countSpan) {
                var currentCount = parseInt(countSpan.textContent) || 0;
                if (currentCount > 0) countSpan.textContent = currentCount - 1;
            }
        }
    });

    var card = byId('review-' + reviewId);
    if (card) card.classList.add('reported-by-me');

    if (activeReview && activeReview.id === reviewId) {
        activeReview.reportCount = reportCount;
        activeReview.isReported = true;
        byId('modal-reports').textContent = reportCount;
        var modalReportBtn = byId('modal-report-btn');
        modalReportBtn.classList.add('reported');
        modalReportBtn.querySelector('i').className = 'fa-solid fa-flag';
        // eunice modified: auto-unlike in modal too
        var modalLikeBtn = byId('modal-like-btn');
        if (modalLikeBtn && modalLikeBtn.classList.contains('liked')) {
            modalLikeBtn.classList.remove('liked');
            modalLikeBtn.querySelector('i').className = 'fa-regular fa-heart';
            var modalLikes = byId('modal-likes');
            if (modalLikes) {
                var lc = parseInt(modalLikes.textContent) || 0;
                if (lc > 0) modalLikes.textContent = lc - 1;
            }
        }
    }
}

function submitReport() {
    if (!pendingReportReviewId) return;

    var selected = document.querySelector('input[name="report-reason"]:checked');
    if (!selected) {
        byId('report-error').textContent = 'Please choose a reason before submitting your report.';
        return;
    }

    // eunice modified: if "Other" is selected, use the custom input text as the reason
    var reason = selected.value;
    if (reason === 'Other') {
        var otherInput = byId('report-other-input');
        var otherText = otherInput ? otherInput.value.trim() : '';
        if (!otherText) {
            byId('report-error').textContent = 'Please describe the issue.';
            return;
        }
        reason = 'Other: ' + otherText;
    }

    $.post(urls.report || '/Reviews/Report', { id: pendingReportReviewId, reason: reason }, function (data) {
        if (!data || !data.ok) {
            byId('report-error').textContent = (data && data.error) ? data.error : 'Unable to submit report.';
            return;
        }

        applyReportedState(pendingReportReviewId, data.reportCount);
        closeReportModal();
    });
}

function updateCommentCount(reviewId, count) {
    document.querySelectorAll('[data-comment-review-id="' + reviewId + '"]').forEach(function (el) {
        var countSpan = el.querySelector('span');
        if (countSpan) countSpan.textContent = count;
    });

    if (activeReview && activeReview.id === reviewId) {
        byId('modal-comment-count').textContent = count;
    }
}

function toggleLike(reviewId, btn) {
    // eunice modified: require login to like
    if (!isLoggedIn) { showLoginRequired(); return; }
    // eunice modified: cannot like a review you already reported
    var reviewObj = reviews.find(function (r) { return r.id === reviewId; });
    if (reviewObj && reviewObj.isReported) {
        var toast = document.createElement('div');
        toast.className = 'toast-small';
        toast.style.cssText = 'position:fixed;top:90px;right:16px;z-index:9999;background:#e85d5d;';
        toast.innerHTML = '<i class="fa-solid fa-circle-xmark"></i> You cannot like a review you have reported.';
        document.body.appendChild(toast);
        setTimeout(function () { toast.remove(); }, 3500);
        return;
    }
    $.post(urls.like || '/Reviews/Like', { id: reviewId }, function (data) {
        if (!data || !data.ok) return;

        var icon = btn.querySelector('i');
        var count = btn.querySelector('span');
        count.textContent = data.likes;
        icon.className = data.liked ? 'fa-solid fa-heart' : 'fa-regular fa-heart';
        btn.classList.toggle('liked', !!data.liked);

        document.querySelectorAll('[data-review-id="' + reviewId + '"]').forEach(function (el) {
            var elCount = el.querySelector('span');
            var elIcon = el.querySelector('i');
            if (elCount) elCount.textContent = data.likes;
            if (elIcon) elIcon.className = data.liked ? 'fa-solid fa-heart' : 'fa-regular fa-heart';
            el.classList.toggle('liked', !!data.liked);
        });

        var reviewIndex = reviews.findIndex(function (r) { return r.id === reviewId; });
        if (reviewIndex >= 0) reviews[reviewIndex].likes = data.likes;

        if (activeReview && activeReview.id === reviewId) {
            activeReview.likes = data.likes;
            byId('modal-likes').textContent = data.likes;
            var modalIcon = document.querySelector('#modal-like-btn i');
            modalIcon.className = data.liked ? 'fa-solid fa-heart' : 'fa-regular fa-heart';
            byId('modal-like-btn').classList.toggle('liked', !!data.liked);
        }
    });
}

byId('modal-comment-form')?.addEventListener('submit', function (e) {
    e.preventDefault();
    // eunice modified: require login to post a comment
    if (!isLoggedIn) { showLoginRequired(); return; }
    if (!activeReview) return;

    var form = this;
    $.ajax({
        url: form.action,
        method: 'POST',
        data: $(form).serialize(),
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    }).done(function (data) {
        if (!data || !data.ok || data.reviewId !== activeReview.id) return;

        activeReview.comments = activeReview.comments || [];
        activeReview.comments.push(data.comment);

        var reviewIndex = reviews.findIndex(function (r) { return r.id === activeReview.id; });
        if (reviewIndex >= 0) reviews[reviewIndex].comments = activeReview.comments;

        renderComments(activeReview.comments);
        updateCommentCount(activeReview.id, data.commentCount);

        var input = form.querySelector('input[name="commentText"]');
        if (input) input.value = '';
    });
});

(function init() {
    initWriteReviewCounter();
    refreshEditedIndicators();
    setInterval(refreshEditedIndicators, 60000);

    var toast = byId('posted-toast');
    if (toast) {
        setTimeout(function () { toast.style.display = 'none'; }, 5000);
    }

    if (openReviewId > 0) {
        var idx = reviews.findIndex(function (r) { return r.id === openReviewId; });
        if (idx >= 0) openModal(idx);
    }
})();
