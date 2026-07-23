# RFC: Implementação do redesign de front-end · Operations PX

| | |
|---|---|
| **Status** | Rascunho para revisão de engenharia |
| **Data** | 23/07/2026 |
| **Autor** | Matheus Donangelo |
| **Documento-pai** | `design/redesign/PRD.md` (diretrizes G1 a G12) |
| **Referência** | Redesign do NPS: `design/nps/` (mockups, tokens, componentes) |
| **Escopo** | **Como** implementar o redesign no cliente Blazor WebAssembly. Só front-end. Sem mudança de contrato de API, sem regeneração de cliente tipado, sem migração. |

> O PRD define o **quê** e o **porquê** (as diretrizes). Esta RFC define o **como**: arquitetura no
> cliente, a biblioteca de componentes, a política de interatividade, a estratégia de migração e os
> riscos. Decisões técnicas recebem código **R** (R1, R2, ...).

---

## 1. Motivação

O PRD estabeleceu que as páginas antigas (Projetos, Marcos, Saúde) devem chegar ao nível do NPS,
sobre o design system BRQ, mudando só o front-end. Esta RFC responde à pergunta que trava a
execução: **o mockup do NPS é HTML/CSS/JS vanilla; o cliente é Blazor WebAssembly.** Portar o
mockup "como está" (com `brq-ui.js` e `<dialog>`) colidiria com o modelo do Blazor, onde o estado
vive em C# e o DOM é reconciliado pelo framework. A RFC define como trazer o **visual** do BRQ sem
importar o **modelo de interação** dele.

## 2. Estado atual (baseline técnico)

Levantado do código, não presumido:

- **Carga de estilo:** `wwwroot/index.html` linka **apenas** `css/app.css` e as fontes Inter +
  Space Mono (Google Fonts). **Bootstrap não é referenciado em runtime**; `wwwroot/lib/bootstrap/`
  é peso morto vendorizado.
- **`app.css`:** um arquivo global de **1312 linhas**, seccionado (topbar, page-actions, buttons,
  stats, toolbar, view-tabs, weekly-pulse, table, badges, kanban, modal, toast, project-health,
  ...). Tokens próprios em `:root` (`--purple: #4B148C`, etc.), diferentes da marca.
- **Shell:** `Layout/MainLayout.razor` = `.app-shell` > `NavMenu` (topbar horizontal) + `.app-content`.
- **Sem biblioteca de componentes compartilhados:** fora de `App.razor`/`_Imports.razor`, tudo é
  por `Features/{Projects,Milestones,ProjectHealth,Nps}`.
- **Blazor idiomático, quase sem JS:** estado em C#. **Modais** são `div.overlay.open > div.modal`
  com `@onclick:stopPropagation` (não `<dialog>`). **Toasts** são CSS + estado. **JS interop** só
  existe em `Features/Nps/NpsPage.razor.cs` (copiar link).
- **Interações específicas a preservar (G11):** o **Kanban de Projetos** usa drag-and-drop HTML5 em
  Blazor; **Marcos** é um calendário semana/mês; **Saúde** tem o formulário do líder.
- **Dados:** `Program.cs` registra `HttpClient` + os clientes tipados `ProjectsClient`,
  `MilestonesClient`, `ProjectHealthClient`, `NpsClient` (gerados por NSwag do OpenAPI). O redesign
  **não toca nisso**.

**Consequência boa:** como o Bootstrap não roda e o CSS já é global por classe (não isolado), a
troca de sistema visual é uma **substituição de folha de estilo**, não uma refatoração de cascata.

## 3. Objetivos e não-objetivos

**Objetivos**

1. Trazer os tokens, a tipografia e os componentes do BRQ para o cliente (G1).
2. Um **shell único com sidebar** e **tema claro/escuro** (G2, G3).
3. Uma **biblioteca de componentes Blazor compartilhados** que todas as páginas usam (G12).
4. Reescrever cada página nos padrões do NPS (G4 a G8) **sem perder capacidade** (G11).

**Não-objetivos**

