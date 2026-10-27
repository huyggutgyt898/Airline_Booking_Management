(() => {
  "use strict";

  const API_BASE = "/api/booking";
  const LOGIN_URL = "/Auth/Login";

  const grid = document.querySelector("#rewards-grid");
  const balance = document.querySelector("#loyalty-balance");
  const loading = document.querySelector("#rewards-loading");
  const toast = document.querySelector("#toast");

  const state = {
    context: { isAuthenticated: false, loyaltyPoints: 0, redeemedPromoCodes: [] },
    promotions: []
  };

  let toastTimer;

  function showToast(message) {
    if (!toast) return;
    toast.textContent = message;
    toast.hidden = false;
    window.clearTimeout(toastTimer);
    toastTimer = window.setTimeout(() => {
      toast.hidden = true;
    }, 3600);
  }

  function renderIcons() {
    if (window.lucide && typeof window.lucide.createIcons === "function") {
      window.lucide.createIcons();
    }
  }

  const escapeHtml = (value) => String(value ?? "").replace(/[&<>"']/g, (character) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;"
  })[character]);

  const pointsLabel = (points) => `${new Intl.NumberFormat("vi-VN").format(points)} điểm`;

  async function apiFetch(path, options = {}) {
    const response = await fetch(`${API_BASE}${path}`, {
      credentials: "same-origin",
      headers: { "Content-Type": "application/json" },
      ...options
    });

    let data = null;
    try {
      data = await response.json();
    } catch {
      data = null;
    }

    if (!response.ok) {
      throw new Error(data?.message || "Không thể kết nối máy chủ. Vui lòng thử lại.");
    }

    return data;
  }

  function updateBalance() {
    if (balance) {
      balance.textContent = new Intl.NumberFormat("vi-VN").format(state.context.loyaltyPoints || 0);
    }
  }

  function buttonState(promo) {
    const points = promo.pointsRequired || 0;
    if (points <= 0) {
      return { label: "Sao chép mã", disabled: false, action: "copy" };
    }

    if (state.context.redeemedPromoCodes.includes(promo.code)) {
      return { label: "Đã đổi", disabled: true, action: "none" };
    }

    if (!state.context.isAuthenticated) {
      return { label: "Đăng nhập để đổi", disabled: false, action: "login" };
    }

    if ((state.context.loyaltyPoints || 0) < points) {
      const missing = points - (state.context.loyaltyPoints || 0);
      return { label: "Chưa đủ điểm", disabled: true, action: "none", title: `Còn thiếu ${pointsLabel(missing)}` };
    }

    return { label: `Đổi · ${pointsLabel(points)}`, disabled: false, action: "redeem" };
  }

  function renderPromotions() {
    if (!grid) return;

    if (!state.promotions.length) {
      grid.innerHTML = '<p class="mock-note">Hiện chưa có chương trình khuyến mãi nào.</p>';
      return;
    }

    grid.innerHTML = state.promotions.map((promo) => {
      const points = promo.pointsRequired || 0;
      const view = buttonState(promo);

      return `
        <article class="offer-card reward-card" data-promo-code="${escapeHtml(promo.code)}">
          <div class="reward-card-heading">
            <span class="offer-icon"><i data-lucide="ticket-percent" aria-hidden="true"></i></span>
            <span class="offer-label">${points > 0 ? "VOUCHER ĐỔI ĐIỂM" : "MÃ KHUYẾN MÃI"}</span>
          </div>
          <strong class="reward-discount">${escapeHtml(promo.discountLabel)}</strong>
          <h3>${escapeHtml(promo.conditionLabel)}</h3>
          <dl class="reward-details">
            <div><dt>Điểm cần đổi</dt><dd>${points > 0 ? pointsLabel(points) : "Không cần điểm"}</dd></div>
            <div><dt>Thời hạn</dt><dd>${escapeHtml(promo.validityLabel)}</dd></div>
            <div><dt>Đã phát hành</dt><dd>${promo.usedCount} / ${promo.usageLimit}</dd></div>
          </dl>
          <button class="button button-primary reward-redeem" type="button"
            data-redeem-voucher="${escapeHtml(promo.code)}"
            data-points-cost="${points}"
            data-action="${view.action}"
            ${view.title ? `title="${escapeHtml(view.title)}"` : ""}
            ${view.disabled ? "disabled" : ""}>${view.label}</button>
        </article>`;
    }).join("");

    renderIcons();
  }

  async function loadPromotions() {
    const data = await apiFetch("/promotions");
    state.context.loyaltyPoints = data.loyaltyPoints ?? state.context.loyaltyPoints;
    state.context.redeemedPromoCodes = data.redeemedPromoCodes || [];
    state.promotions = data.promotions || [];
    updateBalance();
    renderPromotions();
  }

  async function copyCode(code) {
    try {
      await navigator.clipboard.writeText(code);
      showToast(`Đã sao chép mã ${code}. Nhập mã này ở bước thanh toán.`);
    } catch {
      showToast(`Mã khuyến mãi: ${code}`);
    }
  }

  async function redeem(button) {
    const code = button.dataset.redeemVoucher;

    button.disabled = true;
    try {
      const result = await apiFetch("/redeem", {
        method: "POST",
        body: JSON.stringify({ promoCode: code })
      });

      state.context.loyaltyPoints = result.loyaltyPoints ?? state.context.loyaltyPoints;
      state.context.redeemedPromoCodes = [...state.context.redeemedPromoCodes, code];
      updateBalance();
      showToast(result.message || `Đã đổi voucher ${code}.`);
      renderPromotions();
    } catch (error) {
      showToast(error.message);
      renderPromotions();
    }
  }

  grid?.addEventListener("click", (event) => {
    const button = event.target.closest("[data-redeem-voucher]");
    if (!button || button.disabled) return;

    if (button.dataset.action === "login") {
      window.location.href = LOGIN_URL;
      return;
    }

    if (button.dataset.action === "copy") {
      copyCode(button.dataset.redeemVoucher);
      return;
    }

    if (button.dataset.action === "redeem") {
      redeem(button);
    }
  });

  async function initialize() {
    renderIcons();

    const contextPromise = apiFetch("/context").catch(() => null);

    try {
      const context = await contextPromise;
      if (context) {
        state.context = {
          isAuthenticated: Boolean(context.isAuthenticated),
          loyaltyPoints: context.loyaltyPoints || 0,
          redeemedPromoCodes: context.redeemedPromoCodes || []
        };
        updateBalance();
      }

      await loadPromotions();
      loading?.remove();
    } catch (error) {
      loading?.remove();
      if (grid) grid.innerHTML = `<p class="mock-note">${escapeHtml(error.message)}</p>`;
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initialize, { once: true });
  } else {
    initialize();
  }
})();
