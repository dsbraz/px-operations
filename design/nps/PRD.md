# PRD: Redesign do módulo de NPS · Operations PX

| | |
|---|---|
| **Status** | Rascunho para revisão |
| **Data** | 21/07/2026 · revisado em 23/07/2026 |
| **Autor** | Matheus Donangelo |
| **Revisor-alvo** | CTO |
| **Decisão solicitada** | Aprovar (a) o modelo de link compartilhado (D1) e as capacidades B1 a B4; (b) o corte de fases da §8; (c) o destino do histórico em B13/B14 |
| **Artefatos** | Pasta autossuficiente `design/nps/` (abre sem servidor, carrega a própria `assets/`): capa `index.html`, painel navegável `painel.html`, formulário público `formulario.html`, este `PRD.md` e o detalhamento técnico `FLUXOS.md` |
| **Escopo do documento** | O **quê** e o **porquê** das funcionalidades propostas. O **como** (arquivos, esquema, código) fica no FLUXOS.md, este PRD não é uma RFC. |

---

## 1. Resumo executivo

O módulo de NPS existe e funciona (v1): backend completo de disparos, links e respostas + uma
página única em `/nps` com lista de projetos, KPIs, geração de link e detalhe. O redesign não
recomeça nada: **reorganiza o módulo em três subpáginas com URL própria** (Coleta · Resultados ·
Respostas), cada uma com um trabalho claro: *operar a coleta*, *ler os números*, *auditar as
respostas*.

A mudança de fundo é de produto: assumir a **coleta por link compartilhado**: um link por
rodada, colado numa mensagem para o grupo do cliente, respondido por N pessoas, anônimo (D1).
O único bloqueio real para isso é que hoje **todo link aceita uma única resposta, inclusive o
genérico**. Destravar são quatro capacidades pequenas e localizadas no backend (B1 a B4, uma delas
com migração de dados). O restante do redesign se apoia no backend existente ou em ajustes
pequenos já mapeados (B5 a B7); só as médias por dimensão (B11) são agregação nova.

## 2. Contexto: o que existe hoje e onde dói

**Baseline (v1, em produção no branch principal):** uma página única `/nps` com KPIs (NPS
oficial, respostas, vencidos, promotores), filtros de DC e tipo, tabela de projetos, modal de
gerar link (projeto + formato + idioma) com cópia da URL e modal de detalhe com histórico de
links e respostas do projeto. O cliente já gera **apenas o link genérico**: o produto já aponta
para o modelo "um link por rodada".

**Lacunas que o redesign ataca:**