- Nenhuma mudança de backend, contrato ou cliente tipado (sem `dotnet ef`, sem regenerar
  `PxOperationsApiClient.cs`).
- Não portar `brq-ui.js`, `brq-charts.js` nem `<dialog>` para o cliente (máximo de Blazor: R2, R4, R5).
- Não reescrever a lógica de domínio das páginas (só a camada de apresentação).

## 4. Decisões de arquitetura (R1 a R9)

> **Princípio que governa todas as decisões abaixo: máximo de Blazor.** Estado e comportamento em
> C#/Blazor; JavaScript só onde o navegador é imprescindível (persistência local, área de
> transferência, foco/rolagem de modal). Nada de `brq-ui.js`, nada de `brq-charts.js`, nada de
> `<dialog>`. O que o design system BRQ faz em JS, o cliente refaz em componentes Blazor.

### R1 · Fundação de estilo: adotar o CSS do BRQ numa Razor Class Library

Os componentes e os assets do design system vivem numa **Razor Class Library separada**,
`PxOperations.Ui` (decisão da §9). Uma RCL empacota tanto os componentes `.razor` quanto os
**static web assets** no seu `wwwroot`: `brq-tokens.css`, `brq-core.css`, `brq-dashboard.css`, a
fonte **Aspekta** e o `interop.js` (R3). O cliente os consome pelo caminho `_content/PxOperations.Ui/`,
sem copiar arquivo para dentro dele. No `index.html`, **nesta ordem**, antes de qualquer folha de
composição:

```html
<link rel="stylesheet" href="_content/PxOperations.Ui/css/brq-tokens.css" />
<link rel="stylesheet" href="_content/PxOperations.Ui/css/brq-core.css" />
<link rel="stylesheet" href="_content/PxOperations.Ui/css/brq-dashboard.css" />
<link rel="stylesheet" href="css/app.css" />   <!-- encolhe até virar composição fina -->
```

O `app.css` **não é apagado de uma vez**: cada página migrada remove a sua seção; ao final,
`app.css` fica só com composição que não pertence à RCL. `wwwroot/lib/bootstrap/` é **deletado**
(peso morto). Fontes: **Aspekta** via `@font-face` (servida pela RCL), manter **Inter**, e trocar
**Space Mono por Geist Mono** (a fonte de mono do BRQ), no `index.html` e nos tokens.

### R2 · Interatividade em Blazor/C#, NÃO via `brq-ui.js`

O `brq-ui.js` (drawer da sidebar, abas, tabela ordenável, tema, menu de filtro) mantém estado no
DOM. O Blazor mantém estado em C# e reconcilia o DOM: os dois juntos brigam (o Blazor sobrescreve
o que o JS mexeu). **Portanto, o CSS do BRQ entra; o JS do BRQ não.** Cada interação vira um
componente/estado Blazor:

| Interação do mockup (JS) | No cliente (Blazor) |
|---|---|
| Drawer da sidebar (mobile) | Estado `bool` no `AppShell` + classe `.is-open` |
| Abas de subpágina | Segmento de visão com `@bind`/parâmetro |
| Tabela ordenável | Ordenação em C# no componente `DataTable<T>` |
| Menu de filtro (2 camadas, multi) | Componente `FilterMenu` com estado tipado |
| Toggle de tema | `ThemeToggle` + um único módulo de interop (R3) |

### R3 · Modo escuro (requisito firme) e o interop mínimo

**Modo escuro é requisito, não opção.** A plataforma inteira tem **tema claro e escuro**, e todo
componente da RCL nasce validado nos dois. O tema é comandado por `data-theme` no `<html>`,
alternado por um `ThemeToggle` no shell, **persistido** entre sessões e aplicado **sem flash** no
load (R8). Os tokens do BRQ já trazem o mapeamento escuro (superfícies, tinta, roxo elevado,
paleta de gráfico reatribuída); o trabalho é garantir que cada componente novo respeite os tokens
e passe o audit de contraste no escuro.

Isso exige um punhado de coisas que **só o navegador** faz. Ficam num único `interop.js` (módulo
ES na RCL, importado por `IJSRuntime`), com superfície mínima e testável:

