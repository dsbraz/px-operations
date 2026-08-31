(() => {
  const storageKey = "px-theme";
  let theme = "light";

  // Só a escolha explícita, gravada pelo botão de tema, decide. Cair na
  // preferência do sistema aqui impunha escuro a toda a aplicação, e só o
  // foundation.css tem paleta escura: as telas internas, vestidas pelo
  // app.css, ficavam claras com componentes escuros por cima. Pior, o layout
  // interno não tem o botão de tema, então não havia como voltar. Quando o
  // app.css ganhar uma variante escura, a preferência do sistema pode voltar
  // a ser considerada — há um teste que libera isso sozinho.
  try {
    const stored = window.localStorage.getItem(storageKey);
    if (stored === "light" || stored === "dark") {
      theme = stored;
    }
  } catch {
    // Storage indisponível (aba anônima, cookies bloqueados): fica no claro.
  }

  document.documentElement.dataset.theme = theme;
  document.documentElement.style.colorScheme = theme;
})();
