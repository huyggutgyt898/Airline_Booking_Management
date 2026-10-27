(() => {
  "use strict";

  const API_BASE = "/api/lookup";

  const mineList = document.querySelector("#lookup-mine-list");
  const refreshButton = document.querySelector("#lookup-refresh");
  const form = document.querySelector("#lookup-form");
  const codeInput = document.querySelector("#lookup-code");
  const contactInput = document.querySelector("#lookup-contact");
  const submitButton = document.querySelector("#lookup-submit");
  const errorBox = document.querySelector("#lookup-error");
  const results = document.querySelector("#lookup-results");
  const toast = document.querySelector("#toast");

  let toastTimer;

  const money = (amount) => `${new Intl.NumberFormat("vi-VN").format(Math.round(Number(amount) || 0))} đ`;

  const escapeHtml = (value) => String(value ?? "").replace(/[&<>"']/g, (character) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;"
  })[character]);

  function renderIcons() {
    if (window.lucide && typeof window.lucide.createIcons === "function") {
      window.lucide.createIcons();
    }
  }

  function showToast(message) {
    if (!toast) return;
    toast.textContent = message;
    toast.hidden = false;
    window.clearTimeout(toastTimer);
    toastTimer = window.setTimeout(() => {
      toast.hidden = true;
    }, 3200);
  }

  function formatDateTime(value) {
    if (!value) return "—";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "—";
    const pad = (part) => String(part).padStart(2, "0");
    return `${pad(date.getHours())}:${pad(date.getMinutes())} ${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}`;
  }

  async function request(path, options = {}) {
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

    return data || { ok: false, bookings: [] };
  }

  function bookingCard(booking) {
    const tone = ["success", "pending", "muted", "danger"].includes(booking.statusTone)
      ? booking.statusTone
      : "muted";

    const flights = (booking.flights || []).map((flight) => `
      <li class="booking-flight">
        <span class="booking-flight-direction">${escapeHtml(flight.directionLabel)}</span>
        <div class="booking-flight-main">
          <strong>${escapeHtml(flight.flightNumber)}</strong>
          <span>${escapeHtml(flight.departureCode)} ${escapeHtml(flight.departureTime)} → ${escapeHtml(flight.arrivalCode)} ${escapeHtml(flight.arrivalTime)}</span>
        </div>
        <span class="booking-flight-date">${escapeHtml(flight.date)}</span>
      </li>`).join("");

    const passengers = (booking.passengers || []).map((passenger) => `
      <li>
        <span>${escapeHtml(passenger.fullName)}</span>
        <small>${escapeHtml(passenger.typeLabel)} · ${escapeHtml(passenger.seatLabel)}</small>
      </li>`).join("");

    const contact = [booking.contactName, booking.contactEmail || booking.contactPhone]
      .filter((part) => part && part.trim().length > 0)
      .map(escapeHtml)
      .join(" · ");

    const discount = Number(booking.discountAmount || 0) > 0
      ? `<div><dt>Giảm giá</dt><dd>−${money(booking.discountAmount)}</dd></div>`
      : "";

    const hold = booking.status === "PendingPayment" && booking.expiresAt
      ? `<p class="booking-hold">
           <i data-lucide="clock-3" aria-hidden="true"></i>
           <span>Giữ chỗ đến <strong>${formatDateTime(booking.expiresAt)}</strong>. Quá hạn, đơn sẽ tự động huỷ và giải phóng ghế.</span>
         </p>`
      : "";

    return `
      <article class="booking-card">
        <header class="booking-card-head">
          <div>
            <p class="booking-card-label">Mã đặt chỗ</p>
            <p class="booking-pnr">${escapeHtml(booking.pnr)}</p>
          </div>
          <div class="booking-card-badges">
            <span class="status-pill is-${tone}">${escapeHtml(booking.statusLabel)}</span>
            <span class="booking-trip-pill">${escapeHtml(booking.tripLabel)}</span>
          </div>
        </header>
        <ul class="booking-flights">${flights}</ul>
        <ul class="booking-passengers">${passengers}</ul>
        ${hold}
        <dl class="booking-card-foot">
          <div><dt>Người liên hệ</dt><dd>${contact || "—"}</dd></div>
          <div><dt>Thanh toán</dt><dd>${escapeHtml(booking.paymentLabel)}</dd></div>
          <div><dt>Ngày đặt</dt><dd>${formatDateTime(booking.bookingDate)}</dd></div>
          <div><dt>Tổng tiền</dt><dd class="booking-total">${money(booking.totalAmount)}</dd></div>
          ${discount}
        </dl>
      </article>`;
  }

  function emptyMineHtml() {
    return `
      <div class="empty-state">
        <span class="empty-icon"><i data-lucide="ticket" aria-hidden="true"></i></span>
        <h2>Bạn chưa có đơn đặt chỗ nào</h2>
        <p>Đặt chuyến bay đầu tiên để theo dõi đơn tại đây.</p>
        <a class="button button-primary" href="/">Đặt vé ngay</a>
      </div>`;
  }

  function showError(message) {
    if (!errorBox) return;
    errorBox.textContent = message;
    errorBox.hidden = false;
  }

  function hideError() {
    if (errorBox) errorBox.hidden = true;
  }

  async function loadMine() {
    if (!mineList) return;

    try {
      const data = await request("/mine");
      const bookings = data.bookings || [];
      mineList.innerHTML = bookings.length
        ? bookings.map(bookingCard).join("")
        : emptyMineHtml();
    } catch (error) {
      mineList.innerHTML = `
        <div class="empty-state">
          <span class="empty-icon"><i data-lucide="triangle-alert" aria-hidden="true"></i></span>
          <h2>Không tải được đơn đã đặt</h2>
          <p>${escapeHtml(error.message)}</p>
        </div>`;
    }

    renderIcons();
  }

  form?.addEventListener("submit", async (event) => {
    event.preventDefault();
    hideError();

    const code = codeInput.value.trim().toUpperCase();
    const contact = contactInput.value.trim();

    if (code.length < 4) {
      showError("Vui lòng nhập đúng mã đặt chỗ (ví dụ: X7K9P2).");
      codeInput.focus();
      return;
    }

    if (!contact) {
      showError("Nhập email hoặc số điện thoại đã dùng khi đặt chỗ.");
      contactInput.focus();
      return;
    }

    if (submitButton) submitButton.disabled = true;

    try {
      const data = await request("", {
        method: "POST",
        body: JSON.stringify({ code, contact })
      });

      if (!data.ok) {
        results.hidden = true;
        results.innerHTML = "";
        showError(data.message || "Không tìm thấy đơn phù hợp.");
        return;
      }

      results.innerHTML = (data.bookings || []).map(bookingCard).join("");
      results.hidden = false;
      renderIcons();
      results.scrollIntoView({ behavior: "smooth", block: "start" });
      showToast("Đã tìm thấy đơn đặt chỗ.");
    } catch (error) {
      showError(error.message);
    } finally {
      if (submitButton) submitButton.disabled = false;
    }
  });

  refreshButton?.addEventListener("click", () => {
    if (mineList) mineList.innerHTML = '<p class="mock-note">Đang tải đơn đã đặt…</p>';
    loadMine();
  });

  function initialize() {
    renderIcons();
    loadMine();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initialize, { once: true });
  } else {
    initialize();
  }
})();
