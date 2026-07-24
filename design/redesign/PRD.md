# PRD: Redesign e convergência da experiência PxOperations

- **Versão:** 2.0
- **Status:** Proposto para aprovação
- **Autor(es):** Matheus Donangelo
- **Audiência:** Produto, Design, Engenharia, CTO
- **Responsável pela decisão:** CTO
- **Última atualização:** 2026-07-24
- **Relacionados:** [RFC de implementação](RFC.md) ·
  [PRD do NPS](../nps/PRD.md) · [Fluxos e impactos de backend do NPS](../nps/FLUXOS.md)

> **Decisão solicitada.** Aprovar o sistema de experiência descrito por G1–G12, a separação
> entre as trilhas Plataforma e NPS, os gates de qualidade e o rollout incremental. Este
> documento define **o que** deve ser entregue e **como o resultado será aceito**. A RFC define
> a implementação.
>
> **Linguagem normativa.** “Deve” e “não deve” indicam requisito de entrega. “Recomenda-se”
> indica uma decisão preferencial que pode ser alterada com justificativa registrada.
>
> **Precedência.** Este PRD governa shell, acessibilidade e padrões compartilhados. O PRD
> específico do NPS governa regras e capacidades NPS. Para os temas transversais, este documento
> fecha duas questões ainda listadas como abertas no NPS §11: espanhol é preservado e valores
> multiseleção usam parâmetros repetidos na URL. As demais decisões de negócio NPS continuam no
> documento específico.

## 1. Resumo executivo

A PxOperations precisa se comportar como um produto único. Hoje, Projetos, Marcos, Saúde de
Projetos e NPS não compartilham integralmente o mesmo shell, os mesmos padrões de interação ou
o mesmo nível de acessibilidade. Os artefatos em `design/nps/` materializam um bom alvo visual,
mas são protótipos HTML/CSS/JS: eles ainda não representam o comportamento do cliente Blazor nem
o contrato de backend disponível.

Esta iniciativa cria uma fundação de interface comum e migra Projetos, Saúde e Marcos sem mudar
as regras de negócio ou a API por padrão. O NPS é dividido em duas entregas explícitas:

1. **Convergência do NPS v1:** aplica shell, tema e componentes compartilhados às capacidades
   que o produto e a API já suportam.
2. **NPS alvo:** implementa o produto descrito no PRD específico do NPS, incluindo as mudanças
   de backend, dados e contrato que forem aprovadas.

Essa separação elimina uma contradição do rascunho anterior: o mockup completo do NPS não pode
ser prometido como uma mudança somente de frontend, pois inclui link compartilhado
multi-resposta, expiração, novas escalas e dimensões, entre outras capacidades ainda ausentes.

## 2. Estado atual e problema

### 2.1 Fatos do baseline

- O cliente real é Blazor WebAssembly e consome clientes HTTP gerados a partir do OpenAPI.
- O shell atual usa navegação superior e uma folha global `app.css`; não há uma biblioteca de
  componentes de produto separada.
- O modo escuro ainda não é uma capacidade transversal do cliente.
- Projetos, Marcos e Saúde usam padrões de filtro, tabela, calendário, formulário e overlay
  construídos em momentos diferentes.
- Os mockups de NPS em `design/nps/` são referências navegáveis; a página Blazor de NPS ainda
  não foi portada para esse alvo.
- O contrato atual do NPS não suporta todas as decisões do mockup. As diferenças e as mudanças
  necessárias estão inventariadas em `design/nps/FLUXOS.md`.
- O botão de exportação da página atual de Projetos não executa uma exportação. Um controle sem
  comportamento não é uma capacidade funcional a ser preservada.
- O repositório não implementa autenticação ou autorização de usuário. As personas deste PRD
  representam contextos de uso, não permissões atualmente aplicadas pelo sistema.

### 2.2 Problema a resolver

A inconsistência atual aumenta o custo para aprender e operar a plataforma, dificulta a leitura
comparativa dos dados e multiplica soluções locais para o mesmo problema. Também torna arriscado
portar o redesign: uma simples troca de CSS pode criar colisões, enquanto copiar o JavaScript dos
protótipos produziria dois donos para o estado da interface.

