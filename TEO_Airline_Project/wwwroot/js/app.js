(() => {
  "use strict";

  const API_BASE = "/api/booking";
  const SEAT_CLASS_LABEL = { Economy: "Phổ thông", Business: "Thương gia" };
  const TYPE_LABEL = { Adult: "Người lớn", Child: "Trẻ em", Infant: "Em bé" };
  const PLANE_SVG = '<svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M20.8 3.2c-.9-.9-2.2-.4-3.1.5l-4.3 4.3-7.8-2c-.5-.1-1 .1-1.4.4l-.9.9 5.7 3.2-3.1 3.1-2.8-.7L2 14l3.7 1.8L7.5 19l1.1-1.1-.7-2.8 3.1-3.1 3.2 5.7.9-.9c.4-.4.5-.9.4-1.4l-2-7.8 4.3-4.3c.9-.9 1.4-2.2.5-3.1z"></path></svg>';

  const screenTitles = {
    home: "TEO Airlines — Đặt vé máy bay",
    results: "Chọn chuyến bay — TEO Airlines",
    passengers: "Thông tin hành khách — TEO Airlines",
    seats: "Chọn chỗ ngồi — TEO Airlines",
    payment: "Xác nhận & thanh toán — TEO Airlines",
    confirmation: "Đặt chỗ — TEO Airlines"
  };

  const state = {
    screen: "home",
    airports: [],
    context: { isAuthenticated: false, fullName: "", email: "", phone: "", loyaltyPoints: 0, redeemedPromoCodes: [] },
    search: {
      tripType: "OneWay",
      seatClass: "Economy",
      origin: "",
      destination: "",
      departureDate: "",
      returnDate: ""
    },
    passengers: { adult: 1, child: 0, infant: 0 },
    results: { outbound: [], inbound: [] },
    selection: { outbound: null, return: null },
    travelers: [],
    contact: { fullName: "", email: "", phone: "" },
    seats: { outbound: [], return: [] },
    seatMaps: { outbound: null, return: null },
    quote: null,
    promoCode: "",
    paymentMethod: "MoMo",
    hold: { timerId: null, deadline: 0 },
    lastBooking: null
  };

  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  const money = (amount) => `${new Intl.NumberFormat("vi-VN").format(Math.round(Number(amount) || 0))} đ`;
  const escapeHtml = (value) => String(value ?? "").replace(/[&<>"']/g, (character) => ({
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

  const dateLabel = (isoDate) => {
    if (!isoDate) return "Chưa chọn";
    const date = new Date(`${isoDate}T12:00:00`);
    return new Intl.DateTimeFormat("vi-VN", {
      weekday: "long", day: "numeric", month: "long", year: "numeric"
    }).format(date);
  };

  const todayIso = () => {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-${String(now.getDate()).padStart(2, "0")}`;
  };

  const addDaysIso = (isoDate, days) => {
    const date = new Date(`${isoDate}T12:00:00`);
    date.setDate(date.getDate() + days);
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
  };

  const normalizeText = (value) => String(value ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLowerCase()
    .trim();

  const formatDuration = (minutes) => {
    const hours = Math.floor(minutes / 60);
    const rest = minutes % 60;
    return `${hours} giờ ${String(rest).padStart(2, "0")} phút`;
  };

  const timeBucket = (time) => {
    const hour = Number(String(time).slice(0, 2));
    if (hour < 12) return "morning";
    if (hour < 18) return "afternoon";
    return "evening";
  };

  function renderIcons() {
    if (window.lucide && typeof window.lucide.createIcons === "function") {
      window.lucide.createIcons();
    }
  }

  function showToast(message) {
    const toast = $("#toast");
    if (!toast) return;
    toast.textContent = message;
    toast.hidden = false;
    window.clearTimeout(showToast.timeoutId);
    showToast.timeoutId = window.setTimeout(() => {
      toast.hidden = true;
    }, 3600);
  }

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
      const error = new Error(data?.message || "Không thể xử lý yêu cầu. Vui lòng thử lại.");
      error.data = data;
      throw error;
    }

    return data;
  }

  /* ---------- Điều hướng màn hình ---------- */

  function setScreen(nextScreen, { scrollToTop = true } = {}) {
    if (!screenTitles[nextScreen]) return;
    state.screen = nextScreen;

    $$(".screen").forEach((screen) => {
      const isActive = screen.dataset.screen === nextScreen;
      screen.classList.toggle("is-active", isActive);
      screen.hidden = !isActive;
    });

    document.title = screenTitles[nextScreen];

    if (nextScreen === "passengers") preparePassengerScreen();
    if (nextScreen === "seats") loadSeatMaps();
    if (nextScreen === "payment") openPaymentScreen();
    if (nextScreen === "confirmation") renderConfirmation();

    if (scrollToTop) window.scrollTo({ top: 0, behavior: "smooth" });
    renderIcons();
  }

  /* ---------- Tìm kiếm ---------- */

  function updatePassengerSummary() {
    const total = state.passengers.adult + state.passengers.child + state.passengers.infant;
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

  function resolveAirport(raw) {
    const value = String(raw || "").trim();
    if (!value) return null;

    const matched = value.match(/\(\s*([A-Za-z]{3})\s*\)/);
    const code = matched ? matched[1].toUpperCase() : (/^[A-Za-z]{3}$/.test(value) ? value.toUpperCase() : "");
    if (code) {
      const byCode = state.airports.find((airport) => airport.code.toUpperCase() === code);
      if (byCode) return byCode;
    }

    const normalized = normalizeText(value);
    return state.airports.find((airport) =>
      normalizeText(airport.label) === normalized ||
      normalizeText(airport.city) === normalized ||
      normalizeText(airport.name) === normalized) || null;
  }

  function hideSearchError() {
    const error = $("#search-error");
    error.hidden = true;
    error.textContent = "";
    $("#origin").removeAttribute("aria-invalid");
    $("#destination").removeAttribute("aria-invalid");
  }

  function showSearchError(message, field) {
    const error = $("#search-error");
    error.textContent = message;
    error.hidden = false;
    if (field) {
      field.setAttribute("aria-invalid", "true");
      field.focus();
    }
  }

  function validateSearchForm() {
    hideSearchError();

    const originInput = $("#origin");
    const destinationInput = $("#destination");
    const origin = resolveAirport(originInput.value);
    const destination = resolveAirport(destinationInput.value);

    if (!origin || !destination) {
      showSearchError(
        "Vui lòng chọn điểm đi và điểm đến trong danh sách sân bay (ví dụ: TP. Hồ Chí Minh (SGN)).",
        !origin ? originInput : destinationInput);
      return null;
    }

    if (origin.code === destination.code) {
      showSearchError("Điểm đi và điểm đến cần khác nhau.", destinationInput);
      return null;
    }

    const departureDate = $("#departure-date").value;
    if (!departureDate || departureDate < todayIso()) {
      showSearchError("Ngày đi phải từ hôm nay trở đi.");
      return null;
    }

    const isRoundTrip = state.search.tripType === "RoundTrip";
    const returnDate = $("#return-date").value;
    if (isRoundTrip) {
      if (!returnDate) {
        showSearchError("Vui lòng chọn ngày về cho hành trình khứ hồi.");
        return null;
      }
      if (returnDate < departureDate) {
        showSearchError("Ngày về không thể trước ngày đi.");
        return null;
      }
    }

    return {
      origin: origin.code,
      destination: destination.code,
      departureDate,
      returnDate: isRoundTrip ? returnDate : "",
      tripType: state.search.tripType,
      seatClass: state.search.seatClass,
      adults: state.passengers.adult,
      children: state.passengers.child,
      infants: state.passengers.infant
    };
  }

  async function handleSearchSubmit(event) {
    event.preventDefault();
    const query = validateSearchForm();
    if (!query) return;

    const submit = $("#search-submit");
    const label = $("span", submit);
    const originalLabel = label.textContent;
    submit.disabled = true;
    submit.setAttribute("aria-busy", "true");
    label.textContent = "Đang tìm chuyến…";

    try {
      const response = await apiFetch("/search", { method: "POST", body: JSON.stringify(query) });

      if (!response.ok) {
        showSearchError(response.message || "Không tìm thấy chuyến bay phù hợp.");
        return;
      }

      state.search = {
        tripType: query.tripType,
        seatClass: query.seatClass,
        origin: query.origin,
        destination: query.destination,
        departureDate: query.departureDate,
        returnDate: query.returnDate
      };
      state.results = { outbound: response.outbound || [], inbound: response.inbound || [] };
      state.selection = { outbound: null, return: null };
      state.seats = { outbound: [], return: [] };
      state.seatMaps = { outbound: null, return: null };
      state.quote = null;
      state.promoCode = "";
      state.travelers = [];
      state.lastBooking = null;
      clearHoldTimer();
      $("#promo-code").value = "";
      $("#confirm-booking").disabled = false;
      $("#hold-notice").classList.remove("is-expired");

      renderResults(response);
      setScreen("results");
    } catch (error) {
      showSearchError(error.message);
    } finally {
      submit.disabled = false;
      submit.removeAttribute("aria-busy");
      label.textContent = originalLabel;
    }
  }

  /* ---------- Danh sách chuyến bay ---------- */

  function flightCardHtml(flight, direction) {
    const selected = state.selection[direction]?.flightId === flight.flightId;
    return `
      <article class="flight-card ${selected ? "is-selected" : ""}" data-flight-card data-flight-id="${flight.flightId}" data-direction="${direction}" data-direct="true" data-time="${timeBucket(flight.departureTime)}" data-price="${flight.pricePerAdult}">
        <div class="flight-main">
          <div class="airline-mark" aria-hidden="true">${PLANE_SVG}</div>
          <div class="flight-schedule">
            <div class="flight-time"><strong>${flight.departureTime}</strong><span>${flight.departureCode}</span></div>
            <div class="flight-duration"><span>${formatDuration(flight.durationMinutes)}</span><span class="route-line"><i></i></span><span>Bay thẳng · còn ${flight.seatsLeft} ghế</span></div>
            <div class="flight-time"><strong>${flight.arrivalTime}</strong><span>${flight.arrivalCode}</span></div>
          </div>
          <div class="flight-meta"><strong>${escapeHtml(flight.flightNumber)}</strong><span>${escapeHtml(flight.aircraftModel)}</span></div>
        </div>
        <div class="flight-choice">
          <div><strong>${money(flight.pricePerAdult)}</strong><span>từ · đã gồm thuế &amp; phí</span></div>
          <button class="button ${selected ? "button-primary" : "button-outline"} choose-flight" type="button" data-select-flight="${flight.flightId}" data-direction="${direction}">${selected ? "Đã chọn" : "Chọn chuyến"}</button>
        </div>
      </article>`;
  }

  function renderResults(response) {
    const isRoundTrip = state.search.tripType === "RoundTrip";
    const originCode = state.search.origin;
    const destinationCode = state.search.destination;

    $("#outbound-group-title").textContent =
      `Chuyến đi · ${originCode} → ${destinationCode} · ${formatDate(state.search.departureDate)}`;
    $("#outbound-list").innerHTML = (state.results.outbound || [])
      .map((flight) => flightCardHtml(flight, "outbound"))
      .join("");

    const returnGroup = $("#return-group");
    returnGroup.hidden = !isRoundTrip;

    if (isRoundTrip) {
      $("#return-group-title").textContent =
        `Chuyến về · ${destinationCode} → ${originCode} · ${formatDate(state.search.returnDate)}`;
      $("#return-list").innerHTML = (state.results.inbound || [])
        .map((flight) => flightCardHtml(flight, "return"))
        .join("");
    } else {
      $("#return-list").innerHTML = "";
    }

    $$("[data-filter]").forEach((filter) => { filter.checked = false; });
    $("#flight-sort").value = "recommended";

    if (response.message) {
      showToast(response.message);
    }

    updateSelectionUi();
    applyFilters();
    sortFlights();
  }

  function updateSelectionUi() {
    $$("[data-flight-card]").forEach((card) => {
      const direction = card.dataset.direction;
      const isSelected = state.selection[direction]?.flightId === Number(card.dataset.flightId);
      card.classList.toggle("is-selected", isSelected);
      const button = $("[data-select-flight]", card);
      if (button) {
        button.classList.toggle("button-primary", isSelected);
        button.classList.toggle("button-outline", !isSelected);
        button.textContent = isSelected ? "Đã chọn" : "Chọn chuyến";
      }
    });

    const outboundSelected = Boolean(state.selection.outbound);
    const returnSelected = !(state.search.tripType === "RoundTrip") || Boolean(state.selection.return);
    const ready = outboundSelected && returnSelected;

    $("#continue-to-passengers").disabled = !ready;

    if (!outboundSelected) {
      $("#selection-note").textContent = "Chọn chuyến bay để tiếp tục.";
    } else if (!returnSelected) {
      $("#selection-note").textContent = `Đã chọn ${state.selection.outbound.flightNumber} · Chọn thêm chuyến về.`;
    } else {
      const parts = [state.selection.outbound, state.selection.return]
        .filter(Boolean)
        .map((flight) => `${flight.flightNumber} (${flight.departureTime})`);
      $("#selection-note").textContent = `Đã chọn ${parts.join(" · ")}.`;
    }
  }

  function selectFlight(direction, flightId) {
    const list = direction === "return" ? state.results.inbound : state.results.outbound;
    const flight = list.find((item) => String(item.flightId) === String(flightId));
    if (!flight) return;

    state.selection[direction] = flight;
    state.seats[direction] = [];
    state.seatMaps[direction] = null;
    state.quote = null;

    updateSelectionUi();
  }

  function applyFilters() {
    const filters = $$("[data-filter]:checked").map((input) => input.dataset.filter);
    let visibleOutbound = 0;
    let visibleReturn = 0;

    $$("[data-flight-card]").forEach((card) => {
      const price = Number(card.dataset.price);
      const show = filters.every((filter) => {
        if (filter === "direct") return card.dataset.direct === "true";
        if (filter === "stop") return card.dataset.direct === "false";
        if (filter === "morning" || filter === "afternoon") return card.dataset.time === filter;
        if (filter === "under-two") return price < 2000000;
        return true;
      });

      card.hidden = !show;
      if (show) {
        if (card.dataset.direction === "return") visibleReturn += 1;
        else visibleOutbound += 1;
      }
    });

    const total = visibleOutbound + visibleReturn;
    $("#result-count").textContent = `${total} chuyến bay`;

    const isRoundTrip = state.search.tripType === "RoundTrip";
    const hasFlights = (state.results.outbound || []).length > 0 &&
      (!isRoundTrip || (state.results.inbound || []).length > 0);
    const nothingVisible = hasFlights && (visibleOutbound === 0 || (isRoundTrip && visibleReturn === 0));

    $("#empty-results").hidden = !nothingVisible;
  }

  function sortFlights() {
    const sortBy = $("#flight-sort").value;

    ["#outbound-list", "#return-list"].forEach((selector) => {
      const list = $(selector);
      if (!list) return;
      const cards = $$("[data-flight-card]", list);

      cards.sort((first, second) => {
        if (sortBy === "price") return Number(first.dataset.price) - Number(second.dataset.price);
        if (sortBy === "departure") {
          const firstTime = $(".flight-time strong", first).textContent;
          const secondTime = $(".flight-time strong", second).textContent;
          return firstTime.localeCompare(secondTime);
        }
        return Number(first.dataset.price) - Number(second.dataset.price);
      });

      cards.forEach((card) => list.appendChild(card));
    });
  }

  function clearFilters() {
    $$("[data-filter]").forEach((filter) => { filter.checked = false; });
    applyFilters();
  }

  /* ---------- Thông tin hành khách ---------- */

  function travelerPlan() {
    const plan = [];
    for (let index = 0; index < state.passengers.adult; index += 1) plan.push({ type: "Adult" });
    for (let index = 0; index < state.passengers.child; index += 1) plan.push({ type: "Child" });
    for (let index = 0; index < state.passengers.infant; index += 1) plan.push({ type: "Infant" });
    return plan;
  }

  function preparePassengerScreen() {
    renderTravelers();
    $("#passenger-error").hidden = true;
    $("#passenger-error").textContent = "";

    const contact = state.context;
    if (!$("#contact-name").value && contact.fullName) $("#contact-name").value = contact.fullName;
    if (!$("#contact-email").value && contact.email) $("#contact-email").value = contact.email;
    if (!$("#contact-phone").value && contact.phone) $("#contact-phone").value = contact.phone;

    renderSummary($("#passenger-summary-card"));
  }

  function adultOptionsHtml(selectedIndex) {
    const plan = travelerPlan();
    return plan
      .map((item, index) => ({ item, index }))
      .filter(({ item }) => item.type === "Adult")
      .map(({ index }) => {
        const saved = state.travelers[index]?.fullName;
        const label = saved ? `Hành khách ${index + 1} · ${escapeHtml(saved)}` : `Người lớn ${index + 1}`;
        const isSelected = selectedIndex === index ? " selected" : "";
        return `<option value="${index}"${isSelected}>${label}</option>`;
      })
      .join("");
  }

  function renderTravelers() {
    const plan = travelerPlan();
    const container = $("#traveler-list");

    container.innerHTML = plan.map((traveler, index) => {
      const saved = state.travelers[index] || {};
      const gender = saved.gender || "Male";
      const genderRadio = (value, label) =>
        `<label><input type="radio" name="traveler-gender-${index}" value="${value}"${gender === value ? " checked" : ""}> ${label}</label>`;

      const documentLabel = traveler.type === "Infant"
        ? "Số giấy khai sinh / hộ chiếu"
        : "Số CCCD / hộ chiếu";

      const accompanyField = traveler.type === "Infant"
        ? `<div class="field"><label for="traveler-adult-${index}">Người lớn đi cùng</label>
             <select id="traveler-adult-${index}" data-field="accompanyingIndex">${adultOptionsHtml(saved.accompanyingIndex ?? 0)}</select>
           </div>`
        : "";

      return `
        <section class="form-section traveler-section" data-traveler-index="${index}" data-type="${traveler.type}">
          <div class="section-heading">
            <span class="section-number">${index + 2}</span>
            <div>
              <h2>Hành khách ${index + 1} · ${TYPE_LABEL[traveler.type]}</h2>
              <p>${traveler.type === "Infant" ? "Em bé dưới 2 tuổi, không chiếm ghế riêng." : "Nhập thông tin trùng khớp với giấy tờ sử dụng khi làm thủ tục."}</p>
            </div>
          </div>
          <div class="form-grid">
            <div class="field"><label for="traveler-name-${index}">Họ và tên</label>
              <input id="traveler-name-${index}" data-field="fullName" type="text" value="${escapeHtml(saved.fullName || "")}" placeholder="Nhập họ và tên" autocomplete="off" required></div>
            <div class="field"><label for="traveler-dob-${index}">Ngày sinh</label>
              <input id="traveler-dob-${index}" data-field="dob" type="date" value="${escapeHtml(saved.dob || "")}" max="${todayIso()}" required></div>
            <div class="field"><label for="traveler-doc-${index}">${documentLabel}</label>
              <input id="traveler-doc-${index}" data-field="documentNumber" type="text" value="${escapeHtml(saved.documentNumber || "")}" placeholder="Nhập số giấy tờ" inputmode="numeric" required></div>
            <fieldset class="field gender-field"><legend>Giới tính</legend>
              <div class="inline-options">${genderRadio("Male", "Nam")}${genderRadio("Female", "Nữ")}${genderRadio("Other", "Khác")}</div>
            </fieldset>
            ${accompanyField}
          </div>
          <p class="field-error" data-traveler-error="${index}" hidden></p>
        </section>`;
    }).join("");

    renderIcons();
  }

  function ageAt(isoDob, isoDate) {
    if (!isoDob || !isoDate) return null;
    const dob = new Date(`${isoDob}T12:00:00`);
    const reference = new Date(`${isoDate}T12:00:00`);
    if (Number.isNaN(dob.getTime()) || Number.isNaN(reference.getTime())) return null;

    let age = reference.getFullYear() - dob.getFullYear();
    const hasHadBirthday = reference.getMonth() > dob.getMonth() ||
      (reference.getMonth() === dob.getMonth() && reference.getDate() >= dob.getDate());
    if (!hasHadBirthday) age -= 1;
    return age;
  }

  function setTravelerError(index, message) {
    const section = $(`[data-traveler-index="${index}"]`);
    if (!section) return;
    const error = $(`[data-traveler-error="${index}"]`, section);
    if (error) {
      error.textContent = message || "";
      error.hidden = !message;
    }
  }

  function validateTravelers() {
    const departureDate = state.search.departureDate;
    let firstInvalid = null;

    const sections = $$("[data-traveler-index]");
    sections.forEach((section) => {
      const index = Number(section.dataset.travelerIndex);
      const type = section.dataset.type;
      setTravelerError(index, "");

      const name = $('[data-field="fullName"]', section).value.trim();
      const dob = $('[data-field="dob"]', section).value;
      const documentNumber = $('[data-field="documentNumber"]', section).value.trim();

      let message = "";
      let focusTarget = null;

      if (name.length < 2) {
        message = "Vui lòng nhập họ tên đầy đủ.";
        focusTarget = $('[data-field="fullName"]', section);
      } else if (!dob) {
        message = "Vui lòng chọn ngày sinh.";
        focusTarget = $('[data-field="dob"]', section);
      } else if (documentNumber.length < 5) {
        message = "Số giấy tờ cần ít nhất 5 ký tự.";
        focusTarget = $('[data-field="documentNumber"]', section);
      } else {
        const age = ageAt(dob, departureDate);
        if (type === "Adult" && age < 12) {
          message = `Người lớn phải từ 12 tuổi trở lên (hiện ${age} tuổi).`;
        } else if (type === "Child" && (age < 2 || age >= 12)) {
          message = `Trẻ em phải từ 2 đến dưới 12 tuổi (hiện ${age} tuổi).`;
        } else if (type === "Infant" && age >= 2) {
          message = `Em bé phải dưới 2 tuổi (hiện ${age} tuổi).`;
        }
        if (message) focusTarget = $('[data-field="dob"]', section);
      }

      if (message) {
        setTravelerError(index, message);
        if (!firstInvalid) firstInvalid = focusTarget;
      }
    });

    return firstInvalid;
  }

  function validateContact() {
    const name = $("#contact-name").value.trim();
    const email = $("#contact-email").value.trim();
    const phone = $("#contact-phone").value.trim();
    const phoneNormalized = phone.replace(/[\s.\-()]/g, "");
    const error = $("#passenger-error");

    if (name.length < 2) {
      error.textContent = "Vui lòng nhập họ tên người liên hệ.";
      error.hidden = false;
      $("#contact-name").focus();
      return false;
    }

    if (!/^[^@\s]+@[^@\s]+\.[^@\s]{2,}$/.test(email)) {
      error.textContent = "Email người liên hệ không hợp lệ.";
      error.hidden = false;
      $("#contact-email").focus();
      return false;
    }

    if (!/^(0\d{9}|\+84\d{9})$/.test(phoneNormalized)) {
      error.textContent = "Số điện thoại không đúng định dạng. Ví dụ: 0912345678.";
      error.hidden = false;
      $("#contact-phone").focus();
      return false;
    }

    error.hidden = true;
    error.textContent = "";
    return true;
  }

  function collectTravelers() {
    state.travelers = $$("[data-traveler-index]").map((section) => {
      const type = section.dataset.type;
      const genderInput = $('input[type="radio"]:checked', section);
      const accompanyingSelect = $('[data-field="accompanyingIndex"]', section);

      return {
        type,
        fullName: $('[data-field="fullName"]', section).value.trim(),
        dob: $('[data-field="dob"]', section).value,
        documentNumber: $('[data-field="documentNumber"]', section).value.trim(),
        gender: genderInput ? genderInput.value : "Male",
        accompanyingIndex: type === "Infant" && accompanyingSelect ? Number(accompanyingSelect.value) : null
      };
    });

    state.contact = {
      fullName: $("#contact-name").value.trim(),
      email: $("#contact-email").value.trim(),
      phone: $("#contact-phone").value.trim()
    };
  }

  function handlePassengerSubmit(event) {
    event.preventDefault();
    if (!validateContact()) return;
    const invalid = validateTravelers();
    if (invalid) {
      invalid.focus();
      return;
    }

    collectTravelers();
    setScreen("seats");
  }

  /* ---------- Chọn ghế ---------- */

  function requiredSeats() {
    return state.passengers.adult + state.passengers.child;
  }

  function seatPanelHtml(direction, map, flight) {
    const isReturn = direction === "return";
    const selected = state.seats[direction];

    const rows = map.rows.map((row) => {
      const cells = row.cells.map((cell, columnIndex) => {
        if (columnIndex === 3) {
          return `<span class="aisle-space" aria-hidden="true"></span>${cellHtml(cell, direction, selected)}`;
        }
        return cellHtml(cell, direction, selected);
      }).join("");

      return `<div class="seat-row"><span class="seat-row-label">${row.row}</span>${cells}</div>`;
    }).join("");

    const selectedSeats = map.rows
      .flatMap((row) => row.cells.filter(Boolean))
      .filter((cell) => selected.includes(cell.seatId));

    const extraTotal = selectedSeats.reduce((sum, cell) => sum + (cell.extraFee || 0), 0);
    const note = selectedSeats.length
      ? `Đã chọn ${selectedSeats.length}/${requiredSeats()} ghế: ${selectedSeats.map((cell) => cell.number).join(", ")}${extraTotal ? ` · Phụ phí ${money(extraTotal)}` : ""}`
      : `Chưa chọn ghế. Hãy chọn ${requiredSeats()} ghế còn trống.`;

    return `
      <section class="panel seat-panel" data-seat-panel="${direction}">
        <div class="seat-panel-heading">
          <div>
            <h2>Sơ đồ ghế · ${escapeHtml(flight.flightNumber)}</h2>
            <p>${escapeHtml(map.aircraftModel)} · ${isReturn ? "Chiều về" : "Chiều đi"} ${formatDate(flight.date)} · ${flight.departureCode} → ${flight.arrivalCode}</p>
          </div>
          <span class="seat-page-badge">${SEAT_CLASS_LABEL[state.search.seatClass]}</span>
        </div>
        <div class="seat-legend" aria-label="Chú giải sơ đồ ghế">
          <span><i class="seat-swatch available"></i>Còn trống</span>
          <span><i class="seat-swatch selected"></i>Đang chọn</span>
          <span><i class="seat-swatch occupied"></i>Đã đặt</span>
          <span><i class="seat-swatch extra"></i>Ghế phụ phí</span>
        </div>
        <div class="aircraft">
          <div class="aircraft-cabin">
            <div class="column-labels">
              <span class="row-label-spacer" aria-hidden="true"></span>
              <span>A</span><span>B</span><span>C</span><span class="aisle-label" aria-hidden="true"></span><span>D</span><span>E</span><span>F</span>
            </div>
            <div class="seat-grid">${rows}</div>
          </div>
        </div>
        <p class="seat-selection-note" data-seat-note="${direction}">${note}</p>
      </section>`;
  }

  function cellHtml(cell, direction, selected) {
    if (!cell) {
      return '<span class="seat-placeholder" aria-hidden="true"></span>';
    }

    const isSelected = selected.includes(cell.seatId);
    const classes = ["seat"];
    if (cell.occupied) classes.push("is-occupied");
    if (cell.extra) classes.push("is-extra");
    if (isSelected) classes.push("is-selected");

    const stateLabel = cell.occupied ? "đã được đặt" : cell.extra ? "có phụ phí" : "còn trống";

    return `<button type="button" class="${classes.join(" ")}" data-seat="${cell.seatId}" data-seat-direction="${direction}" ${cell.occupied ? "disabled" : ""} aria-pressed="${isSelected}" aria-label="Ghế ${cell.number}, ${stateLabel}">${cell.column}</button>`;
  }

  async function loadSeatMaps() {
    const container = $("#seat-maps");
    const directions = ["outbound"];
    if (state.search.tripType === "RoundTrip") directions.push("return");

    container.innerHTML = '<p class="mock-note">Đang tải sơ đồ ghế…</p>';
    $("#seats-subheading").textContent = `Chọn ${requiredSeats()} chỗ ngồi cho hành khách có ghế${
      state.search.tripType === "RoundTrip" ? " ở cả hai chặng" : ""}.`;

    try {
      const maps = await Promise.all(directions.map(async (direction) => {
        const flight = state.selection[direction];
        const map = await apiFetch(`/seatmap?flightId=${flight.flightId}&seatClass=${state.search.seatClass}`);
        state.seatMaps[direction] = map;
        return { direction, map };
      }));

      container.innerHTML = maps
        .map(({ direction, map }) => seatPanelHtml(direction, map, state.selection[direction]))
        .join("");

      renderSummary($("#seat-summary-card"));
      renderIcons();
    } catch (error) {
      container.innerHTML = `<p class="mock-note">${escapeHtml(error.message)}</p>`;
    }
  }

  function toggleSeat(direction, seatId) {
    const selected = state.seats[direction];
    const index = selected.indexOf(seatId);

    if (index >= 0) {
      selected.splice(index, 1);
    } else if (selected.length >= requiredSeats()) {
      showToast(`Bạn chỉ cần chọn ${requiredSeats()} ghế cho ${direction === "return" ? "chiều về" : "chiều đi"}.`);
      return;
    } else {
      selected.push(seatId);
    }

    const map = state.seatMaps[direction];
    if (!map) return;

    const panel = $(`[data-seat-panel="${direction}"]`);
    if (panel) panel.outerHTML = seatPanelHtml(direction, map, state.selection[direction]);

    const selectedSeats = map.rows
      .flatMap((row) => row.cells.filter(Boolean))
      .filter((cell) => selected.includes(cell.seatId));
    const extraTotal = selectedSeats.reduce((sum, cell) => sum + (cell.extraFee || 0), 0);
    const note = $(`[data-seat-note="${direction}"]`);
    if (note) {
      note.textContent = selectedSeats.length
        ? `Đã chọn ${selectedSeats.length}/${requiredSeats()} ghế: ${selectedSeats.map((cell) => cell.number).join(", ")}${extraTotal ? ` · Phụ phí ${money(extraTotal)}` : ""}`
        : `Chưa chọn ghế. Hãy chọn ${requiredSeats()} ghế còn trống.`;
    }

    renderSummary($("#seat-summary-card"));
    renderIcons();
  }

  function seatsReady() {
    if (state.seats.outbound.length !== requiredSeats()) return false;
    if (state.search.tripType === "RoundTrip" && state.seats.return.length !== requiredSeats()) return false;
    return true;
  }

  function handleContinueToPayment() {
    if (!seatsReady()) {
      showToast(`Hãy chọn đủ ${requiredSeats()} ghế${state.search.tripType === "RoundTrip" ? " cho mỗi chặng" : ""} trước khi tiếp tục.`);
      return;
    }

    setScreen("payment");
  }

  /* ---------- Thanh toán ---------- */

  function estimateTotal() {
    const p = state.passengers;
    let total = 0;

    [state.selection.outbound, state.selection.return].filter(Boolean).forEach((flight) => {
      total += p.adult * Number(flight.pricePerAdult || 0);
      total += p.child * Number(flight.pricePerChild || 0);
      total += p.infant * Number(flight.pricePerInfant || 0);
    });

    Object.values(state.seatMaps).forEach((map) => {
      if (!map) return;
    });

    ["outbound", "return"].forEach((direction) => {
      const map = state.seatMaps[direction];
      if (!map) return;
      const selected = state.seats[direction];
      map.rows.forEach((row) => {
        row.cells.filter(Boolean).forEach((cell) => {
          if (selected.includes(cell.seatId)) total += Number(cell.extraFee || 0);
        });
      });
    });

    return total;
  }

  function buildQuoteRequest() {
    return {
      tripType: state.search.tripType,
      seatClass: state.search.seatClass,
      outboundFlightId: state.selection.outbound.flightId,
      returnFlightId: state.selection.return ? state.selection.return.flightId : null,
      passengers: state.travelers,
      seats: { outbound: state.seats.outbound, return: state.seats.return },
      promoCode: state.promoCode,
      paymentMethod: state.paymentMethod,
      contactFullName: state.contact.fullName,
      contactEmail: state.contact.email,
      contactPhone: state.contact.phone
    };
  }

  async function refreshQuote() {
    $("#confirm-booking").disabled = true;
    try {
      const quote = await apiFetch("/quote", { method: "POST", body: JSON.stringify(buildQuoteRequest()) });
      state.quote = quote;
      renderFare(quote);
      renderSummary($("#payment-summary-card"));
      startHoldTimer(quote);
      $("#confirm-booking").disabled = false;
      return quote;
    } catch (error) {
      showToast(error.message);
      return null;
    }
  }

  function openPaymentScreen() {
    $("#promo-code").value = state.promoCode || "";
    $("#confirm-booking").disabled = false;
    $("#hold-notice").classList.remove("is-expired");
    renderSummary($("#payment-summary-card"));
    refreshQuote();
  }

  function renderFare(quote) {
    const rows = quote.lines.map((line) => ({ label: line.label, amount: line.amount }));
    if (quote.discount > 0) {
      rows.push({ label: quote.promoCode ? `Voucher ${quote.promoCode}` : "Giảm giá", amount: -quote.discount, discount: true });
    }

    $("#fare-breakdown").innerHTML = rows.map((row) => `
      <div class="fare-row ${row.discount ? "discount" : ""}">
        <dt>${escapeHtml(row.label)}</dt>
        <dd>${row.amount < 0 ? "−" : ""}${money(Math.abs(row.amount))}</dd>
      </div>`).join("");

    $("#total-price").textContent = money(quote.total);

    const feedback = $("#promo-feedback");
    if (!state.promoCode) {
      feedback.textContent = "";
      feedback.classList.remove("is-error");
    } else {
      feedback.textContent = quote.promoMessage || "";
      feedback.classList.toggle("is-error", !quote.promoApplied);
    }
  }

  function clearHoldTimer() {
    if (state.hold.timerId) window.clearInterval(state.hold.timerId);
    state.hold.timerId = null;
    state.hold.deadline = 0;
  }

  function startHoldTimer(quote) {
    clearHoldTimer();
    state.hold.deadline = new Date(quote.expiresAt).getTime();

    const tick = () => {
      const remaining = Math.max(0, Math.floor((state.hold.deadline - Date.now()) / 1000));
      const timer = $("#hold-timer");
      if (timer) {
        timer.textContent = `${String(Math.floor(remaining / 60)).padStart(2, "0")}:${String(remaining % 60).padStart(2, "0")}`;
      }

      if (remaining === 0) {
        clearHoldTimer();
        const notice = $("#hold-notice");
        notice.classList.add("is-expired");
        notice.innerHTML = '<i data-lucide="clock-alert" aria-hidden="true"></i><span>Thời gian giữ chỗ đã hết. Vui lòng xác nhận lại để giữ chỗ mới.</span>';
        $("#confirm-booking").disabled = true;
        renderIcons();
      }
    };

    tick();
    state.hold.timerId = window.setInterval(tick, 1000);
  }

  function handlePaymentMethodChange() {
    const selected = $('input[name="payment-method"]:checked');
    if (!selected) return;

    state.paymentMethod = selected.value === "counter" ? "Cash" : "MoMo";
    $$(".payment-option").forEach((option) => {
      option.classList.toggle("is-selected", $("input", option).checked);
    });

    refreshQuote();
  }

  async function handlePromoSubmit(event) {
    event.preventDefault();
    const code = $("#promo-code").value.trim().toUpperCase();
    state.promoCode = code;
    await refreshQuote();
  }

  async function handleConfirmBooking() {
    if (!state.quote) {
      showToast("Chưa có thông tin giá. Vui lòng thử lại.");
      return;
    }

    if (state.hold.deadline && Date.now() >= state.hold.deadline) {
      showToast("Thời gian giữ chỗ đã hết. Vui lòng xác nhận lại.");
      return;
    }

    const button = $("#confirm-booking");
    button.disabled = true;
    button.setAttribute("aria-busy", "true");
    button.textContent = "Đang xác nhận…";

    try {
      const result = await apiFetch("/confirm", { method: "POST", body: "{}" });
      state.lastBooking = result;
      clearHoldTimer();
      setScreen("confirmation");
    } catch (error) {
      showToast(error.message);
      button.disabled = false;
    } finally {
      button.removeAttribute("aria-busy");
      button.textContent = "Xác nhận đặt chỗ";
    }
  }

  /* ---------- Tóm tắt & xác nhận ---------- */

  function renderSummary(container) {
    if (!container) return;
    const outbound = state.selection.outbound;
    if (!outbound) {
      container.innerHTML = "";
      return;
    }

    const flights = [outbound, state.selection.return].filter(Boolean);
    const total = state.quote ? state.quote.total : estimateTotal();
    const seatLabels = ["outbound", "return"]
      .flatMap((direction) => {
        const map = state.seatMaps[direction];
        if (!map) return [];
        const selected = state.seats[direction];
        return map.rows
          .flatMap((row) => row.cells.filter(Boolean))
          .filter((cell) => selected.includes(cell.seatId))
          .map((cell) => cell.number);
      });

    const flightRows = flights.map((flight) => `
      <div class="summary-route">
        <div class="summary-airport"><strong>${flight.departureCode}</strong><span>${flight.departureTime} · ${formatDate(flight.date)}</span></div>
        <i data-lucide="plane" aria-hidden="true"></i>
        <div class="summary-airport"><strong>${flight.arrivalCode}</strong><span>${flight.arrivalTime} · ${escapeHtml(flight.flightNumber)}</span></div>
      </div>`).join("");

    const passengerParts = [];
    if (state.passengers.adult) passengerParts.push(`${state.passengers.adult} người lớn`);
    if (state.passengers.child) passengerParts.push(`${state.passengers.child} trẻ em`);
    if (state.passengers.infant) passengerParts.push(`${state.passengers.infant} em bé`);

    const firstName = state.travelers[0]?.fullName || state.context.fullName || "Khách";
    const passengerSummary = state.travelers.length > 1
      ? `${escapeHtml(firstName)} + ${state.travelers.length - 1} người`
      : escapeHtml(firstName);

    container.innerHTML = `
      <section class="summary-card">
        <div class="summary-heading"><h2>Tóm tắt hành trình</h2></div>
        <div class="summary-body">
          ${flightRows}
          <div class="summary-details">
            <p><span>Hành khách</span><strong>${passengerParts.join(", ")}</strong></p>
            <p><span>Người đặt</span><strong>${passengerSummary}</strong></p>
            <p><span>Hạng ghế</span><strong>${SEAT_CLASS_LABEL[state.search.seatClass]}</strong></p>
            <p><span>Hành trình</span><strong>${state.search.tripType === "RoundTrip" ? "Khứ hồi" : "Một chiều"}</strong></p>
            ${seatLabels.length ? `<p><span>Ghế</span><strong>${seatLabels.join(", ")}</strong></p>` : ""}
          </div>
        </div>
        <div class="summary-cost">
          <span>${state.quote ? "Tổng thanh toán" : "Tạm tính"}</span>
          <strong>${money(total)}</strong>
        </div>
      </section>`;
    renderIcons();
  }

  function renderConfirmation() {
    const booking = state.lastBooking;
    if (!booking) return;

    $("#booking-code").textContent = booking.bookingPnr;
    $("#confirmation-status").textContent =
      booking.status === "PendingPayment" ? "Chờ xác nhận thanh toán" : booking.status;

    const methodLabel = state.paymentMethod === "MoMo" ? "MoMo" : "Tiền mặt / chuyển khoản tại quầy";
    const expiry = new Date(booking.expiresAt);
    const expiryLabel = `${String(expiry.getHours()).padStart(2, "0")}:${String(expiry.getMinutes()).padStart(2, "0")} ${formatDate(`${expiry.getFullYear()}-${String(expiry.getMonth() + 1).padStart(2, "0")}-${String(expiry.getDate()).padStart(2, "0")}`)}`;

    $("#confirmation-status-note").textContent = `Vui lòng thanh toán trước ${expiryLabel}.`;

    const flightLines = booking.flights.map((flight) => `
      <div class="summary-line"><span>${flight.direction === "Return" ? "Chuyến về" : "Chuyến đi"}</span>
        <strong>${escapeHtml(flight.flightNumber)} · ${flight.departureCode} ${flight.departureTime} → ${flight.arrivalCode} ${flight.arrivalTime}</strong></div>`).join("");

    $("#confirmation-summary").innerHTML = `
      ${flightLines}
      <div class="summary-line"><span>Hành khách</span><strong>${escapeHtml(booking.passengerSummaries.join(" · "))}</strong></div>
      ${booking.seatSummaries.length ? `<div class="summary-line"><span>Ghế đã chọn</span><strong>${escapeHtml(booking.seatSummaries.join(" · "))}</strong></div>` : ""}
      <div class="summary-line"><span>Phương thức thanh toán</span><strong>${methodLabel}</strong></div>
      <div class="summary-line"><span>Trạng thái</span><strong>Chờ thanh toán</strong></div>
      <div class="summary-line"><span>Tổng tiền</span><strong>${money(booking.totalAmount)}</strong></div>
      ${booking.pointsEarned ? `<div class="summary-line"><span>Điểm tích lũy</span><strong>+${booking.pointsEarned} điểm</strong></div>` : ""}`;

    $("#confirmation-note").textContent = booking.status === "PendingPayment"
      ? `Đơn giữ chỗ đến ${expiryLabel}. Quá thời hạn trên, hệ thống tự động huỷ đơn và giải phóng ghế.`
      : "Đơn đặt chỗ đã được ghi nhận.";

    renderIcons();
  }

  /* ---------- Lịch chọn ngày ---------- */

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
    const minimum = todayIso();

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
          const isDisabled = value < minimum;
          return `<button type="button" class="${isMuted ? "is-muted" : ""} ${isSelected ? "is-selected" : ""}" data-date="${value}" aria-pressed="${isSelected}" aria-label="${escapeHtml(dateLabel(value))}" ${isDisabled ? "disabled" : ""}>${date.getDate()}</button>`;
        }).join("")}
      </div>`;
    renderIcons();
  }

  function toggleCalendar(field, open) {
    const trigger = $(".date-trigger", field);
    const panel = $(".calendar-popover", field);
    const shouldOpen = typeof open === "boolean" ? open : panel.hidden;
    closeFloatingPanels(panel);
    panel.hidden = !shouldOpen;
    trigger.setAttribute("aria-expanded", String(shouldOpen));
  }

  function closeFloatingPanels(except = null) {
    $$(".calendar-popover, .passenger-popover, .select-popover").forEach((panel) => {
      if (panel === except) return;
      panel.hidden = true;
    });
  }

  function bindDateControl(fieldId, inputId, labelId) {
    const field = $(`#${fieldId}`);
    const trigger = $(".date-trigger", field);
    const panel = $(".calendar-popover", field);
    const input = $(`#${inputId}`);
    let visibleMonth = new Date(`${input.value || todayIso()}T12:00:00`);
    visibleMonth = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth(), 1);

    function refresh() {
      renderCalendar(panel, input.value, visibleMonth);
      $$("[data-date]", panel).forEach((button) => {
        button.addEventListener("click", () => {
          const value = button.dataset.date;
          if (inputId === "return-date" && value < $("#departure-date").value) {
            showToast("Ngày về không thể trước ngày đi.");
            return;
          }

          if (inputId === "departure-date") {
            if (state.search.tripType === "RoundTrip" && $("#return-date").value < value) {
              $("#return-date").value = value;
              $("#return-date-label").textContent = formatDate(value);
            }
          }

          input.value = value;
          $(`#${labelId}`).textContent = formatDate(value);
          toggleCalendar(field, false);
        });
      });
      $$("[data-calendar-shift]", panel).forEach((button) => {
        button.addEventListener("click", () => {
          visibleMonth = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth() + Number(button.dataset.calendarShift), 1);
          refresh();
        });
      });
    }

    trigger.addEventListener("click", () => {
      if (trigger.disabled) return;
      closeFloatingPanels(panel);
      const willOpen = panel.hidden;
      if (willOpen) {
        const current = new Date(`${input.value || todayIso()}T12:00:00`);
        visibleMonth = new Date(current.getFullYear(), current.getMonth(), 1);
        refresh();
      }
      panel.hidden = !willOpen;
      trigger.setAttribute("aria-expanded", String(willOpen));
    });
  }

  function initializeDateControls() {
    const minimum = todayIso();
    const departure = $("#departure-date");
    const returnInput = $("#return-date");

    if (!departure.value || departure.value < minimum) {
      departure.value = addDaysIso(minimum, 7);
      $("#departure-date-label").textContent = formatDate(departure.value);
    }

    if (!returnInput.value || returnInput.value < departure.value) {
      returnInput.value = addDaysIso(departure.value, 7);
      $("#return-date-label").textContent = formatDate(returnInput.value);
    }

    bindDateControl("departure-field", "departure-date", "departure-date-label");
    bindDateControl("return-field", "return-date", "return-date-label");
  }

  /* ---------- Sự kiện ---------- */

  function bindEvents() {
    $("#flight-search-form").addEventListener("submit", handleSearchSubmit);
    $("#swap-route").addEventListener("click", () => {
      const origin = $("#origin");
      const destination = $("#destination");
      [origin.value, destination.value] = [destination.value, origin.value];
    });

    $$('input[name="trip-type"]').forEach((radio) => {
      radio.addEventListener("change", () => {
        state.search.tripType = radio.value === "roundtrip" ? "RoundTrip" : "OneWay";
        const isRoundTrip = state.search.tripType === "RoundTrip";
        $("#return-field").classList.toggle("is-disabled", !isRoundTrip);
        $("#return-field").hidden = !isRoundTrip;
        $("#return-date").disabled = !isRoundTrip;
        $(".date-trigger", $("#return-field")).disabled = !isRoundTrip;
        if (!isRoundTrip) toggleCalendar($("#return-field"), false);
      });
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

    document.addEventListener("click", (event) => {
      if (!event.target.closest(".date-field, .passenger-field, .field")) closeFloatingPanels();

      const cabinOption = event.target.closest("[data-cabin]");
      if (cabinOption) {
        state.search.seatClass = cabinOption.dataset.cabin === "Thương gia" ? "Business" : "Economy";
        $("#cabin-value").textContent = cabinOption.dataset.cabin;
        $("#cabin-options").hidden = true;
        $("#cabin-trigger").setAttribute("aria-expanded", "false");
        $$("#cabin-options [role=option]").forEach((option) => {
          option.setAttribute("aria-selected", String(option === cabinOption));
        });
        return;
      }

      const countButton = event.target.closest("[data-count]");
      if (countButton) {
        const kind = countButton.dataset.count;
        const next = state.passengers[kind] + Number(countButton.dataset.delta);
        const total = state.passengers.adult + state.passengers.child + state.passengers.infant;

        if (kind === "adult" && next < 1) return;
        if (kind === "infant" && next > state.passengers.adult) {
          showToast("Mỗi em bé cần đi cùng một người lớn.");
          return;
        }
        if (next < 0 || (countButton.dataset.delta === "1" && total >= 9)) return;

        state.passengers[kind] = next;
        if (state.passengers.infant > state.passengers.adult) {
          state.passengers.infant = state.passengers.adult;
        }
        updatePassengerSummary();
        return;
      }

      const selectFlightButton = event.target.closest("[data-select-flight]");
      if (selectFlightButton) {
        selectFlight(selectFlightButton.dataset.direction, selectFlightButton.dataset.selectFlight);
        return;
      }

      const seatButton = event.target.closest("[data-seat]");
      if (seatButton && !seatButton.disabled) {
        toggleSeat(seatButton.dataset.seatDirection, Number(seatButton.dataset.seat));
        return;
      }

      const screenLink = event.target.closest("[data-screen-link]");
      if (screenLink) {
        event.preventDefault();
        setScreen(screenLink.dataset.screenLink);
        return;
      }

      if (event.target.closest("#empty-clear-filters")) {
        clearFilters();
      }
    });

    $("#clear-filters").addEventListener("click", clearFilters);
    $$("[data-filter]").forEach((filter) => filter.addEventListener("change", applyFilters));
    $("#flight-sort").addEventListener("change", sortFlights);

    $("#continue-to-passengers").addEventListener("click", () => {
      if ($("#continue-to-passengers").disabled) return;
      setScreen("passengers");
    });

    $("#passenger-form").addEventListener("submit", handlePassengerSubmit);
    $("#continue-to-payment").addEventListener("click", handleContinueToPayment);

    $$('input[name="payment-method"]').forEach((radio) => {
      radio.addEventListener("change", handlePaymentMethodChange);
    });

    $("#promo-form").addEventListener("submit", handlePromoSubmit);
    $("#confirm-booking").addEventListener("click", handleConfirmBooking);

    document.addEventListener("keydown", (event) => {
      if (event.key !== "Escape") return;
      closeFloatingPanels();
    });
  }

  function fillAirportOptions() {
    const datalist = $("#airport-options");
    if (!datalist) return;
    datalist.innerHTML = state.airports
      .map((airport) => `<option value="${escapeHtml(airport.label)}">${escapeHtml(airport.name)}</option>`)
      .join("");
  }

  async function initialize() {
    bindEvents();
    initializeDateControls();
    updatePassengerSummary();
    renderIcons();

    try {
      const [airports, context] = await Promise.all([
        apiFetch("/airports"),
        apiFetch("/context")
      ]);

      state.airports = airports || [];
      state.context = context;
      fillAirportOptions();

      if (context.isAuthenticated) {
        if (context.fullName) $("#contact-name").value = context.fullName;
        if (context.email) $("#contact-email").value = context.email;
        if (context.phone) $("#contact-phone").value = context.phone;
      }
    } catch (error) {
      showToast(error.message);
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initialize, { once: true });
  } else {
    initialize();
  }
})();
