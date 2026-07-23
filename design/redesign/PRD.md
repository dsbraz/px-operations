# PRD: Redesign de front-end da plataforma · Operations PX

| | |
|---|---|
| **Status** | Rascunho para revisão |
| **Data** | 23/07/2026 |
| **Autor** | Matheus Donangelo |
| **Revisor-alvo** | CTO |
| **Decisão solicitada** | Aprovar (a) as diretrizes de design G1 a G12 como padrão único da plataforma; (b) a mudança estrutural de navegação (shell com sidebar); (c) o corte de fases da §7 |
| **Escopo** | **Só front-end.** Backend só muda se for **extremamente necessário** (G-BE), documentado como exceção. O objetivo é levar o salto de qualidade do redesign do NPS para **todas as páginas antigas** (Projetos, Marcos, Saúde), com um sistema único. |
| **Artefatos** | Referência viva: o redesign do NPS em `design/nps/` (PRD, FLUXOS, mockups navegáveis). Este documento define as diretrizes; os mockups por módulo virão depois, um por fase. |

---

## 1. Resumo executivo

O redesign do NPS provou o alvo: uma tela densa, escaneável, acessível e com identidade BRQ,
construída sobre um design system de verdade. As outras três páginas (Projetos, Marcos, Saúde)
seguem hoje um padrão antigo e **inconsistente com esse alvo**: Bootstrap mais um `app.css`
ad-hoc, navegação por topbar, fileira de dropdowns sempre à vista, sem tema escuro e com uma
paleta roxa diferente da marca.

Este PRD não é sobre uma tela. É sobre **as diretrizes** que garantem que cada redesign de página
chegue no mesmo nível do NPS, e que as quatro telas se leiam como **um produto só**. A mudança é
quase toda de front-end: as APIs e os clientes tipados que já servem essas páginas continuam. O
trabalho de fundação (adotar o design system BRQ e o shell no cliente) é grande, mas se paga uma
vez e destrava todas as páginas.

## 2. Contexto: o que existe hoje

**Quatro módulos, um esqueleto repetido.** Projetos (`/`), Marcos (`/milestones`), Saúde
(`/project-health`) e NPS (`/nps`) compartilham a mesma anatomia de página:

```
[ page-actions: Exportar CSV · + Novo X ]
[ stats bar: 4 a 6 KPIs ]
[ toolbar: busca + 3 a 5 <select> sempre visíveis + abas de visão ]
[ área de visão: kanban / lista / calendário / dashboard ]
[ modais: detalhe + formulário ]  [ toast ]
```

**Base visual atual (o que muda):**

- **Bootstrap + `app.css` (1312 linhas)** com tokens próprios: `--purple: #4B148C` (não o roxo da
  marca `#7f2ec9`), neutros e cores de estado ad-hoc, sem escala tipográfica de display.
- **Tipografia:** Inter (corpo, alinhado à marca) + Space Mono (dado). Falta a Aspekta (display).
- **Navegação por topbar horizontal** (Projetos · Marcos · Saúde · NPS), não o shell de dashboard
  com sidebar que o mockup do NPS usa.
- **Sem tema escuro.**
- **Filtros como fileira de `<select>`** ("Todos os DCs", "Todos os Status", ...), o mesmo
  anti-padrão que o NPS já aposentou.

**O NPS já resolveu tudo isso** (ver `design/nps/`). Este PRD generaliza aquelas decisões.

## 3. Princípio norteador: o NPS é a régua

Cada decisão do redesign do NPS vira uma **diretriz de plataforma**. Onde o NPS decidiu D-algo,
a plataforma adota G-algo. O redesign de qualquer página está "pronto" quando passaria pela mesma
barra do NPS: 0 falha no audit de acessibilidade, densidade e leitura no nível do mockup do NPS,
e componentes idênticos aos de lá.

## 4. Diretrizes de design (G1 a G12)