O redesign deve melhorar a experiência sem:

- mascarar limitações de API com dados simulados;
- remover capacidades que funcionam hoje;
- reproduzir controles atuais que não funcionam;
- criar autorização apenas no cliente;
- trocar uma base global difícil de manter por uma biblioteca genérica maior do que o produto.

## 3. Resultado de produto

### 3.1 Objetivos

- **O1 · Unidade:** os módulos internos compartilham shell, tokens e gramática de interação.
- **O2 · Eficiência:** as tarefas principais exigem menos procura visual e mantêm o contexto ao
  alternar visão, filtro ou período.
- **O3 · Inclusão:** toda jornada em escopo atende WCAG 2.2 nível AA e funciona por teclado,
  toque, zoom e tecnologia assistiva.
- **O4 · Confiança:** estados, métricas e falhas são explícitos; nenhuma ação aparenta ter
  funcionado antes de ser confirmada.
- **O5 · Evolução segura:** cada página pode ser entregue e revertida independentemente, com
  paridade comprovada e sem quebrar o contrato HTTP.

### 3.2 Não objetivos

- Reescrever regras de domínio, agregados, casos de uso ou persistência na trilha Plataforma.
- Criar login, papéis ou regras de autorização.
- Prometer o NPS alvo sem aprovar suas dependências de backend.
- Fazer todas as páginas terem o mesmo layout quando a tarefa pede outra representação.
- Criar um framework genérico de tabela, gráfico ou formulário antes de haver usos reais.
- Substituir pesquisa com usuários por aprovação visual de screenshots.

## 4. Escopo e fronteiras

### 4.1 Trilhas de entrega

| Trilha | Incluído | Contrato de backend |
|---|---|---|
| **F0 · Fundação** | Baseline verificável, design tokens, shell interno e público, temas, componentes essenciais e infraestrutura de teste | Sem mudança |
| **F1 · Projetos** | Lista, Kanban, Renovações, indicadores, formulários e ações que funcionam no baseline | Sem mudança por padrão |
| **F2 · Saúde** | Resumo da carteira, visão por projeto e formulário semanal do líder | Sem mudança por padrão |
| **F3 · Marcos** | Calendário semanal/mensal, navegação temporal, filtros e detalhe | Sem mudança por padrão |
| **N0 · NPS v1 convergente** | Capacidades atuais do NPS dentro do shell, tema e componentes comuns | Preserva o contrato v1 |
| **N1 · NPS alvo** | Capacidades aprovadas no PRD específico do NPS | Inclui os itens B1–B15 aprovados; entrega separada |

N0 pode avançar depois de F0. N1 só inicia quando Produto e Engenharia fecharem o recorte de
backend, migrações, compatibilidade e rollout. Um protótipo visual não autoriza a simulação de
uma capacidade N1 em produção.

### 4.2 Regra para exceções de backend

Se uma jornada F1–F3 depender de dado, comando ou política ausente no JSON atual, a página não
deve inventar esse dado nem usar o CSV como API informal. Deve ser aberto um change request
`CR-BE` contendo:

1. problema de produto e jornada afetada;
2. alteração de contrato proposta e compatibilidade;
3. regra de domínio, segurança e migração, quando aplicável;
4. testes e plano de rollback;
5. atualização deste PRD, da RFC e do OpenAPI.

O `CR-BE` exige aprovação explícita. As capacidades N1 não são “exceções” escondidas nessa
regra; elas pertencem à trilha NPS e já têm inventário próprio.

## 5. Pessoas e acesso

| Pessoa | Necessidade principal | Jornadas |
|---|---|---|
| **Operação / PMO** | Manter a carteira, acompanhar marcos e operar coletas | Projetos, Marcos, NPS |
| **Delivery Manager** | Acompanhar projetos e registrar saúde semanal | Projetos, Saúde |
| **Liderança** | Ler indicadores, tendências e detalhes confiáveis | Projetos, Saúde, NPS |
| **Líder de projeto** | Preencher a reflexão semanal do projeto | Formulário de Saúde |
| **Respondente externo** | Responder uma coleta com clareza e privacidade | Formulário público do NPS |