- **Tema:** ler/gravar a preferência em `localStorage` e estampar `data-theme` no `<html>`.
- **Modal:** trava de rolagem do body + foco inicial + devolver foco ao gatilho ao fechar.
- **Clipboard:** copiar link/mensagem (já usado no NPS).

Nada de estado de UI vive nesse módulo; ele é só a ponte com APIs do navegador. Fora disso, é tudo
Blazor.

### R4 · Gráficos em Blazor (SVG), sem `brq-charts.js`

Seguindo o princípio de máximo de Blazor, o `BrqChart` **não** é uma ilha de JS: é um componente
Blazor que **gera o SVG inline em C#** a partir de um config tipado, com as interações (tooltip,
crosshair, legenda) em Blazor. Reaproveita a **paleta validada dos tokens** (`--chart-*`), então
re-tematiza no escuro de graça. É mais trabalho que embrulhar o `brq-charts.js`, e é a escolha
deliberada: zero JS de apresentação, componente testável em `bUnit`, um dono de estado só. Escopo
inicial aos tipos que a plataforma usa (barra/empilhada para distribuição, donut, sparkline);
outros entram sob demanda.

### R5 · Modais: manter o padrão Blazor (overlay div), não `<dialog>`

O `<dialog>` nativo exige `showModal()` via interop e não conversa bem com o ciclo de vida do
Blazor. Mantém-se o padrão atual (`div.overlay > div.modal`) como o componente `Modal`,
retematizado com tokens BRQ e ganhando foco/Escape/scroll-lock via R3. Menos interop, mais
idiomático.

### R6 · Biblioteca de componentes numa Razor Class Library (`PxOperations.Ui`)

Os blocos que **todas** as páginas usam vivem num **projeto separado**, uma Razor Class Library
`PxOperations.Ui`, não numa pasta do cliente. Motivos: isola o design system do app (testável e
versionável por conta própria), empacota os static web assets junto (R1), e permite reuso futuro
por outro cliente. O `BlazorWasm` referencia a RCL e passa a compor as páginas com estes
componentes, cada um encapsulando um componente do BRQ:

| Componente | Papel | Substitui hoje |
|---|---|---|
| `AppShell` + `Sidebar` + `Topbar` | Shell com sidebar, claro/escuro (G2, G3) | `MainLayout` + `NavMenu` |
| `ThemeToggle` | Alterna e persiste o tema (R3) | (novo) |
| `PageHeader` | Título + ações de página | `.page-actions-bar` por página |
| `KpiBar` + `Kpi` | Faixa de KPIs (G5) | as 4 `*StatsBar` |
| `FilterBar` (`SearchBox` + `FilterMenu` + `FilterChips`) | Busca + botão Filtro multi (G4) | as 4 `*Toolbar` |
| `DataTable<T>` | Tabela escaneável + ordenação + abertura de detalhe (G6) | `ProjectListView`, tabelas ad-hoc |
| `Modal` | Overlay + foco/Escape (R5) | os `*FormModal`/`*DetailModal` |
| `Card`, `Pill`, `Tag`, `Chip`, `EmptyState`, `Toast` | Primitivas do BRQ | classes soltas no `app.css` |
| `BrqChart` | Gráfico SVG em Blazor (R4) | (novo) |

`FilterMenu` é o mais denso: recebe uma lista tipada de facetas (rótulo, escopo, multi/single,
opções) e devolve o estado selecionado. É a tradução em C# do menu do NPS (D11). Todos nascem com
teste `bUnit` e validados nos dois temas (R3).

**Estrutura de projetos:**

```
src/Client/
  PxOperations.Ui/              (Razor Class Library: o design system em Blazor)
    Components/                 AppShell, KpiBar, FilterMenu, DataTable, Modal, BrqChart, ...
    wwwroot/css/                brq-tokens.css, brq-core.css, brq-dashboard.css
    wwwroot/fonts/              Aspekta
    wwwroot/js/                 interop.js
  PxOperations.BlazorWasm/      (o app: referencia PxOperations.Ui e compõe as páginas)
```

