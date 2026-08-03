const storageKey = "px-theme";

export function getTheme() {
  return document.documentElement.dataset.theme === "dark" ? "dark" : "light";
}

export function setTheme(value) {
  const theme = value === "dark" ? "dark" : "light";

  try {
    window.localStorage.setItem(storageKey, theme);
  } catch {
    // A preferência continua válida na sessão mesmo quando storage está indisponível.
  }

  document.documentElement.dataset.theme = theme;
  document.documentElement.style.colorScheme = theme;
  return theme;
}
