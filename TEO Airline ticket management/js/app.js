(() => {
  const state = {
    screen: "home",
    tripType: "oneway",
    origin: "TP. Hồ Chí Minh (SGN)",
    destination: "Hà Nội (HAN)",
    departureDate: "2026-11-21",
    returnDate: "2026-11-28",
    cabin: "Phổ thông",
    passengers: { adult: 1, child: 0, infant: 0 },
    selectedFlightId: "teo218",
    selectedSeats: ["7A"],
    promoApplied: false,
    promoCode: "",
    redeemedPromoCodes: [],
    loyaltyPoints: 2450,
    paymentMethod: "momo",
    timerStarted: false,
    remainingSeconds: 600,
    timerId: null,
    travelerName: "Nguyễn Minh Anh",
    travelerNames: ["Nguyễn Minh Anh"],
    travelerFormsGenerated: false,
    contactEmail: "minhanh@example.com",
    contactPhone: "090 123 4567"
  };

  const flights = {
    teo218: {
      id: "teo218",
      code: "TEO 218",
      departure: "08:15",
      arrival: "10:20",
      duration: "2 giờ 05 phút",
      baseFare: 1650000,
      tax: 180000,
      fee: 60000,
      businessPremium: 1200000
    },
    teo426: {
      id: "teo426",
      code: "TEO 426",
      departure: "14:10",
      arrival: "16:20",
      duration: "2 giờ 10 phút",
      baseFare: 1900000,
      tax: 210000,
      fee: 80000,
      businessPremium: 1400000
    }
  };

  const promotions = {
    TEO10: { discountPercent: 10, minOrderValue: 2000000 },
    TEO100: { discountAmount: 100000, minOrderValue: 1500000 },
    TEO300: { discountAmount: 300000, minOrderValue: 3000000 }
  };

  const screenTitles = {
    home: "TEO Airlines — Đặt vé máy bay",
    results: "Chọn chuyến bay — TEO Airlines",
    passengers: "Thông tin hành khách — TEO Airlines",
    seats: "Chọn chỗ ngồi — TEO Airlines",
    payment: "Xác nhận & thanh toán — TEO Airlines",
    confirmation: "Đặt chỗ — TEO Airlines"
  };

  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  const money = (amount) => `${new Intl.NumberFormat("vi-VN").format(amount)} đ`;
  const formatPoints = (points) => new Intl.NumberFormat("vi-VN").format(points);
  const escapeHtml = (value) => String(value).replace(/[&<>"']/g, (character) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;"
  })[character]);
  const formatDate = (isoDate) => {
    if (!isoDate) return "Chưa chọn";
    const [year, month, day] = isoDate.split("-");
    return `${day}/${month}/${year}`;
  };

  function renderIcons() {
    if (window.lucide && typeof window.lucide.createIcons === "function") {
      window.lucide.createIcons();
    }
  }

  function showToast(message) {
    const toast = $("#toast");
    toast.textContent = message;
    toast.hidden = false;
    window.clearTimeout(showToast.timeoutId);
    showToast.timeoutId = window.setTimeout(() => {
      toast.hidden = true;
    }, 3200);
  }

  function updateRewards() {
    const balance = $("#loyalty-balance");
    if (!balance) return;
    balance.textContent = formatPoints(state.loyaltyPoints);

    $$("[data-redeem-voucher]").forEach((button) => {
      if (button.dataset.redeemed === "true") return;
      button.disabled = Number(button.dataset.pointsCost) > state.loyaltyPoints;
      button.textContent = button.disabled ? "Chưa đủ điểm" : "Đổi voucher";
    });
  }

  function setScreen(nextScreen, { scrollToTop = true } = {}) {
    if (!screenTitles[nextScreen]) return;
    state.screen = nextScreen;

    $$(".screen").forEach((screen) => {
      const isActive = screen.dataset.screen === nextScreen;
      screen.classList.toggle("is-active", isActive);
      screen.hidden = !isActive;
    });

    const isBooking = !["home"].includes(nextScreen);
    $("#booking-progress").hidden = !isBooking;
    document.title = screenTitles[nextScreen];
    updateProgress(nextScreen);
    updateNavigation(nextScreen);

    if (nextScreen === "passengers") {
      if (!state.travelerFormsGenerated) {
        renderTravelerForms();
        state.travelerFormsGenerated = true;
      }
      renderSummary($("#passenger-summary-card"));
    }
    if (nextScreen === "seats") {
      renderSummary($("#seat-summary-card"));
      renderSeats();
      const seatCount = getRequiredSeatCount();
      $("#seats-subheading").textContent = `Chọn ${seatCount} chỗ ngồi cho hành khách có ghế.`;
    }
    if (nextScreen === "payment") {
      renderSummary($("#payment-summary-card"));
      renderFare();
      startHoldTimer();
    }
    if (nextScreen === "confirmation") renderConfirmation();

    if (scrollToTop) window.scrollTo({ top: 0, behavior: "smooth" });
    renderIcons();
  }

  function updateProgress(nextScreen) {
    const order = ["results", "passengers", "seats", "payment", "confirmation"];
    const activeIndex = order.indexOf(nextScreen);

    $$("#booking-progress [data-progress]").forEach((item) => {
      const itemIndex = order.indexOf(item.dataset.progress);
      item.classList.toggle("is-complete", itemIndex < activeIndex);
      item.classList.toggle("is-current", itemIndex === activeIndex);
      if (itemIndex === activeIndex) {
        item.setAttribute("aria-current", "step");
      } else {
        item.removeAttribute("aria-current");
      }
    });
  }

  function updateNavigation(nextScreen) {
    $$(".main-nav [data-screen-link]").forEach((link) => {
      const isCurrent = link.dataset.screenLink === nextScreen || (nextScreen !== "home" && link.dataset.screenLink === "home");
      link.classList.toggle("is-active", isCurrent);
      if (isCurrent) {
        link.setAttribute("aria-current", "page");
      } else {
        link.removeAttribute("aria-current");
      }
    });
  }

  function dateLabel(isoDate) {
    if (!isoDate) return "Chọn ngày";
    const date = new Date(`${isoDate}T12:00:00`);
    return new Intl.DateTimeFormat("vi-VN", {
      weekday: "long",
      day: "numeric",
      month: "long",
      year: "numeric"
    }).format(date);
  }

  function renderCalendar(container, selectedDate, monthDate) {
    const year = monthDate.getFullYear();
    const month = monthDate.getMonth();
    const firstDay = new Date(year, month, 1);
    const offset = (firstDay.getDay() + 6) % 7;
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPreviousMonth = new Date(year, month, 0).getDate();
    const weekdayNames = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];
    const monthName = new Intl.DateTimeFormat("vi-VN", { month: "long", year: "numeric" }).format(firstDay);
    const selectedParts = selectedDate ? selectedDate.split("-").map(Number) : [];

    container.innerHTML = `
      <div class="calendar-header">
        <button type="button" data-calendar-shift="-1" aria-label="Tháng trước"><i data-lucide="chevron-left"></i></button>
        <strong>${monthName.charAt(0).toUpperCase()}${monthName.slice(1)}</strong>
        <button type="button" data-calendar-shift="1" aria-label="Tháng sau"><i data-lucide="chevron-right"></i></button>
      </div>
      <div class="calendar-grid">
        ${weekdayNames.map((day) => `<span class="weekday">${day}</span>`).join("")}
        ${Array.from({ length: 42 }, (_, index) => {
          const dayNumber = index - offset + 1;
          let date;
          let isMuted = false;
          if (dayNumber < 1) {
            date = new Date(year, month - 1, daysInPreviousMonth + dayNumber);
            isMuted = true;
          } else if (dayNumber > daysInMonth) {
            date = new Date(year, month + 1, dayNumber - daysInMonth);
            isMuted = true;
          } else {
            date = new Date(year, month, dayNumber);
          }

          const value = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
          const isSelected = selectedParts.length === 3 &&
            selectedParts[0] === date.getFullYear() &&
            selectedParts[1] === date.getMonth() + 1 &&
            selectedParts[2] === date.getDate();
          return `<button type="button" class="${isMuted ? "is-muted" : ""} ${isSelected ? "is-selected" : ""}" data-date="${value}" aria-pressed="${isSelected}" aria-label="${dateLabel(value)}">${date.getDate()}</button>`;
        }).join("")}
      </div>`;
  }

  function toggleCalendar(field, open) {
    const trigger = $(".date-trigger", field);
    const panel = $(".calendar-popover", field);
    const shouldOpen = typeof open === "boolean" ? open : panel.hidden;
    closeFloatingPanels(panel);
    panel.hidden = !shouldOpen;
    trigger.setAttribute("aria-expanded", String(shouldOpen));
    if (shouldOpen) renderIcons();
  }

  function closeFloatingPanels(except = null) {
    $$(".calendar-popover, .passenger-popover, .select-popover").forEach((panel) => {
      if (panel === except) return;
      panel.hidden = true;
      const trigger = document.querySelector(`[aria-controls="${panel.id}"]`);
      if (trigger) trigger.setAttribute("aria-expanded", "false");
    });
  }

  function updatePassengerSummary() {
    const total = Object.values(state.passengers).reduce((sum, count) => sum + count, 0);
    const parts = [];
    if (state.passengers.adult) parts.push(`${state.passengers.adult} người lớn`);
    if (state.passengers.child) parts.push(`${state.passengers.child} trẻ em`);
    if (state.passengers.infant) parts.push(`${state.passengers.infant} em bé`);
    $("#passenger-summary").textContent = parts.join(", ");
    $("#adult-count").textContent = state.passengers.adult;
    $("#child-count").textContent = state.passengers.child;
    $("#infant-count").textContent = state.passengers.infant;
    $$("[data-count]").forEach((button) => {
      const kind = button.dataset.count;
      const atMinimum = kind === "adult" ? state.passengers[kind] <= 1 : state.passengers[kind] <= 0;
      button.disabled = button.dataset.delta === "1" ? total >= 9 : atMinimum;
    });
  }

  function getSelectedFlight() {
    return flights[state.selectedFlightId];
  }

  function renderTravelerForms() {
    const passengers = [
      { kind: "adult", count: state.passengers.adult, label: "Người lớn" },
      { kind: "child", count: state.passengers.child, label: "Trẻ em" },
      { kind: "infant", count: state.passengers.infant, label: "Em bé" }
    ].flatMap(({ kind, count, label }) => Array.from({ length: count }, () => ({ kind, label })));

    $("#traveler-list").innerHTML = passengers.map(({ kind, label }, index) => {
      const name = state.travelerNames[index] || (index === 0 ? state.travelerName : "");
      const birthDate = index === 0 ? "18/04/1996" : "";
      const documentNumber = index === 0 ? "079096012345" : "";
      const documentLabel = kind === "adult" ? "Số giấy tờ tùy thân" : "Số giấy tờ tùy thân/giấy khai sinh";
      return `
        <section class="form-section traveler-section">
          <div class="section-heading">
            <span class="section-number">${index + 1}</span>
            <div><h2>Hành khách ${index + 1} · ${label}</h2><p>Nhập thông tin trùng khớp với giấy tờ sử dụng khi làm thủ tục.</p></div>
          </div>
          <div class="form-grid">
            <div class="field"><label for="traveler-name-${index}">Họ và tên</label><input id="traveler-name-${index}" data-traveler-name type="text" value="${escapeHtml(name)}" autocomplete="${index === 0 ? "name" : "off"}" required></div>
            <div class="field"><label for="traveler-birth-${index}">Ngày sinh</label><input id="traveler-birth-${index}" type="text" value="${escapeHtml(birthDate)}" placeholder="dd/mm/yyyy" inputmode="numeric" required></div>
            <div class="field"><label for="traveler-id-${index}">${documentLabel}</label><input id="traveler-id-${index}" type="text" value="${escapeHtml(documentNumber)}" inputmode="numeric" required></div>
            <fieldset class="field gender-field"><legend>Giới tính</legend><div class="inline-options"><label><input type="radio" name="traveler-gender-${index}" value="Nam" checked> Nam</label><label><input type="radio" name="traveler-gender-${index}" value="Nữ"> Nữ</label></div></fieldset>
          </div>
        </section>`;
    }).join("");
  }

  function getRequiredSeatCount() {
    return state.passengers.adult + state.passengers.child;
  }

  function getSeatFee(seat) {
    return ["1B", "1C", "1D", "1E", "2A", "2F", "7A", "7F"].includes(seat) ? 250000 : 0;
  }

  function getAirportCode(location) {
    return location.match(/\(([A-Za-z]{3})\)/)?.[1]?.toUpperCase() || location.trim().slice(0, 3).toUpperCase();
  }

  function getFlightDisplayedTotal(flight) {
    const premium = state.cabin === "Thương gia" ? flight.businessPremium : 0;
    return flight.baseFare + flight.tax + flight.fee + premium;
  }

  function getPassengerSummary() {
    const passengerCount = Object.values(state.passengers).reduce((total, count) => total + count, 0);
    const firstName = escapeHtml(state.travelerNames[0] || state.travelerName);
    return passengerCount <= 1 ? firstName : `${firstName} + ${passengerCount - 1} người`;
  }

  function getPriceBreakdown() {
    const flight = getSelectedFlight();
    const quantity = getRequiredSeatCount();
    const base = flight.baseFare * quantity;
    const premium = (state.cabin === "Thương gia" ? flight.businessPremium : 0) * quantity;
    const tax = flight.tax * quantity;
    const fee = flight.fee * quantity;
    const seat = state.selectedSeats.reduce((total, selectedSeat) => total + getSeatFee(selectedSeat), 0);
    const promotion = promotions[state.promoCode];
    const discount = state.promoApplied && promotion
      ? promotion.discountPercent
        ? Math.round(base * promotion.discountPercent / 100)
        : Math.min(base, promotion.discountAmount)
      : 0;
    return {
      base,
      premium,
      tax,
      fee,
      seat,
      discount,
      total: base + premium + tax + fee + seat - discount
    };
  }

  function renderSummary(container) {
    if (!container) return;
    const flight = getSelectedFlight();
    const prices = getPriceBreakdown();
    const route = `${escapeHtml(state.origin)} → ${escapeHtml(state.destination)}`;
    const date = dateLabel(state.departureDate);

    container.innerHTML = `
      <section class="summary-card">
        <div class="summary-heading"><h2>Tóm tắt hành trình</h2></div>
        <div class="summary-body">
          <div class="summary-route">
            <div class="summary-airport"><strong>${escapeHtml(getAirportCode(state.origin))}</strong><span>${flight.departure} · ${date}</span></div>
            <i data-lucide="plane" aria-hidden="true"></i>
            <div class="summary-airport"><strong>${escapeHtml(getAirportCode(state.destination))}</strong><span>${flight.arrival} · ${flight.duration}</span></div>
          </div>
          <div class="summary-details">
            <p><span>Hành khách</span><strong>${getPassengerSummary()}</strong></p>
            <p><span>Chuyến bay</span><strong>${flight.code}</strong></p>
            <p><span>Hạng ghế</span><strong>${state.cabin}</strong></p>
            ${state.selectedSeats.length ? `<p><span>Ghế</span><strong>${state.selectedSeats.join(", ")}</strong></p>` : ""}
            <p><span>Hành trình</span><strong>${route}</strong></p>
          </div>
        </div>
        <div class="summary-cost"><span>Tạm tính</span><strong>${money(prices.total)}</strong></div>
      </section>`;
    renderIcons();
  }

  function renderFare() {
    const breakdown = getPriceBreakdown();
    const quantity = getRequiredSeatCount();
    const travelerSuffix = quantity > 1 ? ` × ${quantity} hành khách` : "";
    const rows = [
      ["Giá vé" + travelerSuffix, breakdown.base],
      ...(breakdown.premium ? [["Phụ thu hạng thương gia", breakdown.premium]] : []),
      ["Thuế" + travelerSuffix, breakdown.tax],
      ["Phí dịch vụ" + travelerSuffix, breakdown.fee],
      ...state.selectedSeats.map((seat) => ["Ghế " + seat, getSeatFee(seat)]).filter(([, amount]) => amount > 0),
      ...(breakdown.discount ? [[`Voucher ${state.promoCode}`, -breakdown.discount, true]] : [])
    ];

    $("#fare-breakdown").innerHTML = rows.map(([label, amount, isDiscount]) => `
      <div class="fare-row ${isDiscount ? "discount" : ""}">
        <dt>${label}</dt><dd>${amount < 0 ? "−" : ""}${money(Math.abs(amount))}</dd>
      </div>`).join("");
    $("#total-price").textContent = money(breakdown.total);
    $("#promo-feedback").textContent = state.promoApplied ? `Đã áp dụng mã ${state.promoCode} cho giá vé.` : "";
    $("#promo-feedback").classList.toggle("is-error", false);
  }

  function renderSeats() {
    const occupied = new Set(["1A", "1F", "2B", "2E", "3C", "4D", "5A", "6F", "8B"]);
    const extra = new Set(["1B", "1C", "1D", "1E", "2A", "2F", "7A", "7F"]);
    const rows = Array.from({ length: 8 }, (_, index) => index + 1);
    const columns = ["A", "B", "C", "", "D", "E", "F"];

    $("#seat-grid").innerHTML = rows.map((row) => `
      <div class="seat-row">
        <span class="seat-row-label">${row}</span>
        ${columns.map((column) => {
          if (!column) return '<span class="aisle-space" aria-hidden="true"></span>';
          const seatId = `${row}${column}`;
          const isOccupied = occupied.has(seatId);
          const isSelected = state.selectedSeats.includes(seatId);
          const hasExtra = extra.has(seatId);
          const classes = ["seat", isOccupied ? "is-occupied" : "", isSelected ? "is-selected" : "", hasExtra ? "is-extra" : ""].filter(Boolean).join(" ");
          const accessibleState = isOccupied ? "đã được đặt" : hasExtra ? "có phụ phí" : "còn trống";
          return `<button type="button" class="${classes}" data-seat="${seatId}" ${isOccupied ? "disabled" : ""} aria-pressed="${isSelected}" aria-label="Ghế ${seatId}, ${accessibleState}${isSelected ? ", đang chọn" : ""}">${column}</button>`;
        }).join("")}
      </div>`).join("");

    const selectedSeatNames = state.selectedSeats.join(", ");
    const seatTotal = state.selectedSeats.reduce((total, seat) => total + getSeatFee(seat), 0);
    $("#seat-selection-note").textContent = state.selectedSeats.length
      ? `Đã chọn ${state.selectedSeats.length}/${getRequiredSeatCount()} ghế: ${selectedSeatNames} · Phụ phí ${money(seatTotal)}`
      : `Chưa chọn ghế. Hãy chọn ${getRequiredSeatCount()} ghế còn trống.`;
    renderIcons();
  }

  function renderConfirmation() {
    const flight = getSelectedFlight();
    const prices = getPriceBreakdown();
    $("#confirmation-summary").innerHTML = `
      <div class="summary-line"><span>Hành trình</span><strong>${escapeHtml(state.origin)} → ${escapeHtml(state.destination)}</strong></div>
      <div class="summary-line"><span>Ngày khởi hành</span><strong>${dateLabel(state.departureDate)}</strong></div>
      <div class="summary-line"><span>Chuyến bay</span><strong>${flight.code} · ${flight.departure}–${flight.arrival}</strong></div>
      <div class="summary-line"><span>Hành khách</span><strong>${getPassengerSummary()}</strong></div>
      <div class="summary-line"><span>Trạng thái thanh toán</span><strong>Chờ xác nhận</strong></div>
      <div class="summary-line"><span>Tổng tiền minh họa</span><strong>${money(prices.total)}</strong></div>`;
    renderIcons();
  }

  function updateFlightSearchSummary() {
    const departure = dateLabel(state.departureDate);
    $("#results-title").textContent = `Chuyến bay đến ${state.destination}`;
    $("#results-summary").textContent = `${state.origin} → ${state.destination} · ${departure}${state.tripType === "roundtrip" ? ` · Về ${dateLabel(state.returnDate)}` : ""} · ${state.cabin}`;
    $$("[data-flight-card]").forEach((card) => {
      const flight = flights[card.dataset.flightId];
      const total = getFlightDisplayedTotal(flight);
      card.dataset.price = String(total);
      $(".flight-choice strong", card).textContent = money(total);
      $(".flight-choice span", card).textContent = "đã gồm thuế & phí/người";
      $(".flight-meta span", card).textContent = state.cabin;
      const airportLabels = $$(".flight-time span", card);
      airportLabels[0].textContent = getAirportCode(state.origin);
      airportLabels[1].textContent = getAirportCode(state.destination);
    });
  }

  function applyFilters() {
    const filters = $$("[data-filter]:checked").map((input) => input.dataset.filter);
    const cards = $$("[data-flight-card]");
    let visibleCount = 0;

    cards.forEach((card) => {
      const price = Number(card.dataset.price);
      const show = filters.every((filter) => {
        if (filter === "direct") return card.dataset.direct === "true";
        if (filter === "stop") return card.dataset.direct === "false";
        if (filter === "morning" || filter === "afternoon") return card.dataset.time === filter;
        if (filter === "under-two") return price < 2000000;
        return true;
      });
      card.hidden = !show;
      if (show) visibleCount += 1;
    });

    $("#result-count").textContent = `${visibleCount} chuyến bay`;
    $("#empty-results").hidden = visibleCount !== 0;
  }

  function sortFlights() {
    const list = $("#flight-list");
    const cards = $$("[data-flight-card]", list);
    const sortBy = $("#flight-sort").value;
    cards.sort((first, second) => {
      if (sortBy === "price") return Number(first.dataset.price) - Number(second.dataset.price);
      if (sortBy === "departure") return first.dataset.flightId.localeCompare(second.dataset.flightId);
      return first.dataset.flightId === "teo218" ? -1 : 1;
    });
    cards.forEach((card) => list.insertBefore(card, $("#empty-results")));
  }

  function startHoldTimer() {
    if (state.timerStarted) return;
    state.timerStarted = true;
    state.remainingSeconds = state.paymentMethod === "momo" ? 600 : 3600;
    refreshHoldNotice();
    state.timerId = window.setInterval(() => {
      if (state.remainingSeconds > 0) state.remainingSeconds -= 1;
      updateTimerDisplay();
      if (state.remainingSeconds === 0) {
        window.clearInterval(state.timerId);
        $("#hold-notice").classList.add("is-expired");
        $("#hold-notice span").innerHTML = "Thời gian giữ chỗ đã hết. Đơn chưa được xác nhận.";
        $("#confirm-booking").disabled = true;
      }
    }, 1000);
    updateTimerDisplay();
  }

  function refreshHoldNotice() {
    const minutes = Math.floor(state.remainingSeconds / 60);
    const seconds = state.remainingSeconds % 60;
    const duration = state.paymentMethod === "momo" ? "10 phút" : "1 giờ";
    $("#hold-notice span").innerHTML = `Thời gian giữ chỗ còn <strong id="hold-timer">${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}</strong> (${duration}). Đơn chưa được xác nhận thanh toán.`;
  }

  function updateTimerDisplay() {
    const minutes = Math.floor(state.remainingSeconds / 60);
    const seconds = state.remainingSeconds % 60;
    $("#hold-timer").textContent = `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
  }

  function updatePaymentMethod() {
    const selected = $('input[name="payment-method"]:checked');
    if (!selected) return;
    state.paymentMethod = selected.value;
    $$(".payment-option").forEach((option) => {
      option.classList.toggle("is-selected", $("input", option).checked);
    });
    if (state.timerId) window.clearInterval(state.timerId);
    state.timerId = null;
    state.timerStarted = false;
    state.remainingSeconds = state.paymentMethod === "momo" ? 600 : 3600;
    $("#confirm-booking").disabled = false;
    $("#hold-notice").classList.remove("is-expired");
    refreshHoldNotice();
    if (state.screen === "payment") startHoldTimer();
  }

  function validateSearch() {
    const origin = $("#origin");
    const destination = $("#destination");
    const error = $("#search-error");
    origin.removeAttribute("aria-invalid");
    destination.removeAttribute("aria-invalid");

    if (!origin.value.trim() || !destination.value.trim()) {
      error.textContent = "Vui lòng nhập cả điểm đi và điểm đến.";
      error.hidden = false;
      if (!origin.value.trim()) origin.setAttribute("aria-invalid", "true");
      if (!destination.value.trim()) destination.setAttribute("aria-invalid", "true");
      (!origin.value.trim() ? origin : destination).focus();
      return false;
    }
    if (origin.value.trim().toLocaleLowerCase("vi") === destination.value.trim().toLocaleLowerCase("vi")) {
      error.textContent = "Điểm đi và điểm đến cần khác nhau.";
      error.hidden = false;
      destination.setAttribute("aria-invalid", "true");
      destination.focus();
      return false;
    }
    error.hidden = true;
    return true;
  }

  function bindDateControl(fieldId, inputId, labelId) {
    const field = $(`#${fieldId}`);
    const trigger = $(".date-trigger", field);
    const panel = $(".calendar-popover", field);
    let visibleMonth = new Date(2026, 10, 1);

    function refresh() {
      renderCalendar(panel, $(`#${inputId}`).value, visibleMonth);
      panel.querySelectorAll("[data-date]").forEach((button) => {
        button.addEventListener("click", () => {
          const value = button.dataset.date;
          if (inputId === "return-date" && value < state.departureDate) {
            showToast("Ngày về không thể trước ngày đi.");
            return;
          }
          if (inputId === "departure-date" && state.tripType === "roundtrip" && value > state.returnDate) {
            state.returnDate = value;
            $("#return-date").value = value;
            $("#return-date-label").textContent = formatDate(value);
          }
          if (inputId === "departure-date") state.departureDate = value;
          if (inputId === "return-date") state.returnDate = value;
          $(`#${inputId}`).value = value;
          $(`#${labelId}`).textContent = formatDate(value);
          toggleCalendar(field, false);
        });
      });
      panel.querySelectorAll("[data-calendar-shift]").forEach((button) => {
        button.addEventListener("click", () => {
          visibleMonth = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth() + Number(button.dataset.calendarShift), 1);
          refresh();
        });
      });
      renderIcons();
    }

    trigger.addEventListener("click", () => {
      if (trigger.disabled) return;
      refresh();
      toggleCalendar(field);
    });
  }

  function initializeSeatMap() {
    renderSeats();
  }

  document.addEventListener("click", (event) => {
    if (!event.target.closest(".date-field, .passenger-field, .field")) closeFloatingPanels();

    const screenLink = event.target.closest("[data-screen-link]");
    if (screenLink) {
      event.preventDefault();
      setScreen(screenLink.dataset.screenLink);
      $("#mobile-menu").hidden = true;
      $(".menu-toggle").setAttribute("aria-expanded", "false");
      return;
    }

    const anchor = event.target.closest("[data-home-anchor]");
    if (anchor) {
      event.preventDefault();
      setScreen("home", { scrollToTop: false });
      $("#mobile-menu").hidden = true;
      $(".menu-toggle").setAttribute("aria-expanded", "false");
      $(".menu-toggle").setAttribute("aria-label", "Mở menu");
      window.setTimeout(() => {
        const target = document.getElementById(anchor.dataset.homeAnchor);
        if (target) target.scrollIntoView({ behavior: "smooth", block: "start" });
      }, 0);
      return;
    }

    const socialChannel = event.target.closest("[data-social-channel]");
    if (socialChannel) {
      event.preventDefault();
      showToast(`${socialChannel.dataset.socialChannel} đang là liên kết mẫu, chưa kết nối tài khoản chính thức.`);
      return;
    }

    const redeemButton = event.target.closest("[data-redeem-voucher]");
    if (redeemButton) {
      const pointsCost = Number(redeemButton.dataset.pointsCost);
      if (!Number.isInteger(pointsCost) || pointsCost <= 0) {
        showToast("Voucher này chưa có thông tin điểm đổi hợp lệ.");
        return;
      }
      if (redeemButton.disabled) return;

      state.loyaltyPoints -= pointsCost;
      redeemButton.dataset.redeemed = "true";
      redeemButton.disabled = true;
      state.redeemedPromoCodes.push(redeemButton.dataset.promoCode);
      updateRewards();
      redeemButton.textContent = `Đã đổi · ${redeemButton.dataset.promoCode}`;
      showToast(`Đổi voucher thành công. Mã ${redeemButton.dataset.promoCode} đã được thêm vào tài khoản mẫu.`);
      return;
    }

    if (event.target.closest("[data-coming-soon]")) {
      event.preventDefault();
      showToast("Màn hình này nằm ngoài phạm vi bản xem trước hiện tại.");
      return;
    }

    const cabinOption = event.target.closest("[data-cabin]");
    if (cabinOption) {
      state.cabin = cabinOption.dataset.cabin;
      $("#cabin-value").textContent = state.cabin;
      $("#cabin-options").hidden = true;
      $("#cabin-trigger").setAttribute("aria-expanded", "false");
      $$("#cabin-options [role=option]").forEach((option) => option.setAttribute("aria-selected", String(option === cabinOption)));
      return;
    }

    const countButton = event.target.closest("[data-count]");
    if (countButton) {
      const kind = countButton.dataset.count;
      const next = state.passengers[kind] + Number(countButton.dataset.delta);
      const total = Object.values(state.passengers).reduce((sum, count) => sum + count, 0);
      if (kind === "adult" && (next < 1 || next < state.passengers.infant)) return;
      if (kind === "infant" && next > state.passengers.adult) {
        showToast("Mỗi em bé cần đi cùng một người lớn.");
        return;
      }
      if (next < 0 || (countButton.dataset.delta === "1" && total >= 9)) return;
      state.passengers[kind] = next;
      updatePassengerSummary();
      return;
    }

    const calendarShift = event.target.closest("[data-calendar-shift]");
    if (calendarShift) return;

    const dateOption = event.target.closest(".calendar-grid [data-date]");
    if (dateOption) return;

    const seatButton = event.target.closest("[data-seat]");
    if (seatButton && !seatButton.disabled) {
      const seat = seatButton.dataset.seat;
      if (state.selectedSeats.includes(seat)) {
        state.selectedSeats = state.selectedSeats.filter((selectedSeat) => selectedSeat !== seat);
      } else if (state.selectedSeats.length < getRequiredSeatCount()) {
        state.selectedSeats = [...state.selectedSeats, seat];
      } else {
        showToast(`Đã chọn đủ ${getRequiredSeatCount()} ghế.`);
        return;
      }
      renderSeats();
      renderSummary($("#seat-summary-card"));
      return;
    }

    const selectFlight = event.target.closest("[data-select-flight]");
    if (selectFlight) {
      state.selectedFlightId = selectFlight.dataset.selectFlight;
      setScreen("passengers");
      return;
    }

    if (event.target.closest("#empty-clear-filters")) {
      clearFilters();
      return;
    }
  });

  function clearFilters() {
    $$("[data-filter]").forEach((filter) => { filter.checked = false; });
    applyFilters();
  }

  $(".menu-toggle").addEventListener("click", () => {
    const menu = $("#mobile-menu");
    const isOpen = menu.hidden;
    menu.hidden = !isOpen;
    $(".menu-toggle").setAttribute("aria-expanded", String(isOpen));
    $(".menu-toggle").setAttribute("aria-label", isOpen ? "Đóng menu" : "Mở menu");
    $(".menu-toggle").innerHTML = `<i data-lucide="${isOpen ? "x" : "menu"}" aria-hidden="true"></i>`;
    renderIcons();
  });

  $$('input[name="trip-type"]').forEach((radio) => {
    radio.addEventListener("change", () => {
      state.tripType = radio.value;
      const isRoundTrip = state.tripType === "roundtrip";
      $("#return-field").classList.toggle("is-disabled", !isRoundTrip);
      $("#return-field").hidden = !isRoundTrip;
      $("#return-date").disabled = !isRoundTrip;
      $(".date-trigger", $("#return-field")).disabled = !isRoundTrip;
      if (!isRoundTrip) toggleCalendar($("#return-field"), false);
    });
  });

  $("#swap-route").addEventListener("click", () => {
    const origin = $("#origin");
    const destination = $("#destination");
    [origin.value, destination.value] = [destination.value, origin.value];
    state.origin = origin.value;
    state.destination = destination.value;
  });

  $("#cabin-trigger").addEventListener("click", () => {
    closeFloatingPanels($("#cabin-options"));
    const options = $("#cabin-options");
    options.hidden = !options.hidden;
    $("#cabin-trigger").setAttribute("aria-expanded", String(!options.hidden));
  });

  $("#passenger-trigger").addEventListener("click", () => {
    closeFloatingPanels($("#passenger-options"));
    const options = $("#passenger-options");
    options.hidden = !options.hidden;
    $("#passenger-trigger").setAttribute("aria-expanded", String(!options.hidden));
  });

  $("#passenger-done").addEventListener("click", () => {
    $("#passenger-options").hidden = true;
    $("#passenger-trigger").setAttribute("aria-expanded", "false");
  });

  $("#flight-search-form").addEventListener("submit", (event) => {
    event.preventDefault();
    if (!validateSearch()) return;
    state.origin = $("#origin").value.trim();
    state.destination = $("#destination").value.trim();
    state.departureDate = $("#departure-date").value;
    state.returnDate = $("#return-date").value;
    if (state.timerId) window.clearInterval(state.timerId);
    state.timerId = null;
    state.timerStarted = false;
    state.remainingSeconds = state.paymentMethod === "momo" ? 600 : 3600;
    state.selectedFlightId = "teo218";
    state.selectedSeats = ["7A"];
    state.promoApplied = false;
    state.promoCode = "";
    state.travelerNames = ["Nguyễn Minh Anh"];
    state.travelerName = "Nguyễn Minh Anh";
    state.travelerFormsGenerated = false;
    $("#promo-code").value = "";
    $("#confirm-booking").disabled = false;
    $("#hold-notice").classList.remove("is-expired");
    const submit = $("#search-submit");
    submit.setAttribute("aria-busy", "true");
    submit.disabled = true;
    submit.querySelector("span").textContent = "Đang tìm chuyến…";
    window.setTimeout(() => {
      submit.removeAttribute("aria-busy");
      submit.disabled = false;
      submit.querySelector("span").textContent = "Tìm chuyến bay";
      updateFlightSearchSummary();
      clearFilters();
      setScreen("results");
    }, 550);
  });

  $("#passenger-form").addEventListener("submit", (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    if (!form.reportValidity()) return;
    state.travelerNames = $$("[data-traveler-name]", form).map((input) => input.value.trim());
    state.travelerName = state.travelerNames[0];
    state.contactEmail = $("#contact-email").value.trim();
    state.contactPhone = $("#contact-phone").value.trim();
    setScreen("seats");
  });

  $("#clear-filters").addEventListener("click", clearFilters);
  $$("[data-filter]").forEach((filter) => filter.addEventListener("change", applyFilters));
  $("#flight-sort").addEventListener("change", sortFlights);

  $("#continue-to-payment").addEventListener("click", () => {
    if (state.selectedSeats.length !== getRequiredSeatCount()) {
      showToast(`Hãy chọn đủ ${getRequiredSeatCount()} ghế trước khi tiếp tục.`);
      return;
    }
    setScreen("payment");
  });

  $$('input[name="payment-method"]').forEach((radio) => {
    radio.addEventListener("change", updatePaymentMethod);
  });

  $("#promo-form").addEventListener("submit", (event) => {
    event.preventDefault();
    const code = $("#promo-code").value.trim().toUpperCase();
    const feedback = $("#promo-feedback");
    const promotion = promotions[code];
    const breakdown = getPriceBreakdown();
    const orderValue = breakdown.base + breakdown.premium + breakdown.tax + breakdown.fee + breakdown.seat;

    if (promotion && state.redeemedPromoCodes.includes(code) && orderValue >= promotion.minOrderValue) {
      state.promoApplied = true;
      state.promoCode = code;
      feedback.classList.remove("is-error");
      feedback.textContent = `Đã áp dụng mã ${code} cho giá vé minh họa.`;
      renderFare();
      renderSummary($("#payment-summary-card"));
    } else {
      state.promoApplied = false;
      state.promoCode = "";
      const feedbackMessage = !code
        ? "Nhập mã khuyến mãi để áp dụng."
        : !promotion || !state.redeemedPromoCodes.includes(code)
          ? "Bạn cần đổi voucher này bằng điểm trước khi áp dụng."
          : `Đơn hàng cần đạt tối thiểu ${money(promotion.minOrderValue)} để dùng mã này.`;
      renderFare();
      feedback.classList.add("is-error");
      feedback.textContent = feedbackMessage;
      renderSummary($("#payment-summary-card"));
    }
  });

  $("#confirm-booking").addEventListener("click", () => {
    if (state.remainingSeconds <= 0) {
      showToast("Thời gian giữ chỗ đã hết.");
      return;
    }
    if (state.timerId) window.clearInterval(state.timerId);
    state.timerId = null;
    setScreen("confirmation");
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      if ($("#logout-dialog")?.open) return;
      closeFloatingPanels();
      const menu = $("#mobile-menu");
      const menuToggle = $(".menu-toggle");
      const wasOpen = !menu.hidden;
      menu.hidden = true;
      menuToggle.setAttribute("aria-expanded", "false");
      menuToggle.setAttribute("aria-label", "Mở menu");
      if (wasOpen) {
        menuToggle.innerHTML = '<i data-lucide="menu" aria-hidden="true"></i>';
        renderIcons();
      }
    }
  });

  function initializeDateControls() {
    bindDateControl("departure-field", "departure-date", "departure-date-label");
    bindDateControl("return-field", "return-date", "return-date-label");
  }

  function initialize() {
    initializeDateControls();
    updatePassengerSummary();
    initializeSeatMap();
    renderFare();
    applyFilters();
    updateRewards();
    renderIcons();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initialize, { once: true });
  } else {
    initialize();
  }
})();
