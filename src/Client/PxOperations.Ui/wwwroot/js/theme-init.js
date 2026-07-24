(() => {
  const storageKey = "px-theme";
  let theme = "light";

  try {
    const stored = window.localStorage.getItem(storageKey);
    if (stored === "light" || stored === "dark") {
      theme = stored;
    } else if (window.matchMedia?.("(prefers-color-scheme: dark)").matches) {
      theme = "dark";
    }
  } catch {
    if (window.matchMedia?.("(prefers-color-scheme: dark)").matches) {
      theme = "dark";
    }
  }

  document.documentElement.dataset.theme = theme;
  document.documentElement.style.colorScheme = theme;
})();
