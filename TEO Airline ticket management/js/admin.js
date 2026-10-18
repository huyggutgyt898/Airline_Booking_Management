(() => {
  const screens = {
    overview: "Tổng quan",
    airports: "Sân bay",
    routes: "Tuyến bay",
    flights: "Chuyến bay",
    aircraft: "Máy bay & sơ đồ ghế",
    pricing: "Bảng giá",
    promotions: "Khuyến mãi"
  };
  const periodData = {
    day: {
      caption: "21/10/2026",
      revenue: "42,8 tr ₫",
      bookings: "128",
      occupancy: "78,4%",
      seats: "164",
      sold: "118",
      cancelled: "5",
      changed: "5",
      promo: [8, 6, 3, 1],
      labels: ["08h", "10h", "12h", "14h", "16h", "18h"],
      revenueBars: [4, 6, 9, 5, 8, 10.8],
      ticketBars: [12, 16, 24, 18, 27, 31]
    },
    week: {
      caption: "19–25/10/2026",
      revenue: "286 tr ₫",
      bookings: "812",
      occupancy: "79,2%",
      seats: "1.025",
      sold: "764",
      cancelled: "22",
      changed: "26",
      promo: [18, 12, 5, 3],
      labels: ["T2", "T3", "T4", "T5", "T6", "T7"],
      revenueBars: [35, 44, 51, 42, 55, 59],
      ticketBars: [110, 130, 144, 126, 148, 154]
    },
    month: {
      caption: "01–31 tháng 10, 2026",
      revenue: "1,28 tỷ ₫",
      bookings: "3.842",
      occupancy: "82,6%",
      seats: "4.650",
      sold: "3.610",
      cancelled: "106",
      changed: "126",
      promo: [58, 42, 17, 9],
      labels: ["01–05", "06–10", "11–15", "16–20", "21–25", "26–31"],
      revenueBars: [160, 190, 215, 185, 250, 280],
      ticketBars: [520, 610, 720, 590, 690, 712]
    },
    quarter: {
      caption: "Quý 4, 2026",
      revenue: "3,64 tỷ ₫",
      bookings: "10.945",
      occupancy: "81,8%",
      seats: "13.380",
      sold: "10.310",
      cancelled: "301",
      changed: "334",
      promo: [168, 120, 62, 40],
      labels: ["Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12"],
      revenueBars: [540, 580, 630, 570, 650, 670],
      ticketBars: [1700, 1800, 1900, 1750, 1800, 1995]
    },
    year: {
      caption: "Năm 2026",
      revenue: "14,2 tỷ ₫",
      bookings: "42.820",
      occupancy: "84,1%",
      seats: "50.915",
      sold: "40.521",
      cancelled: "1.021",
      changed: "1.278",
      promo: [760, 610, 280, 170],
      labels: ["T1–T2", "T3–T4", "T5–T6", "T7–T8", "T9–T10", "T11–T12"],
      revenueBars: [1800, 1950, 2100, 2200, 2400, 3750],
      ticketBars: [6000, 6500, 6800, 7000, 8000, 8520]
    },
    all: {
      caption: "Toàn bộ lịch sử",
      revenue: "31,8 tỷ ₫",
      bookings: "96.940",
      occupancy: "82,1%",
      seats: "118.100",
      sold: "91.620",
      cancelled: "2.380",
      changed: "2.940",
      promo: [1800, 1400, 620, 390],
      labels: ["2021", "2022", "2023", "2024", "2025", "2026"],
      revenueBars: [3500, 4200, 4800, 5100, 5600, 8600],
      ticketBars: [13000, 14000, 15500, 16000, 18500, 19940]
    }
  };
  const formConfigs = {
    airport: {
      title: "sân bay",
      fields: [
        { name: "code", label: "Mã sân bay", required: true, fromCell: 0 },
        { name: "name", label: "Tên sân bay", required: true, fromCell: 1 },
        { name: "city", label: "Thành phố", required: true, fromCell: 2 },
        { name: "country", label: "Quốc gia", required: true, fromCell: 3 }
      ]
    },
    route: {
      title: "tuyến bay",
      fields: [
        { name: "departure", label: "Sân bay đi", required: true, fromCell: 1, read: (row) => row.cells[1]?.textContent.split("·")[0].trim() },
        { name: "arrival", label: "Sân bay đến", required: true, fromCell: 2, read: (row) => row.cells[2]?.textContent.split("·")[0].trim() },
        { name: "type", label: "Loại tuyến", type: "select", options: ["Nội địa", "Quốc tế"], required: true, fromCell: 3 }
      ]
    },
    flight: {
      title: "chuyến bay",
      fields: [
        { name: "number", label: "Số hiệu chuyến bay", required: true, fromCell: 0 },
        { name: "route", label: "Tuyến bay", required: true, fromCell: 1 },
        { name: "departure", label: "Thời gian khởi hành", type: "datetime-local", required: true, fromCell: 2, read: (row) => dateTimeValue(row.cells[2]) },
        { name: "arrival", label: "Thời gian đến", type: "datetime-local", required: true, fromCell: 3, read: (row) => dateTimeValue(row.cells[3]) },
        { name: "aircraft", label: "Máy bay", required: true, fromCell: 4 },
        { name: "status", label: "Trạng thái", type: "select", options: ["Theo lịch", "Trễ chuyến", "Đã hủy", "Đã hoàn tất"], required: true, fromCell: 5 }
      ]
    },
    pricing: {
      title: "mức giá",
      fields: [
        { name: "flight", label: "Chuyến bay", required: true, value: "TEO 218 · SGN – HAN" },
        { name: "class", label: "Hạng ghế", type: "select", options: ["Phổ thông", "Thương gia"], required: true, fromCell: 0 },
        { name: "passenger", label: "Loại khách", type: "select", options: ["Người lớn", "Trẻ em", "Em bé"], required: true, fromCell: 1 },
        { name: "basePrice", label: "Giá cơ bản (₫)", type: "number", required: true, min: 0, fromCell: 2 },
        { name: "tax", label: "Thuế (₫)", type: "number", required: true, min: 0, fromCell: 3 },
        { name: "serviceFee", label: "Phí dịch vụ (₫)", type: "number", required: true, min: 0, fromCell: 4 }
      ]
    },
    promotion: {
      title: "mã khuyến mãi",
      fields: [
        { name: "code", label: "Mã khuyến mãi", required: true, fromCell: 0, read: (row) => row.querySelector(".admin-code")?.textContent.trim() },
        { name: "discountPercent", label: "Giảm theo phần trăm (%)", type: "number", min: 0, max: 100, value: "10", read: (row) => row.cells[1]?.textContent.match(/(\d+(?:,\d+)?)%/)?.[1].replace(",", ".") ?? "0" },
        { name: "discountAmount", label: "Giảm theo số tiền (₫)", type: "number", min: 0, value: "0", read: (row) => row.cells[1]?.textContent.includes("%") ? "0" : parseMoney(row.cells[1]?.textContent) },
        { name: "minimum", label: "Giá trị đơn tối thiểu (₫)", type: "number", min: 0, value: "0", read: (row) => parseMoney(row.cells[2]?.textContent) },
        { name: "points", label: "Điểm cần để đổi", type: "number", min: 0, value: "0", read: (row) => row.querySelector(".admin-code")?.textContent === "VIPREDEEM100" ? "100" : "0" },
        { name: "startDate", label: "Ngày bắt đầu", type: "date", required: true, value: "2026-10-01", read: (row) => dateValue(row.cells[3]?.textContent.split("–")[0]) },
        { name: "endDate", label: "Ngày kết thúc", type: "date", required: true, value: "2026-12-31", read: (row) => dateValue(row.cells[3]?.textContent.split("–")[1]) },
        { name: "usageLimit", label: "Giới hạn lượt dùng", type: "number", min: 1, required: true, value: "200", read: (row) => row.cells[4]?.textContent.split("/")[1].trim() }
      ]
    }
  };
  const promoCodes = ["TEOAUTUMN26", "FREESHIP50K", "VIPREDEEM100", "WELCOME25"];
  const promoDescriptions = ["Giảm 10%", "Giảm 50.000 ₫", "Đổi 100 điểm · 15%", "Giảm 25%"];
  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  let toastTimer;
  let activePeriod = "month";
  let activeChartMode = "revenue";
  let pendingDeleteName = "";

  function renderIcons() {
    if (window.lucide && typeof window.lucide.createIcons === "function") {
      window.lucide.createIcons();
    }
  }

  function showToast(message) {
    const toast = $(".admin-toast");
    toast.textContent = message;
    toast.hidden = false;
    window.clearTimeout(toastTimer);
    toastTimer = window.setTimeout(() => {
      toast.hidden = true;
    }, 3400);
  }

  function setMenuOpen(isOpen) {
    document.body.dataset.menu = isOpen ? "open" : "closed";
    $("[data-menu-toggle]").setAttribute("aria-expanded", String(isOpen));
    $("[data-menu-close]").hidden = !isOpen;
    const sidebar = $("#admin-sidebar");
    const wasFocused = sidebar.contains(document.activeElement);
    const isMobile = window.matchMedia("(max-width: 980px)").matches;
    sidebar.inert = isMobile && !isOpen;
    if (!isOpen && isMobile && wasFocused) $("[data-menu-toggle]").focus();
  }

  function setScreen(screen) {
    const activeScreen = Object.hasOwn(screens, screen) ? screen : "overview";
    $$(".admin-screen").forEach((panel) => {
      panel.hidden = panel.dataset.screenPanel !== activeScreen;
    });
    $$("[data-admin-screen]").forEach((link) => {
      if (link.dataset.adminScreen === activeScreen) {
        link.setAttribute("aria-current", "page");
      } else {
        link.removeAttribute("aria-current");
      }
    });
    $("[data-page-title]").textContent = screens[activeScreen];
    document.title = `${screens[activeScreen]} — TEO Airlines`;
    setMenuOpen(false);
  }

  function formatMetric(value) {
    return Number(value).toLocaleString("vi-VN");
  }

  function chartValue(value, mode) {
    if (mode === "tickets") return formatMetric(value);
    if (value >= 1000) return `${(value / 1000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tỷ`;
    return `${value.toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tr`;
  }

  function renderChart() {
    const data = periodData[activePeriod];
    const values = activeChartMode === "revenue" ? data.revenueBars : data.ticketBars;
    const maxValue = Math.max(...values);
    const chart = $("[data-chart]");
    chart.replaceChildren();
    chart.setAttribute("aria-label", `Biểu đồ cột ${activeChartMode === "revenue" ? "doanh thu" : "số vé"}`);
    values.forEach((value, index) => {
      const column = document.createElement("div");
      column.className = "admin-chart-column";
      const valueLabel = document.createElement("span");
      valueLabel.className = "admin-chart-value";
      valueLabel.textContent = chartValue(value, activeChartMode);
      const area = document.createElement("div");
      area.className = "admin-chart-bar-area";
      const bar = document.createElement("span");
      bar.className = "admin-chart-bar";
      bar.style.height = `${Math.max(5, (value / maxValue) * 100)}%`;
      bar.setAttribute("aria-hidden", "true");
      area.append(bar);
      const label = document.createElement("span");
      label.className = "admin-chart-label";
      label.textContent = data.labels[index];
      column.append(valueLabel, area, label);
      chart.append(column);
    });
    $("[data-chart-caption]").textContent = activeChartMode === "revenue"
      ? `Doanh thu thuần theo ${activePeriod === "day" ? "giờ" : activePeriod === "week" ? "ngày trong tuần" : activePeriod === "month" ? "ngày trong tháng" : "kỳ"}`
      : `Số vé đã đặt theo ${activePeriod === "day" ? "giờ" : activePeriod === "week" ? "ngày trong tuần" : activePeriod === "month" ? "ngày trong tháng" : "kỳ"}`;
  }

  function renderPromotions() {
    const counts = periodData[activePeriod].promo;
    const list = $("[data-promo-summary]");
    list.replaceChildren();
    promoCodes.forEach((code, index) => {
      const row = document.createElement("div");
      row.className = "admin-promo-row";
      const details = document.createElement("div");
      const name = document.createElement("strong");
      name.textContent = code;
      const description = document.createElement("span");
      description.textContent = promoDescriptions[index];
      details.append(name, description);
      const count = document.createElement("b");
      count.textContent = `${formatMetric(counts[index])} lượt`;
      row.append(details, count);
      list.append(row);
    });
    $("[data-promo-total]").textContent = formatMetric(counts.reduce((sum, count) => sum + count, 0));
  }

  function updateDashboard(period) {
    activePeriod = Object.hasOwn(periodData, period) ? period : "month";
    const data = periodData[activePeriod];
    $$("[data-period]").forEach((button) => {
      button.setAttribute("aria-pressed", String(button.dataset.period === activePeriod));
    });
    $("[data-period-caption]").textContent = data.caption;
    $("[data-metric='revenue']").textContent = data.revenue;
    $("[data-metric='bookings']").textContent = data.bookings;
    $("[data-metric='occupancy']").textContent = data.occupancy;
    $("[data-metric='seat-count']").textContent = data.seats;
    $("[data-metric='sold']").textContent = data.sold;
    $("[data-metric='cancelled']").textContent = data.cancelled;
    $("[data-metric='changed']").textContent = data.changed;
    $("[data-booking-total]").textContent = `${data.bookings} lượt`;
    $("[data-ticket-caption]").textContent = `Trong kỳ đã chọn`;
    $("[data-breakdown='sold']").textContent = data.sold;
    $("[data-breakdown='cancelled']").textContent = data.cancelled;
    $("[data-breakdown='changed']").textContent = data.changed;
    renderChart();
    renderPromotions();
  }

  function createSeatMap(aircraft, rowCount, seatCount) {
    const map = $("[data-seat-map]");
    map.replaceChildren();
    map.setAttribute("aria-label", `Sơ đồ ghế ${aircraft}`);
    for (let row = 1; row <= rowCount; row += 1) {
      const rowNumber = String(row).padStart(2, "0");
      const label = document.createElement("span");
      label.className = "admin-seat-row-label";
      label.textContent = rowNumber;
      map.append(label);
      ["A", "B", "C"].forEach((letter) => {
        const seat = document.createElement("span");
        const unavailable = row === 2 && letter === "C";
        seat.className = `admin-seat${row <= 2 ? " admin-seat-business" : ""}${unavailable ? " admin-seat-unavailable" : ""}`;
        seat.textContent = `${rowNumber}${letter}`;
        seat.setAttribute("aria-label", `Ghế ${rowNumber}${letter}${unavailable ? ", không khai thác" : row <= 2 ? ", thương gia" : ", phổ thông"}`);
        map.append(seat);
      });
      const aisle = document.createElement("span");
      aisle.className = "admin-seat-aisle";
      aisle.setAttribute("aria-hidden", "true");
      map.append(aisle);
      ["D", "E", "F"].forEach((letter) => {
        const seat = document.createElement("span");
        seat.className = `admin-seat${row <= 2 ? " admin-seat-business" : ""}`;
        seat.textContent = `${rowNumber}${letter}`;
        seat.setAttribute("aria-label", `Ghế ${rowNumber}${letter}${row <= 2 ? ", thương gia" : ", phổ thông"}`);
        map.append(seat);
      });
    }
  }

  function setAircraft(button) {
    $$("[data-aircraft]").forEach((option) => {
      option.setAttribute("aria-pressed", String(option === button));
    });
    const aircraft = button.dataset.aircraft;
    const counts = {
      "Airbus A321": { rows: 30, seats: 180 },
      "Airbus A320": { rows: 25, seats: 150 },
      "Boeing 787-9": { rows: 49, seats: 294 }
    };
    const selected = counts[aircraft];
    $("[data-aircraft-title]").textContent = aircraft;
    $("[data-aircraft-seats]").textContent = formatMetric(selected.seats);
    createSeatMap(aircraft, selected.rows, selected.seats);
  }

  function cellValue(row, field) {
    if (typeof field.read === "function") return field.read(row) ?? "";
    if (field.fromCell === undefined) return field.value ?? "";
    const cell = row.cells[field.fromCell];
    if (!cell) return "";
    let value = cell.textContent.trim();
    if (field.type === "number") value = value.replace(/[^\d,.-]/g, "").replace(/\./g, "").replace(",", ".");
    if (field.type === "select" && field.name === "type") {
      value = value.includes("Quốc tế") ? "Quốc tế" : "Nội địa";
    }
    return value;
  }

  function parseMoney(value = "") {
    return value.replace(/[^\d]/g, "");
  }

  function dateValue(value = "") {
    const match = value.trim().match(/^(\d{2})\/(\d{2})\/(\d{4})$/);
    return match ? `${match[3]}-${match[2]}-${match[1]}` : "";
  }

  function dateTimeValue(cell) {
    if (!cell) return "";
    const parts = cell.textContent.trim().split(/\s+/);
    const date = dateValue(parts[0] ?? "");
    const time = parts[1] ?? "";
    return date && /^\d{2}:\d{2}$/.test(time) ? `${date}T${time}` : "";
  }

  function openRecordDialog(type, mode, row = null) {
    const config = formConfigs[type];
    if (!config) return;
    const dialog = $("#record-dialog");
    const fields = $("[data-dialog-fields]");
    fields.replaceChildren();
    $("#record-dialog-title").textContent = `${mode === "edit" ? "Sửa" : "Thêm"} ${config.title}`;
    $("[data-dialog-description]").textContent = "Giao diện minh họa — thông tin chưa được lưu vào cơ sở dữ liệu.";
    $("[data-record-form]").dataset.recordType = type;
    config.fields.forEach((field, index) => {
      const wrapper = document.createElement("div");
      wrapper.className = `admin-field${config.fields.length === 1 || index === config.fields.length - 1 && config.fields.length % 2 === 1 ? " admin-field-full" : ""}`;
      const label = document.createElement("label");
      const input = document.createElement(field.type === "select" ? "select" : "input");
      const id = `record-${type}-${field.name}`;
      label.htmlFor = id;
      label.textContent = field.label;
      input.id = id;
      input.name = field.name;
      input.required = Boolean(field.required);
      if (field.type === "select") {
        field.options.forEach((optionText) => {
          const option = document.createElement("option");
          option.value = optionText;
          option.textContent = optionText;
          input.append(option);
        });
      } else {
        input.type = field.type ?? "text";
        if (field.min !== undefined) input.min = String(field.min);
        if (field.max !== undefined) input.max = String(field.max);
      }
      const value = mode === "edit" && row ? cellValue(row, field) : field.value ?? "";
      if (value) input.value = value;
      input.addEventListener("input", () => input.setCustomValidity(""));
      wrapper.append(label, input);
      fields.append(wrapper);
    });
    dialog.showModal();
    const firstField = $("input, select", fields);
    if (firstField) firstField.focus();
  }

  function csvCell(value) {
    return `"${String(value).replaceAll('"', '""')}"`;
  }

  function downloadCsv(name, rows) {
    const content = `\uFEFF${rows.map((row) => row.map(csvCell).join(",")).join("\r\n")}`;
    const blob = new Blob([content], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = name;
    document.body.append(link);
    link.click();
    link.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    showToast("Đã tải CSV UTF-8 tương thích Excel.");
  }

  function exportPromotions() {
    const table = $("#promotions-table");
    const rows = $$("tr", table).map((row) => $$("th, td", row).filter((cell) => !cell.matches('[data-label="Thao tác"]')).map((cell) => cell.innerText.replace(/\s+/g, " ").trim()));
    downloadCsv("teo-airlines-khuyen-mai.csv", rows);
  }

  function exportReport() {
    const data = periodData[activePeriod];
    const rows = [
      ["Chỉ số", "Giá trị", "Kỳ báo cáo"],
      ["Doanh thu", data.revenue, data.caption],
      ["Vé đã đặt", data.bookings, data.caption],
      ["Tỉ lệ lấp đầy ghế", data.occupancy, data.caption],
      ["Vé bán", data.sold, data.caption],
      ["Vé hủy", data.cancelled, data.caption],
      ["Vé đổi", data.changed, data.caption],
      ["Lượt dùng khuyến mãi", formatMetric(data.promo.reduce((sum, count) => sum + count, 0)), data.caption]
    ];
    downloadCsv("teo-airlines-bao-cao.csv", rows);
  }

  function setTableFilters(table) {
    const search = $(`[data-table-filter="${table.id}"]`);
    const select = $(`[data-table-select="${table.id}"]`);
    const query = search?.value.trim().toLocaleLowerCase("vi");
    const selected = select?.value.toLocaleLowerCase("vi");
    $$("tbody tr", table).forEach((row) => {
      const matchesSearch = !query || row.textContent.toLocaleLowerCase("vi").includes(query);
      const matchesSelect = !selected || row.textContent.toLocaleLowerCase("vi").includes(selected);
      row.hidden = !(matchesSearch && matchesSelect);
    });
  }

  $$("[data-admin-screen]").forEach((link) => {
    link.addEventListener("click", (event) => {
      event.preventDefault();
      const screen = link.dataset.adminScreen;
      if (window.location.hash !== `#${screen}`) window.location.hash = screen;
      setScreen(screen);
    });
  });

  window.addEventListener("hashchange", () => {
    setScreen(window.location.hash.slice(1));
  });

  $("[data-menu-toggle]").addEventListener("click", () => {
    setMenuOpen(document.body.dataset.menu !== "open");
  });
  $("[data-menu-close]").addEventListener("click", () => setMenuOpen(false));
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") setMenuOpen(false);
  });
  window.addEventListener("resize", () => {
    setMenuOpen(document.body.dataset.menu === "open");
  });

  $$("[data-period]").forEach((button) => {
    button.addEventListener("click", () => updateDashboard(button.dataset.period));
  });
  $$("[data-chart-mode]").forEach((button) => {
    button.addEventListener("click", () => {
      activeChartMode = button.dataset.chartMode;
      $$("[data-chart-mode]").forEach((modeButton) => {
        modeButton.setAttribute("aria-pressed", String(modeButton === button));
      });
      renderChart();
    });
  });

  $$("[data-aircraft]").forEach((button) => {
    button.addEventListener("click", () => setAircraft(button));
  });

  $$("[data-table-filter]").forEach((input) => {
    input.addEventListener("input", () => {
      const table = document.getElementById(input.dataset.tableFilter);
      if (table) setTableFilters(table);
    });
  });
  $$("[data-table-select]").forEach((select) => {
    select.addEventListener("change", () => {
      const table = document.getElementById(select.dataset.tableSelect);
      if (table) setTableFilters(table);
    });
  });

  $$("[data-record-open]").forEach((button) => {
    button.addEventListener("click", () => openRecordDialog(button.dataset.recordOpen, "add"));
  });
  $$("[data-record-edit]").forEach((button) => {
    button.addEventListener("click", () => openRecordDialog(button.dataset.recordEdit, "edit", button.closest("tr")));
  });
  $$("[data-record-delete]").forEach((button) => {
    button.addEventListener("click", () => {
      pendingDeleteName = button.dataset.recordDelete;
      $("[data-confirm-copy]").textContent = `Bạn có chắc muốn xóa ${pendingDeleteName}? Bản ghi liên quan có thể ảnh hưởng đến lịch sử khai thác.`;
      $("#confirm-dialog").showModal();
    });
  });

  $$(".admin-dialog [data-dialog-close]").forEach((button) => {
    button.addEventListener("click", () => button.closest("dialog").close());
  });
  $("[data-record-form]").addEventListener("submit", (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    form.querySelectorAll("input, select").forEach((field) => field.setCustomValidity(""));
    if (!form.reportValidity()) return;
    if (form.dataset.recordType === "promotion") {
      const percent = form.elements.namedItem("discountPercent");
      const amount = form.elements.namedItem("discountAmount");
      if (!percent.value && !amount.value) {
        percent.setCustomValidity("Nhập mức giảm theo phần trăm hoặc số tiền.");
        percent.reportValidity();
        percent.focus();
        return;
      }
    }
    $("#record-dialog").close();
    showToast("Giao diện minh họa — thay đổi chưa được lưu vào cơ sở dữ liệu.");
  });
  $("#confirm-dialog").addEventListener("close", (event) => {
    if (event.currentTarget.returnValue === "confirm") {
      showToast(`Giao diện minh họa — không xóa ${pendingDeleteName}.`);
    }
    pendingDeleteName = "";
  });

  $$("[data-export]").forEach((button) => {
    button.addEventListener("click", () => {
      if (button.dataset.export === "promotions") exportPromotions();
      else exportReport();
    });
  });

  $$(".admin-pagination button:not(:disabled)").forEach((button) => {
    button.addEventListener("click", () => showToast("Giao diện minh họa — phân trang chưa kết nối dữ liệu."));
  });

  setScreen(window.location.hash.slice(1) || "overview");
  updateDashboard("month");
  setAircraft($("[data-aircraft][aria-pressed='true']"));
  renderIcons();
})();