| # | Diretriz | Vem de | Consequência |
|---|---|---|---|
| G1 | **Fundação única: design system BRQ.** Tokens (cor, tipografia, espaço, raio, sombra), componentes (botões, tags, KPIs, painéis, tabela, pills, chips) e a paleta de gráfico validada substituem o Bootstrap e o `app.css` ad-hoc. | NPS inteiro | Roxo passa a `#7f2ec9` (funcional) e laranja `#ee7c38` (marca); tipografia = **Aspekta** (display) + **Inter** (corpo) + **Geist Mono** (decorativo/dado, no lugar do Space Mono); o `app.css` vira uma folha fina de composição por página. |
| G2 | **Shell de dashboard unificado.** Sidebar editorial (item ativo com trilho roxo, rótulos em mono) + topbar fina, no lugar da topbar horizontal. Os módulos viram itens da sidebar. | Shell do mockup NPS | Muda a navegação de nível superior; o layout (header, sidebar, área de conteúdo) é compartilhado por todas as páginas. |
| G3 | **Modo escuro é requisito, não opção.** A plataforma tem tema claro **e** escuro, via tokens (`prefers-color-scheme` + toggle que estampa `data-theme`), com a preferência persistida e aplicada sem flash no load. | Tokens BRQ | Todo componente nasce validado nos **dois** temas (contraste no escuro incluso); nada de cor fixa fora dos tokens. O toggle mora no shell. |
| G4 | **Filtros = busca + um botão "Filtro"** com menu de duas camadas e **multi-seleção**; nunca fileira de `<select>`. O que está ligado vira chip removível; o botão mostra o nº de filtros ativos. | D11 | Toda toolbar de módulo é reescrita nesse padrão; facetas por aba entram no mesmo menu. |
| G5 | **KPIs num padrão único**: faixa de stats leve, número em mono tabular, rótulo curto, sem caixa pesada. | F7 / stats do NPS | As quatro `StatsBar` convergem para o mesmo componente. |
| G6 | **Listas viram tabelas escaneáveis com abertura de detalhe.** Colunas fixas, prévia curta, e "abrir" leva ao registro completo num modal ou linha expansível. | D12 | A lista de Projetos e a leitura de Saúde herdam a tabela + detalhe do NPS. |
| G7 | **Cards com estado semântico explícito**: sempre ícone + cor + texto, nunca só cor; e **tipos de informação diferentes se leem diferentes** (a lição do card do NPS: prazo é alerta acionável com badge, recência é contexto neutro). | Cards do Kanban NPS | Kanban de Projetos e cards de Marcos/Saúde seguem essa gramática. |
| G8 | **Modais enxutos**: sem redundância (não repetir o mesmo dado duas vezes), sem chrome à toa, sem controle que a tela-mãe já oferece. | Modal de detalhe do NPS (versão enxuta) | Todos os modais de detalhe/form são revisados por densidade. |
| G9 | **Acessibilidade WCAG 2.2 AA é o piso, não o teto.** Teclado percorre tudo, foco visível, alvos ≥ 24px, contraste (texto pequeno só em roxo/preto/cinza-escuro), estados ARIA, `prefers-reduced-motion`. Um `<h1>` por página. | Gate 1 do NPS | Cada página passa pelo `a11y-audit` com 0 falha antes de "pronto". |
| G10 | **Escrita pt-BR, voz ativa, sem travessão.** Rótulos pelo que a pessoa reconhece, não pelo nome do sistema; o controle diz o que faz. | Estilo de escrita do projeto | Vale em toda a copy, inclusive estados vazios e erros. |
| G11 | **Paridade funcional.** O redesign **não remove capacidade**: toda ação, filtro, visão e estado atual continua acessível. Muda a forma, não o que dá para fazer. | Princípio de migração | Antes de redesenhar uma página, listar o que ela faz hoje; o alvo cobre tudo. |
| G12 | **Consistência entre módulos.** Mesmos componentes, mesmos espaçamentos, mesma gramática de interação. Quem aprende um módulo entende os outros. | Objetivo do redesign | Nada de componente "só desta página" quando um compartilhado serve. |

