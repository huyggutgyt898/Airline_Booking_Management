(() => {
  const accountKey = "teo-demo-account";
  const toast = document.querySelector("#toast");
  let toastTimer;
  let logoutOpener = null;

  function showToast(message) {
    toast.textContent = message;
    toast.hidden = false;
    window.clearTimeout(toastTimer);
    toastTimer = window.setTimeout(() => {
      toast.hidden = true;
    }, 3200);
  }

  function readDemoAccount() {
    const savedAccount = sessionStorage.getItem(accountKey);
    return savedAccount ? JSON.parse(savedAccount) : null;
  }

  function updateAccountNavigation() {
    const account = readDemoAccount();
    document.querySelectorAll("[data-auth-link]").forEach((link) => {
      link.hidden = Boolean(account);
    });

    const accountTrigger = document.querySelector("[data-account-trigger]");
    if (accountTrigger) accountTrigger.hidden = !account;

    const mobileAccount = document.querySelector("[data-auth-account]");
    if (mobileAccount) mobileAccount.hidden = !account;

    const label = document.querySelector(".account-popover-label");
    if (label) label.textContent = account ? `Xin chào, ${account.displayName}` : "Tài khoản mẫu";
  }

  function setFormMessage(message, kind = "error") {
    const output = document.querySelector("#auth-form-message");
    if (!output) return;
    output.textContent = message;
    output.classList.toggle("is-success", kind === "success");
    output.classList.toggle("is-error", kind === "error");
  }

  function setInvalid(field, message, errorTarget) {
    field.setAttribute("aria-invalid", "true");
    if (errorTarget) errorTarget.textContent = message;
  }

  function clearInvalid(field, errorTarget) {
    field.removeAttribute("aria-invalid");
    if (errorTarget) errorTarget.textContent = "";
  }

  function showPasswordToggle(button) {
    const field = document.getElementById(button.getAttribute("aria-controls"));
    const isVisible = field.type === "password";
    field.type = isVisible ? "text" : "password";
    button.setAttribute("aria-pressed", String(isVisible));
    button.setAttribute("aria-label", isVisible ? "Ẩn mật khẩu" : "Hiện mật khẩu");
    button.innerHTML = `<i data-lucide="${isVisible ? "eye-off" : "eye"}" aria-hidden="true"></i>`;
    if (window.lucide && typeof window.lucide.createIcons === "function") window.lucide.createIcons();
  }

  function setLoading(form, isLoading) {
    const submit = form.querySelector("[data-auth-submit]");
    form.classList.toggle("is-loading", isLoading);
    submit.setAttribute("aria-busy", String(isLoading));
    submit.setAttribute("aria-disabled", String(isLoading));
  }

  function getDemoAccount(form) {
    const values = new FormData(form);
    const isRegistration = form.dataset.authForm === "register";
    const identity = isRegistration ? values.get("email").trim() : values.get("identity").trim();
    const displayName = isRegistration
      ? values.get("name").trim()
      : (identity.includes("@") ? identity.split("@")[0] : "Khách hàng");

    return {
      displayName,
      email: isRegistration ? identity : "",
      phone: isRegistration ? values.get("phone").trim() : (identity.includes("@") ? "" : identity)
    };
  }

  document.querySelectorAll("[data-password-toggle]").forEach((button) => {
    button.addEventListener("click", () => showPasswordToggle(button));
  });

  const registerForm = document.querySelector('[data-auth-form="register"]');
  if (registerForm) {
    const password = registerForm.querySelector("#register-password");
    const confirmation = registerForm.querySelector("#register-confirm-password");
    const error = registerForm.querySelector("#confirm-password-error");
    const validateConfirmation = () => {
      const mismatch = confirmation.value && confirmation.value !== password.value;
      confirmation.setCustomValidity(mismatch ? "Mật khẩu xác nhận chưa khớp." : "");
      if (mismatch) {
        setInvalid(confirmation, "Mật khẩu xác nhận chưa khớp.", error);
      } else {
        clearInvalid(confirmation, error);
      }
    };
    password.addEventListener("input", validateConfirmation);
    confirmation.addEventListener("input", validateConfirmation);
  }

  document.querySelectorAll("[data-auth-form]").forEach((form) => {
    form.addEventListener("submit", (event) => {
      event.preventDefault();
      if (form.classList.contains("is-loading")) return;
      if (form.dataset.authForm === "register") {
        const password = form.querySelector("#register-password");
        const confirmation = form.querySelector("#register-confirm-password");
        const error = form.querySelector("#confirm-password-error");
        if (password.value !== confirmation.value) {
          setInvalid(confirmation, "Mật khẩu xác nhận chưa khớp.", error);
          confirmation.focus();
          setFormMessage("Vui lòng nhập lại mật khẩu cho khớp.");
          return;
        }
      }

      setFormMessage("Đang xử lý yêu cầu…", "success");
      setLoading(form, true);
      window.setTimeout(() => {
        const account = getDemoAccount(form);
        sessionStorage.setItem(accountKey, JSON.stringify(account));
        window.location.assign("index.html");
      }, 800);
    });
  });

  document.querySelectorAll("[data-google-auth]").forEach((button) => {
    button.addEventListener("click", () => showToast("Đăng nhập Google chưa được kết nối."));
  });

  document.querySelectorAll("[data-forgot-password]").forEach((link) => {
    link.addEventListener("click", (event) => {
      event.preventDefault();
      showToast("Luồng đặt lại mật khẩu chưa được dựng.");
    });
  });

  document.querySelectorAll("[data-legal-link]").forEach((link) => {
    link.addEventListener("click", (event) => {
      event.preventDefault();
      showToast(`${link.dataset.legalLink} chưa có nội dung được kết nối.`);
    });
  });

  const accountTrigger = document.querySelector("[data-account-trigger]");
  const accountPopover = document.querySelector("#account-popover");
  if (accountTrigger && accountPopover) {
    accountTrigger.addEventListener("click", () => {
      const isOpen = accountPopover.hidden;
      accountPopover.hidden = !isOpen;
      accountTrigger.setAttribute("aria-expanded", String(isOpen));
    });

    document.addEventListener("click", (event) => {
      if (event.target.closest(".account-menu")) return;
      accountPopover.hidden = true;
      accountTrigger.setAttribute("aria-expanded", "false");
    });
  }

  const logoutDialog = document.querySelector("#logout-dialog");
  if (logoutDialog) {
    document.querySelectorAll("[data-signout-open]").forEach((button) => {
      button.addEventListener("click", () => {
        logoutOpener = button;
        if (accountPopover) accountPopover.hidden = true;
        accountTrigger?.setAttribute("aria-expanded", "false");
        if (!logoutDialog.open) logoutDialog.showModal();
        logoutDialog.querySelector("[data-signout-cancel]").focus();
      });
    });

    logoutDialog.querySelector("[data-signout-cancel]").addEventListener("click", () => logoutDialog.close());
    logoutDialog.querySelector("[data-signout-confirm]").addEventListener("click", () => {
      sessionStorage.removeItem(accountKey);
      updateAccountNavigation();
      logoutDialog.close();
      showToast("Bạn đã đăng xuất khỏi tài khoản minh hoạ.");
    });
    logoutDialog.addEventListener("cancel", (event) => {
      event.preventDefault();
      logoutDialog.close();
    });
    logoutDialog.addEventListener("close", () => {
      if (logoutOpener?.isConnected && logoutOpener.getClientRects().length) {
        logoutOpener.focus();
      } else if (logoutOpener?.closest("#account-popover") && sessionStorage.getItem(accountKey)) {
        accountPopover.hidden = false;
        accountTrigger.setAttribute("aria-expanded", "true");
        logoutOpener.focus();
      } else {
        [...document.querySelectorAll('[data-auth-link][href="login.html"]')]
          .find((link) => link.getClientRects().length)
          ?.focus();
      }
      logoutOpener = null;
    });
  }

  document.addEventListener("keydown", (event) => {
    if (event.key !== "Escape") return;
    if (logoutDialog?.open) {
      event.preventDefault();
      logoutDialog.close();
      return;
    }
    if (!accountPopover || !accountTrigger) return;
    accountPopover.hidden = true;
    accountTrigger.setAttribute("aria-expanded", "false");
  });

  updateAccountNavigation();
})();
