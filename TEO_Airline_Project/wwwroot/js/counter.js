(() => {
  const pageTitles = {
    booking: "Đặt vé",
    exchange: "Đổi / hủy vé",
    customers: "Tài khoản khách hàng",
    profile: "Hồ sơ nhân viên"
  };
  const params = new URLSearchParams(window.location.search);
  const requestedScreen = params.get("screen");
  const activeScreen = Object.hasOwn(pageTitles, requestedScreen) ? requestedScreen : "booking";
  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  let toastTimer;
  const bookingState = {
    flights: [],
    selectedFlight: null,
    seatMap: null,
    selectedSeat: null,
    quoteFingerprint: null,
    quoteRequestVersion: 0,
    completed: false
  };
  const bookingApi = "/api/booking";
  let bookingQuoteTimer;
  const money = (value) => `${new Intl.NumberFormat("vi-VN").format(Math.round(Number(value) || 0))} ₫`;
  const escapeHtml = (value) => String(value ?? "").replace(/[&<>"']/g, (character) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;"
  })[character]);

  async function bookingFetch(path, { method = "GET", body, antiforgery = false } = {}) {
    const headers = {};
    if (body !== undefined) headers["Content-Type"] = "application/json";
    if (antiforgery) {
      const token = $("input[name='__RequestVerificationToken']", $("#counter-antiforgery"))?.value;
      if (!token) throw new Error("Không lấy được mã bảo vệ phiên. Vui lòng tải lại trang.");
      headers.RequestVerificationToken = token;
    }

    const response = await fetch(`${bookingApi}/${path}`, {
      method,
      credentials: "same-origin",
      headers,
      body: body === undefined ? undefined : JSON.stringify(body)
    });

    let data = null;
    try {
      data = await response.json();
    } catch {
      data = null;
    }

    if (!response.ok) {
      throw new Error(data?.message || "Không thể xử lý yêu cầu. Vui lòng thử lại.");
    }

    return data;
  }

  function setBookingFeedback(id, message = "", kind = "") {
    const element = $(`#${id}`);
    element.textContent = message;
    element.hidden = !message;
    element.classList.toggle("is-error", kind === "error");
    element.classList.toggle("is-success", kind === "success");
  }

  function localIsoDate(offset = 0) {
    const date = new Date();
    date.setDate(date.getDate() + offset);
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
  }

  function displayDate(isoDate) {
    if (!isoDate) return "—";
    const [year, month, day] = isoDate.split("-");
    return `${day}/${month}/${year}`;
  }

  function resetSummary() {
    $("#summary-origin").textContent = "—";
    $("#summary-destination").textContent = "—";
    $("#summary-departure").textContent = "Chưa chọn";
    $("#summary-arrival").textContent = "Chưa chọn";
    $("#summary-lines").innerHTML = '<div class="counter-summary-line"><span>Chọn chuyến bay và ghế</span><span>—</span></div>';
    $("#summary-total").textContent = "—";
    $("#booking-status").textContent = "Nháp";
    $("#confirm-counter-booking").disabled = true;
  }

  function clearBookingSelection() {
    bookingState.selectedFlight = null;
    bookingState.seatMap = null;
    bookingState.selectedSeat = null;
    bookingState.quoteFingerprint = null;
    bookingState.quoteRequestVersion++;
    bookingState.completed = false;
    $("#selected-flight-label").textContent = "Chọn chuyến bay trước";
    $("#seat-map").replaceChildren();
    $("#seat-map-message").textContent = "Chọn chuyến bay để xem ghế còn trống.";
    resetSummary();
    setBookingFeedback("booking-feedback");
  }

  function renderFlights(flights) {
    const rows = flights.map((flight) => `
      <tr class="${bookingState.selectedFlight?.flightId === flight.flightId ? "is-selected" : ""}">
        <td><strong>${escapeHtml(flight.flightNumber)}</strong><span class="counter-record-meta">${Math.floor(flight.durationMinutes / 60)}h ${String(flight.durationMinutes % 60).padStart(2, "0")}m</span></td>
        <td>${escapeHtml(flight.departureTime)}<span class="counter-record-meta">${escapeHtml(flight.departureCode)} · ${displayDate(flight.date)}</span></td>
        <td>${escapeHtml(flight.arrivalTime)}<span class="counter-record-meta">${escapeHtml(flight.arrivalCode)}</span></td>
        <td><span class="counter-badge ${flight.seatsLeft < 4 ? "counter-badge-warning" : "counter-badge-success"}">${flight.seatsLeft} ghế</span></td>
        <td class="counter-price">${money(flight.pricePerAdult)}</td>
        <td><button class="counter-button ${bookingState.selectedFlight?.flightId === flight.flightId ? "counter-button-primary" : ""} counter-button-sm" type="button" data-select-flight="${flight.flightId}">Chọn</button></td>
      </tr>`).join("");
    $("#flight-results").innerHTML = rows || '<tr><td colspan="6" class="counter-empty-cell">Không có chuyến phù hợp với tiêu chí đã chọn.</td></tr>';

    const cards = flights.map((flight) => `
      <article class="counter-flight-option ${bookingState.selectedFlight?.flightId === flight.flightId ? "is-selected" : ""}">
        <div class="counter-flight-title"><strong>${escapeHtml(flight.flightNumber)}</strong><span class="counter-price">${money(flight.pricePerAdult)}</span></div>
        <p class="counter-flight-route">${escapeHtml(flight.departureTime)} ${escapeHtml(flight.departureCode)} → ${escapeHtml(flight.arrivalTime)} ${escapeHtml(flight.arrivalCode)} · ${displayDate(flight.date)}</p>
        <div class="counter-flight-bottom"><span><span class="counter-badge ${flight.seatsLeft < 4 ? "counter-badge-warning" : "counter-badge-success"}">${flight.seatsLeft} ghế</span> · ${Math.floor(flight.durationMinutes / 60)}h ${String(flight.durationMinutes % 60).padStart(2, "0")}m</span><button class="counter-button counter-button-sm" type="button" data-select-flight="${flight.flightId}">Chọn</button></div>
      </article>`).join("") || '<p class="counter-empty-mobile">Không có chuyến phù hợp với tiêu chí đã chọn.</p>';
    $("#flight-cards").innerHTML = cards;
  }

  function updateUnquotedSummary() {
    const flight = bookingState.selectedFlight;
    if (!flight) {
      resetSummary();
      return;
    }

    const seat = bookingState.selectedSeat;
    const seatClass = $("#cabin").value === "Business" ? "Thương gia" : "Phổ thông";
    $("#summary-origin").textContent = flight.departureCode;
    $("#summary-destination").textContent = flight.arrivalCode;
    $("#summary-departure").textContent = `${flight.departureTime} · ${displayDate(flight.date)}`;
    $("#summary-arrival").textContent = flight.arrivalTime;
    $("#summary-lines").innerHTML = `
      <div class="counter-summary-line"><span>${escapeHtml(flight.flightNumber)} · ${seatClass}</span><strong>${seat ? escapeHtml(seat.number) : "Chưa chọn ghế"}</strong></div>
      <div class="counter-summary-line"><span>Người lớn × 1 · Giá dự kiến</span><span>${money(flight.pricePerAdult)}</span></div>`;
    $("#summary-total").textContent = money(flight.pricePerAdult);
    $("#booking-status").textContent = "Nháp";
    $("#confirm-counter-booking").disabled = !seat || bookingState.completed;
  }

  function renderQuote(quote) {
    $("#summary-lines").innerHTML = quote.lines.map((line) => `
      <div class="counter-summary-line"><span>${escapeHtml(line.label)}</span><span>${money(line.amount)}</span></div>`).join("");
    $("#summary-total").textContent = money(quote.total);
    $("#booking-status").textContent = "Báo giá";
    $("#confirm-counter-booking").disabled = bookingState.completed;
  }

  async function loadAirports() {
    try {
      const airports = await bookingFetch("airports");
      for (const id of ["origin", "destination"]) {
        const select = $(`#${id}`);
        select.innerHTML = '<option value="">Chọn sân bay</option>' + airports.map((airport) =>
          `<option value="${escapeHtml(airport.code)}">${escapeHtml(airport.label)}</option>`).join("");
      }
      if (airports.some((airport) => airport.code === "SGN")) $("#origin").value = "SGN";
      if (airports.some((airport) => airport.code === "HAN")) $("#destination").value = "HAN";
    } catch (error) {
      $("#origin").innerHTML = '<option value="">Không tải được sân bay</option>';
      $("#destination").innerHTML = '<option value="">Không tải được sân bay</option>';
      setBookingFeedback("search-feedback", error.message, "error");
    }
  }

  async function searchFlights() {
    const origin = $("#origin");
    const destination = $("#destination");
    const departure = $("#departure");
    if (!origin.value || !destination.value || !departure.value) {
      for (const field of [origin, destination, departure]) {
        if (!field.checkValidity()) {
          field.reportValidity();
          return;
        }
      }
    }

    if (origin.value === destination.value) {
      setBookingFeedback("search-feedback", "Điểm đi và điểm đến phải khác nhau.", "error");
      destination.focus();
      return;
    }

    const searchButton = $("#search-flights");
    searchButton.disabled = true;
    setBookingFeedback("search-feedback");
    clearBookingSelection();
    $("#flight-count").textContent = "Đang tìm chuyến...";
    $("#flight-results").innerHTML = '<tr><td colspan="6" class="counter-empty-cell">Đang tải dữ liệu chuyến bay...</td></tr>';
    $("#flight-cards").innerHTML = '<p class="counter-empty-mobile">Đang tải dữ liệu chuyến bay...</p>';

    try {
      const result = await bookingFetch("search", {
        method: "POST",
        body: {
          origin: origin.value,
          destination: destination.value,
          departureDate: departure.value,
          tripType: "OneWay",
          seatClass: $("#cabin").value,
          adults: 1,
          children: 0,
          infants: 0
        }
      });
      bookingState.flights = result.outbound || [];
      renderFlights(bookingState.flights);
      $("#flight-count").textContent = `${bookingState.flights.length} chuyến · Sắp xếp theo giờ bay`;
      if (!bookingState.flights.length) {
        setBookingFeedback("search-feedback", result.message || "Không tìm thấy chuyến bay phù hợp.", "error");
      }
    } catch (error) {
      bookingState.flights = [];
      renderFlights([]);
      $("#flight-count").textContent = "Không tải được chuyến bay";
      setBookingFeedback("search-feedback", error.message, "error");
    } finally {
      searchButton.disabled = false;
    }
  }

  async function chooseFlight(flightId) {
    const flight = bookingState.flights.find((item) => item.flightId === Number(flightId));
    if (!flight) return;
    bookingState.selectedFlight = flight;
    bookingState.selectedSeat = null;
    bookingState.seatMap = null;
    renderFlights(bookingState.flights);
    $("#selected-flight-label").textContent = `${flight.flightNumber} · ${flight.departureCode} → ${flight.arrivalCode}`;
    $("#seat-map").replaceChildren();
    $("#seat-map-message").textContent = "Đang tải sơ đồ ghế...";
    updateUnquotedSummary();
    setBookingFeedback("booking-feedback");

    try {
      const seatMap = await bookingFetch(`seatmap?flightId=${flight.flightId}&seatClass=${encodeURIComponent($("#cabin").value)}`);
      if (bookingState.selectedFlight?.flightId !== flight.flightId) return;
      bookingState.seatMap = seatMap;
      renderSeatMap(seatMap);
    } catch (error) {
      $("#seat-map-message").textContent = error.message;
      setBookingFeedback("booking-feedback", error.message, "error");
    }
  }

  function renderSeatMap(seatMap) {
    const fragment = document.createDocumentFragment();
    for (const row of seatMap.rows || []) {
      for (const seat of row.cells) {
        if (!seat) {
          const gap = document.createElement("span");
          gap.setAttribute("aria-hidden", "true");
          fragment.append(gap);
          continue;
        }
        const button = document.createElement("button");
        button.className = "counter-seat";
        button.type = "button";
        button.textContent = seat.number;
        button.dataset.seatId = seat.seatId;
        button.dataset.seatNumber = seat.number;
        button.setAttribute("aria-pressed", "false");
        button.disabled = seat.occupied;
        if (seat.occupied) button.title = "Ghế đã có người đặt";
        fragment.append(button);
      }
    }
    $("#seat-map").replaceChildren(fragment);
    $("#seat-map-message").textContent = bookingState.seatMap.rows.length
      ? "Chọn một ghế còn trống cho hành khách."
      : "Máy bay chưa có sơ đồ ghế cho hạng này.";
  }

  function buildQuoteRequest() {
    const selectedSeat = bookingState.selectedSeat;
    const flight = bookingState.selectedFlight;
    return {
      tripType: "OneWay",
      seatClass: $("#cabin").value,
      outboundFlightId: flight.flightId,
      passengers: [{
        type: "Adult",
        fullName: $("#passenger-name").value.trim(),
        gender: $("#passenger-gender").value,
        dob: $("#passenger-dob").value,
        documentNumber: $("#passenger-id").value.trim()
      }],
      seats: { outbound: [selectedSeat.seatId], return: [] },
      paymentMethod: $("[name='payment']:checked").value,
      contactFullName: $("#contact-name").value.trim(),
      contactEmail: $("#contact-email").value.trim(),
      contactPhone: $("#contact-phone").value.trim()
    };
  }

  function quoteFormIsReady() {
    return Boolean(
      bookingState.selectedFlight &&
      bookingState.selectedSeat &&
      [
        $("#passenger-name"),
        $("#passenger-dob"),
        $("#passenger-id"),
        $("#contact-name"),
        $("#contact-phone"),
        $("#contact-email")
      ].every((field) => field.value.trim() && field.checkValidity())
    );
  }

  async function refreshCounterQuote() {
    if (!quoteFormIsReady()) return false;
    const request = buildQuoteRequest();
    const fingerprint = JSON.stringify(request);
    if (bookingState.quoteFingerprint === fingerprint) return true;

    const version = ++bookingState.quoteRequestVersion;
    const button = $("#confirm-counter-booking");
    button.disabled = true;
    $("#booking-status").textContent = "Đang tính...";

    try {
      const quote = await bookingFetch("counter/quote", {
        method: "POST",
        body: request,
        antiforgery: true
      });
      if (version !== bookingState.quoteRequestVersion ||
          fingerprint !== JSON.stringify(buildQuoteRequest())) return false;
      bookingState.quoteFingerprint = fingerprint;
      renderQuote(quote);
      setBookingFeedback("booking-feedback");
      return true;
    } catch (error) {
      if (version === bookingState.quoteRequestVersion) {
        bookingState.quoteFingerprint = null;
        $("#booking-status").textContent = "Chưa báo giá";
        button.disabled = !bookingState.selectedSeat || bookingState.completed;
        setBookingFeedback("booking-feedback", error.message, "error");
      }
      return false;
    }
  }

  function invalidateCounterQuote() {
    window.clearTimeout(bookingQuoteTimer);
    bookingState.quoteFingerprint = null;
    bookingState.quoteRequestVersion++;
    updateUnquotedSummary();
    if (quoteFormIsReady()) {
      bookingQuoteTimer = window.setTimeout(refreshCounterQuote, 450);
    }
  }

  function validateBooking() {
    if (!bookingState.selectedFlight) {
      setBookingFeedback("booking-feedback", "Vui lòng tìm và chọn chuyến bay.", "error");
      return false;
    }
    if (!bookingState.selectedSeat) {
      setBookingFeedback("booking-feedback", "Vui lòng chọn ghế cho hành khách.", "error");
      return false;
    }
    for (const field of [
      $("#passenger-name"),
      $("#passenger-dob"),
      $("#passenger-id"),
      $("#contact-name"),
      $("#contact-phone"),
      $("#contact-email")
    ]) {
      if (!field.checkValidity()) {
        field.reportValidity();
        field.focus();
        return false;
      }
    }
    return true;
  }

  async function confirmCounterBooking() {
    if (bookingState.completed || !validateBooking()) return;

    const button = $("#confirm-counter-booking");
    button.disabled = true;
    setBookingFeedback("booking-feedback");

    try {
      const currentFingerprint = JSON.stringify(buildQuoteRequest());
      if (bookingState.quoteFingerprint !== currentFingerprint) {
        const quoteReady = await refreshCounterQuote();
        if (!quoteReady) return;
        setBookingFeedback(
          "booking-feedback",
          "Đã cập nhật tổng tiền. Vui lòng kiểm tra lại rồi nhấn xác nhận thanh toán lần nữa."
        );
        return;
      }

      button.textContent = "Đang xác nhận...";
      const result = await bookingFetch("counter/confirm", {
        method: "POST",
        antiforgery: true
      });
      bookingState.completed = true;
      $("#booking-status").textContent = "Đã thanh toán";
      button.textContent = "Đã hoàn tất";
      setBookingFeedback(
        "booking-feedback",
        `Đặt vé thành công · Mã đặt chỗ ${result.bookingPnr}. Vé đã được phát hành.`,
        "success"
      );
      showToast(`Đã tạo đặt chỗ ${result.bookingPnr} và ghi nhận thanh toán.`);
    } catch (error) {
      button.textContent = "Xác nhận thanh toán";
      button.disabled = !bookingState.selectedSeat;
      setBookingFeedback("booking-feedback", error.message, "error");
      if (/ghế|seat/i.test(error.message) && bookingState.selectedFlight) {
        chooseFlight(bookingState.selectedFlight.flightId);
      }
    }
  }

  function resetBooking() {
    window.clearTimeout(bookingQuoteTimer);
    $("#origin").value = $("#origin").querySelector("option[value='SGN']") ? "SGN" : "";
    $("#destination").value = $("#destination").querySelector("option[value='HAN']") ? "HAN" : "";
    $("#departure").value = localIsoDate(1);
    $("#cabin").value = "Economy";
    $("#passenger-name").value = "";
    $("#passenger-gender").value = "Male";
    $("#passenger-dob").value = "";
    $("#passenger-id").value = "";
    $("#contact-name").value = "";
    $("#contact-phone").value = "";
    $("#contact-email").value = "";
    $$("[name='payment']").forEach((radio) => {
      radio.checked = radio.value === "Cash";
      radio.closest(".counter-payment-option").classList.toggle("is-selected", radio.checked);
    });
    bookingState.flights = [];
    clearBookingSelection();
    renderFlights([]);
    $("#flight-results").innerHTML = '<tr><td colspan="6" class="counter-empty-cell">Nhập hành trình và nhấn “Tìm chuyến”.</td></tr>';
    $("#flight-count").textContent = "Chưa tìm chuyến";
    setBookingFeedback("search-feedback");
    $("#flight-cards").innerHTML = '<p class="counter-empty-mobile">Nhập hành trình và nhấn “Tìm chuyến”.</p>';
  }

  function renderIcons() {
    if (window.lucide && typeof window.lucide.createIcons === "function") {
      window.lucide.createIcons();
    }
  }

  function showToast(message) {
    const toast = $(".counter-toast");
    toast.textContent = message;
    toast.hidden = false;
    window.clearTimeout(toastTimer);
    toastTimer = window.setTimeout(() => {
      toast.hidden = true;
    }, 3200);
  }

  function setMenuOpen(isOpen) {
    document.body.dataset.menu = isOpen ? "open" : "closed";
    $("[data-menu-toggle]").setAttribute("aria-expanded", String(isOpen));
    $("[data-menu-close]").hidden = !isOpen;
  }

  function showConfirmation(action) {
    const dialog = $(".counter-dialog");
    const copy = $("[data-dialog-copy]");
    const reason = $("[data-dialog-reason]");
    const confirm = $("[data-dialog-confirm]");
    const actions = {
      cancel: {
        copy: "Đặt chỗ TEO-240918 sẽ được gửi yêu cầu hủy. Tiền hoàn 2.450.000 ₫ là ước tính mẫu; kiểm tra điều kiện trước khi xác nhận.",
        confirm: "Xác nhận hủy",
        danger: true,
        needsReason: true
      },
      exchange: {
        copy: "Đặt chỗ TEO-240918 sẽ được gửi yêu cầu đổi. Phụ phí cần được tính theo điều kiện vé và chuyến mới.",
        confirm: "Xác nhận đổi",
        danger: false,
        needsReason: true
      },
      lock: {
        copy: "Tài khoản Lê Ngọc Hà sẽ không thể đăng nhập sau khi bị khóa. Dữ liệu đặt chỗ vẫn được giữ nguyên.",
        confirm: "Khóa tài khoản",
        danger: true,
        needsReason: false
      }
    };
    const selected = actions[action];
    if (!selected) return;

    copy.textContent = selected.copy;
    reason.hidden = !selected.needsReason;
    const reasonInput = $("input", reason);
    reasonInput.value = "";
    reasonInput.setCustomValidity("");
    confirm.textContent = selected.confirm;
    confirm.classList.toggle("counter-button-danger", selected.danger);
    confirm.classList.toggle("counter-button-primary", !selected.danger);
    dialog.showModal();
  }

  document.title = `${pageTitles[activeScreen]} — TEO Airlines`;
  $$(".counter-view").forEach((view) => {
    view.hidden = view.dataset.screen !== activeScreen;
  });
  $$("[data-screen-link]").forEach((link) => {
    if (link.dataset.screenLink === activeScreen) {
      link.setAttribute("aria-current", "page");
    }
  });
  $("[data-page-title]").textContent = pageTitles[activeScreen];

  $("[data-menu-toggle]").addEventListener("click", () => {
    setMenuOpen(document.body.dataset.menu !== "open");
  });
  $("[data-menu-close]").addEventListener("click", () => setMenuOpen(false));
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      setMenuOpen(false);
      $$(".counter-select-trigger[aria-expanded='true']").forEach((trigger) => {
        trigger.setAttribute("aria-expanded", "false");
        trigger.nextElementSibling.classList.remove("is-open");
        trigger.nextElementSibling.setAttribute("aria-hidden", "true");
        trigger.nextElementSibling.inert = true;
        trigger.focus();
      });
    }
  });

  $$("[data-select-trigger]").forEach((trigger) => {
    trigger.addEventListener("click", () => {
      const menu = trigger.nextElementSibling;
      const isOpen = trigger.getAttribute("aria-expanded") === "true";
      trigger.setAttribute("aria-expanded", String(!isOpen));
      menu.classList.toggle("is-open", !isOpen);
      menu.setAttribute("aria-hidden", String(isOpen));
      menu.inert = isOpen;
    });
  });
  $$(".counter-select-option").forEach((option) => {
    option.addEventListener("click", () => {
      const menu = option.closest(".counter-select-menu");
      const trigger = menu.previousElementSibling;
      menu.querySelectorAll(".counter-select-option").forEach((item) => {
        item.setAttribute("aria-selected", String(item === option));
      });
      trigger.querySelector("span").textContent = option.textContent.trim();
      trigger.setAttribute("aria-expanded", "false");
      menu.classList.remove("is-open");
      menu.setAttribute("aria-hidden", "true");
      menu.inert = true;
      trigger.focus();
    });
  });

  document.addEventListener("click", (event) => {
    $$(".counter-select-trigger[aria-expanded='true']").forEach((trigger) => {
      if (trigger.parentElement.contains(event.target)) return;
      trigger.setAttribute("aria-expanded", "false");
      trigger.nextElementSibling.classList.remove("is-open");
      trigger.nextElementSibling.setAttribute("aria-hidden", "true");
      trigger.nextElementSibling.inert = true;
    });
  });

  $$("[name='payment']").forEach((radio) => {
    radio.addEventListener("change", () => {
      $$(".counter-payment-option").forEach((option) => {
        option.classList.toggle("is-selected", $("input", option).checked);
      });
      invalidateCounterQuote();
    });
  });
  $("#search-flights").addEventListener("click", searchFlights);
  $("#reset-booking").addEventListener("click", resetBooking);
  $("#confirm-counter-booking").addEventListener("click", confirmCounterBooking);
  for (const field of [$("#origin"), $("#destination"), $("#departure"), $("#cabin")]) {
    field.addEventListener("change", () => {
      if (!bookingState.flights.length) return;
      bookingState.flights = [];
      clearBookingSelection();
      renderFlights([]);
      $("#flight-count").textContent = "Tìm lại chuyến sau khi đổi điều kiện";
    });
  }
  $("#flight-results").addEventListener("click", (event) => {
    const button = event.target.closest("[data-select-flight]");
    if (button) chooseFlight(button.dataset.selectFlight);
  });
  $("#flight-cards").addEventListener("click", (event) => {
    const button = event.target.closest("[data-select-flight]");
    if (button) chooseFlight(button.dataset.selectFlight);
  });
  $("#seat-map").addEventListener("click", (event) => {
    const button = event.target.closest("[data-seat-id]");
    if (!button || button.disabled) return;
    $("#seat-map").querySelectorAll("[aria-pressed='true']").forEach((seat) => {
      seat.setAttribute("aria-pressed", "false");
    });
    button.setAttribute("aria-pressed", "true");
    bookingState.selectedSeat = {
      seatId: Number(button.dataset.seatId),
      number: button.dataset.seatNumber
    };
    invalidateCounterQuote();
  });
  for (const field of [
    $("#passenger-name"),
    $("#passenger-gender"),
    $("#passenger-dob"),
    $("#passenger-id"),
    $("#contact-name"),
    $("#contact-phone"),
    $("#contact-email")
  ]) {
    field.addEventListener("input", invalidateCounterQuote);
    field.addEventListener("change", invalidateCounterQuote);
  }

  $$("[data-open-confirm]").forEach((button) => {
    button.addEventListener("click", () => showConfirmation(button.dataset.openConfirm));
  });
  const dialog = $(".counter-dialog");
  $("[data-dialog-close]").addEventListener("click", () => dialog.close());
  $("[data-dialog-reason] input").addEventListener("input", (event) => {
    event.currentTarget.setCustomValidity("");
  });
  $("[data-dialog-confirm]").addEventListener("click", () => {
    const reason = $("[data-dialog-reason]");
    const reasonInput = $("input", reason);
    if (!reason.hidden && !reasonInput.value.trim()) {
      reasonInput.setCustomValidity("Vui lòng nhập lý do xử lý.");
      reasonInput.reportValidity();
      reasonInput.focus();
      return;
    }
    dialog.close();
    showToast("Giao diện minh họa — thao tác chưa được gửi đến hệ thống.");
  });
  $$("[data-demo-action]").forEach((button) => {
    button.addEventListener("click", () => showToast("Giao diện minh họa — chức năng chưa được kết nối."));
  });
  $$("[data-demo-form]").forEach((form) => {
    form.addEventListener("submit", (event) => {
      event.preventDefault();
      showToast("Giao diện minh họa — thông tin chưa được lưu.");
    });
  });

  $("#departure").min = localIsoDate();
  $("#departure").value = localIsoDate(1);
  loadAirports();
  renderIcons();
})();