## 5. Restrição de backend (G-BE)

**Padrão: zero backend.** As páginas antigas já são servidas pelas APIs atuais e pelos clientes
tipados gerados; o redesign reusa esse dado. Uma mudança de backend só entra se for
**extremamente necessária**, e então é documentada como exceção (no espírito das capacidades B do
NPS), com justificativa de por que o alvo de front-end não se sustenta sem ela.

Casos que **podem** exigir backend (a validar por página, não presumir):

- Um filtro que hoje varre no cliente vira inviável com o volume real e precisa ir para o servidor.
- Uma coluna/campo que a tela-alvo mostra existe no domínio mas **hoje só sai no CSV**, não no JSON.
- Uma agregação nova de leitura (como as médias por dimensão do NPS) que a API ainda não calcula.

Fora disso, **o redesign é de front-end**. Se aparecer uma tentação de mexer no backend, ela vira
uma linha na §9 (questões em aberto) para decisão explícita, não um commit silencioso.

## 6. Diretrizes por módulo

Cada módulo herda G1 a G12. Abaixo, só o que é **específico** de cada um.

### 6.1 Projetos (a carteira): `/`, `/projects`

A home, maior tráfego. Hoje: page-actions (Exportar CSV, + Novo Projeto), `ProjectsStatsBar` (6
KPIs), `ProjectsToolbar` (busca + DC + Status + Tipo + Renovação + abas Lista/Kanban/Renovações),
`WeeklyPulse`, e três visões, mais o `ProjectFormModal`.

- **Toolbar → G4.** Busca + botão "Filtro" (DC, Status, Tipo, Renovação como facetas multi); as
  três visões (Lista, Kanban, Renovações) viram um **segmento de visão** ao lado da busca.
- **Lista → G6.** Tabela escaneável (projeto, cliente, DC, tipo, status, renovação, datas-chave),
  com abertura do projeto no detalhe. O "Editar" mora no detalhe, não repetido por linha.
- **Kanban → G7.** Cards BRQ com estado semântico (status/renovação por pill, datas-chave com o
  tratamento certo: vencimento é alerta, criação é contexto).
- **Renovações:** manter a visão, no padrão de tabela/cards; renovação vencendo é o alerta.
- **Weekly Pulse:** repensar como **painel de leitura** dentro do sistema (KPIs + destaque da
  semana), não uma faixa solta acima da toolbar.
- **`ProjectFormModal` → G8.** Modal de formulário enxuto, campos no componente `.field` do BRQ.

### 6.2 Marcos (o calendário): `/milestones`

Padrão **único na plataforma**: visões de **Semana** e **Mês** em calendário. Hoje: stats bar,
toolbar (busca + DC + tipo de projeto + tipo de marco + projeto + abas Semana/Mês), e os modais.

- **Manter o calendário**, retematizado no BRQ. Densidade de célula, cores por **tipo de marco**
  como rótulo + cor (nunca só cor), hoje destacado, navegação semana/mês clara.
- **Toolbar → G4** (facetas: DC, tipo de projeto, tipo de marco, projeto) + **segmento** Semana/Mês.
- **Stats → G5.** KPIs (total, na semana, no mês, sponsors).
- **Detalhe e formulário → G8.** Modais enxutos; o `MilestoneDetailModal` abre o marco completo.
- *Atenção:* o calendário é o componente mais custoso de retematizar; validar densidade e
  responsividade (semana no estreito) com cuidado.

### 6.3 Saúde de Projetos: `/project-health` (+ formulário do líder)

Hoje: header próprio, stats bar (4 KPIs), toolbar (busca + DC + tipo + semana + score + abas
Dashboard/Projetos), duas visões, `ProjectHealthDetailModal`, e uma **página de formulário do
líder** (`/project-health/new`, `/{id}/edit`) com hero, progresso e as 5 dimensões (práticas,
escopo, cronograma, qualidade, relacionamento).

