function paginationSearch(event, form, page) {
    if (event) event.preventDefault();
    if (!form) return;

    const url = form.action;
    const method = (form.method || "GET").toUpperCase();
    const targetId = form.dataset.target;
    const formData = new FormData(form);
    formData.append("page", page);

    let fetchUrl = url;
    if (method === "GET") {
        fetchUrl = url + "?" + new URLSearchParams(formData).toString();
    }

    const targetEl = targetId ? document.getElementById(targetId) : null;
    if (targetEl) {
        targetEl.innerHTML = `
            <div class="text-center py-5">
                <div class="spinner-border text-primary" role="status"></div>
                <div class="mt-2 text-muted">Đang tải...</div>
            </div>`;
    }

    fetch(fetchUrl, { method: method, body: method === "GET" ? null : formData })
        .then(res => res.text())
        .then(html => { if (targetEl) targetEl.innerHTML = html; })
        .catch(() => {
            if (targetEl) targetEl.innerHTML =
                `<div class="text-danger text-center py-4">
                    <i class="bi bi-exclamation-circle me-2"></i>Không tải được dữ liệu
                 </div>`;
        });
}

function updateCartBadge(count) {
    const badge = document.getElementById("cartBadge") || document.getElementById("cart-badge");
    if (!badge) return;
    if (count > 0) {
        badge.textContent = count > 99 ? "99+" : count;
        badge.style.display = "flex";
        badge.classList.remove('d-none');
    } else {
        badge.style.display = "none";
    }
}

window.addEventListener("scroll", function () {
    const btn = document.getElementById("scrollToTop");
    if (!btn) return;
    btn.style.display = window.scrollY > 300 ? "flex" : "none";
});

async function handleAddToCart(productId, quantity) {
    const formData = new FormData();
    formData.append('productId', productId);
    formData.append('quantity', quantity);

    try {
        const response = await fetch('/Cart/Add', {
            method: 'POST',
            body: formData
        });

        if (response.redirected && response.url.includes('/User/Login')) {
            window.location.href = '/User/Login';
            return;
        }

        const result = await response.json();

        if (result.ok) {
            const toastEl = document.getElementById('cartSuccessToast');
            if (toastEl) {
                const toast = new bootstrap.Toast(toastEl);
                toast.show();
            }
            if (result.cartCount !== undefined) {
                updateCartBadge(result.cartCount);
            }
            return result;
        } else if (result.redirectUrl) {
            window.location.href = result.redirectUrl;
        } else {
            alert(result.message || "Có lỗi xảy ra.");
        }
    } catch (error) {
        console.error('Lỗi kết nối hoặc chưa đăng nhập:', error);
        window.location.href = '/User/Login';
    }
}

document.addEventListener('DOMContentLoaded', function () {
    document.body.addEventListener('submit', function (e) {
        if (e.target.classList.contains('ajax-add-to-cart')) {
            e.preventDefault();
            const form = e.target;
            const productId = form.querySelector('input[name="productId"]').value;
            const quantity = form.querySelector('input[name="quantity"]').value;
            handleAddToCart(productId, quantity);
        }
    });
});