O redesign deve preservar a exposição atual de ações até existir autorização no servidor.
Quando autorização for criada em outra iniciativa, o servidor será a fonte de verdade; esconder
um controle no cliente será apenas uma melhoria de experiência, nunca uma barreira de segurança.
O formulário público do NPS não deve exibir a navegação interna.

## 6. Princípios de experiência G1–G12

- **G1 · Fundação única.** Cores, tipografia, espaçamento, elevação, movimento e estados vêm de
  tokens semânticos versionados. Valores de marca não são espalhados por páginas.
- **G2 · Shell coerente.** Módulos internos usam sidebar responsiva, indicação de rota ativa,
  atalho para o conteúdo e cabeçalho de página previsível. Jornadas públicas usam um shell
  reduzido com a mesma identidade, sem navegação interna.
- **G3 · Tema é preferência do usuário.** Claro e escuro têm paridade funcional e de contraste.
  A escolha explícita persiste; sem escolha, vale a preferência do sistema. O primeiro frame não
  deve piscar no tema incorreto.
- **G4 · Filtros preservam contexto.** Busca, facetas, chips e visão têm semântica estável,
  estado compartilhável na URL e ação clara para limpar. Dentro de uma faceta, valores combinam
  por **OU**; entre facetas, por **E**.
- **G5 · Indicadores são auditáveis.** Todo KPI tem rótulo, unidade, período/escopo, fonte/fórmula
  e estado de ausência. Regra de negócio vem do servidor; a UI só deriva contagem simples do
  conjunto carregado quando essa fórmula está documentada.
- **G6 · Dados são escaneáveis e detalháveis.** Tabelas usam HTML semântico; cartões e calendários
  não substituem uma representação acessível. O detalhe mantém o contexto de origem.
- **G7 · Estado não depende só de cor.** RAG, prazo, classificação, sucesso e erro combinam texto
  com forma ou ícone. Rótulos usam o vocabulário do produto.
- **G8 · Overlays são previsíveis.** Diálogos têm título, foco inicial deliberado, fechamento por
  Escape quando seguro, contenção de foco, retorno ao gatilho e ação destrutiva explícita.
- **G9 · WCAG 2.2 AA é o piso.** Teclado, foco não encoberto, reflow, contraste, nome/papel/valor,
  alternativa a arrastar e alvos de toque conformes são requisitos de aceite.
- **G10 · Conteúdo é pt-BR por padrão.** Voz ativa, termos consistentes, datas e números
  localizados, `lang` correto e título único por rota. Idiomas já suportados pelo formulário
  público do NPS não podem regredir.
- **G11 · Paridade vale para comportamento real.** Toda capacidade comprovadamente funcional no
  baseline continua disponível. Controles sem implementação não contam como capacidade e não
  devem ser clonados sem uma decisão de produto.
- **G12 · Consistência sem abstração prematura.** O mesmo problema usa o mesmo componente e a
  mesma interação. Diferenças de tarefa continuam específicas da feature; algo só vira abstração
  genérica quando sua API permanece simples em usos reais.

## 7. Jornadas e critérios de aceite

### 7.1 Navegar e escolher o tema

**Fluxo.** A pessoa abre uma rota diretamente, identifica o módulo ativo, pula para o conteúdo,
navega entre módulos e escolhe tema claro ou escuro. O botão voltar/avançar do navegador conserva
rota, visão e filtros.

**Aceite.**

- [ ] A sidebar lista Projetos, Marcos, Saúde e NPS, marca a rota ativa e recolhe em viewport
      estreito sem encobrir o foco.
- [ ] Existe um link “Pular para o conteúdo” visível ao receber foco.
- [ ] A ordem de tabulação acompanha a ordem visual e não cria armadilha.
- [ ] A escolha de tema persiste; ausência ou falha de armazenamento usa
      `prefers-color-scheme`.