- **Dashboard/Projetos → padrão da aba Resultados do NPS:** KPIs, distribuição/leitura executiva,
  e drill-down por projeto. Score e RAG por pill (ícone + cor + texto).
- **Toolbar → G4** (DC, tipo, semana, score como facetas) + **segmento** Dashboard/Projetos.
- **Detalhe → G8.**
- **Formulário do líder = o análogo do formulário público do NPS.** Hero + progresso + as 5
  dimensões numa escala clara, um bloco por pergunta, estados de conclusão/erro, no visual BRQ.
  **Recomendação:** unificar com o formulário público do NPS num **mesmo componente de formulário
  de coleta** (mesma gramática de escala, progresso e estados), já que os dois são "uma pessoa
  respondendo um questionário sob dimensões". Ver §9.

### 6.4 NPS: referência, já redesenhado

Não é objeto desta fase. Serve de **referência viva** dos padrões (`design/nps/`) e é a primeira
página a nascer no design system quando a fundação (Fase 0) existir no cliente.

## 7. Fases de entrega

| Fase | Entrega | Por quê nesta ordem |
|---|---|---|
| **0 · Fundação** | Portar o design system BRQ (tokens, componentes, `a11y-audit`), o **shell com sidebar** (G2) e o **tema** (G3) para o cliente Blazor, sem mudar conteúdo das páginas. | Destrava todas as páginas; é a maior peça e se paga uma vez. Nada de página redesenhada convence antes disso. |
| **1 · Projetos** | A carteira inteira no novo sistema (G4 a G8). | Home, maior tráfego, maior retorno visível. |
| **2 · Saúde** | Dashboard/Projetos + **formulário do líder**. | Reusa o padrão de leitura e o de formulário (do NPS). |
| **3 · Marcos** | O calendário retematizado. | Componente mais específico; melhor por último, com o sistema já maduro. |
| **(paralela)** | Portar o **NPS** do mockup para o cliente assim que a Fase 0 existir. | O desenho já está pronto; só depende da fundação. |

## 8. Métricas de sucesso (o que "melhor" significa)

1. **Consistência:** as quatro telas usam os mesmos componentes e espaçamentos; um checklist de
   "componentes compartilhados vs específicos" fica majoritariamente compartilhado.
2. **Acessibilidade:** 0 falha no `a11y-audit` por página, nos dois temas.
3. **Densidade e leitura:** cada tela passa pela revisão por imagem (375 · 768 · 1440) no nível do NPS.
4. **Zero regressão funcional (G11):** toda ação/filtro/visão de antes continua fazendo o que fazia.
5. **Tema escuro** funcionando em toda a plataforma.
6. **Backend intocado** (ou as exceções da §5 explicitamente aprovadas).

## 9. Questões em aberto

1. **Unificar formulários (§6.3):** o formulário do líder (Saúde) e o formulário público do NPS
   viram **um componente só** de coleta? Reduz manutenção, mas acopla dois módulos.
2. **Paridade de dados (G11):** algum filtro/coluna atual depende de dado que a API só entrega no
   CSV, não no JSON? Mapear na Fase 0, por página, para não descobrir tarde (candidato a G-BE).
3. **Exportar CSV:** manter em todas as páginas onde existe hoje; confirmar que o endpoint atende.

**Já decididas:** a **navegação passa a shell com sidebar** (G2); tipografia com **Geist Mono**
(não Space Mono) no decorativo (G1); e o **Bootstrap** sai na Fase 0 sem risco de cascata, pois a
RFC confirmou que ele **não roda em runtime hoje** (é peso morto vendorizado).

## 10. Referências

- Redesign do NPS (padrões provados): `design/nps/PRD.md`, `design/nps/FLUXOS.md`, e os mockups
  navegáveis `design/nps/painel.html` e `design/nps/formulario.html`.
- Design system BRQ: `design/nps/assets/` (tokens, componentes, `a11y-audit`).
- Páginas antigas (baseline): `src/Client/PxOperations.BlazorWasm/Features/{Projects,Milestones,ProjectHealth}`.
