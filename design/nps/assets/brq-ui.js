/* =============================================================
   BRQ · UI (interações vanilla, portáveis)
   Contrato: cada interação é initAlgo(), chamada no DOMContentLoaded,
   seleciona por data-* (nunca por classe de estilo) e alterna estado .is-*.
   Sem dependências. Gráficos ficam em brq-charts.js.
   ============================================================= */
(function () {
  "use strict";

  var mqDesktop = window.matchMedia("(min-width: 64em)");
  var prefersReduced = window.matchMedia("(prefers-reduced-motion: reduce)");

  /* ---------- App shell: sidebar drawer (mobile) ---------- */
  function initAppNav() {
    var app = document.querySelector("[data-app]");
    if (!app) return;
    var toggle = app.querySelector("[data-nav-toggle]");
    var scrim = app.querySelector("[data-nav-scrim]");
    var sidebar = app.querySelector("[data-sidebar]");
    var main = app.querySelector(".app__main");
    if (!toggle || !sidebar) return;

    function focusables() {
      return Array.prototype.slice.call(sidebar.querySelectorAll(
        'a[href],button:not([disabled]),input:not([type=hidden]),select,textarea,[tabindex]:not([tabindex="-1"])'
      )).filter(function (e) { return e.offsetParent !== null; });
    }
    function open() {
      app.classList.add("is-nav-open");
      toggle.setAttribute("aria-expanded", "true");
      // Mobile = drawer MODAL: tranca scroll, torna o fundo inerte e o painel dialog.
      if (!mqDesktop.matches) {
        document.body.style.overflow = "hidden";
        if (main) { main.setAttribute("inert", ""); main.setAttribute("aria-hidden", "true"); }
        sidebar.setAttribute("role", "dialog");
        sidebar.setAttribute("aria-modal", "true");
      }
      var f = focusables();
      if (f.length) f[0].focus();
      document.addEventListener("keydown", onKey);
    }
    function close() {
      app.classList.remove("is-nav-open");
      toggle.setAttribute("aria-expanded", "false");
      document.body.style.overflow = "";
      // Remove a inércia ANTES de devolver o foco (o gatilho vive dentro de .app__main).
      if (main) { main.removeAttribute("inert"); main.removeAttribute("aria-hidden"); }
      sidebar.removeAttribute("role");
      sidebar.removeAttribute("aria-modal");
      document.removeEventListener("keydown", onKey);
      toggle.focus();   // devolve o foco ao gatilho (trigger) — WCAG 2.4.3
    }
    function onKey(e) {
      if (e.key === "Escape") { close(); return; }
      // Trava o foco dentro do drawer no mobile (WCAG 2.4.3 / overlay convention).
      if (e.key === "Tab" && !mqDesktop.matches) {
        var f = focusables();
        if (!f.length) return;
        var first = f[0], last = f[f.length - 1];
        if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
      }
    }

    toggle.setAttribute("aria-expanded", "false");
    toggle.addEventListener("click", function () {
      if (app.classList.contains("is-nav-open")) close(); else open();
    });
    if (scrim) scrim.addEventListener("click", close);
    // fechar ao navegar (mobile) e ao voltar p/ desktop
    sidebar.addEventListener("click", function (e) {
      if (e.target.closest("a") && !mqDesktop.matches) close();
    });
    mqDesktop.addEventListener("change", function (e) { if (e.matches) close(); });
  }

  /* ---------- Tabela ordenável ---------- */
  function initSortableTable() {
    document.querySelectorAll("[data-sortable]").forEach(function (table) {
      var ths = table.querySelectorAll("thead th");
      ths.forEach(function (th, colIndex) {
        var btn = th.querySelector("[data-sort]") || (th.matches("[data-sort]") ? th : null);
        if (!btn) return;
        var trigger = th.querySelector(".data-table__sort") || btn;
        trigger.addEventListener("click", function () {
          var current = th.getAttribute("aria-sort");
          var dir = current === "ascending" ? "descending" : "ascending";
          ths.forEach(function (t) { t.removeAttribute("aria-sort"); });
          th.setAttribute("aria-sort", dir);
          sortRows(table, colIndex, dir);
        });
      });
    });
  }
  function cellSortValue(cell) {
    // data-sort-value = número de MÁQUINA (ponto decimal, sem separador de milhar)
    var raw = cell.getAttribute("data-sort-value");
    if (raw !== null) { var n0 = parseFloat(raw); return { num: isNaN(n0) ? null : n0, str: String(raw).toLowerCase() }; }
    // Texto exibido = formato pt-BR: ponto = milhar, vírgula = decimal
    var txt = cell.textContent.trim();
    var cleaned = txt.replace(/[^0-9.,\-]/g, "").replace(/\./g, "").replace(",", ".");
    var num = parseFloat(cleaned);
    return { num: (cleaned === "" || isNaN(num)) ? null : num, str: txt.toLowerCase() };
  }
  function sortRows(table, col, dir) {
    var tbody = table.querySelector("tbody");
    var rows = Array.prototype.slice.call(tbody.querySelectorAll("tr"));
    rows.sort(function (a, b) {
      var av = cellSortValue(a.cells[col]), bv = cellSortValue(b.cells[col]);
      var cmp;
      if (av.num !== null && bv.num !== null) cmp = av.num - bv.num;
      else cmp = av.str < bv.str ? -1 : av.str > bv.str ? 1 : 0;
      return dir === "ascending" ? cmp : -cmp;
    });
    rows.forEach(function (r) { tbody.appendChild(r); });
  }

  /* ---------- Filtro de tabela: busca + chips → linhas + contador + vazio ---------- */
  function initFilterTable() {
    document.querySelectorAll("[data-filter-table]").forEach(function (root) {
      var search = root.querySelector("[data-search]");
      var chips = Array.prototype.slice.call(root.querySelectorAll("[data-chip]"));
      var table = root.querySelector("[data-table-target]") || root.querySelector("table");
      if (!table) return;
      var rows = Array.prototype.slice.call(table.querySelectorAll("tbody tr"));
      var counters = root.querySelectorAll("[data-count]");   // pode haver mais de um (sub-título + pager)
      var empty = root.querySelector("[data-empty]");
      var active = {}; // group -> value ("" = todos)

      function apply() {
        var q = search ? search.value.trim().toLowerCase() : "";
        var shown = 0;
        rows.forEach(function (row) {
          var okSearch = !q || row.textContent.toLowerCase().indexOf(q) !== -1;
          var okChips = Object.keys(active).every(function (g) {
            if (!active[g]) return true;
            return (row.getAttribute("data-" + g) || "") === active[g];
          });
          var show = okSearch && okChips;
          row.hidden = !show;
          if (show) shown++;
        });
        counters.forEach(function (c) { c.textContent = String(shown); });
        if (empty) empty.hidden = shown !== 0;
      }

      if (search) search.addEventListener("input", apply);
      chips.forEach(function (chip) {
        chip.addEventListener("click", function () {
          var group = chip.getAttribute("data-chip");
          var value = chip.getAttribute("data-value") || "";
          var isOn = chip.getAttribute("aria-pressed") === "true";
          // rádio dentro do grupo
          chips.filter(function (c) { return c.getAttribute("data-chip") === group; })
               .forEach(function (c) { c.setAttribute("aria-pressed", "false"); });
          if (isOn) { active[group] = ""; }
          else { chip.setAttribute("aria-pressed", "true"); active[group] = value; }
          apply();
        });
      });
      chips.forEach(function (c) { if (!c.hasAttribute("aria-pressed")) c.setAttribute("aria-pressed", "false"); });
      apply();
    });
  }

  /* ---------- Abas WAI-ARIA (roving tabindex) ---------- */
  function initTabs() {
    document.querySelectorAll("[data-tabs]").forEach(function (root) {
      var tabs = Array.prototype.slice.call(root.querySelectorAll("[role='tab']"));
      var panels = Array.prototype.slice.call(root.querySelectorAll("[role='tabpanel']"));
      if (!tabs.length) return;
      function select(tab) {
        tabs.forEach(function (t) {
          var sel = t === tab;
          t.setAttribute("aria-selected", sel ? "true" : "false");
          t.tabIndex = sel ? 0 : -1;
        });
        panels.forEach(function (p) {
          p.hidden = p.id !== tab.getAttribute("aria-controls");
        });
      }
      tabs.forEach(function (tab, i) {
        tab.addEventListener("click", function () { select(tab); tab.focus(); });
        tab.addEventListener("keydown", function (e) {
          var idx = null;
          if (e.key === "ArrowRight" || e.key === "ArrowDown") idx = (i + 1) % tabs.length;
          else if (e.key === "ArrowLeft" || e.key === "ArrowUp") idx = (i - 1 + tabs.length) % tabs.length;
          else if (e.key === "Home") idx = 0;
          else if (e.key === "End") idx = tabs.length - 1;
          if (idx !== null) { e.preventDefault(); select(tabs[idx]); tabs[idx].focus(); }
        });
      });
      var initial = tabs.filter(function (t) { return t.getAttribute("aria-selected") === "true"; })[0] || tabs[0];
      select(initial);
    });
  }

  /* ---------- Alternar tema (data-theme no <html>) ---------- */
  function initThemeToggle() {
    var toggles = document.querySelectorAll("[data-theme-toggle]");
    if (!toggles.length) return;
    function current() { return document.documentElement.getAttribute("data-theme") || "light"; }
    function setTheme(theme) {
      document.documentElement.setAttribute("data-theme", theme);
      toggles.forEach(function (b) { b.setAttribute("aria-pressed", theme === "dark" ? "true" : "false"); });
      try { localStorage.setItem("brq-theme", theme); } catch (e) {}
      // avisa gráficos p/ redesenhar com a paleta do tema
      window.dispatchEvent(new CustomEvent("brq:themechange", { detail: { theme: theme } }));
    }
    try { var saved = localStorage.getItem("brq-theme"); if (saved) setTheme(saved); } catch (e) {}
    toggles.forEach(function (b) {
      b.addEventListener("click", function () { setTheme(current() === "dark" ? "light" : "dark"); });
    });
  }

  /* ---------- Reveal ao rolar ---------- */
  function initReveal() {
    var els = document.querySelectorAll(".reveal");
    if (!els.length) return;
    if (prefersReduced.matches || !("IntersectionObserver" in window)) {
      els.forEach(function (el) { el.classList.add("is-visible"); });
      return;
    }
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (en) {
        if (en.isIntersecting) { en.target.classList.add("is-visible"); io.unobserve(en.target); }
      });
    }, { threshold: 0.12, rootMargin: "0px 0px -8% 0px" });
    els.forEach(function (el) { io.observe(el); });
  }

  /* ---------- Ano corrente ---------- */
  function initCurrentYear() {
    var y = String(new Date().getFullYear());
    document.querySelectorAll("[data-current-year]").forEach(function (el) { el.textContent = y; });
  }

  document.addEventListener("DOMContentLoaded", function () {
    initAppNav();
    initSortableTable();
    initFilterTable();
    initTabs();
    initThemeToggle();
    initReveal();
    initCurrentYear();
  });
})();