- [ ] O tema correto é aplicado antes da primeira pintura útil, sem script inline incompatível
      com a política de segurança.
- [ ] A rota pública `/nps/{token}` usa o shell público e não revela navegação interna.

### 7.2 Buscar, filtrar e alternar visões

**Fluxo.** A pessoa digita uma busca, abre o painel de filtros, seleciona valores e vê chips
removíveis. O resultado e a URL são atualizados. Recarregar ou compartilhar a URL restaura o
mesmo estado válido.

**Aceite.**

- [ ] Dois valores da mesma faceta produzem união; facetas diferentes produzem interseção.
- [ ] O botão de filtro expõe `aria-expanded`, o número de facetas ativas e o painel associado.
- [ ] Checkboxes e campos mantêm sua semântica nativa; o painel não usa `role="menu"` para
      controles de formulário.
- [ ] Remover um chip altera somente sua faceta; “Limpar tudo” remove busca, filtros e parâmetros
      correspondentes.
- [ ] Busca, visão, período e facetas ficam em query string canônica; valores múltiplos usam
      parâmetros repetidos, não uma string separada por vírgulas.
- [ ] Parâmetros desconhecidos ou valores obsoletos são ignorados com segurança.
- [ ] Alterações anunciam a contagem de resultados sem deslocar o foco.

### 7.3 Operar a carteira de Projetos

**Fluxo.** Operação ou Delivery abre `/projects`, lê os indicadores e o Weekly Pulse, alterna
entre Lista, Kanban e Renovações, filtra, abre detalhes, cria ou edita e move um projeto de
status.

**Aceite.**

- [ ] As três visões usam o mesmo conjunto filtrado e preservam o estado ao alternar.
- [ ] O Weekly Pulse continua disponível, recolhível e coerente com o conjunto/período que seu
      rótulo declara.
- [ ] Lista e detalhe cobrem todos os campos e ações funcionais registrados no baseline de F0.
- [ ] Arrastar um cartão altera status somente após confirmação da API; falha restaura posição,
      explica o erro e permite tentar novamente.
- [ ] Toda ação de arrastar também pode ser concluída por teclado por uma ação “Mover para…”.
- [ ] Alertas de renovação usam texto e ícone além de cor.
- [ ] Criar, editar, cancelar e validar preservam as regras e mensagens observáveis hoje.
- [ ] O redesign não exibe “Exportar CSV” como ação vazia. A decisão de implementar ou remover
      o controle deve ser fechada antes da saída de F1.

### 7.4 Ler e navegar Marcos

**Fluxo.** A pessoa abre `/milestones`, alterna Semana/Mês, muda a data de referência, filtra e
abre o detalhe de um marco.

**Aceite.**

- [ ] Alternar visão conserva filtros e uma data de referência coerente.
- [ ] Tipo, projeto, data e estado do marco são perceptíveis sem depender apenas de cor.
- [ ] Controles de período e itens são operáveis por teclado e têm nomes acessíveis.
- [ ] Em largura estreita, uma visão de agenda/lista preserva informação e ações sem exigir uma
      grade ilegível em duas dimensões.
- [ ] Datas respeitam locale pt-BR e não mudam de dia por conversão implícita de fuso horário.

### 7.5 Ler e registrar Saúde de Projetos

**Fluxo de leitura.** Liderança escolhe a semana, lê o resumo da carteira e abre um projeto.
**Fluxo de registro.** O líder percorre as cinco dimensões atuais — práticas, escopo,
cronograma, qualidade e relacionamento —, revisa e envia.

**Aceite.**

- [ ] A interface declara a semana selecionada e distingue snapshot semanal de visão histórica.
- [ ] O resumo usa a definição de carteira do backend: projetos ativos em `InProgress`.
- [ ] Resumo e detalhe não divergem por um recálculo paralelo no cliente.
- [ ] RAG e score sempre combinam rótulo, ícone/forma e cor.
- [ ] O formulário informa dimensão e progresso, associa erros aos campos e move o foco para o
      resumo de erro no envio inválido.
