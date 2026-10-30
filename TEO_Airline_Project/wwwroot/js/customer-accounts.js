(() => {
  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  const apiUrl = "/api/counter/customers";
  const state = {
    page: 1,
    pageSize: 10,
    status: "all",
    search: "",
    selectedId: null,
    listRequest: 0,
    detailRequest: 0,
    searchTimer: null
  };
  const numberFormat = new Intl.NumberFormat("vi-VN");
  const escapeHtml = (value) => String(value ?? "").replace(/[&<>"']/g, (character) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;"
  })[character]);

  function getErrorMessage(data, fallback) {
    if (data?.detail) return data.detail;
    if (data?.title) return data.title;
    if (data?.errors) {
      const validationMessage = Object.values(data.errors)
        .flat()
        .find((message) => typeof message === "string" && message.trim());
      if (validationMessage) return validationMessage;
    }
    return fallback;
  }

  async function request(path = "", { method = "GET", body, antiforgery = false } = {}) {
    const headers = {};
    if (body !== undefined) headers["Content-Type"] = "application/json";
    if (antiforgery) {
      const token = $("input[name='__RequestVerificationToken']", $("#counter-antiforgery"))?.value;
      if (!token) throw new Error("Không lấy được mã bảo vệ phiên. Vui lòng tải lại trang.");
      headers.RequestVerificationToken = token;
    }

    const response = await fetch(`${apiUrl}${path}`, {
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
      throw new Error(getErrorMessage(data, "Không thể xử lý yêu cầu. Vui lòng thử lại."));
    }
    return data;
  }

  function initials(name) {
    return String(name ?? "")
      .trim()
      .split(/\s+/)
      .filter(Boolean)
      .slice(-2)
      .map((part) => part[0].toLocaleUpperCase("vi-VN"))
      .join("") || "?";
  }

  function formatDate(value) {
    if (!value) return "—";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "—";
    return new Intl.DateTimeFormat("vi-VN", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric"
    }).format(date);
  }

  function statusBadge(isActive) {
    return isActive
      ? '<span class="counter-badge counter-badge-success">Hoạt động</span>'
      : '<span class="counter-badge counter-badge-danger">Đã khóa</span>';
  }

  function setListMessage(message, isError = false) {
    const summary = $("#customer-results-summary");
    summary.textContent = message;
    summary.classList.toggle("counter-profile-feedback-error", isError);
  }

  function renderEmptyList(message) {
    $("#customer-results").innerHTML =
      `<tr><td colspan="5" class="counter-empty-cell">${escapeHtml(message)}</td></tr>`;
    $("#customer-cards").innerHTML =
      `<p class="counter-empty-mobile">${escapeHtml(message)}</p>`;
    $("#customer-pagination").replaceChildren();
  }

  function renderRows(items) {
    const selectedId = state.selectedId;
    $("#customer-results").innerHTML = items.length
      ? items.map((customer) => `
        <tr class="${customer.userId === selectedId ? "is-selected" : ""}">
          <td>
            <button class="counter-customer-select" type="button" data-customer-id="${customer.userId}" aria-current="${customer.userId === selectedId ? "true" : "false"}">
              <span class="counter-account-cell">
                <span class="counter-customer-avatar" aria-hidden="true">${escapeHtml(initials(customer.fullName))}</span>
                <span><strong>${escapeHtml(customer.fullName)}</strong><span class="counter-record-meta">${escapeHtml(customer.username)}</span></span>
              </span>
            </button>
          </td>
          <td>${escapeHtml(customer.email)}<span class="counter-record-meta">${escapeHtml(customer.phone)}</span></td>
          <td>${escapeHtml(formatDate(customer.createdAt))}</td>
          <td>${numberFormat.format(customer.bookingCount)}</td>
          <td>${statusBadge(customer.isActive)}</td>
        </tr>`).join("")
      : '<tr><td colspan="5" class="counter-empty-cell">Không tìm thấy tài khoản phù hợp.</td></tr>';

    $("#customer-cards").innerHTML = items.length
      ? items.map((customer) => `
        <article class="counter-customer-card ${customer.userId === selectedId ? "is-selected" : ""}">
          <div class="counter-customer-card-head">
            <button class="counter-customer-select" type="button" data-customer-id="${customer.userId}" aria-current="${customer.userId === selectedId ? "true" : "false"}">
              <span class="counter-account-cell">
                <span class="counter-customer-avatar" aria-hidden="true">${escapeHtml(initials(customer.fullName))}</span>
                <span><strong>${escapeHtml(customer.fullName)}</strong><span class="counter-record-meta">${escapeHtml(customer.username)}</span></span>
              </span>
            </button>
            ${statusBadge(customer.isActive)}
          </div>
          <span>${escapeHtml(customer.email)}</span>
          <span>${escapeHtml(customer.phone)} · ${numberFormat.format(customer.bookingCount)} đặt chỗ</span>
          <span class="counter-record-meta">Tạo ${escapeHtml(formatDate(customer.createdAt))}</span>
        </article>`).join("")
      : '<p class="counter-empty-mobile">Không tìm thấy tài khoản phù hợp.</p>';
  }

  function renderPagination(page, pageSize, totalCount) {
    const container = $("#customer-pagination");
    container.replaceChildren();
    if (totalCount === 0) return;

    const pageCount = Math.max(1, Math.ceil(totalCount / pageSize));
    const start = (page - 1) * pageSize + 1;
    const end = Math.min(page * pageSize, totalCount);
    const summary = document.createElement("span");
    summary.className = "counter-small counter-muted";
    summary.textContent = `Hiển thị ${numberFormat.format(start)}–${numberFormat.format(end)} / ${numberFormat.format(totalCount)}`;

    const controls = document.createElement("div");
    controls.className = "counter-pagination-controls";
    controls.innerHTML = `
      <button class="counter-button counter-button-sm" type="button" data-customer-page="${page - 1}" ${page <= 1 ? "disabled" : ""} aria-label="Trang trước">Trước</button>
      <span class="counter-small">Trang ${numberFormat.format(page)} / ${numberFormat.format(pageCount)}</span>
      <button class="counter-button counter-button-sm" type="button" data-customer-page="${page + 1}" ${page >= pageCount ? "disabled" : ""} aria-label="Trang sau">Tiếp</button>`;
    container.append(summary, controls);
  }

  function renderCounts(data) {
    $("#customer-total-count").textContent = `${numberFormat.format(data.allCount)} tài khoản`;
    $("[data-customer-count='all']").textContent = numberFormat.format(data.allCount);
    $("[data-customer-count='active']").textContent = numberFormat.format(data.activeCount);
    $("[data-customer-count='locked']").textContent = numberFormat.format(data.lockedCount);
  }

  function setSelectedRow(customerId) {
    $$("#customer-results tr").forEach((row) => {
      const selected = Number($("[data-customer-id]", row)?.dataset.customerId) === customerId;
      row.classList.toggle("is-selected", selected);
    });
    $$("#customer-cards .counter-customer-card").forEach((card) => {
      card.classList.toggle(
        "is-selected",
        Number($("[data-customer-id]", card)?.dataset.customerId) === customerId
      );
    });
    $$("[data-customer-id]").forEach((button) => {
      button.setAttribute("aria-current", String(Number(button.dataset.customerId) === customerId));
    });
  }

  function renderDetail(customer) {
    $("#customer-detail-empty").hidden = true;
    $("#customer-detail").hidden = false;
    $("#customer-detail-avatar").textContent = initials(customer.fullName);
    $("#customer-detail-name").textContent = customer.fullName;
    $("#customer-detail-username").textContent = `Tên đăng nhập: ${customer.username}`;
    $("#customer-detail-email").textContent = customer.email || "—";
    $("#customer-detail-phone").textContent = customer.phone || "—";
    $("#customer-detail-created").textContent = formatDate(customer.createdAt);
    $("#customer-detail-bookings").textContent = `${numberFormat.format(customer.bookingCount)} đặt chỗ`;
    const badge = $("#customer-detail-status");
    badge.className = `counter-badge ${customer.isActive ? "counter-badge-success" : "counter-badge-danger"}`;
    badge.textContent = customer.isActive ? "Hoạt động" : "Đã khóa";
    badge.hidden = false;
    $("#customer-lock").disabled = !customer.isActive;
    $("#customer-lock").innerHTML = customer.isActive
      ? '<i data-lucide="lock-keyhole" aria-hidden="true"></i>Khóa tài khoản'
      : '<i data-lucide="lock-keyhole" aria-hidden="true"></i>Tài khoản đã khóa';
    if (window.lucide && typeof window.lucide.createIcons === "function") {
      window.lucide.createIcons();
    }
  }

  async function selectCustomer(customerId) {
    state.selectedId = customerId;
    state.customer = null;
    setSelectedRow(customerId);
    const requestId = ++state.detailRequest;
    $("#customer-detail-empty").hidden = false;
    $("#customer-detail-empty").classList.remove("counter-profile-feedback-error");
    $("#customer-detail-empty").textContent = "Đang tải hồ sơ khách hàng…";
    $("#customer-detail").hidden = true;
    $("#customer-detail-status").hidden = true;
    try {
      const customer = await request(`/${customerId}`);
      if (requestId !== state.detailRequest) return;
      state.customer = customer;
      renderDetail(customer);
      setSelectedRow(customerId);
    } catch (error) {
      if (requestId !== state.detailRequest) return;
      $("#customer-detail-empty").textContent = error.message;
      $("#customer-detail-empty").classList.add("counter-profile-feedback-error");
    }
  }

  function renderDialogFields(type, customer) {
    if (type === "edit") {
      return `
        <div class="counter-customer-dialog-fields">
          <div class="counter-field"><label for="customer-edit-name">Họ và tên</label><input id="customer-edit-name" name="fullName" type="text" required minlength="2" maxlength="100" value="${escapeHtml(customer.fullName)}"></div>
          <div class="counter-field"><label for="customer-edit-email">Email</label><input id="customer-edit-email" name="email" type="email" required maxlength="100" value="${escapeHtml(customer.email)}"></div>
          <div class="counter-field"><label for="customer-edit-phone">Số điện thoại</label><input id="customer-edit-phone" name="phone" type="tel" required maxlength="20" value="${escapeHtml(customer.phone)}"></div>
        </div>`;
    }
    if (type === "password") {
      return `
        <p>Đặt mật khẩu mới cho <strong>${escapeHtml(customer.fullName)}</strong>. Mật khẩu sẽ được lưu dưới dạng hash.</p>
        <div class="counter-customer-dialog-fields">
          <div class="counter-field"><label for="customer-new-password">Mật khẩu mới</label><input id="customer-new-password" name="newPassword" type="password" required minlength="8" maxlength="128" autocomplete="new-password"></div>
          <div class="counter-field"><label for="customer-confirm-password">Xác nhận mật khẩu</label><input id="customer-confirm-password" name="confirmPassword" type="password" required minlength="8" maxlength="128" autocomplete="new-password"></div>
        </div>`;
    }
    return `<p>Khóa tài khoản <strong>${escapeHtml(customer.fullName)}</strong>? Khách sẽ không thể đăng nhập; dữ liệu đặt chỗ vẫn được giữ nguyên.</p>`;
  }

  function openDialog(type) {
    const customer = state.customer;
    if (!customer) return;
    const dialog = $("#customer-dialog");
    const title = {
      edit: "Sửa thông tin khách hàng",
      password: "Đặt mật khẩu mới",
      lock: "Khóa tài khoản khách hàng"
    }[type];
    $("#customer-dialog-title").textContent = title;
    $("#customer-dialog-fields").innerHTML = renderDialogFields(type, customer);
    $("#customer-dialog-error").hidden = true;
    $("#customer-dialog-error").textContent = "";
    $("#customer-dialog-submit").textContent = {
      edit: "Lưu thay đổi",
      password: "Đặt mật khẩu",
      lock: "Khóa tài khoản"
    }[type];
    $("#customer-dialog-submit").classList.toggle("counter-button-danger", type === "lock");
    $("#customer-dialog-submit").classList.toggle("counter-button-primary", type !== "lock");
    dialog.dataset.action = type;
    dialog.showModal();
  }

  async function loadCustomers() {
    const requestId = ++state.listRequest;
    const tableWrap = $("#customer-table-wrap");
    tableWrap.setAttribute("aria-busy", "true");
    setListMessage("Đang tải danh sách khách hàng…");
    $("#customer-total-count").textContent = "Đang tải…";
    const query = new URLSearchParams({
      page: String(state.page),
      pageSize: String(state.pageSize),
      status: state.status,
      search: state.search
    });

    try {
      const data = await request(`?${query}`);
      if (requestId !== state.listRequest) return;
      renderCounts(data);
      renderRows(data.items);
      renderPagination(data.page, data.pageSize, data.totalCount);
      setListMessage(
        data.totalCount
          ? `Tìm thấy ${numberFormat.format(data.totalCount)} tài khoản theo bộ lọc hiện tại.`
          : "Không có tài khoản khách hàng phù hợp."
      );
      if (state.selectedId && data.items.some((customer) => customer.userId === state.selectedId)) {
        setSelectedRow(state.selectedId);
        await selectCustomer(state.selectedId);
      } else if (data.items.length) {
        await selectCustomer(data.items[0].userId);
      } else {
        state.selectedId = null;
        state.customer = null;
        ++state.detailRequest;
        $("#customer-detail").hidden = true;
        $("#customer-detail-status").hidden = true;
        $("#customer-detail-empty").hidden = false;
        $("#customer-detail-empty").textContent = "Không có tài khoản trong kết quả hiện tại.";
      }
    } catch (error) {
      if (requestId !== state.listRequest) return;
      renderEmptyList("Không tải được dữ liệu.");
      setListMessage(error.message, true);
      $("#customer-total-count").textContent = "Không tải được";
    } finally {
      if (requestId === state.listRequest) tableWrap.removeAttribute("aria-busy");
    }
  }

  async function submitCustomerAction(event) {
    event.preventDefault();
    const form = event.currentTarget;
    if (!form.reportValidity()) return;
    const dialog = $("#customer-dialog");
    const action = dialog.dataset.action;
    const customerId = state.selectedId;
    const submit = $("#customer-dialog-submit");
    const errorMessage = $("#customer-dialog-error");
    let method = "PUT";
    let path = `/${customerId}`;
    let body;

    if (action === "edit") {
      body = {
        fullName: $("#customer-edit-name").value.trim(),
        email: $("#customer-edit-email").value.trim(),
        phone: $("#customer-edit-phone").value.trim()
      };
    } else if (action === "password") {
      body = {
        newPassword: $("#customer-new-password").value,
        confirmPassword: $("#customer-confirm-password").value
      };
      if (!/[^\p{L}\p{N}\s]/u.test(body.newPassword) || !/\p{L}/u.test(body.newPassword)) {
        errorMessage.textContent = "Mật khẩu cần có ít nhất một chữ cái và một ký tự đặc biệt.";
        errorMessage.hidden = false;
        return;
      }
      if (body.newPassword !== body.confirmPassword) {
        errorMessage.textContent = "Mật khẩu xác nhận không khớp.";
        errorMessage.hidden = false;
        return;
      }
      method = "POST";
      path += "/password";
    } else if (action === "lock") {
      method = "PATCH";
      path += "/lock";
    } else {
      return;
    }

    submit.disabled = true;
    errorMessage.hidden = true;
    const originalText = submit.textContent;
    submit.textContent = "Đang xử lý…";
    try {
      const customer = await request(path, { method, body, antiforgery: true });
      state.customer = customer;
      state.selectedId = customer.userId;
      dialog.close();
      await loadCustomers();
      const toast = $(".counter-toast");
      if (toast) {
        toast.textContent = action === "lock"
          ? "Đã khóa tài khoản khách hàng."
          : action === "password"
            ? "Đã đặt mật khẩu mới."
            : "Đã cập nhật thông tin khách hàng.";
        toast.hidden = false;
        window.setTimeout(() => { toast.hidden = true; }, 3200);
      }
    } catch (error) {
      errorMessage.textContent = error.message;
      errorMessage.hidden = false;
    } finally {
      submit.disabled = false;
      submit.textContent = originalText;
    }
  }

  $("#customer-search").addEventListener("input", (event) => {
    window.clearTimeout(state.searchTimer);
    state.search = event.currentTarget.value.trim();
    state.searchTimer = window.setTimeout(() => {
      state.page = 1;
      loadCustomers();
    }, 300);
  });

  $$(".counter-customer-status-button").forEach((button) => {
    button.addEventListener("click", () => {
      state.status = button.dataset.customerStatus;
      state.page = 1;
      $$(".counter-customer-status-button").forEach((item) => {
        const selected = item === button;
        item.classList.toggle("is-active", selected);
        item.setAttribute("aria-pressed", String(selected));
      });
      loadCustomers();
    });
  });

  $("#customer-results").addEventListener("click", (event) => {
    const button = event.target.closest("[data-customer-id]");
    if (button) selectCustomer(Number(button.dataset.customerId));
  });
  $("#customer-cards").addEventListener("click", (event) => {
    const button = event.target.closest("[data-customer-id]");
    if (button) selectCustomer(Number(button.dataset.customerId));
  });
  $("#customer-pagination").addEventListener("click", (event) => {
    const button = event.target.closest("[data-customer-page]");
    if (!button || button.disabled) return;
    state.page = Number(button.dataset.customerPage);
    loadCustomers();
  });
  $("#customer-edit").addEventListener("click", () => openDialog("edit"));
  $("#customer-password").addEventListener("click", () => openDialog("password"));
  $("#customer-lock").addEventListener("click", () => openDialog("lock"));
  $("#customer-action-form").addEventListener("submit", submitCustomerAction);
  $$("[data-customer-dialog-close]").forEach((button) => {
    button.addEventListener("click", () => $("#customer-dialog").close());
  });

  if (!$('[data-screen="customers"]').hidden) loadCustomers();
})();
