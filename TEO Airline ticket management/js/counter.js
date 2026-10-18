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
    });
  });
  $$(".counter-seat:not(:disabled)").forEach((seat) => {
    seat.addEventListener("click", () => {
      $$(".counter-seat[aria-pressed='true']").forEach((selected) => {
        selected.setAttribute("aria-pressed", "false");
      });
      seat.setAttribute("aria-pressed", "true");
    });
  });

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

  renderIcons();
})();