- [ ] Recarregar uma submissão concluída não provoca envio duplicado.
- [ ] O formulário de Saúde e o formulário público do NPS podem compartilhar primitivas visuais,
      mas não compartilham estado ou regra de domínio por pressuposto.

### 7.6 Convergir o NPS sem prometer backend inexistente

**N0.** A página Blazor recebe shell, tema e componentes comuns mantendo o contrato e as regras
do NPS v1. Estados não suportados pela API não aparecem como se fossem reais.

**N1.** O fluxo completo segue `design/nps/PRD.md` e só é aceito com suas capacidades de backend
entregues. Entre as diferenças que impedem equivalência apenas visual estão:

- token compartilhado multi-resposta versus token atual de uso único;
- validade de 20 dias e recusa de link expirado;
- escalas, nomes e significado das dimensões;
- leitura JSON de respostas por projeto e formato explícito;
- dispensa de coleta e filtros adicionais/multivalorados.

**Aceite.**

- [ ] N0 não altera silenciosamente a semântica dos dados existentes.
- [ ] Cada item visual de N1 tem rastreabilidade até uma decisão D, capacidade F e dependência B
      do PRD específico.
- [ ] Cliente novo e API antiga, e API nova com cliente anterior, têm comportamento de
      compatibilidade documentado antes do rollout N1.
- [ ] O formulário público preserva os idiomas suportados, não carrega recursos de terceiros e
      não expõe o token em logs de cliente.

## 8. Requisitos transversais

### 8.1 Acessibilidade e responsividade

- Conformidade alvo: **WCAG 2.2 AA** para páginas completas e todos os estados em escopo.
- Reflow sem perda a 320 CSS px, texto a 200% e zoom a 400%, exceto conteúdo genuinamente
  bidimensional; nesse caso deve existir uma alternativa utilizável.
- Alvo mínimo conforme WCAG para controles: 24 × 24 CSS px ou espaçamento equivalente. Para
  ações primárias de toque, o alvo de design é 44 × 44 CSS px.
- Foco visível e não encoberto por header, sidebar, toast ou diálogo.
- `prefers-reduced-motion` desativa movimento não essencial; conteúdo não depende de animação.
- Verificação automatizada não é prova de conformidade. O gate combina automação, teclado,
  zoom, contraste e teste com leitor de tela.

### 8.2 Estados, erros e integridade

Toda superfície de dados deve ter estados distintos de carregamento, vazio, erro recuperável,
sem permissão (quando vier do servidor) e sucesso. Uma requisição obsoleta não pode substituir
resposta mais recente. Ações mutáveis devem:

1. indicar progresso sem apagar o contexto;
2. impedir duplo envio quando necessário;
3. confirmar sucesso somente após resposta válida;
4. manter ou restaurar o estado anterior em falha;
5. fornecer nova tentativa quando segura.

### 8.3 Desempenho

- F0 registra baseline de carregamento, interação, estabilidade visual e bytes transferidos.
- Alvo de campo no percentil 75, separado entre mobile e desktop:
  **LCP ≤ 2,5 s**, **INP ≤ 200 ms** e **CLS ≤ 0,1**.
- Enquanto não houver volume de medição de campo, o release usa cenário de laboratório
  versionado e compara com o baseline.
- Nenhuma fase pode aumentar em mais de 10% os bytes comprimidos iniciais ou piorar uma Core Web
  Vital sem exceção aprovada, causa e plano de correção.

### 8.4 Segurança, privacidade e conteúdo externo

- Tema é o único estado F0–F3/N0 permitido em `localStorage`; nenhum token, dado de projeto ou
  resposta de NPS deve ser persistido ali. N1 só pode adicionar um marcador opaco anti-replay,
  sem dado pessoal ou token recuperável, se o desenho de segurança B4 o aprovar.
- A política CSP atual deve continuar efetiva; não se adiciona `unsafe-inline` para viabilizar
  tema ou componente.
- Rotas públicas com token usam `Referrer-Policy: no-referrer`; logs, traces e analytics não
  armazenam o token bruto. F0a deve comprovar a redação antes de N0/N1.
