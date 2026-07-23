/* =============================================================
   BRQ · CHARTS (gráficos SVG inline, vanilla, portáveis)
   Sem dependências. Paleta VALIDADA (dataviz: CVD + contraste) lida
   dos tokens CSS (--chart-1..5), então re-tematiza sozinha.
   Marcas: linhas 2px · pontas arredondadas 4px · marcadores ≥8px ·
   gap 2px entre fatias · grid/eixos recessivos · legenda p/ ≥2 séries.

   USO DECLARATIVO (recomendado):
     <div class="chart__plot" data-chart='{"type":"line", ...}'></div>
   ou config em <script type="application/json"> filho.
   USO PROGRAMÁTICO:
     BRQCharts.render(el, config);
   Config: { type:"line|area|bar|stacked|donut|spark",
             categories:[...], series:[{name, data:[...], color?}],
             options:{ yFormat, valueLabels, legend, height, area } }
   ============================================================= */
(function () {
  "use strict";
  var SVGNS = "http://www.w3.org/2000/svg";
  var registry = []; // {el, config} p/ re-render em resize/tema
  var gradSeq = 0;   // ids de gradiente únicos no documento (evita colisão entre gráficos)

  /* ---------- helpers ---------- */
  function el(tag, attrs, parent) {
    var n = document.createElementNS(SVGNS, tag);
    if (attrs) for (var k in attrs) if (attrs[k] != null) n.setAttribute(k, attrs[k]);
    if (parent) parent.appendChild(n);
    return n;
  }
  function cssVar(name, fallback) {
    var v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    return v || fallback;
  }
  function palette() {
    return [1, 2, 3, 4, 5].map(function (i) { return cssVar("--chart-" + i, "#7f2ec9"); });
  }
  function fmt(n, spec) {
    if (n == null || (typeof n === "number" && isNaN(n))) return "—";
    if (spec === "pct") return (Math.round(n * 10) / 10).toLocaleString("pt-BR") + "%";
    if (spec === "compact") {
      var abs = Math.abs(n);
      if (abs >= 1e9) return (n / 1e9).toLocaleString("pt-BR", { maximumFractionDigits: 1 }) + "B";
      if (abs >= 1e6) return (n / 1e6).toLocaleString("pt-BR", { maximumFractionDigits: 1 }) + "M";
      if (abs >= 1e3) return (n / 1e3).toLocaleString("pt-BR", { maximumFractionDigits: 1 }) + "k";
    }
    return n.toLocaleString("pt-BR");
  }
  function niceCeil(v) {
    if (v <= 0) return 1;
    var mag = Math.pow(10, Math.floor(Math.log10(v)));
    var f = v / mag;
    var nf = f <= 1 ? 1 : f <= 2 ? 2 : f <= 2.5 ? 2.5 : f <= 5 ? 5 : 10;
    return nf * mag;
  }
  function seriesColor(s, i, pal) { return s.color ? cssVar(s.color, s.color) : pal[i % pal.length]; }

  /* ---------- tooltip (um por documento, reaproveitado) ---------- */
  function ensureTooltip(plot) {
    var tip = plot.querySelector(".chart-tooltip");
    if (!tip) { tip = document.createElement("div"); tip.className = "chart-tooltip"; plot.appendChild(tip); }
    return tip;
  }
  function moveTip(tip, plot, x, y, html) {
    tip.innerHTML = html;
    tip.classList.add("is-visible");
    var pw = plot.clientWidth, tw = tip.offsetWidth;
    var left = x + 14; if (left + tw > pw) left = x - tw - 14; if (left < 0) left = 4;
    tip.style.left = left + "px";
    tip.style.top = Math.max(0, y - tip.offsetHeight - 10) + "px";
  }
  function hideTip(tip) { if (tip) tip.classList.remove("is-visible"); }

  /* ---------- legenda (renderiza no .chart__legend irmão, se houver) ---------- */
  function renderLegend(plot, series, pal, opts) {
    if (opts.legend === false) return;
    var chart = plot.closest(".chart");
    var box = chart ? chart.querySelector(".chart__legend") : null;
    if (!box) return;                 // legenda opcional: só se o autor incluiu o slot
    if (series.length < 2 && opts.legend !== true) { box.innerHTML = ""; return; }
    box.innerHTML = "";
    series.forEach(function (s, i) {
      var item = document.createElement("span");
      item.className = "chart__legend-item";
      var sw = document.createElement("span");
      sw.className = "chart__swatch"; sw.style.setProperty("--c", seriesColor(s, i, pal));
      item.appendChild(sw);
      item.appendChild(document.createTextNode(s.name || "Série " + (i + 1)));
      box.appendChild(item);
    });
  }

  /* ---------- geometria comum (eixos cartesianos) ---------- */
  function cartesian(svg, W, H, opts, maxY, minY) {
    var m = { t: 12, r: 12, b: 26, l: 40 };
    if (opts.valueLabels) m.t = 22;
    var pw = W - m.l - m.r, ph = H - m.t - m.b;
    // gridlines + rótulos Y
    var ticks = 4, g = el("g", null, svg);
    for (var i = 0; i <= ticks; i++) {
      var val = minY + (maxY - minY) * (i / ticks);
      var y = m.t + ph - (ph * (i / ticks));
      el("line", { x1: m.l, y1: y, x2: m.l + pw, y2: y, class: "grid-line" }, g);
      var t = el("text", { x: m.l - 6, y: y + 3, "text-anchor": "end", class: "axis-label" }, g);
      t.textContent = fmt(val, opts.yFormat);
    }
    return { m: m, pw: pw, ph: ph };
  }

  /* ---------- LINE / AREA ---------- */
  function drawLine(cfg, plot, svg, W, H) {
    var pal = palette(), opts = cfg.options || {}, cats = cfg.categories || [];
    var series = cfg.series || [];
    var maxY = niceCeil(Math.max.apply(null, series.reduce(function (a, s) { return a.concat(s.data); }, [0])));
    var minY = Math.min(0, Math.min.apply(null, series.reduce(function (a, s) { return a.concat(s.data); }, [0])));
    var ax = cartesian(svg, W, H, opts, maxY, minY);
    var n = cats.length || (series[0] ? series[0].data.length : 0);
    var xAt = function (i) { return ax.m.l + (n <= 1 ? ax.pw / 2 : ax.pw * (i / (n - 1))); };
    var yAt = function (v) { return ax.m.t + ax.ph - (ax.ph * ((v - minY) / (maxY - minY || 1))); };

    // eixo X (rótulos de categoria, seletivos p/ não colidir)
    var step = Math.ceil(n / Math.max(2, Math.floor(ax.pw / 64)));
    cats.forEach(function (c, i) {
      if (i % step !== 0 && i !== n - 1) return;
      var t = el("text", { x: xAt(i), y: H - 8, "text-anchor": "middle", class: "axis-label" }, svg);
      t.textContent = c;
    });
    el("line", { x1: ax.m.l, y1: ax.m.t + ax.ph, x2: ax.m.l + ax.pw, y2: ax.m.t + ax.ph, class: "axis-line" }, svg);

    series.forEach(function (s, si) {
      var color = seriesColor(s, si, pal);
      var d = "", area = "";
      s.data.forEach(function (v, i) {
        var x = xAt(i), y = yAt(v);
        d += (i === 0 ? "M" : "L") + x + " " + y + " ";
      });
      if (cfg.type === "area") {
        var uid = "brqgrad-" + (gradSeq++);
        var defs = el("defs", null, svg);
        var grad = el("linearGradient", { id: uid, x1: 0, y1: 0, x2: 0, y2: 1 }, defs);
        el("stop", { offset: "0%", "stop-color": color, "stop-opacity": "0.22" }, grad);
        el("stop", { offset: "100%", "stop-color": color, "stop-opacity": "0" }, grad);
        area = d + "L" + xAt(s.data.length - 1) + " " + (ax.m.t + ax.ph) + " L" + xAt(0) + " " + (ax.m.t + ax.ph) + " Z";
        el("path", { d: area, fill: "url(#" + uid + ")", stroke: "none" }, svg);
      }
      el("path", { d: d.trim(), fill: "none", stroke: color, "stroke-width": 2, "stroke-linejoin": "round", "stroke-linecap": "round" }, svg);
      // marcadores só se poucos pontos (senão poluição)
      if (n <= 12) s.data.forEach(function (v, i) {
        el("circle", { cx: xAt(i), cy: yAt(v), r: 3.5, fill: cssVar("--color-surface-2", "#fff"), stroke: color, "stroke-width": 2 }, svg);
      });
    });

    // camada de hover: crosshair + tooltip (ponto mais próximo no eixo X).
    // Só anexa se há série e ≥2 pontos (senão não há o que rastrear).
    if (series.length && n >= 1) {
      var tip = ensureTooltip(plot);
      var cross = el("line", { x1: 0, y1: ax.m.t, x2: 0, y2: ax.m.t + ax.ph, class: "chart__crosshair" }, svg);
      var hot = el("rect", { x: ax.m.l, y: ax.m.t, width: ax.pw, height: ax.ph, fill: "transparent" }, svg);
      hot.style.cursor = "crosshair";
      var onMove = function (e) {
        var rect = plot.getBoundingClientRect();
        var px = (e.touches ? e.touches[0].clientX : e.clientX) - rect.left;
        var i = n <= 1 ? 0 : Math.round(((px - ax.m.l) / (ax.pw || 1)) * (n - 1));
        i = Math.max(0, Math.min(n - 1, i));
        cross.setAttribute("x1", xAt(i)); cross.setAttribute("x2", xAt(i)); cross.classList.add("is-visible");
        var rows = series.map(function (s, si) {
          var v = s.data ? s.data[i] : null;   // valor pode faltar se cats > data
          return "<span class='chart-tooltip__row'><span class='chart-tooltip__dot' style='background:" + seriesColor(s, si, pal) +
            "'></span>" + (s.name || "Série " + (si + 1)) + "<span class='chart-tooltip__val'>" + fmt(v, opts.yFormat) + "</span></span>";
        }).join("");
        var v0 = series[0].data ? series[0].data[i] : null;
        var y0 = v0 == null ? ax.m.t : yAt(v0);
        moveTip(tip, plot, xAt(i), y0, "<strong>" + (cats[i] != null ? cats[i] : i) + "</strong>" + rows);
      };
      hot.addEventListener("pointermove", onMove);
      hot.addEventListener("pointerleave", function () { cross.classList.remove("is-visible"); hideTip(tip); });
    }

    renderLegend(plot, series, pal, opts);
  }

  /* ---------- BAR (grouped) / STACKED ---------- */
  function drawBar(cfg, plot, svg, W, H) {
    var pal = palette(), opts = cfg.options || {}, cats = cfg.categories || [];
    var series = cfg.series || [], stacked = cfg.type === "stacked";
    var totals = cats.map(function (_, i) {
      return stacked ? series.reduce(function (a, s) { return a + (s.data[i] || 0); }, 0)
                     : Math.max.apply(null, series.map(function (s) { return s.data[i] || 0; }));
    });
    var maxY = niceCeil(Math.max.apply(null, totals.concat([0])));
    var ax = cartesian(svg, W, H, opts, maxY, 0);
    var n = cats.length, band = ax.pw / Math.max(1, n);
    var yAt = function (v) { return ax.m.t + ax.ph - (ax.ph * (v / (maxY || 1))); };
    var groupPad = band * 0.22, inner = band - groupPad;
    var bw = stacked ? inner : inner / Math.max(1, series.length);
    var tip = ensureTooltip(plot);
    var GAP = 2, RAD = 4;

    el("line", { x1: ax.m.l, y1: ax.m.t + ax.ph, x2: ax.m.l + ax.pw, y2: ax.m.t + ax.ph, class: "axis-line" }, svg);
    cats.forEach(function (c, i) {
      var t = el("text", { x: ax.m.l + band * i + band / 2, y: H - 8, "text-anchor": "middle", class: "axis-label" }, svg);
      t.textContent = c;
    });

    cats.forEach(function (c, i) {
      var x0 = ax.m.l + band * i + groupPad / 2, acc = 0;
      series.forEach(function (s, si) {
        var v = s.data[i] || 0, color = seriesColor(s, si, pal), x, yTop, hgt;
        if (stacked) {
          x = x0; hgt = ax.ph * (v / (maxY || 1)); yTop = yAt(acc + v);
          if (si > 0) hgt = Math.max(0, hgt - GAP);
          acc += v;
        } else {
          x = x0 + bw * si; yTop = yAt(v); hgt = (ax.m.t + ax.ph) - yTop;
        }
        var w = Math.max(0, bw - (stacked ? 0 : GAP));
        var r = el("rect", { x: x, y: yTop, width: w, height: Math.max(0, hgt), rx: Math.min(RAD, w / 2), fill: color }, svg);
        r.style.cursor = "pointer";
        (function (label, val) {
          r.addEventListener("pointermove", function (e) {
            var rect = plot.getBoundingClientRect();
            moveTip(tip, plot, e.clientX - rect.left, e.clientY - rect.top,
              "<strong>" + cats[i] + "</strong><span class='chart-tooltip__row'><span class='chart-tooltip__dot' style='background:" +
              color + "'></span>" + label + "<span class='chart-tooltip__val'>" + fmt(val, opts.yFormat) + "</span></span>");
          });
          r.addEventListener("pointerleave", function () { hideTip(tip); });
        })(s.name || "Série " + (si + 1), v);
        // rótulo direto seletivo (barra única não-empilhada)
        if (opts.valueLabels && (!stacked && series.length === 1)) {
          var t = el("text", { x: x + w / 2, y: yTop - 5, "text-anchor": "middle", class: "value-label" }, svg);
          t.textContent = fmt(v, opts.yFormat);
        }
      });
    });
    renderLegend(plot, series, pal, opts);
  }

  /* ---------- DONUT ---------- */
  function drawDonut(cfg, plot, svg, W, H) {
    var pal = palette(), opts = cfg.options || {};
    var data = (cfg.series && cfg.series[0] && cfg.series[0].data) || cfg.data || [];
    var labels = cfg.categories || [];
    var total = data.reduce(function (a, b) { return a + b; }, 0) || 1;
    var cx = W / 2, cy = H / 2, R = Math.min(W, H) / 2 - 8, r = R * 0.62;
    var tip = ensureTooltip(plot);
    var a0 = -Math.PI / 2, GAP = 0.03;
    data.forEach(function (v, i) {
      var frac = v / total, a1 = a0 + frac * Math.PI * 2;
      var color = pal[i % pal.length];
      var large = (a1 - a0) > Math.PI ? 1 : 0;
      var s = a0 + GAP / 2, e = a1 - GAP / 2;
      var p = ["M", cx + R * Math.cos(s), cy + R * Math.sin(s),
        "A", R, R, 0, large, 1, cx + R * Math.cos(e), cy + R * Math.sin(e),
        "L", cx + r * Math.cos(e), cy + r * Math.sin(e),
        "A", r, r, 0, large, 0, cx + r * Math.cos(s), cy + r * Math.sin(s), "Z"].join(" ");
      var path = el("path", { d: p, fill: color }, svg);
      path.style.cursor = "pointer";
      (function (label, val) {
        path.addEventListener("pointermove", function (ev) {
          var rect = plot.getBoundingClientRect();
          moveTip(tip, plot, ev.clientX - rect.left, ev.clientY - rect.top,
            "<span class='chart-tooltip__row'><span class='chart-tooltip__dot' style='background:" + color + "'></span>" +
            label + "<span class='chart-tooltip__val'>" + fmt(val, opts.yFormat) + " · " + Math.round(frac * 100) + "%</span></span>");
        });
        path.addEventListener("pointerleave", function () { hideTip(tip); });
      })(labels[i] || "Item " + (i + 1), v);
      a0 = a1;
    });
    // total no centro
    var c1 = el("text", { x: cx, y: cy - 2, "text-anchor": "middle", class: "value-label" }, svg);
    c1.setAttribute("font-size", "18"); c1.textContent = fmt(total, opts.yFormat);
    if (opts.centerLabel) { var c2 = el("text", { x: cx, y: cy + 16, "text-anchor": "middle", class: "axis-label" }, svg); c2.textContent = opts.centerLabel; }
    // legenda a partir das categorias
    renderLegend(plot, labels.map(function (l, i) { return { name: l, color: pal[i % pal.length] }; }), pal, { legend: true });
  }

  /* ---------- SPARKLINE (sem eixos) ---------- */
  function drawSpark(cfg, plot, svg, W, H) {
    var pal = palette();
    var data = (cfg.series && cfg.series[0] && cfg.series[0].data) || cfg.data || [];
    var color = (cfg.series && cfg.series[0] && cfg.series[0].color) ? cssVar(cfg.series[0].color, cfg.series[0].color) : pal[0];
    var max = Math.max.apply(null, data), min = Math.min.apply(null, data), pad = 3;
    var xAt = function (i) { return pad + (W - 2 * pad) * (i / (data.length - 1 || 1)); };
    var yAt = function (v) { return H - pad - (H - 2 * pad) * ((v - min) / (max - min || 1)); };
    var d = data.map(function (v, i) { return (i ? "L" : "M") + xAt(i) + " " + yAt(v); }).join(" ");
    el("path", { d: d, fill: "none", stroke: color, "stroke-width": 2, "stroke-linecap": "round", "stroke-linejoin": "round" }, svg);
    if (data.length) el("circle", { cx: xAt(data.length - 1), cy: yAt(data[data.length - 1]), r: 3, fill: color }, svg);
  }

  /* ---------- render dispatcher ---------- */
  function render(plot, cfg) {
    if (!plot || !cfg) return;
    plot.querySelectorAll("svg").forEach(function (s) { s.remove() ; });
    var W = Math.max(120, plot.clientWidth || 320);
    var H = (cfg.options && cfg.options.height) || (cfg.type === "spark" ? 40 : plot.clientHeight || 256);
    var svg = el("svg", { viewBox: "0 0 " + W + " " + H, width: W, height: H, role: "img" }, plot);
    var label = cfg.title || (cfg.type + " chart");
    svg.setAttribute("aria-label", label);
    try {
      if (cfg.type === "line" || cfg.type === "area") drawLine(cfg, plot, svg, W, H);
      else if (cfg.type === "bar" || cfg.type === "stacked") drawBar(cfg, plot, svg, W, H);
      else if (cfg.type === "donut" || cfg.type === "pie") drawDonut(cfg, plot, svg, W, H);
      else if (cfg.type === "spark") drawSpark(cfg, plot, svg, W, H);
    } catch (err) { if (window.console) console.error("[BRQCharts]", err); }
  }

  /* ---------- auto-init + re-render ---------- */
  function parseConfig(node) {
    var raw = node.getAttribute("data-chart") || node.getAttribute("data-chart-json");
    if (raw) { try { return JSON.parse(raw); } catch (e) { console.error("[BRQCharts] JSON inválido em data-chart", e); return null; } }
    var script = node.querySelector("script[type='application/json']");
    if (script) { try { return JSON.parse(script.textContent); } catch (e) { console.error("[BRQCharts] JSON inválido", e); return null; } }
    return null;
  }
  function initAll() {
    document.querySelectorAll("[data-chart], [data-chart-json]").forEach(function (node) {
      var cfg = parseConfig(node);
      if (!cfg) return;
      registry.push({ el: node, config: cfg });
      render(node, cfg);
    });
  }
  function rerenderAll() { registry.forEach(function (r) { render(r.el, r.config); }); }

  var resizeT;
  window.addEventListener("resize", function () { clearTimeout(resizeT); resizeT = setTimeout(rerenderAll, 150); });
  window.addEventListener("brq:themechange", rerenderAll);
  document.addEventListener("DOMContentLoaded", initAll);

  window.BRQCharts = { render: render, rerenderAll: rerenderAll,
    add: function (el, cfg) { registry.push({ el: el, config: cfg }); render(el, cfg); } };
})();
