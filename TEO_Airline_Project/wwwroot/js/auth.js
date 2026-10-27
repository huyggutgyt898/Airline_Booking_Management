(() => {
  const toast = document.querySelector("#toast");
  const logoutDialog = document.querySelector("#logout-dialog");
  const menuToggle = document.querySelector(".menu-toggle");
  const mobileMenu = document.querySelector("#mobile-menu");
  let toastTimer;

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

  function showPasswordToggle(button) {
    const field = document.getElementById(button.getAttribute("aria-controls"));
    if (!field) return;
    const isVisible = field.type === "password";
    field.type = isVisible ? "text" : "password";
    button.setAttribute("aria-pressed", String(isVisible));
    button.setAttribute("aria-label", isVisible ? "Ẩn mật khẩu" : "Hiện mật khẩu");
    button.classList.toggle("is-visible", isVisible);
  }

  function setMobileMenu(open) {
    if (!menuToggle || !mobileMenu) return;
    mobileMenu.hidden = !open;
    menuToggle.setAttribute("aria-expanded", String(open));
    menuToggle.setAttribute("aria-label", open ? "Đóng menu" : "Mở menu");
    menuToggle.innerHTML = `<i data-lucide="${open ? "x" : "menu"}" aria-hidden="true"></i>`;
    renderIcons();
  }

  document.querySelectorAll("[data-password-toggle]").forEach((button) => {
    button.addEventListener("click", () => showPasswordToggle(button));
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

  document.querySelectorAll("[data-signout-open]").forEach((button) => {
    button.addEventListener("click", () => {
      if (logoutDialog && !logoutDialog.open) logoutDialog.showModal();
    });
  });

  document.querySelectorAll("[data-signout-cancel]").forEach((button) => {
    button.addEventListener("click", () => logoutDialog?.close());
  });

  menuToggle?.addEventListener("click", () => {
    setMobileMenu(mobileMenu.hidden);
  });

  mobileMenu?.addEventListener("click", (event) => {
    if (event.target.closest("a")) setMobileMenu(false);
  });

  document.addEventListener("click", (event) => {
    const comingSoon = event.target.closest("[data-coming-soon]");
    if (comingSoon) {
      event.preventDefault();
      showToast("Tính năng này sẽ sớm được cập nhật.");
    }
  });

  document.addEventListener("keydown", (event) => {
    if (event.key !== "Escape" || logoutDialog?.open) return;
    setMobileMenu(false);
  });

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", renderIcons, { once: true });
  } else {
    renderIcons();
  }
})();