- Fontes, ícones e scripts de produção devem ser próprios e servidos pela aplicação. Se a licença
  de uma fonte de marca não estiver comprovada, usa-se a pilha de sistema.
- Conteúdo vindo da API é tratado como texto por padrão; HTML bruto exige sanitização e revisão
  explícita.

## 9. Métricas e gates de release

Cada fase guarda sua evidência em `design/redesign/evidence/<fase>/` ou em artefato equivalente
do CI, com link no PR.

| Dimensão | Gate | Evidência mínima |
|---|---|---|
| **Paridade** | 100% das capacidades funcionais inventariadas passam; nenhum controle sem ação | Matriz baseline → teste/critério |
| **Efetividade** | ≥ 90% de conclusão sem ajuda nas tarefas críticas do piloto e nenhuma regressão frente ao baseline | Roteiro, participantes representativos e síntese |
| **Acessibilidade automática** | 0 violação crítica ou séria; demais achados resolvidos ou justificados como falso positivo | Relatório versionado com ferramenta/versão |
| **Acessibilidade manual** | Checklist WCAG aplicável concluído para teclado, zoom, contraste e leitor de tela | Matriz por página e ambiente |
| **Responsividade** | Sem perda funcional em 320, 375, 768, 1024 e 1440 CSS px | Capturas e testes de interação |
| **Desempenho** | Metas da §8.3 ou exceção aprovada; sem regressão de orçamento | Relatório de laboratório e, quando disponível, campo |
| **Confiabilidade** | Jornadas críticas passam sem erro de console não tratado ou requisição duplicada | Testes end-to-end e logs do job |
| **Consistência** | Página migrada não depende de seletor legado da feature | Busca estática + revisão |

Não pode haver violação WCAG A/AA conhecida. “0 falha de acessibilidade” sem ferramenta, versão,
URL, tema e relatório não conta como evidência. Aprovação visual isolada também não substitui os
gates.

## 10. Fases e critérios de saída

| Fase | Entrega | Critério de saída |
|---|---|---|
| **F0a · Baseline** | Inventário por rota, estados e ações; identifica no-op, dívida, API e métricas atuais | Produto e Engenharia assinam a matriz de paridade e os baselines |
| **F0b · Fundação** | Shells, temas, tokens, componentes essenciais, testes e convivência controlada com legado | Todas as rotas antigas continuam operáveis; gates transversais da fundação passam |
| **F1 · Projetos** | Migração completa da carteira | Jornada §7.3 e decisão de CSV fechadas; seção legada removida |
| **F2 · Saúde** | Dashboard, detalhe e formulário | Jornada §7.5 passa; sem divergência do resumo da carteira |
| **F3 · Marcos** | Calendário e detalhe | Jornada §7.4 passa, inclusive agenda estreita |
| **N0 · NPS v1** | Convergência visual sobre o contrato atual | Jornada N0 passa e não simula estados N1 |
| **N1 · NPS alvo** | Incrementos aprovados do PRD NPS | Go/no-go próprio para backend, migração, compatibilidade e cliente |

Uma fase é concluída quando está implantável e reversível sozinha. Não é necessário esperar todas
as páginas para remover CSS ou componente legado que só atendia à fase concluída.

## 11. Riscos e mitigação

| Risco | Sinal precoce | Mitigação |
|---|---|---|
| Mockup confundido com contrato disponível | UI depende de campo inexistente ou usa dado fixo | Rastreabilidade NPS D/F/B e regra `CR-BE` |
| Colisão entre CSS novo e legado | Mudança fora da página migrada | Escopo, namespace e camadas definidos na RFC; testes visuais |
| Paridade baseada em memória | Ação descoberta apenas em homologação | Inventário F0a derivado do código e validado por Produto |
| Componente genérico cresce antes do segundo uso | Muitos parâmetros condicionais por feature | Manter composição na feature; promover abstração depois de uso real |
| “Permissão” apenas visual | Controle oculto, endpoint ainda acessível | Não modelar autorização no redesign; servidor será fonte de verdade |
| Calendário, gráfico ou drag-and-drop inacessível | Tarefa depende de ponteiro/cor | Agenda/tabela alternativa e ação “Mover para…” |
| Fonte externa afeta privacidade ou CLS | Requisição a terceiro ou troca de fonte tardia | Auto-hospedagem licenciada ou pilha de sistema |

