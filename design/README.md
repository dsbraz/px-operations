# Redesign e convergência da experiência PxOperations

Este diretório reúne o pacote de decisão de produto e engenharia para convergir Projetos, Saúde,
Marcos e NPS. Os documentos separam deliberadamente o que pode preservar o contrato atual do que
depende de evolução de backend.

Os artefatos HTML/CSS/JavaScript em `nps/` são protótipos estáticos e referências de experiência.
Eles não são a implementação Blazor, não comprovam paridade com a API e não substituem os gates de
acessibilidade, performance e testes definidos no PRD e na RFC.

## Ordem recomendada de revisão

1. **[`redesign/PRD.md`](redesign/PRD.md)** — objetivos, G1–G12, jornadas, critérios de aceite,
   fases, métricas, riscos e decisões de produto.
2. **[`redesign/RFC.md`](redesign/RFC.md)** — arquitetura da RCL, CSS, estado, interop, segurança,
   acessibilidade, testes, rollout e rollback.
3. **[`nps/PRD.md`](nps/PRD.md)** — produto-alvo do NPS e decisões D1–D12.
4. **[`nps/FLUXOS.md`](nps/FLUXOS.md)** — diferenças entre o contrato v1 e as capacidades de
   backend B1–B15 necessárias ao alvo.
5. **[`nps/index.html`](nps/index.html)** — ponto de entrada do protótipo navegável; o painel e o
   formulário também podem ser abertos diretamente.

Cada documento de plataforma apresenta no topo a decisão solicitada e mantém, ao final, decisões
fechadas, pendências com dono e referências.

## Trilhas e fases

| Fase | Resultado | Regra de contrato |
|---|---|---|
| **F0a · Baseline** | Inventário verificável de rotas, ações, estados e métricas | Sem mudança |
| **F0b · Fundação** | RCL, tokens, shells, temas, componentes essenciais e testes | Sem mudança |
| **F1 · Projetos** | Migração da carteira | Preserva o OpenAPI por padrão |
| **F2 · Saúde** | Dashboard, detalhe e formulário semanal | Preserva o OpenAPI por padrão |
| **F3 · Marcos** | Calendário, agenda e detalhe | Preserva o OpenAPI por padrão |
| **N0 · NPS v1** | Convergência visual sobre as capacidades atuais | Preserva o contrato v1 |
| **N1 · NPS alvo** | Incrementos aprovados do PRD específico | Backend, OpenAPI e rollout próprios |

N0 pode avançar depois da fundação. N1 só começa após Produto e Engenharia aprovarem o recorte de
backend, migrações, compatibilidade e rollout. O protótipo completo não autoriza dados simulados ou
uma capacidade apenas aparente em produção.

## O que há em `nps/`

- **[`nps/index.html`](nps/index.html)** — apresentação e orientação do protótipo.
- **[`nps/painel.html`](nps/painel.html)** — Coleta, Resultados e Respostas em tema claro/escuro.
- **[`nps/formulario.html`](nps/formulario.html)** — formulário público do respondente.
- **[`nps/PRD.md`](nps/PRD.md)** — regras, prioridades e critérios do produto NPS.
- **[`nps/FLUXOS.md`](nps/FLUXOS.md)** — impactos técnicos e de backend por fluxo.
- **[`nps/README.md`](nps/README.md)** — instruções específicas para abrir o pacote estático.

## Fronteiras importantes

- F1–F3 e N0 são frontend-first e preservam o contrato por padrão; uma exceção exige change
  request explícito com compatibilidade, testes e rollback.
- O NPS alvo não é somente front-end: link compartilhado multi-resposta, expiração, escalas,
  dimensões, filtros e outras capacidades exigem mudanças mapeadas em B1–B15.
- O repositório ainda não implementa autenticação/autorização; personas não são permissões.
- “Exportar CSV” em Projetos é atualmente um controle sem comportamento e depende de decisão de
  produto, não de preservação visual.
- Nenhuma alegação de conformidade vale sem ferramenta, versão, cobertura, tema e evidência
  versionada. O alvo é WCAG 2.2 nível AA com automação e verificação manual.
