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
  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  let toastTimer;
  let activeChartMode = "revenue";
  let airportPage = 1;
  const airportPageSize = 5;

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

  function chartValue(value, mode) {
    if (mode === "tickets") return Number(value).toLocaleString("vi-VN");
    if (value >= 1_000_000_000) {
      return `${(value / 1_000_000_000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tỷ`;
    }
    if (value >= 1_000_000) {
      return `${(value / 1_000_000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tr`;
    }
    return `${Number(value).toLocaleString("vi-VN")} ₫`;
  }

  function renderChart() {
    const chart = $("[data-chart]");
    const points = JSON.parse(chart.dataset.chartPoints);
    const values = points.map((point) =>
      activeChartMode === "revenue" ? point.Revenue : point.SoldTicketCount
    );
    const maxValue = Math.max(...values, 0);
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
      bar.style.height = maxValue > 0 ? `${Math.max(5, (value / maxValue) * 100)}%` : "0";
      bar.setAttribute("aria-hidden", "true");
      area.append(bar);
      const label = document.createElement("span");
      label.className = "admin-chart-label";
      label.textContent = points[index].Label;
      column.append(valueLabel, area, label);
      chart.append(column);
    });
    const period = $("[data-period][aria-pressed='true']").dataset.period;
    const periodUnit = {
      day: "giờ trong ngày",
      week: "ngày trong tuần",
      month: "ngày trong tháng",
      quarter: "tháng trong quý",
      year: "tháng trong năm",
      all: "giai đoạn lịch sử"
    }[period];
    $("[data-chart-caption]").textContent = activeChartMode === "revenue"
      ? `Doanh thu thanh toán thành công theo ${periodUnit}`
      : `Vé đã bán theo ${periodUnit}`;
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

  function configureAircraftDialog(mode, preserveModel = false, model = "") {
    const dialog = $("#aircraft-dialog");
    const form = $("#aircraft-form");
    const isCreate = mode !== "edit";
    dialog.dataset.mode = isCreate ? "create" : "edit";
    form.action = isCreate ? form.dataset.createUrl : form.dataset.editUrl;
    $("#aircraft-dialog-title").textContent = isCreate ? "Thêm máy bay" : "Sửa tên model";
    $("[data-aircraft-dialog-description]").textContent = isCreate
      ? "Máy bay và các ghế sẽ được lưu cùng nhau."
      : "Chỉ thay đổi tên model; sơ đồ ghế giữ nguyên.";
    $("[data-aircraft-submit]").textContent = isCreate ? "Thêm máy bay" : "Lưu tên model";
    $$(".aircraft-create-only").forEach((field) => {
      field.hidden = !isCreate;
      field.querySelectorAll("input, select").forEach((input) => {
        input.disabled = !isCreate;
      });
    });
    if (!preserveModel) $("#AircraftInput_Model").value = isCreate ? "" : model;
  }

  function configurePromotionDialog(mode, button = null, preserveInput = false) {
    const dialog = $("#promotion-dialog");
    const form = $("[data-promotion-form]");
    const isEdit = mode === "edit";
    form.action = isEdit ? form.dataset.editUrl : form.dataset.createUrl;
    $("#promotion-dialog-title").textContent = isEdit
      ? "Sửa mã khuyến mãi"
      : "Thêm mã khuyến mãi";
    $("[data-promotion-id]").value = isEdit
      ? button?.dataset.promoId ?? dialog.dataset.promoId ?? ""
      : "";

    if (button) {
      $("#PromotionInput_Code").value = button.dataset.code ?? "";
      $("#PromotionInput_DiscountPercent").value = button.dataset.discountPercent ?? "";
      $("#PromotionInput_DiscountAmount").value = button.dataset.discountAmount ?? "";
      $("#PromotionInput_MinOrderValue").value = button.dataset.minOrderValue ?? "";
      $("#PromotionInput_PointsRequired").value = button.dataset.pointsRequired ?? "";
      $("#PromotionInput_StartDate").value = button.dataset.startDate ?? "";
      $("#PromotionInput_EndDate").value = button.dataset.endDate ?? "";
      $("#PromotionInput_UsageLimit").value = button.dataset.usageLimit ?? "";
    } else if (!preserveInput) {
      form.reset();
      if (!isEdit) {
        $("#PromotionInput_Code").value = "";
        $("#PromotionInput_DiscountPercent").value = "";
        $("#PromotionInput_DiscountAmount").value = "";
        $("#PromotionInput_MinOrderValue").value = "";
        $("#PromotionInput_PointsRequired").value = "";
        $("#PromotionInput_StartDate").value = "";
        $("#PromotionInput_EndDate").value = "";
        $("#PromotionInput_UsageLimit").value = "1";
      }
    }

    dialog.dataset.mode = mode;
    dialog.showModal();
    $("#PromotionInput_Code").focus();
  }

  function openAircraftDialog(mode, model = "") {
    configureAircraftDialog(mode, false, model);
    $("#aircraft-dialog").showModal();
    $("#AircraftInput_Model").focus();
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

  function exportReport() {
    const caption = $("[data-period-caption]").textContent.trim();
    const metric = (name) => $(`[data-metric="${name}"]`).textContent.trim();
    const rows = [
      ["Chỉ số", "Giá trị", "Kỳ báo cáo"],
      ["Doanh thu", metric("revenue"), caption],
      ["Vé đã đặt", metric("bookings"), caption],
      ["Tỉ lệ lấp đầy ghế", metric("occupancy"), caption],
      ["Vé bán", metric("sold"), caption],
      ["Vé hủy", metric("cancelled"), caption],
      ["Yêu cầu đổi vé đã duyệt", metric("changed"), caption],
      ["Lượt truy cập website", metric("website-visits"), caption],
      ["Lượt dùng khuyến mãi", $(".admin-promo-summary strong").textContent.trim(), "Toàn bộ lịch sử"]
    ];
    downloadCsv("teo-airlines-bao-cao.csv", rows);
  }

  function setTableFilters(table) {
    if (table.id === "airports-table") {
      airportPage = 1;
      renderAirportTable();
      return;
    }

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

  function renderAirportTable() {
    const table = $("#airports-table");
    const search = $('[data-table-filter="airports-table"]');
    const rows = $$("tbody tr[data-airport-row]", table);
    const query = search.value.trim().toLocaleLowerCase("vi");
    const matches = rows.filter((row) => row.textContent.toLocaleLowerCase("vi").includes(query));
    const pageCount = Math.max(1, Math.ceil(matches.length / airportPageSize));
    airportPage = Math.min(airportPage, pageCount);

    rows.forEach((row) => {
      const matchIndex = matches.indexOf(row);
      row.hidden = matchIndex < 0 ||
        Math.floor(matchIndex / airportPageSize) + 1 !== airportPage;
    });

    const emptyRow = $("[data-airports-empty]", table);
    emptyRow.hidden = matches.length !== 0;
    $("[data-airports-count]").textContent = `${matches.length} sân bay`;
    $("[data-airports-summary]").textContent = matches.length === 0
      ? "Không có sân bay phù hợp"
      : `Hiển thị ${(airportPage - 1) * airportPageSize + 1}–${Math.min(airportPage * airportPageSize, matches.length)} trên ${matches.length} sân bay`;

    const pagination = $("[data-airports-pagination]");
    pagination.replaceChildren();
    if (matches.length <= airportPageSize) return;

    const addPageButton = (label, page, ariaLabel, current = false) => {
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = label;
      button.setAttribute("aria-label", ariaLabel);
      if (current) button.setAttribute("aria-current", "page");
      button.addEventListener("click", () => {
        airportPage = page;
        renderAirportTable();
      });
      pagination.append(button);
    };

    addPageButton("‹", Math.max(1, airportPage - 1), "Trang trước");
    for (let page = 1; page <= pageCount; page += 1) {
      addPageButton(String(page), page, `Trang ${page}`, page === airportPage);
    }
    addPageButton("›", Math.min(pageCount, airportPage + 1), "Trang sau");
  }

  function openAirportDialog(mode, button = null) {
    const dialog = $("#airport-dialog");
    const form = $("[data-airport-form]");
    const code = $("[data-airport-code-field]");
    form.reset();
    form.action = mode === "edit" ? form.dataset.editUrl : form.dataset.createUrl;
    $("#airport-dialog-title").textContent = mode === "edit" ? "Sửa sân bay" : "Thêm sân bay";
    code.readOnly = mode === "edit";
    $("[data-airport-original-code]").value = mode === "edit" ? button.dataset.code : "";

    if (mode === "edit") {
      code.value = button.dataset.code;
      $("#AirportInput_Name").value = button.dataset.name;
      $("#AirportInput_City").value = button.dataset.city;
      $("#AirportInput_Country").value = button.dataset.country;
    }

    dialog.showModal();
    code.focus();
  }

  function openRouteDialog(mode, button = null) {
    const dialog = $("#route-dialog");
    const form = $("[data-route-form]");
    const departure = $("#RouteInput_DepartureAirportCode");
    const arrival = $("#RouteInput_ArrivalAirportCode");
    const type = $("#RouteInput_RouteType");
    form.reset();
    form.action = mode === "edit" ? form.dataset.editUrl : form.dataset.createUrl;
    $("#route-dialog-title").textContent = mode === "edit" ? "Sửa tuyến bay" : "Thêm tuyến bay";
    $("[data-route-id]").value = mode === "edit" ? button.dataset.id : "";

    if (mode === "edit") {
      departure.value = button.dataset.departure;
      arrival.value = button.dataset.arrival;
      type.value = button.dataset.type;
    } else {
      type.value = "Domestic";
    }

    dialog.showModal();
    departure.focus();
  }

  $$("[data-route-create]").forEach((button) => {
    button.addEventListener("click", () => openRouteDialog("create"));
  });
  $$("[data-route-edit]").forEach((button) => {
    button.addEventListener("click", () => openRouteDialog("edit", button));
  });
  $$("[data-route-delete]").forEach((button) => {
    button.addEventListener("click", () => {
      $("[data-route-delete-id]").value = button.dataset.id;
      $("[data-route-delete-name]").textContent = button.dataset.name;
      $("#route-delete-dialog").showModal();
    });
  });
  $$("[data-route-dialog-close]").forEach((button) => {
    button.addEventListener("click", () => $("#route-dialog").close());
  });
  $("[data-route-delete-cancel]").addEventListener("click", () => {
    $("#route-delete-dialog").close();
  });

  let routeSearchTimer;
  const routeSearchForm = $('[data-route-search]').form;
  $('[data-route-search]').addEventListener("input", () => {
    window.clearTimeout(routeSearchTimer);
    routeSearchTimer = window.setTimeout(() => routeSearchForm.requestSubmit(), 350);
  });
  $('[data-route-type-filter]').addEventListener("change", () => routeSearchForm.requestSubmit());

  if ($("#route-dialog").dataset.open === "true") {
    const dialog = $("#route-dialog");
    const form = $("[data-route-form]");
    const mode = dialog.dataset.mode;
    form.action = mode === "edit" ? form.dataset.editUrl : form.dataset.createUrl;
    dialog.showModal();
  }

  $$("[data-airport-create]").forEach((button) => {
    button.addEventListener("click", () => openAirportDialog("create"));
  });
  $$("[data-airport-edit]").forEach((button) => {
    button.addEventListener("click", () => openAirportDialog("edit", button));
  });
  $$("[data-airport-delete]").forEach((button) => {
    button.addEventListener("click", () => {
      $("[data-airport-delete-code]").value = button.dataset.code;
      $("[data-airport-delete-name]").textContent = button.dataset.code;
      const confirmation = $("[data-airport-delete-confirmation]");
      confirmation.value = "";
      $("[data-airport-delete-submit]").disabled = true;
      $("#airport-delete-dialog").showModal();
      confirmation.focus();
    });
  });
  $$("[data-airport-dialog-close]").forEach((button) => {
    button.addEventListener("click", () => $("#airport-dialog").close());
  });
  $("[data-airport-delete-cancel]").addEventListener("click", () => {
    $("#airport-delete-dialog").close();
  });
  $("[data-airport-delete-confirmation]").addEventListener("input", (event) => {
    const expectedCode = $("[data-airport-delete-code]").value;
    $("[data-airport-delete-submit]").disabled = event.currentTarget.value !== expectedCode;
  });
  $("[data-airport-delete-form]").addEventListener("submit", (event) => {
    const confirmation = $("[data-airport-delete-confirmation]");
    if (confirmation.value !== $("[data-airport-delete-code]").value) {
      event.preventDefault();
      confirmation.setCustomValidity("Nhập chính xác mã sân bay để xác nhận.");
      confirmation.reportValidity();
      confirmation.focus();
      return;
    }

    confirmation.setCustomValidity("");
  });
  $("#airport-delete-dialog").addEventListener("close", () => {
    $("[data-airport-delete-confirmation]").value = "";
    $("[data-airport-delete-submit]").disabled = true;
  });

  if ($("#airport-dialog").dataset.open === "true") {
    const dialog = $("#airport-dialog");
    const form = $("[data-airport-form]");
    const mode = dialog.dataset.mode;
    form.action = mode === "edit" ? form.dataset.editUrl : form.dataset.createUrl;
    const code = $("[data-airport-code-field]");
    code.readOnly = mode === "edit";
    if (mode === "edit") {
      $("[data-airport-original-code]").value = code.value;
    }
    dialog.showModal();
  }

  function openFlightDialog(mode, button = null) {
    const dialog = $("#flight-dialog");
    const form = $("[data-flight-form]");
    const flightNumber = $("#FlightInput_FlightNumber");
    form.reset();
    form.action = mode === "edit" ? form.dataset.editUrl : form.dataset.createUrl;
    $("#flight-dialog-title").textContent = mode === "edit" ? "Sửa chuyến bay" : "Thêm chuyến bay";
    $("[data-flight-id]").value = mode === "edit" ? button.dataset.id : "0";

    if (mode === "edit") {
      flightNumber.value = button.dataset.number;
      $("#FlightInput_RouteId").value = button.dataset.routeId;
      $("#FlightInput_DepartureTime").value = button.dataset.departure;
      $("#FlightInput_ArrivalTime").value = button.dataset.arrival;
      $("#FlightInput_AircraftId").value = button.dataset.aircraftId;
      $("#FlightInput_Status").value = button.dataset.status;
    }

    dialog.showModal();
    flightNumber.focus();
  }

  $$("[data-flight-create]").forEach((button) => {
    button.addEventListener("click", () => openFlightDialog("create"));
  });
  $$("[data-flight-edit]").forEach((button) => {
    button.addEventListener("click", () => openFlightDialog("edit", button));
  });
  $$("[data-flight-dialog-close]").forEach((button) => {
    button.addEventListener("click", () => $("#flight-dialog").close());
  });
  $$("[data-flight-delete]").forEach((button) => {
    button.addEventListener("click", () => {
      $("[data-flight-delete-id]").value = button.dataset.id;
      $("[data-flight-delete-hidden-number]").value = button.dataset.number;
      $("[data-flight-delete-number]").textContent = button.dataset.number;
      $("[data-flight-delete-route]").textContent = button.dataset.route;
      $("[data-flight-delete-departure]").textContent = button.dataset.departure;
      const confirmation = $("[data-flight-delete-confirmation]");
      confirmation.value = "";
      confirmation.setCustomValidity("");
      $("[data-flight-delete-submit]").disabled = true;
      $("#flight-delete-dialog").showModal();
      confirmation.focus();
    });
  });
  $("[data-flight-delete-cancel]").addEventListener("click", () => {
    $("#flight-delete-dialog").close();
  });
  $("[data-flight-delete-confirmation]").addEventListener("input", (event) => {
    const expectedNumber = $("[data-flight-delete-hidden-number]").value;
    $("[data-flight-delete-submit]").disabled = event.currentTarget.value !== expectedNumber;
    event.currentTarget.setCustomValidity("");
  });
  $("[data-flight-delete-form]").addEventListener("submit", (event) => {
    const confirmation = $("[data-flight-delete-confirmation]");
    if (confirmation.value !== $("[data-flight-delete-hidden-number]").value) {
      event.preventDefault();
      confirmation.setCustomValidity("Nhập chính xác số hiệu chuyến bay để xác nhận.");
      confirmation.reportValidity();
      confirmation.focus();
      return;
    }

    confirmation.setCustomValidity("");
  });
  $("#flight-delete-dialog").addEventListener("close", () => {
    $("[data-flight-delete-confirmation]").value = "";
    $("[data-flight-delete-submit]").disabled = true;
  });

  let flightSearchTimer;
  const flightSearchInput = $("[data-flight-search-input]");
  const flightSearchForm = flightSearchInput.form;
  flightSearchInput.addEventListener("input", () => {
    window.clearTimeout(flightSearchTimer);
    flightSearchTimer = window.setTimeout(() => flightSearchForm.requestSubmit(), 350);
  });
  $("[data-flight-status-filter]").addEventListener("change", () => flightSearchForm.requestSubmit());

  renderAirportTable();

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
    button.addEventListener("click", () => {
      const url = new URL(window.location.href);
      url.searchParams.set("dashboardPeriod", button.dataset.period);
      url.hash = "overview";
      window.location.assign(url.href);
    });
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

  $$("[data-aircraft-open]").forEach((button) => {
    button.addEventListener("click", () => {
      openAircraftDialog(button.dataset.aircraftOpen, button.dataset.aircraftModel ?? "");
    });
  });
  $$("[data-aircraft-dialog-close]").forEach((button) => {
    button.addEventListener("click", () => $("#aircraft-dialog").close());
  });
  $("#aircraft-dialog").addEventListener("click", (event) => {
    if (event.target === event.currentTarget) event.currentTarget.close();
  });

  const pricingCreateDialog = $("#pricing-create-dialog");
  const pricingEditDialog = $("#pricing-edit-dialog");
  const promotionDialog = $("#promotion-dialog");
  $$("[data-promotion-create]").forEach((button) => {
    button.addEventListener("click", () => configurePromotionDialog("create"));
  });
  $$("[data-promotion-edit]").forEach((button) => {
    button.addEventListener("click", () => configurePromotionDialog("edit", button));
  });
  $$("[data-promotion-dialog-close]").forEach((button) => {
    button.addEventListener("click", () => promotionDialog.close());
  });
  promotionDialog.addEventListener("click", (event) => {
    if (event.target === event.currentTarget) promotionDialog.close();
  });
  $("[data-promotion-status]").addEventListener("change", (event) => {
    event.currentTarget.form.requestSubmit();
  });

  $$("[data-pricing-create]").forEach((button) => {
    button.addEventListener("click", () => {
      if (!button.disabled) pricingCreateDialog.showModal();
    });
  });
  $$("[data-pricing-dialog-close]").forEach((button) => {
    button.addEventListener("click", () => button.closest("dialog").close());
  });
  [pricingCreateDialog, pricingEditDialog].forEach((dialog) => {
    dialog.addEventListener("click", (event) => {
      if (event.target === event.currentTarget) dialog.close();
    });
  });
  $$("[data-pricing-edit]").forEach((button) => {
    button.addEventListener("click", () => {
      $("#pricing-edit-dialog [name='priceId']").value = button.dataset.priceId;
      $("#pricing-edit-dialog [name='pricingFlightId']").value = $("#pricing-flight-select").value;
      $("#pricing-edit-title").textContent = "Sửa mức giá";
      $("[data-pricing-edit-description]").textContent =
        `${button.dataset.seatClass} · ${button.dataset.passengerType}`;
      $("#FlightPricingEditInput_BasePrice").value = button.dataset.basePrice;
      $("#FlightPricingEditInput_TaxAmount").value = button.dataset.taxAmount;
      $("#FlightPricingEditInput_ServiceFee").value = button.dataset.serviceFee;
      pricingEditDialog.showModal();
      $("#FlightPricingEditInput_BasePrice").focus();
    });
  });
  $$("[data-pricing-toggle]").forEach((button) => {
    button.addEventListener("click", () => {
      const isExpanded = button.getAttribute("aria-expanded") === "true";
      button.setAttribute("aria-expanded", String(!isExpanded));
      button.setAttribute(
        "aria-label",
        `${isExpanded ? "Mở rộng" : "Thu gọn"} mức giá hạng ${button.querySelector("span").textContent}`
      );
      button.closest("tbody").querySelectorAll("[data-pricing-row]").forEach((row) => {
        row.hidden = isExpanded;
      });
    });
  });

  $$("[data-export]").forEach((button) => {
    button.addEventListener("click", () => {
      exportReport();
    });
  });

  $$(".admin-pagination button:not(:disabled)").forEach((button) => {
    button.addEventListener("click", () => showToast("Giao diện minh họa — phân trang chưa kết nối dữ liệu."));
  });

  const flightDialog = $("#flight-dialog");
  const shouldOpenFlightDialog = flightDialog.dataset.open === "true";
  const aircraftDialog = $("#aircraft-dialog");
  const shouldOpenAircraftDialog = aircraftDialog.dataset.open === "true";
  const shouldOpenPricingCreateDialog = pricingCreateDialog.dataset.open === "true";
  const shouldOpenPricingEditDialog = pricingEditDialog.dataset.open === "true";
  const shouldOpenPromotionDialog = promotionDialog.dataset.open === "true";
  setScreen(shouldOpenAircraftDialog
    ? "aircraft"
    : shouldOpenFlightDialog
      ? "flights"
      : shouldOpenPricingCreateDialog || shouldOpenPricingEditDialog
        ? "pricing"
        : shouldOpenPromotionDialog
          ? "promotions"
          : window.location.hash.slice(1) || "overview");
  if (shouldOpenFlightDialog) {
    const form = $("[data-flight-form]");
    form.action = flightDialog.dataset.mode === "edit" ? form.dataset.editUrl : form.dataset.createUrl;
    flightDialog.showModal();
    $("#FlightInput_FlightNumber").focus();
  }
  if (shouldOpenAircraftDialog) {
    configureAircraftDialog(aircraftDialog.dataset.mode, true);
    aircraftDialog.showModal();
    $("#AircraftInput_Model").focus();
  }
  if (shouldOpenPricingCreateDialog) {
    pricingCreateDialog.showModal();
    $("#FlightPricingInput_FlightId").focus();
  }
  if (shouldOpenPricingEditDialog) {
    pricingEditDialog.showModal();
    $("#FlightPricingEditInput_BasePrice").focus();
  }
  if (shouldOpenPromotionDialog) {
    configurePromotionDialog(promotionDialog.dataset.mode, null, true);
  }
  const initialAircraftNotice = $(".admin-toast").dataset.initialMessage;
  if (initialAircraftNotice) showToast(initialAircraftNotice);
  renderChart();
  renderIcons();
})();
