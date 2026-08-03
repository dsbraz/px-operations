# Redesign do módulo de NPS — Operations PX (BRQ)

Pacote autossuficiente para revisão. **Abra `index.html` no navegador** para começar.

| Arquivo | O que é |
|---|---|
| `index.html` | Capa: o que muda, como revisar e o que a decisão destrava. Ponto de entrada. |
| `painel.html` | Painel navegável (Coleta · Resultados · Respostas). |
| `formulario.html` | Formulário público do respondente. |
| `PRD.md` | Decisões (D), requisitos (F) e fases de entrega. |
| `FLUXOS.md` | Mapeamento técnico: capacidades (B), arquivos e migrações. |
| `assets/` | Design system BRQ (CSS, JS, fontes) usado pelos mockups. |

Tudo abre **offline**, sem servidor nem instalação. Os mockups carregam a pasta
`assets/` que acompanha o pacote; os dados exibidos vêm de um seed determinístico
de desenvolvimento (os números são os que a API calcula sobre esse seed).

## Para quem for implementar

Esta pasta é **referência visual**, não código a ser importado. O que vale e o que
não vale ao migrar as telas para `src/Client/PxOperations.Ui`:

| Arquivo | Uso na implementação |
|---|---|
| `assets/brq-tokens.css` | **Fonte de verdade dos tokens.** Foi dele que saiu o `wwwroot/css/foundation.css` da RCL. Em dúvida de cor, espaçamento ou tipo, este arquivo decide. |
| `assets/brq-core.css` | Referência das primitivas (botão, tag, pill). Os nomes divergem: o protótipo usa `.btn`/`.tag`, a RCL usa `.brq-button` e classes isoladas por componente. |
| `assets/brq-dashboard.css` | Referência de composição do painel (grid de KPI, tabela, filtros). |
| `assets/brq-charts.js` | **Não entra na aplicação.** Gráficos serão componentes Blazor. |
| `assets/brq-ui.js` | **Não entra na aplicação.** Estado de UI vive em Blazor, não em JS. |
| `assets/fonts/Aspekta-450.ttf` | **Fica só aqui.** Não é promovida à RCL enquanto licença e arquivo WOFF2 não estiverem resolvidos. |

Duas divergências deliberadas entre os protótipos e a aplicação:

- os protótipos carregam Inter e Geist Mono do Google Fonts e usam Aspekta local;
  a aplicação não carrega nenhuma das três, e as stacks degradam para `system-ui`
  e `ui-monospace` até a decisão de tipografia ser fechada;
- os protótipos aplicam a fundação à página inteira; na aplicação o reset é
  escopado em `[data-brq-ui]` (emitido pelo `BrqAppShell`) para conviver com o
  `app.css` legado enquanto a migração acontece.