### R7 · Aplicação por módulo (mapa de reescrita)

Cada página troca seu markup pelos componentes de R6, preservando comportamento (G11):

- **Projetos:** `PageHeader` (Exportar CSV, + Novo) · `KpiBar` · `FilterBar` (DC, Status, Tipo,
  Renovação + segmento Lista/Kanban/Renovações) · Lista → `DataTable<Project>` com detalhe · Kanban
  → cards BRQ **mantendo o drag-and-drop atual** · `WeeklyPulse` vira painel de leitura ·
  `ProjectFormModal` → `Modal`.
- **Marcos:** `KpiBar` · `FilterBar` + segmento Semana/Mês · **o calendário é retematizado no lugar**
  (não há componente BRQ equivalente; é CSS sobre a grade atual) · `MilestoneDetailModal`/`FormModal`
  → `Modal`.
- **Saúde:** `KpiBar` · `FilterBar` (DC, tipo, semana, score) + segmento Dashboard/Projetos · leitura
  no padrão Resultados do NPS (`BrqChart` para distribuição) · `ProjectHealthDetailModal` → `Modal` ·
  **formulário do líder** reescrito como o formulário público do NPS (ver §7 do PRD; candidato a
  componente de coleta compartilhado).
- **NPS:** portado do mockup para os mesmos componentes assim que a Fase 0 existir.

### R8 · Tema sem "flash" (anti-FOUC)

No `<head>` do `index.html`, antes do `blazor.webassembly.js`:

```html
<script>
  try { var t = localStorage.getItem("px-theme");
        if (t) document.documentElement.setAttribute("data-theme", t); } catch (e) {}
</script>
```

Assim a página nasce no tema certo; o `ThemeToggle` (R3) só atualiza dali em diante.

### R9 · Migração incremental atrás do shell novo

A Fase 0 entrega o shell, a biblioteca e os tokens **com as páginas ainda funcionando**. Cada
página é migrada depois, uma por vez; enquanto não migra, ela roda com sua seção do `app.css`
intacta sob o shell novo (o CSS do BRQ é aditivo, não conflita porque o Bootstrap não existe e as
classes não colidem). Isso permite entregar e revisar página a página, sem um "big bang".

## 5. Backend

**Nenhuma mudança.** As páginas já são servidas pelas APIs atuais e pelos clientes tipados; o
redesign reusa esse dado. Sem `dotnet ef`, sem `OpenApiGenerateDocumentsOnBuild` relevante (o
contrato não muda), sem tocar em `specs/openapi/`. Qualquer necessidade de backend que apareça vira
uma exceção **G-BE** no PRD (§5), aprovada explicitamente, não um commit.

## 6. Acessibilidade e testes

- **Gate de a11y (G9):** rodar o `a11y-audit.js` (de `design/nps/assets/scripts/` no skill) contra
  cada página no app rodando, nos dois temas, alvo **0 falha**, antes de considerar a página pronta.
- **Testes de componente:** `bUnit` (norma do projeto) para os componentes de R6 (`FilterMenu`,
  `DataTable`, `KpiBar`, `Modal`, `ThemeToggle`): estado, eventos, render.
- **Paridade (G11):** para cada página, um checklist "o que faz hoje" (ações, filtros, visões,
  estados) que o alvo precisa cobrir; vira critério de revisão do PR.
- **Revisão por imagem (Gate 2):** 375 · 768 · 1440, tema claro e escuro.
- **Regressão de interação:** cobrir especificamente o drag-and-drop do Kanban e a navegação do
  calendário, que são os comportamentos mais frágeis à reescrita.

## 7. Riscos e mitigações

