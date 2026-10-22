(() => {
  const accountKey = "teo-demo-account";
  const toast = document.querySelector("#toast");
  let toastTimer;

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
    button.innerHTML = `<i data-lucide="${isVisible ? "eye-off" : "eye"}" aria-hidden="true"></i>`;
    if (window.lucide && typeof window.lucide.createIcons === "function") window.lucide.createIcons();
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
})();