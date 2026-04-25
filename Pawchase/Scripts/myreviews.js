const myReviews = JSON.parse((document.getElementById('my-reviews-json')?.value || '[]'));
let likedReviews = JSON.parse((document.getElementById('my-liked-reviews-json')?.value || '[]'));

const configEl = document.getElementById('my-reviews-config');
const likeUrl = configEl?.getAttribute('data-like-url') || '/Reviews/Like';
const indexUrl = configEl?.getAttribute('data-index-url') || '/Reviews/Index';
const displayName = configEl?.getAttribute('data-display-name') || 'alex';

let activeMyReview = null;
let activeMyReviewIndex = -1;

function formatRelativeEditedTime(isoDate) {
    if (!isoDate) return '';
    const date = new Date(isoDate);
    const now = new Date();
    const diffMs = now - date;
    const mins = Math.floor(diffMs / 60000);
    const hours = Math.floor(diffMs / 3600000);
    const days = Math.floor(diffMs / 86400000);

    if (mins < 1) return 'Edited just now';
    if (mins < 60) return 'Edited ' + mins + ' minute' + (mins === 1 ? '' : 's') + ' ago';
    if (hours < 24) return 'Edited ' + hours + ' hour' + (hours === 1 ? '' : 's') + ' ago';
    if (days === 1) return 'Edited yesterday';
    return 'Edited ' + days + ' days ago';
}

function refreshMyEditedIndicators() {
    document.querySelectorAll('[data-edited-at]').forEach(function (el) {
        const editedAt = el.getAttribute('data-edited-at');
        if (editedAt) {
            el.textContent = formatRelativeEditedTime(editedAt);
        }
    });
}