| Risco | Mitigação |
|---|---|
| **Flash de tema** no load | Script anti-FOUC inline (R8). |
| **Payload WASM** (Aspekta ~55KB + CSS BRQ) | Remover `lib/bootstrap` e Space Mono; Aspekta é uma fonte só; CSS do BRQ é enxuto. Medir o bundle antes/depois. |
| **`BrqChart` em Blazor** custar mais que interop | Escopo inicial aos tipos usados; SVG gerado em C# reusa a paleta dos tokens; interações simples (tooltip/crosshair). É a troca aceita por máximo de Blazor. |
| **Modo escuro** com contraste insuficiente em algum componente | Cada componente passa pelo audit de contraste no escuro antes de "pronto"; tokens já trazem o mapa escuro (roxo elevado, paleta de gráfico reatribuída). |
| **Perder o drag-and-drop** do Kanban | Preservar o handler Blazor atual; só reskin do card. Teste de regressão dedicado. |
| **Calendário de Marcos** (sem equivalente BRQ) | É o maior esforço de CSS; deixar por último (Fase 3), com o sistema maduro. |
| **`app.css` conflitar durante a transição** | O BRQ é aditivo e não colide (sem Bootstrap); migração por seção; deletar cada seção ao migrar a página. |
| **Interop de tema/modal** em WASM (latência de boot) | Módulo único, importado sob demanda; o essencial (tema) resolve no script inline síncrono. |

## 8. Fases (técnicas, espelham o PRD §7)

| Fase | Entrega técnica |
|---|---|
| **0 · Fundação** | Criar a RCL `PxOperations.Ui` (static web assets: CSS BRQ, Aspekta, `interop.js`); referenciar no app e linkar `_content/PxOperations.Ui/css/*`; deletar `lib/bootstrap`; fontes; anti-FOUC (R8); `AppShell`/`Sidebar`/`Topbar`/`ThemeToggle` (claro/escuro) substituindo `MainLayout`/`NavMenu`; primitivas (`Kpi`, `Card`, `Pill`, `Tag`, `Chip`, `Modal`, `EmptyState`, `Toast`, `BrqChart`) e os compostos (`KpiBar`, `FilterBar`/`FilterMenu`, `DataTable<T>`), todos com `bUnit` e validados nos dois temas. Páginas antigas seguem funcionando sob o shell novo. |
| **1 · Projetos** | Reescrever `ProjectsPage` e views nos componentes; preservar dnd; remover a seção de Projetos do `app.css`. |
| **2 · Saúde** | Dashboard/Projetos + formulário do líder; `BrqChart` na distribuição. |
| **3 · Marcos** | Retematizar o calendário; remover a seção de Marcos do `app.css`. |
| **Paralela** | Portar o NPS do mockup para os componentes. |

Ao fim das quatro, o `app.css` deve estar reduzido a quase nada e `lib/bootstrap` removido.

## 9. Decisões e questões em aberto (técnicas)

**Decididas nesta revisão:**

- **Máximo de Blazor** é o princípio que governa a RFC (topo da §4). JavaScript só onde o navegador
  é imprescindível.
- **Componentes numa Razor Class Library separada** (`PxOperations.Ui`), não numa pasta do cliente (R1, R6).
- **Gráficos em Blazor/SVG** (`BrqChart`), sem `brq-charts.js` (R4).
- **Modais no padrão overlay-div do Blazor**, sem `<dialog>` (R5).
- **Modo escuro é requisito firme** (R3), com paridade de componente nos dois temas.
- **Mono decorativo: Geist Mono** (a fonte de mono real do BRQ), no lugar do Space Mono (R1).

**Em aberto:**

1. **CSS isolado vs global:** a composição de uma página só vai em `.razor.css` isolado, ou tudo
   fica global? Recomendação: a RCL é global (é a lib); composição de página única pode ser isolada.
2. **Componente de coleta compartilhado:** unificar o formulário do líder (Saúde) e o público do NPS
   num só componente Blazor? (Espelha PRD §9-3.)

## 10. Referências

- PRD: `design/redesign/PRD.md`.
- Design system e padrões provados: `design/nps/` (mockups, `assets/` com tokens/componentes,
  `a11y-audit`).
- Baseline: `src/Client/PxOperations.BlazorWasm/` (`Layout/`, `Features/`, `wwwroot/css/app.css`,
  `Program.cs`).
