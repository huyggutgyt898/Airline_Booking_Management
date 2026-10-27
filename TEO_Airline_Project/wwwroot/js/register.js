(() => {
  const form = document.querySelector("#register-form");
  if (!form) return;

  const emailPattern = /^[^@\s]+@[^@\s]+\.[^@\s]{2,}$/;
  const phonePattern = /^(0\d{9}|\+84\d{9})$/;
  const letterPattern = /\p{L}/u;
  const specialPattern = /[^\p{L}\p{Nd}\s]/u;

  const submitButton = form.querySelector("[data-auth-submit]");

  const fields = [
    {
      input: document.querySelector("#register-name"),
      error: document.querySelector("#register-name-error"),
      validate(value) {
        if (!value) return "Vui lòng nhập họ và tên.";
        if (value.length < 2) return "Họ và tên phải có ít nhất 2 ký tự.";
        if (value.length > 200) return "Họ và tên không được vượt quá 200 ký tự.";
        return "";
      },
    },
    {
      input: document.querySelector("#register-email"),
      error: document.querySelector("#register-email-error"),
      validate(value) {
        if (!value) return "Vui lòng nhập email.";
        if (value.length > 100 || !emailPattern.test(value)) return "Email không đúng định dạng.";
        return "";
      },
    },
    {
      input: document.querySelector("#register-phone"),
      error: document.querySelector("#register-phone-error"),
      normalize: (value) => value.replace(/[\s().-]/g, ""),
      validate(value) {
        if (!value) return "Vui lòng nhập số điện thoại.";
        if (!phonePattern.test(this.normalize(value))) {
          return "Số điện thoại không đúng định dạng. Ví dụ: 0912345678.";
        }
        return "";
      },
    },
    {
      input: document.querySelector("#register-password"),
      error: document.querySelector("#register-password-error"),
      validate(value) {
        if (!value) return "Vui lòng nhập mật khẩu.";
        if (value.length < 8) return "Mật khẩu phải có ít nhất 8 ký tự.";
        if (!letterPattern.test(value) || !specialPattern.test(value)) {
          return "Mật khẩu phải bao gồm cả chữ cái và ký tự đặc biệt.";
        }
        return "";
      },
    },
    {
      input: document.querySelector("#register-confirm-password"),
      error: document.querySelector("#confirm-password-error"),
      validate(value) {
        if (!value) return "Vui lòng nhập lại mật khẩu.";
        const password = document.querySelector("#register-password")?.value ?? "";
        if (value !== password) return "Mật khẩu xác nhận không khớp.";
        return "";
      },
    },
  ];

  const termsInput = document.querySelector("#terms-agreement");
  const termsError = document.querySelector("#terms-error");
  const formMessage = document.querySelector("#auth-form-message");

  function setFieldError(field, message) {
    if (!field.input || !field.error) return;
    field.error.textContent = message;
    if (message) {
      field.input.setAttribute("aria-invalid", "true");
    } else {
      field.input.removeAttribute("aria-invalid");
    }
  }

  function setTermsError(message) {
    if (!termsError || !termsInput) return;
    termsError.textContent = message;
    if (message) {
      termsInput.setAttribute("aria-invalid", "true");
    } else {
      termsInput.removeAttribute("aria-invalid");
    }
  }

  fields.forEach((field) => {
    if (!field.input) return;
    field.input.addEventListener("input", () => setFieldError(field, ""));
    field.input.addEventListener("blur", () => {
      if (!field.input.value) return;
      setFieldError(field, field.validate(field.input.value.trim()));
    });
  });

  termsInput?.addEventListener("change", () => setTermsError(""));

  form.addEventListener("submit", (event) => {
    let firstInvalid = null;
    let isValid = true;

    fields.forEach((field) => {
      if (!field.input) return;
      const message = field.validate(field.input.value.trim());
      setFieldError(field, message);
      if (message) {
        isValid = false;
        firstInvalid ??= field.input;
      }
    });

    if (termsInput && !termsInput.checked) {
      setTermsError("Bạn cần đồng ý với Điều khoản sử dụng để tạo tài khoản.");
      isValid = false;
      firstInvalid ??= termsInput;
    } else {
      setTermsError("");
    }

    if (!isValid) {
      event.preventDefault();
      formMessage.textContent = "Vui lòng kiểm tra lại các thông tin đã nhập.";
      firstInvalid?.focus();
      return;
    }

    formMessage.textContent = "";
    form.classList.add("is-loading");
    if (submitButton) {
      submitButton.setAttribute("aria-busy", "true");
    }
  });
})();
