# Redesign · Operations PX (BRQ) — material para decisão do CTO

Este diretório reúne tudo que é preciso para decidir **como seguir com a implementação do
redesign**. São dois pacotes: a proposta para a plataforma inteira e a prova de conceito já pronta
(o NPS).

## 1. A proposta (plataforma)

- **`redesign/PRD.md`** — o **quê** e o **porquê**: as diretrizes G1 a G12 para redesenhar as
  páginas antigas (Projetos, Marcos, Saúde) no design system BRQ. Só front-end.
- **`redesign/RFC.md`** — o **como** técnico: design system numa Razor Class Library, máximo de
  Blazor, modo escuro firme, gráficos em Blazor, migração incremental. Sem mudança de backend.

Cada documento tem no topo a **decisão solicitada** e, no fim, o que já foi decidido e o que
segue em aberto.

## 2. A referência já pronta (NPS)

A pasta **`nps/`** é **autossuficiente**: abre no navegador, offline, sem servidor. É a prova de
que o alvo funciona.

- **`nps/index.html`** — capa: o que muda e como revisar (ponto de entrada).
- **`nps/painel.html`** — o painel navegável (Coleta · Resultados · Respostas), com modo escuro.
- **`nps/formulario.html`** — o formulário público do respondente.
- **`nps/PRD.md`** e **`nps/FLUXOS.md`** — as decisões e o mapeamento técnico do NPS, que a
  proposta da plataforma generaliza.

## Como navegar

Comece pelo **`redesign/PRD.md`** (a proposta), use o **`nps/`** como evidência do padrão-alvo, e
veja o **`redesign/RFC.md`** para a viabilidade técnica. Tudo é front-end; nenhuma decisão aqui
exige mudança de backend.