1. **O link genérico aceita só 1 resposta.** O fluxo real ("envio um link ao grupo, várias
   pessoas respondem") quebra na segunda resposta: o formulário fecha como "já respondido".
2. **Não há visão de trabalho.** A tabela não responde às perguntas do dia a dia do operador:
   onde falta link? quem cobrar? o que venceu?: hoje isso é varredura manual linha a linha.
3. **Leitura rasa dos resultados.** O número por projeto existe, mas não dá para abrir e ver as
   notas que o compõem, nem um feed de respostas com busca.
4. **Não existe saída para projeto sem coleta.** Projeto em que NPS não se aplica fica contando
   como "vencido" para sempre, e não há como dizer "aqui não se aplica" sem perder o projeto de
   vista.

## 3. Usuários e papéis

| Papel | Uso do módulo | Subpágina principal |
|---|---|---|
| **Operador da coleta** (PMO/operações) | Gera links, envia mensagens, cobra projetos parados, dispensa coleta onde não se aplica | Coleta |
| **Delivery Manager** | Acompanha os projetos sob sua gestão; é o nome exibido no card como responsável | Coleta / Resultados |
| **Liderança** (diretoria, CTO) | Lê score, distribuição, dimensões e drill-down por projeto | Resultados |
| **Respondente** (cliente externo) | Recebe o link, responde anônimo em < 1 minuto | Formulário público |

## 4. Objetivos e não-objetivos

**Objetivos**

1. O fluxo "gerar link → colar mensagem → grupo responde" funcionar de ponta a ponta sem atrito (D1).
2. O operador enxergar a carteira por estágio de coleta e agir de dentro do quadro.
3. O gestor descer do score agregado até a nota individual sem sair da tela.
4. Projetos sem coleta aplicável saírem da régua de cobrança de forma explícita e reversível.

**Não-objetivos (desta fase)**

- Rastreio individual "quem respondeu / quem falta" (links por contato seguem existindo no
  backend; nudge individual fica como evolução: §10).
- Editor/persistência de template de mensagem no backend (a mensagem sugerida é montada no cliente).
- Automação de envio (e-mail/WhatsApp): o envio é humano; o produto prepara link + mensagem.
- Registro formal de lembretes/cobranças (mapeado como B9, fora das fases 1 e 2).

## 5. Decisões de produto já tomadas

| # | Decisão | Racional | Consequência |
|---|---|---|---|
| D1 | **Link compartilhado (multi-resposta)** é o modelo padrão de coleta | O acompanhamento é por projeto ("tem resposta? cobrar"), não por pessoa; um link + uma mensagem é o menor atrito | Requer B1 a B4; links por contato continuam de uso único |
| D2 | **"Taxa de resposta %" aposentada** | Link aberto não tem denominador confiável | Acompanhar por contagem absoluta + estágio no quadro; B8 (`~X pessoas`) é a válvula de escape se sentirem falta |
| D3 | Um projeto pode ter **dois links ativos**, um **Completo** e um **Simplificado** | Formatos atendem públicos diferentes; respostas somam no mesmo projeto | Toda tela que mostra "o link" exibe 1 ou 2, rotulados por formato; gera-se um formato por vez |
| D4 | Resposta **anônima por padrão**, nome/e-mail opcionais | Mais respostas e mais sinceras | Telas só exibem atribuição quando informada |
| D5 | **Cada subpágina é uma URL** (deep link + histórico) | 3 trabalhos distintos ≠ 1 página com estado de aba; permite compartilhar "a visão certa" | Rotas `/nps/coleta` · `/nps/resultados` · `/nps/respostas` (`/nps` redireciona) |
| D6 | Ordem de exibição das classes: **detrator → neutro → promotor** | Leitura orientada a ação: problema primeiro | Vale na distribuição e no drill-down |
| D7 | **Link vale 20 dias** contados da geração, e o prazo é exibido | Sem validade o link vira um canal aberto sem dono; com prazo à vista o operador sabe quando agir e o respondente sabe até quando responder | A expiração aparece no modal de geração, na mensagem sugerida, no card do quadro, **no detalhe da coleta (por link, com destaque quando falta pouco)** e no formulário público |
| D8 | **Link expirado não se reaproveita**: gera-se um novo, e o card volta para *Aguardando resposta* | Reabrir um link vencido confunde o histórico da rodada; cada rodada tem o seu link | O card expirado troca a ação de "Cobrar" para "Gerar novo link"; gerar recoloca o card em *Aguardando resposta* com o prazo zerado |
| D9 | **O NPS não tem o conceito de "projeto encerrado"**. Existe um estado só, **coleta dispensada**, marcado à mão e **reversível** a qualquer momento | Ocultar por status ou data de projeto some com projeto cujo NPS ainda precisa de acompanhamento, e quem decide se a coleta se aplica é a pessoa, não uma regra de calendário | O quadro mostra a carteira inteira; a dispensa é a única coisa que tira card da vista, e o card dispensado traz a ação de reativar (F6) |
| D10 | **Escalas distintas por tipo de pergunta**: NPS de **1 a 10**, aspectos da entrega de **1 a 5** | A nota de recomendação e a avaliação de aspecto são perguntas de natureza diferente; escala curta reduz o esforço nos quatro aspectos | Muda a régua de classificação (detrator passa a ser 1 a 6) e a agregação por aspecto |
| D11 | **Barra de filtros = busca + um botão "Filtro"** (não fileira de dropdowns), com **multi-seleção** por faceta | Seis dropdowns sempre à vista poluem a tela e não informam nada quando estão em "Todos"; o padrão de dashboards maduros (Linear, Vercel, Shopify) é esconder no menu e mostrar só o que está ligado, como chip | Facetas de lista aceitam vários valores; as consultas de backend (B15) precisam receber listas, não valor único; período e dispensa seguem single |
| D12 | **Respostas é uma tabela, não uma lista solta**, com abertura de cada resposta para o detalhe completo | Em lista corrida era difícil ler rápido e saber de qual projeto era cada resposta; comentário longo estourava o layout | Tabela com colunas fixas + comentário só em prévia; modal de detalhe com nota, aspectos (Completo), comentário inteiro e atribuição; filtro por data nessa visão |

**Regras de negócio de referência.** Nota de NPS de **1 a 10** (D10): Promotor = 9 ou 10 ·
Neutro = 7 ou 8 · Detrator = 1 a 6 · **NPS = %promotores − %detratores** (1 casa decimal).
**Completo** = nota + 4 aspectos + comentário; **Simplificado** = nota + comentário. Os quatro
aspectos, avaliados de **1 a 5** e todos opcionais, são:

1. **Qualidade técnica da entrega** (estabilidade, poucos defeitos, soluções bem construídas)
2. **Aderência aos prazos acordados** (combinados cumpridos e desvios avisados a tempo)
3. **Comunicação, clareza e transparência** (informação clara, na hora certa, sem surpresa)
4. **Valor gerado para o negócio** (o quanto a entrega ajudou no resultado do cliente)

O NPS agrega os dois formatos; os aspectos só existem no Completo. **Vencido** = projeto sem
disparo aberto **e** sem resposta nos últimos 90 dias. **Link expirado** = 20 dias após a
geração (D7). Idiomas suportados: português, inglês e espanhol.

> **Atenção:** a faixa de notas e os dois primeiros itens acima mudam o que hoje está
> implementado (o backend valida 0 a 10 e guarda `Scope`, `Schedule`, `Quality`,
> `Communication` de 0 a 10). Ver B12 a B15 na §7.

## 6. Requisitos funcionais

Prioridade: **Must** (sem isso o redesign não cumpre o objetivo) · **Should** (completa a
proposta) · **Could** (por demanda). Dependências de backend referenciam a §7.

### Navegação e base

- **F1 · Must: Três subpáginas com URL própria** (D5), abas compartilhadas e **uma barra de
  filtros enxuta**: apenas **busca livre + um botão "Filtro"** (padrão Linear/Vercel), não uma
  fileira de dropdowns sempre à vista. O botão abre um menu de duas camadas (faceta → valores);
  o que está ligado vira **chip removível** abaixo da barra, e o botão mostra um contador de
  filtros ativos. Facetas: empresa, DC, tipo, DM, data de coleta, mais as específicas de cada
  aba (status em Resultados; formato e classificação em Respostas; coletas dispensadas em
  Coleta), todas no mesmo menu, oferecidas conforme a aba ativa, sem poluir a barra quando não
  estão em uso.
  - **Multi-seleção (D11):** empresa, DC, tipo, DM, status, formato e classificação aceitam
    **vários valores** (ex.: empresa = Santander + Itaú); o chip junta os valores e o menu mantém
    o painel aberto para marcar vários de uma vez. **Data de coleta** é single (é um intervalo) e
    **coletas dispensadas** é um toggle Ocultar/Mostrar.
  - **Data de coleta:** períodos prontos (30 / 90 dias, 6 / 12 meses) e intervalo livre; a régua
    é a **data da resposta**, então a faceta só é oferecida em Resultados e Respostas, **não na
    Coleta** (lá esvaziaria justamente as colunas de quem ainda não respondeu, que é o trabalho
    da tela).

  Ações de página: **Exportar CSV** (F11) e **Gerar link** (F3).
  *Critérios de aceite:* abrir `/nps/resultados` direto carrega a subpágina certa; voltar/avançar
  do navegador transita entre subpáginas; filtros persistem ao trocar de subpágina; marcar dois
  valores da mesma faceta filtra pela união deles; o botão "Filtro" exibe o número de facetas
  ativas e "Limpar tudo" zera todas.

### Coleta

- **F2 · Must: Quadro Kanban por estágio de coleta**, com colunas definidas por regra objetiva
  (dados que a API já entrega):

  | Coluna | Regra | Ação esperada |
  |---|---|---|
  | Sem link | Sem disparo aberto | Gerar link |
  | Aguardando resposta | Disparo aberto e 0 respostas | Cobrar (recopiar link/mensagem) |
  | Recoleta | Última resposta há mais de 45 dias | Nova rodada |
  | Em dia | Última resposta há 45 dias ou menos | nenhuma |

  **Link expirado (D7/D8)** é um estado dentro de *Aguardando resposta*, não uma coluna: passados
  os 20 dias sem resposta, o card troca o chip de prazo ("expira em 3d") por "link expirado há
  Xd" em vermelho e a ação de "Cobrar" para **"Gerar novo link"**. Ele fica no topo da coluna.
  A razão de não virar coluna própria: a história é a mesma ("mandei e ninguém respondeu"), só o
  desfecho mudou, e uma coluna que passa a maior parte do tempo vazia lê como defeito. Se o
  volume de links vencidos sem resposta se mostrar alto na operação real, promover a coluna é
  barato.

  Card: projeto, cliente · DC, nº de respostas, o elemento temporal do estágio, DM e a ação.
  **O temporal do card tem dois tipos, deliberadamente diferentes** para não se confundirem:
  **(a) prazo do link** (Aguardando), um alerta acionável olhando para frente, com ícone de ampulheta,
  neutro com folga e **badge âmbar/vermelho** quando aperta ou vence; **(b) recência da última
  coleta** (Recoleta/Em dia), contexto olhando para trás, com ícone de histórico, **sempre neutro**,
  sem cor de alarme (a coluna já carrega a urgência). Só o prazo pode virar badge de alarme; a
  recência é sempre texto discreto. (Sem link mostra a idade "sem link há Xd", também neutra.)
  **Nada é ocultado por status ou data de projeto** (D9): a carteira inteira aparece no quadro, e
  a única ocultação é a **coleta dispensada** (F6), que volta com o toggle "Mostrar dispensados".
  Faixa de KPIs no topo para contexto.
  *Critérios de aceite:* todo projeto da carteira aparece em exatamente uma coluna; card sem
  link tem ação "Gerar link"; card com link expirado tem ação "Gerar novo link"; prazo e recência
  são distinguíveis à primeira vista (ícone e tratamento diferentes); card com link abre o detalhe
  (F5); gerar link a partir de qualquer card leva o card para *Aguardando resposta* com o prazo
  zerado em 20 dias.

- **F3 · Must: Gerar link em 2 passos**: (1) projeto (pré-selecionado quando parte de um card),
  formato (um por vez, D3) e idioma; (2) URL pública, **validade em destaque** ("este link vale
  por 20 dias: expira em 12/08/2026") e **mensagem sugerida** pronta para colar, já com o link e
  o prazo dentro ("a pesquisa fica aberta até 12/08"), com botões "Copiar link" e
  "Copiar mensagem".
  *Critérios de aceite:* gerar Completo e depois Simplificado para o mesmo projeto resulta em dois
  links ativos, distintos e rotulados; a data de expiração exibida é a da geração mais 20 dias.

- **F4 · Must: Link compartilhado multi-resposta no formulário público** (D1): o link genérico
  aceita N respostas e nunca exibe "já respondido"; respostas anônimas com nome/e-mail opcionais
  (D4); proteção antiabuso proporcional (bloqueio de reenvio no mesmo navegador, deduplicação por
  e-mail quando informado, limite por IP). Links por contato permanecem de uso único.
  Depende de **B1 a B4**.
  O formulário mostra ao respondente **até quando a pesquisa fica aberta** (D7) e, passado o
  prazo, exibe "o prazo desta pesquisa terminou" em vez do questionário, com a orientação de
  pedir um link novo.
  *Critérios de aceite:* duas pessoas diferentes respondem o mesmo link com sucesso; reenvio no
  mesmo navegador é bloqueado com mensagem amigável; link expirado ou disparo fechado não aceita
  resposta.

- **F5 · Must: Detalhe da coleta** (do card): KPIs do projeto (NPS, respostas, última resposta),
  **links ativos rotulados por formato** (1 ou 2, D3), cada um com **prazo de validade e o estado
  de proximidade** (neutro; atenção quando faltam 5 dias ou menos; crítico quando vencido). Link
  válido tem ação **Copiar**; link vencido troca para **Gerar novo** (D8). Mais as respostas
  recentes (com filtro por formato quando houver os dois). Evolução do modal de detalhe da v1.
  *Critério de aceite:* abrir um projeto com link a vencer mostra "expira em Xd" em destaque; um
  com link vencido mostra "expirado há Xd" e a ação de gerar novo.

- **F6 · Must: Dispensar / reativar coleta** (menu do card): com motivo; projeto dispensado sai
  do quadro e da conta de vencidos; toggle "Mostrar dispensados" para revê-los. **A volta atrás é
  parte do fluxo, não um detalhe**: o card dispensado exibe a ação **"Reativar coleta"** no
  próprio card, além do item no menu, e reativar devolve o projeto à coluna que a regra indicar.
  Depende de **B7**. Subiu de *Should* para *Must* por causa de D9: é a **única** forma de tirar
  ruído do quadro.
  *Critérios de aceite:* dispensar tira o card do quadro e da conta de vencidos; ligar "Mostrar
  dispensados" traz o card de volta marcado como dispensado e com a ação de reativar; reativar
  desfaz por completo, sem perder histórico de respostas.

### Resultados

- **F7 · Must: Leitura executiva**: KPIs (NPS oficial com a fórmula à vista, respostas, score
  médio, projetos vencidos) e **distribuição das respostas** na ordem detrator → neutro →
  promotor (D6), com contagens e percentuais. Dados já disponíveis na API.

- **F8 · Should: NPS por projeto com drill-down "todas as notas"**: tabela ordenável (projeto,
  cliente, DC, DM, respostas, NPS, status Respondido/Link gerado/Pendente) respeitando busca e
  filtros; clicar num projeto com respostas expande a linha com mini-distribuição, contagens por
  classe, formato e recência, **todas as notas individuais** (com autor quando informado: D4) e
  os comentários. Uma expansão por vez. Depende de **B6**.
  *Critério de aceite:* as notas expandidas fecham com o NPS exibido na linha (mesma contagem,
  mesma fórmula).

- **F9 · Could: Médias por aspecto** (qualidade técnica, prazos, comunicação, valor para o
  negócio), na escala de 1 a 5 (D10), somente respostas do formato Completo, com o recorte
  explícito no subtítulo. Depende de **B11** (agregação nova) e de **B13** (o quarto aspecto):
  o painel só liga quando os dois existirem.

### Respostas

- **F10 · Should: Tabela de auditoria de respostas** (D12). A visão deixa de ser uma lista solta
  (difícil de ler rápido, sem separar de qual projeto é cada resposta) e vira uma **tabela
  escaneável**: colunas **Projeto · Nota · Classificação · Formato · Autor · Comentário · Recebida**,
  da mais recente para a mais antiga. Para equilibrar leitura rápida e detalhe, o **comentário
  aparece só como prévia de uma linha** na tabela; **abrir uma resposta** (clique na linha ou no
  nome do projeto) leva a um **detalhe com todos os campos**: nota (1 a 10) com a classificação,
  formato, os **quatro aspectos (1 a 5)** quando for Completo (com a **média dos aspectos** ao lado, leitura
  da entrega distinta da nota de recomendação), o **comentário inteiro** (que pode
  ser longo, rola no próprio modal) e a **atribuição** (nome/e-mail quando houver, senão "resposta
  anônima", ver D4). A busca global cobre projeto, pessoa e comentário; os filtros de formato,
  classificação e **data de coleta** (o pedido de recortar por período com volume alto) vêm do
  menu de filtros único (D11). Rotular o formato por resposta depende de **B5**; a listagem por
  projeto via API é **B6**.
  *Critérios de aceite:* cada linha identifica claramente o projeto de origem; abrir uma resposta
  mostra todos os campos respondidos; resposta Simplificada não exibe o bloco de aspectos;
  comentário longo não estoura a tabela (fica na prévia) e é lido por inteiro no detalhe.

### Transversais

- **F11 · Must: Exportar CSV** das respostas (endpoint já existe na v1; manter).
- Tema claro/escuro e acessibilidade (navegação por teclado, estados ARIA) conforme o mockup.

## 7. Capacidades requeridas do backend

O modelo de dados atual (projeto → disparo → link → resposta, com formato, idioma e status)
**permanece**. O que muda, em linguagem de capacidade: o detalhamento técnico de cada item
(arquivos, esquema, riscos de migração) está no `FLUXOS.md`:

| ID | Capacidade | Habilita | Classe | Migração de dados | Esforço |
|---|---|---|---|---|---|
| B1 | Link genérico passa a aceitar **N respostas** (uso único continua valendo por contato) | F4 | **Obrigatória** | **Sim**, segura no estado atual (hoje há no máx. 1 resposta por link, a regra nova nasce válida) | P |
| B2 | Envio de resposta deixa de rejeitar "já respondido" em link genérico | F4 | **Obrigatória** | Não | P |
| B3 | Formulário público deixa de "fechar" link genérico respondido | F4 | **Obrigatória** | Não | P |
| B4 | Antiabuso para link aberto (navegador + e-mail + IP) | F4 | **Obrigatória** | Não | P a M |
| B5 | Resposta expõe o **formato** do formulário de origem | F10 | Recomendada | Não | P |
| B6 | Listagem de respostas **por projeto** via API JSON (a consulta já existe internamente; hoje só sai no CSV) | F8 | Recomendada | Não | P |
| B7 | **Dispensa de coleta** por projeto (flag + motivo, reversível) excluída de vencidos/quadro | F6 | Recomendada | Sim | M |
| B8 | Público estimado do disparo ("enviei para ~X pessoas") para taxa aproximada | D2 (opcional) | Opcional | Sim | P |
| B9 | Registro de cobrança (última cobrança, nº de lembretes) | §10 | Opcional | Sim | P |
| B10 | Responsável pela coleta distinto do DM | §10 | Opcional | Sim | P |
| B11 | Agregação de médias por aspecto (só formato Completo) | F9 | Opcional | Não | M |
| B12 | **Validade de 20 dias no disparo**: data de expiração gravada/calculada, estado "expirado" exposto na API e link vencido recusando resposta | F2, F3, F4, F5 | **Obrigatória** | **Sim**, definir o que fazer com disparos abertos antigos na virada | P a M |
| B13 | **Quarto aspecto "Valor gerado para o negócio"**: campo novo ou renomeação do atual `Scope` | F9, formulário Completo | **Obrigatória** | **Sim** | P a M |
| B14 | **Faixa das notas**: NPS passa a aceitar 1 a 10 e os aspectos 1 a 5 (validação de domínio, classificação e agregações) | Formulário, F7, F8, F9 | **Obrigatória** | **Sim**, dados antigos estão em 0 a 10 | M |
| B15 | Filtros de **empresa** e de **data de coleta** nas consultas de projeto e de resposta, aceitando **múltiplos valores** por faceta (empresa, DC, tipo, DM, status, formato, classificação), ver D11 | F1 | Recomendada | Não | P |

Esforço: P = pequeno (horas), M = médio (dias). Estimativas de quem mapeou o código; validar no
refinamento.

**B13 e B14: decidido converter.** O quarto aspecto trocou de assunto — era "Escopo", virou
"Valor gerado para o negócio" — e as escalas mudaram: a nota do NPS passou de 0–10 para 1–10 e os
aspectos de 0–10 para 1–5. Entre renomear o campo e criar um novo preservando as duas séries,
optou-se por **renomear e converter**, na migração `20260827232330_RebuildNpsPhaseOne`:

- `scope` foi renomeada para `business_value`;
- os aspectos foram convertidos com `CEIL(x / 2.0)`, limitados a 1–5;
- a nota 0 foi elevada a 1 e a classificação recalculada pela régua nova.

**Consequência aceita:** a média de "Valor para o negócio" no painel soma respostas que mediram
valor e respostas antigas que mediram escopo, sem distinguir as duas. Se um dia for preciso
separar as séries, será por migração nova — a `Down` desta lança `NotSupportedException`, de
propósito, porque a conversão não é semanticamente reversível.

A conversão tem prova: `NpsEndpointsTests.Historical_nps_data_should_be_converted_without_changing_ids`
sobe o schema anterior num contêiner, semeia dados no formato antigo, migra para a frente e afirma
linha a linha o resultado — ids preservados, escalas convertidas, e-mail normalizado e disparos
duplicados fechados.

## 8. Fases de entrega (proposta)

| Fase | Entrega | Requisitos | Backend |
|---|---|---|---|
| **1, Coleta destravada** (MVP) | O fluxo completo do operador: quadro com validade de link, gerar link, mensagem, link multi-resposta, dispensa de coleta, leitura básica | F1, F2, F3, F4, F5, F6, F7, F11 | B1 a B4, B7, B12, B13, B14 |
| **2, Profundidade** | Drill-down de notas, feed de auditoria, filtros no servidor | F8, F10 | B5, B6, B15 |
| **3, Por demanda** | Médias por aspecto e opcionais de gestão | F9 (+ B8 a B10 se priorizados) | B11 (+ B8 a B10) |

A fase 1 engordou em relação ao corte original: **B7** entrou porque a dispensa virou a única
forma de tirar item do quadro (D9), e **B12 a B14** entraram porque validade de link e faixa de
notas são regra de domínio, não acabamento de tela. Não dá para entregar o quadro novo sem elas.

## 9. Métricas de sucesso

Sem "taxa de resposta" (D2), o sucesso é **cobertura e frescor da carteira** (donos e alvos a
definir na revisão deste PRD; baseline = leitura do dashboard atual na ativação):

1. % de projetos ativos **Em dia** (resposta ≤ 45 dias): métrica principal.
2. **Projetos vencidos** (sem disparo aberto e sem resposta há 90+ dias): tendência de queda,
   excluídos os dispensados.
3. Tempo mediano **gerar link → 1ª resposta** (eficácia da cobrança).
4. Respostas por rodada e % com comentário (qualidade do sinal).

## 10. Riscos e pontos de atenção

- **Link aberto** → risco de duplicidade/spam. A mitigação (B4) é deliberadamente leve: é NPS
  de relacionamento, não votação; aceita-se o risco residual e reavalia-se diante de anomalia.
- **Anonimato (D4/LGPD)**: nome/e-mail são autodeclarados e opcionais; nenhum rastreio implícito
  no link genérico; telas só mostram atribuição existente.
- **Narrativa sem denominador (D2)**: gestores habituados a "taxa de resposta" precisam da
  régua nova (cobertura da carteira); B8 existe como válvula de escape.
- **Limiar 45/90 dias** (recoleta/vencido): 90 já é regra do backend; 45 é proposta do quadro;
  confirmar a política antes de considerar parametrização.

## 11. Questões em aberto

1. ~~**Idiomas**: mantém espanhol no redesign?~~ **Mantido.** O formulário público responde em
   português, inglês e espanhol, escolhidos na geração do link.
2. ~~O subtexto do KPI de respostas ("% de enviados") fica ou entra B8?~~ **Contagem simples.**
   B8 não foi construído.
3. **Permissões**: quem pode gerar link e quem pode dispensar coleta? O motivo da dispensa é
   auditado?
4. ~~Filtros na querystring (compartilhar visão filtrada) e o formato da multi-seleção?~~
   **Feito, com a chave repetida** em vez de lista separada por vírgula: `?client=Santander&client=Itaú`.
   Evita ambiguidade com nomes que contêm vírgula.
5. **Fase futura**: links por contato para nudge individual ("quem não respondeu"), mantém no
   radar?
6. ~~**B13/B14**: converter, segregar ou descartar o histórico?~~ **Decidido: converter.** Ver §7.
7. ~~O prazo de 20 dias (D7) é fixo ou vira parâmetro?~~ **Fixo** por ora, em
   `NpsCollectionPolicy.LinkValidityDays`. Vira parâmetro quando alguém pedir.
8. Com D9, o KPI de **vencidos** passa a contar todo projeto sem coleta recente, inclusive os que
   antes eram descartados por estarem encerrados. Confirmar que é isso mesmo que a liderança quer
   ver, ou se a régua de "vencido" também precisa mudar.

## 12. Referências

- Mockups navegáveis (dados = seed determinístico de desenvolvimento; os números exibidos são os
  que a API calcula sobre esse seed), na pasta autossuficiente `design/nps/`: painel `painel.html`
  e formulário público `formulario.html`, com a capa `index.html` como ponto de entrada. Abrem
  direto no navegador, carregando a `assets/` que acompanha a pasta.
- Detalhamento técnico dos fluxos, do backend atual e de cada capacidade B1 a B11:
  `design/nps/FLUXOS.md`.
- Baseline v1: página `/nps` do cliente atual (lista, KPIs, gerar link, detalhe).