## 12. Decisões e pendências

### 12.1 Decisões fechadas

- G1–G12 são requisitos de plataforma.
- F1–F3 são frontend-first e preservam o OpenAPI por padrão.
- N0 e N1 são entregas diferentes; o mockup completo pertence a N1.
- O shell público do NPS não exibe navegação interna.
- Estado compartilhável usa query string; valores múltiplos usam parâmetros repetidos.
- Paridade cobre comportamento funcional, não controles no-op.
- Autorização não será inventada no cliente.
- Acessibilidade exige evidência automática e manual.

### 12.2 Pendências que exigem dono

| # | Decisão | Recomendação | Dono | Prazo / bloqueio |
|---|---|---|---|---|
| **P1** | O que fazer com “Exportar CSV” em Projetos | Remover até haver conteúdo, colunas e codificação aprovados; se aprovado, implementar e testar como feature | Produto + CTO | Antes da saída de F1; não bloqueia F0 |
| **P2** | Qual recorte N1 será financiado primeiro | Usar os incrementos do PRD NPS e começar somente pelo menor fluxo ponta a ponta com backend real | Produto + Engenharia | Antes de iniciar N1 |
| **P3** | Fonte de marca para produção | Usar pilha de sistema até licença e arquivos WOFF2 estarem registrados | Design + Jurídico/CTO | Antes de trocar a tipografia de produção |

Compartilhar o formulário de Saúde e o formulário público de NPS como um único componente de
domínio **não é pendência**: eles compartilham primitivas de apresentação, mas permanecem
componentes de feature separados enquanto regras, escalas e submissão forem diferentes.

## 13. Definition of Done por página

Uma página só está pronta quando:

- [ ] matriz de baseline e critérios desta jornada estão aprovados;
- [ ] estados de loading, vazio, erro, sucesso e dados ausentes estão implementados;
- [ ] testes unitários/de componente e jornadas end-to-end passam;
- [ ] claro, escuro, larguras-alvo e impressão aplicável foram revisados;
- [ ] gates automático e manual de acessibilidade têm evidência;
- [ ] orçamento e Core Web Vitals foram comparados ao baseline;
- [ ] nenhuma regra de negócio foi duplicada no cliente;
- [ ] CSS e componentes legados exclusivos da página foram removidos;
- [ ] alteração de contrato, se houver, seguiu `CR-BE`;
- [ ] rollout, monitoramento e rollback foram registrados no PR.

## 14. Rastreabilidade

| Requisito | Especificação técnica | Gate principal |
|---|---|---|
| G1–G3 | RFC §§4–5 | Consistência, contraste, tema sem flash |
| G4 | RFC §6 | Testes do codec de URL e filtros |
| G5–G7 | RFC §§6–7 | Paridade, semântica e acessibilidade |
| G8 | RFC §6.5 | Teclado, foco, Escape e retorno |
| G9 | RFC §9 | Automação + matriz manual |
| G10 | RFC §§7 e 9 | Conteúdo, locale e idiomas |
| G11 | RFC §§2 e 11 | Matriz baseline → teste |
| G12 | RFC §§3–4 | Revisão de limites RCL/feature |

## 15. Referências

- [RFC técnica desta iniciativa](RFC.md)
- [PRD específico do NPS](../nps/PRD.md)
- [Fluxos e mudanças de backend do NPS](../nps/FLUXOS.md)
- Baseline do cliente:
  `src/Client/PxOperations.BlazorWasm/Features/`,
  `src/Client/PxOperations.BlazorWasm/Layout/` e
  `src/Client/PxOperations.BlazorWasm/wwwroot/css/app.css`
- [WCAG 2.2, W3C](https://www.w3.org/TR/WCAG22/)
- [WAI-ARIA Authoring Practices: Modal Dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/)
- [Core Web Vitals](https://web.dev/articles/vitals)