function openMyModal(index) {
    const r = myReviews[index];
    activeMyReview = r;
    activeMyReviewIndex = index;

    const modal = document.getElementById('my-review-modal');
    const photo = document.getElementById('my-modal-photo');
    if (r.isTextOnly) {
        modal.classList.add('single');
    } else {
        modal.classList.remove('single');
        const photoUrl = r.resolvedPhotoUrl || r.photoUrl;
        if (photoUrl) {
            photo.style.background = '#f4f7fb';
            photo.innerHTML = '<img src="' + photoUrl + '" alt="Review photo" style="width:100%;height:100%;object-fit:cover;display:block"><i class="fa-solid ' + r.icon + '" style="font-size:5rem;color:rgba(0,0,0,.15);display:none"></i>';

            const img = photo.querySelector('img');
            const fallbackIcon = photo.querySelector('i');
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

    document.getElementById('my-modal-date').textContent = r.date;
    const modalEdited = document.getElementById('my-modal-edited');
    if (r.isEdited && r.editedAt) {
        modalEdited.textContent = formatRelativeEditedTime(r.editedAt);
        modalEdited.style.display = 'block';
        modalEdited.setAttribute('data-edited-at', r.editedAt);
    } else {
        modalEdited.style.display = 'none';
        modalEdited.removeAttribute('data-edited-at');
    }
    document.getElementById('my-modal-caption').textContent = '"' + r.comment + '"';
    const myModalProduct = document.getElementById('my-modal-product');
    if (r.productName) {
        myModalProduct.style.display = 'inline-flex';
        myModalProduct.innerHTML = '<i class="fa-solid fa-bone" style="color:var(--blue)"></i> ' + r.productName;
    } else {
        myModalProduct.style.display = 'none';
        myModalProduct.innerHTML = '';
    }
    document.getElementById('my-modal-product-link').href = '/Product/Details/' + r.productId;
    document.getElementById('my-modal-likes').textContent = r.likes;
    document.getElementById('my-modal-comments-count').textContent = r.comments.length;
    document.getElementById('my-comment-review-id').value = r.id;
    const liked = document.querySelectorAll('.my-review-card')[index].querySelector('.my-review-actions .card-action')?.classList.contains('liked');
    const modalLikeBtn = document.getElementById('my-modal-like-btn');
    modalLikeBtn.setAttribute('data-review-id', r.id);
    modalLikeBtn.classList.toggle('liked', !!liked);
    modalLikeBtn.querySelector('i').className = liked ? 'fa-solid fa-heart' : 'fa-regular fa-heart';

    const stars = [];
    for (let s = 1; s <= 5; s++) {
        stars.push('<i class="' + (s <= r.stars ? 'fa-solid' : 'fa-regular') + ' fa-star"></i>');
    }
    document.getElementById('my-modal-stars').innerHTML = stars.join('');

    renderMyComments(r.comments);

    document.getElementById('my-review-modal-overlay').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeMyModal() {
    document.getElementById('my-review-modal-overlay').classList.remove('open');
    document.body.style.overflow = '';
}

function openMyModalComments(index) {
    openMyModal(index);
    document.getElementById('my-modal-comments-panel').style.display = 'block';
}

function renderMyComments(comments) {
    const box = document.getElementById('my-modal-comments-list');
    if (!comments || comments.length === 0) {
        box.innerHTML = '<div style="font-size:11px;color:var(--text-light);padding:8px 0;">No comments yet.</div>';
        return;
    }
    box.innerHTML = comments.map(function (c) {
        const initial = (c.userName || 'U')[0];
        return '<div class="comment-item"><div class="comment-avatar">' + initial + '</div><div><div class="comment-name">' + c.userName + '</div><div class="comment-time">' + (c.date || '') + '</div><div class="comment-text">' + c.text + '</div></div></div>';
    }).join('');
}

document.getElementById('my-modal-comment-form')?.addEventListener('submit', function (e) {
    e.preventDefault();
    if (!activeMyReview) return;

    const form = this;
    $.ajax({
        url: form.action,
        method: 'POST',
        data: $(form).serialize(),
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    }).done(function (data) {
        if (!data || !data.ok || data.reviewId !== activeMyReview.id) return;

        activeMyReview.comments = activeMyReview.comments || [];
        activeMyReview.comments.push(data.comment);
        if (activeMyReviewIndex >= 0 && myReviews[activeMyReviewIndex]) {
            myReviews[activeMyReviewIndex].comments = activeMyReview.comments;
            const card = document.querySelectorAll('.my-review-card')[activeMyReviewIndex];
            if (card) {
                const commentCount = card.querySelectorAll('.my-review-actions .card-action span')[1];
                if (commentCount) commentCount.textContent = data.commentCount;
            }
        }

        document.getElementById('my-modal-comments-count').textContent = data.commentCount;
        renderMyComments(activeMyReview.comments);

        const input = form.querySelector('input[name="commentText"]');
        if (input) input.value = '';
    });
});

function toggleMyComments() {
    const panel = document.getElementById('my-modal-comments-panel');
    panel.style.display = panel.style.display === 'none' || !panel.style.display ? 'block' : 'none';
}

function toggleMyLike(reviewId, btn, index) {
    $.post(likeUrl, { id: reviewId }, function (data) {
        if (!data || !data.ok) return;

        const allLikeButtons = document.querySelectorAll('[data-review-id="' + reviewId + '"]');
        allLikeButtons.forEach(function (likeBtn) {
            const icon = likeBtn.querySelector('i');
            const count = likeBtn.querySelector('span');
            if (count) {
                count.textContent = data.likes;
            }
            if (icon) {
                icon.className = data.liked ? 'fa-solid fa-heart' : 'fa-regular fa-heart';
            }
            likeBtn.classList.toggle('liked', !!data.liked);
        });

        if (typeof index === 'number' && myReviews[index]) {
            myReviews[index].likes = data.likes;
        }

        if (activeMyReview && activeMyReview.id === reviewId) {
            activeMyReview.likes = data.likes;
            document.getElementById('my-modal-likes').textContent = data.likes;
            const modalIcon = document.querySelector('#my-modal-like-btn i');
            modalIcon.className = data.liked ? 'fa-solid fa-heart' : 'fa-regular fa-heart';
            document.getElementById('my-modal-like-btn').classList.toggle('liked', !!data.liked);
        }

        const exists = likedReviews.some(function (r) { return r.id === reviewId; });
        if (data.liked && !exists) {
            const source = myReviews.find(function (r) { return r.id === reviewId; });
            if (source) {
                likedReviews.push({
                    id: source.id,
                    name: displayName,
                    stars: source.stars,
                    comment: source.comment,
                    likes: data.likes,
                    date: source.date
                });
            }
        } else if (!data.liked && exists) {
            likedReviews = likedReviews.filter(function (r) { return r.id !== reviewId; });
        }

        likedReviews = likedReviews.map(function (r) {
            return r.id === reviewId ? Object.assign({}, r, { likes: data.likes }) : r;
        });

        const countEl = document.querySelector('.stat-item.clickable strong');
        if (countEl) countEl.textContent = likedReviews.length;
    });
}

function openLikedModal() {
    const list = document.getElementById('liked-modal-list');
    if (!likedReviews.length) {
        list.innerHTML = '<div style="padding:20px 0;font-size:12px;color:var(--text-light);text-align:center;">No liked reviews yet.</div>';
    } else {
        list.innerHTML = likedReviews.map(function (r) {
            const stars = Array.from({ length: 5 }).map(function (_, idx) {
                return '<i class="' + (idx < r.stars ? 'fa-solid' : 'fa-regular') + ' fa-star" style="color:#E6A520;font-size:11px;"></i>';
            }).join('');
            return '<div class="liked-item">' +
                '<div style="font-size:12px;font-weight:700;color:var(--text);">' + r.name + '</div>' +
                '<div style="font-size:10px;color:var(--text-light);">' + r.date + '</div>' +
                '<div style="margin:6px 0;">' + stars + '</div>' +
                '<div style="font-size:12px;color:var(--text-mid);line-height:1.4;">"' + r.comment + '"</div>' +
                '<div class="liked-actions">' +
                '<button class="review-action liked" onclick="toggleLikeFromLiked(' + r.id + ', this)"><i class="fa-solid fa-heart"></i><span>' + r.likes + '</span></button>' +
                '<a class="view-review-link" href="' + indexUrl + '?openReviewId=' + r.id + '">View Review</a>' +
                '</div>' +
                '</div>';
        }).join('');
    }

    document.getElementById('liked-modal-overlay').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeLikedModal() {
    document.getElementById('liked-modal-overlay').classList.remove('open');
    document.body.style.overflow = '';
}

function toggleLikeFromLiked(reviewId, btn) {
    $.post(likeUrl, { id: reviewId }, function (data) {
        if (!data || !data.ok) return;
        const liked = !!data.liked;
        btn.classList.toggle('liked', liked);
        btn.querySelector('i').className = liked ? 'fa-solid fa-heart' : 'fa-regular fa-heart';
        btn.querySelector('span').textContent = data.likes;

        if (!liked) {
            likedReviews = likedReviews.filter(function (r) { return r.id !== reviewId; });
            const item = btn.closest('.liked-item');
            if (item) item.remove();
            const countEl = document.querySelector('.stat-item.clickable strong');
            if (countEl) countEl.textContent = likedReviews.length;
            const list = document.getElementById('liked-modal-list');
            if (likedReviews.length === 0 && list) {
                list.innerHTML = '<div style="padding:20px 0;font-size:12px;color:var(--text-light);text-align:center;">No liked reviews yet.</div>';
            }
        }
    });
}

function openEditModal(id, stars, comment) {
    document.getElementById('edit-id').value = id;
    document.getElementById('edit-stars').value = stars;
    document.getElementById('edit-comment').value = comment;
    setEditStar(stars);
    // Store originals to detect changes later
    originalEditValues.stars = String(stars);
    originalEditValues.comment = comment;
    document.getElementById('edit-modal-overlay').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeEditModal() {
    document.getElementById('edit-modal-overlay').classList.remove('open');
    document.body.style.overflow = '';
}

function setEditStar(val) {
    document.getElementById('edit-stars').value = val;
    const stars = document.querySelectorAll('#edit-stars-ui i');
    stars.forEach(function (s, idx) {
        s.className = idx < val ? 'fa-solid fa-star' : 'fa-regular fa-star';
        s.style.color = '#E6A520';
        s.style.cursor = 'pointer';
    });
}

(function initToasts() {
    refreshMyEditedIndicators();
    setInterval(refreshMyEditedIndicators, 60000);

    const updated = document.getElementById('updated-toast');
    const deleted = document.getElementById('deleted-toast');
    if (updated) setTimeout(function () { updated.style.display = 'none'; }, 5000);
    if (deleted) setTimeout(function () { deleted.style.display = 'none'; }, 5000);
})();

// ── DELETE MODAL ──────────────────────────────────────────────────────────
var pendingDeleteId = null;
var originalEditValues = { stars: null, comment: null };

function openDeleteModal(reviewId) {
    pendingDeleteId = reviewId;
    // Close edit modal if somehow open
    document.getElementById('edit-modal-overlay').classList.remove('open');
    document.getElementById('deleteModal').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeDeleteModal() {
    pendingDeleteId = null;
    document.getElementById('deleteModal').classList.remove('open');
    document.body.style.overflow = '';
}

function submitDeleteForm() {
    if (!pendingDeleteId) return;
    var form = document.getElementById('delete-form-' + pendingDeleteId);
    if (form) form.submit();
}

// ── SAVE MODAL ────────────────────────────────────────────────────────────
function openSaveModal() {
    var comment = document.getElementById('edit-comment')?.value || '';
    var stars = document.getElementById('edit-stars')?.value || '';
    // Always close the edit modal first — no stacking
    document.getElementById('edit-modal-overlay').classList.remove('open');
    // Check if anything actually changed
    if (comment === originalEditValues.comment && stars === originalEditValues.stars) {
        openNoChangesModal();
        return;
    }
    var preview = comment.length > 60 ? comment.substring(0, 60) + '...' : comment;
    var msgEl = document.getElementById('saveModalText');
    if (msgEl) msgEl.textContent = 'Save your updated review: "' + preview + '"?';
    document.getElementById('saveModal').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeSaveModal() {
    document.getElementById('saveModal').classList.remove('open');
    // Reopen the edit modal so the user doesn't lose their changes
    document.getElementById('edit-modal-overlay').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function submitEditForm() {
    var overlay = document.getElementById('edit-modal-overlay');
    var form = overlay ? overlay.querySelector('form') : null;
    if (form) form.submit();
}

// ── NO CHANGES MODAL ──────────────────────────────────────────────────────
function openNoChangesModal() {
    // edit-modal-overlay is already closed before this is called
    document.getElementById('noChangesModal').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeNoChangesModal() {
    document.getElementById('noChangesModal').classList.remove('open');
    // Return to edit modal so user can keep editing
    document.getElementById('edit-modal-overlay').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function cancelNoChangesModal() {
    // Cancel: just close the modal entirely, do not reopen the edit modal
    document.getElementById('noChangesModal').classList.remove('open');
    document.body.style.overflow = '';
}
