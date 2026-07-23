# NPS: Fluxos e o que muda no backend

> Documento de desenho técnico, par do PRD. **Nada do backend foi alterado ainda**: aqui ficam
> registradas, fluxo a fluxo, as mudanças necessárias para implementar o redesign, com arquivo e
> risco de migração. As decisões de produto (D1 a D12) e a lista de capacidades (B1 a B15) moram
> no `PRD.md`; este documento aterrissa cada uma no código.
>
> **Base do redesign (D1): LINK COMPARTILHADO (multi-resposta).** Um link por rodada, colado numa
> mensagem para um grupo; N pessoas respondem o mesmo link (anônimo, com nome/e-mail opcionais).
> Revisado em 23/07/2026 para cobrir também: validade de 20 dias do link (D7/D8), escalas 1 a 10 e
> 1 a 5 (D10), quarto aspecto "Valor para o negócio" (B13), filtros (D11) e a aba Respostas como
> tabela (D12).

## 0. Como o backend funciona hoje (fatos)

Cadeia: **Project → Dispatch (disparo/rodada) → DispatchTarget (o "link", com `Token` GUID) → SurveyResponse.**

- Um `Dispatch` tem período, formato (`Simplified`/`Complete`), idioma, status (`Open`/`Closed`) e **N targets**.
- Um `DispatchTarget` é **por-contato** (`ContactId` preenchido) **ou genérico** (`ContactId` nulo, `IsGeneric`).
  Cada target carrega um `Token` único; o link público é `{{base}}/nps/{{token}}`.
- `POST /api/nps/dispatches` (`CreateNpsDispatchUseCase`): cria o disparo, um target por `ContactId`
  informado, e, se `CreateGenericToken` **ou** lista de contatos vazia, **um** target genérico.
- Resposta pública: `GET /api/nps/public/{token}` (form) e `POST /api/nps/public/{token}/responses` (envio).
- **O cliente atual já cria só o token genérico** (`ContactIds=[]`, `CreateGenericToken=true`) e oferece
  copiar a URL; ou seja, o produto **já aponta para o modelo "um link"**.
- **Escala hoje:** a nota e as quatro dimensões (`Scope/Schedule/Quality/Communication`) são `int`
  na faixa **0 a 10**. O redesign muda isso (ver §6): NPS passa a **1 a 10** e os aspectos a **1 a 5**.

### ⚠️ Restrição central: todo token é de USO ÚNICO, inclusive o genérico

Três camadas impõem "no máximo 1 resposta por target":

| Camada | Onde | Regra |
|---|---|---|
| Banco | `SurveyResponseConfiguration.cs:48` | `HasIndex(r => r.TargetId).IsUnique()` |
| Use case | `SubmitNpsPublicResponse.cs:32` | rejeita se `TargetHasResponseAsync(target.Id)` |
| View pública | `NpsRepository.GetPublicSurveyAsync` | `AlreadyAnswered = target.Responses.Count != 0` |

Como o link genérico é **um** target, ele aceita **uma** resposta e depois exibe "já respondido".
Isso é o oposto do fluxo desejado ("envia para 1 a N pessoas responderem esse link").

---

## 1. Fluxo: Gerar link e enviar para 1..N pessoas (D1 · B1 a B4)

**Escolhido: link compartilhado (multi-resposta).** Um link só, colado numa mensagem para um grupo;
N pessoas respondem. Os links por-contato continuam existindo no backend e ficam como evolução
opcional (nudge individual, "quem não respondeu").

### Mudanças de backend (documentadas, não implementadas)

Para o target **genérico** aceitar N respostas, as três camadas acima precisam abrir exceção para ele:

1. **B1 · Índice único condicional** (`SurveyResponseConfiguration.cs`)
   Trocar `HasIndex(r => r.TargetId).IsUnique()` por um índice **parcial**:
   `...IsUnique().HasFilter("contact_id IS NOT NULL")`.
   O uso único vale só para respostas atribuídas a um contato (por-contato); respostas genéricas
   (`contact_id` nulo) podem repetir no mesmo target. **Requer migration** (segura: hoje há no
   máximo 1 resposta por target, a regra nova nasce válida).
2. **B2 · Submit** (`SubmitNpsPublicResponse.cs`)
   O check `TargetHasResponseAsync` só deve valer para target **não** genérico:
   `if (!target.IsGeneric && await repository.TargetHasResponseAsync(...))`.
3. **B3 · View pública** (`NpsRepository.GetPublicSurveyAsync`)
   `AlreadyAnswered` deve ser `!target.IsGeneric && target.Responses.Count != 0`
   (o form do link compartilhado nunca "fecha" por já ter resposta).
4. **B4 · Antiabuso** (novo, obrigatório para link aberto): hoje o uso único protegia de reenvio/spam.
   Com link aberto é preciso outro freio: 1 resposta por navegador (cookie/localStorage) e/ou
   dedupe por `RespondentEmail` no mesmo target quando informado, e/ou rate limit por IP.
   `RespondentName`/`RespondentEmail` já existem no submit (atribuição parcial opcional).

### Implicação para o dashboard: "taxa de resposta" perde denominador (D2)

Hoje a taxa = `AnsweredLinkTargetsCount / LinkTargetsCount` (assume 1 target por contato). Com link
compartilhado há **1** target genérico, então a taxa vira 100% após a 1ª resposta, sem sentido.
O redesign aposenta a taxa e acompanha por **contagem absoluta + estágio no quadro**. Válvula de
escape opcional: **B8** (`ExpectedRecipients int?` em `Dispatch`, "enviei para ~X pessoas") para uma
taxa aproximada. Sem esse campo, o sistema não sabe o tamanho do público de um link aberto.

---

## 2. Fluxo: Mensagem sugerida para copiar junto com o link (front)

**Não precisa de backend.** O `NpsDispatchDetailView` já devolve `ProjectName`, `Language` e o `Token`;
o cliente monta a URL e a mensagem. No mockup, a mensagem já inclui o **prazo de validade** (D7):

> Olá! Estamos coletando o NPS do projeto **{Projeto}** e a sua opinião faz diferença. Leva menos de
> 1 minuto e a resposta é anônima: {link}. A pesquisa fica aberta até **{DD/MM}**. Obrigado!

- Variante por `Language` do disparo. O modal de gerar link do mockup monta **PT e EN**; o suporte a
  **ES** segue como questão em aberto do PRD (§11-1), já que o formulário público tem os três idiomas.
- Botões separados: "copiar link" e "copiar mensagem" (a mensagem inclui o link e o prazo).
- **Opcional futuro** (só se quiserem editar/persistir a mensagem): campo de template no backend.
  Não é necessário agora; mantê-lo no front é mais simples e flexível.

---

## 3. Fluxo: Quadro de coleta (Kanban por estágio) (F2)

O quadro tem **quatro colunas** por regra objetiva sobre dados que a API já entrega
(`ActiveDispatches`, `ResponsesCount`, `LastResponseAt`, `IsOverdue`):

| Coluna | Regra | Ação |
|---|---|---|
| Sem link | sem disparo aberto | Gerar link |
| Aguardando resposta | disparo aberto e `ResponsesCount == 0` | Cobrar (recopiar link/mensagem) |
| Recoleta | última resposta há mais de 45 dias | Nova rodada |
| Em dia | última resposta há 45 dias ou menos | nenhuma |

**Nada é ocultado por status de projeto (D9):** a carteira inteira aparece; a única ocultação é a
**coleta dispensada** (§7). O limiar de 45 dias (recoleta) é proposta do quadro; 90 dias (vencido) já
é regra do backend (`IsOverdue`). Confirmar a política antes de parametrizar.

### Cobrança como ação (opcionais)

- **Não há registro de cobrança/reenvio.** O backend guarda `Dispatch.CreatedAt`, mas não "última
  cobrança" nem "nº de lembretes". Para medir/mostrar: **B9** (`LastRemindedAt`, `ReminderCount` em
  `Dispatch` ou log de lembretes). Sem isso, "cobrar" = recopiar link/mensagem.
- **"Responsável pela coleta" não existe** (só `DeliveryManager`, por isso o card usa o DM). Se a
  cobrança tiver dono diferente do DM: **B10**, campo novo em `Project`/`Dispatch`.

---

## 4. Fluxo: Validade de 20 dias do link (D7/D8 · B12)

**Decisão de produto:** o link vale **20 dias** contados da geração; o prazo é exibido em todo lugar
(modal de geração, mensagem sugerida, card no quadro e formulário público). Passados os 20 dias sem
resposta, o link **não se reaproveita**: gera-se um novo, e o card volta para *Aguardando resposta*
com o prazo zerado.

**Hoje não existe validade.** `Dispatch` tem período/status, mas nenhuma regra de expiração calculada
a partir da geração, nem estado "expirado" exposto, e o submit não recusa link vencido.

**B12 · Mudança de backend (documentar, não implementar):**
1. **Data de expiração** no disparo: `ExpiresAt = CreatedAt + 20 dias` (calculado ou persistido).
   Se persistido, **requer migration**; decidir o que fazer com disparos abertos antigos na virada.
2. **Estado "expirado" exposto** na API (dashboard/projects), para o card trocar o chip e a ação
   ("Cobrar" → "Gerar novo link"). No mockup: card com borda vermelha, "expirado há Xd" e a ação nova.
3. **Submit recusa link vencido**: `POST .../responses` valida `ExpiresAt` e responde erro amigável;
   o `GetPublicSurveyAsync` sinaliza o estado "prazo vencido" para o formulário mostrar o aviso em
   vez do questionário.
4. **Gerar novo link** é um `Dispatch` novo (a cadeia já suporta), e o card retorna a *Aguardando*.

*Aberto (PRD §11-7):* o prazo de 20 dias é fixo ou vira parâmetro por projeto/rodada?

---

## 5. Fluxo: Escalas e aspectos (D10 · B13 · B14)

O redesign muda o **conteúdo** das perguntas do formato Completo e as **faixas** de nota.

**Escalas (B14):**
- **NPS passa a 1 a 10** (era 0 a 10): Promotor 9 a 10, Neutro 7 a 8, Detrator 1 a 6.
- **Aspectos passam a 1 a 5** (eram 0 a 10).
- Mexe em validação de domínio, na classificação e nas agregações. **Dados antigos estão em 0 a 10**:
  decidir se são convertidos, segregados por data ou descartados da série (PRD §11-6). **Requer migration**
  se houver conversão.

**Os quatro aspectos (Completo), agora explícitos (B13):**

| # | Rótulo na UI | Apoio | Campo atual |
|---|---|---|---|
| 1 | Qualidade técnica da entrega | estabilidade, poucos defeitos | `Quality` |
| 2 | Aderência aos prazos acordados | combinados cumpridos | `Schedule` |
| 3 | Comunicação, clareza e transparência | informação clara, sem surpresa | `Communication` |
| 4 | Valor gerado para o negócio | o quanto ajudou o resultado | `Scope` ⚠️ |

Os três primeiros são **renomeações**; o quarto **trocou de assunto** (era "Escopo").
**B13 · decisão pendente:** renomear o campo `Scope` (migração simples, mistura histórico) **ou**
criar `BusinessValue` e aposentar `Scope` (histórico honesto, uma migração a mais). Recomendação:
a opção 2 se houver volume de respostas Completo a comparar; a 1 se o histórico for pequeno.

**Onde diferenciar (não é uniforme):**
- **NPS:** não diferencia formato; agrega os dois juntos.
- **Aspectos:** só existem no Completo; médias saem só de respostas Completas (agregação nova = **B11**,
  `GetDimensionAverages` filtrando por `Format == Complete`, na escala de 1 a 5).
- **Média dos aspectos por resposta** (mostrada no detalhe da resposta) é **cálculo de front** sobre os
  quatro valores da própria resposta; não é campo de backend.

---

## 6. Fluxo: Aba Respostas como tabela + detalhe (D12 · B5 · B6)

A aba deixou de ser uma lista solta e virou **tabela escaneável** (Projeto · Nota · Classificação ·
Formato · Autor · Comentário · Recebida), com **abertura de cada resposta** num modal de detalhe:
nota (1 a 10), classificação, formato, os **quatro aspectos (1 a 5) e sua média** quando Completo, o
**comentário inteiro** e a **atribuição** (nome/e-mail quando houver, senão "resposta anônima", D4).
O comentário na tabela é só prévia de uma linha, para não estourar o layout.

**Backend:**
- **B5 · Expor `Format` na resposta.** O `NpsResponseView` tem `DispatchId` mas **não expõe `Format`**;
  hoje o front infere pelo conteúdo (dimensões nulas = Simplificado). Adicionar `Format` (lido de
  `Dispatch.Format`) ao `NpsResponseView` e ao contract `NpsSurveyResponse` rotula sem join no front.
- **B6 · Listar respostas por projeto (JSON).** O use case de listar já aceita `NpsFilter.ProjectId`
  (usado no export CSV), mas **não há rota JSON por projeto**: só `dispatches/{id}/responses` e
  `responses/export` (CSV). Mudança pequena, sem migration: `GET /api/nps/responses?projectId=`.
  Serve tanto o feed de Respostas quanto a expansão "todas as notas" na tabela de Resultados (F8).

*No mockup os dados são determinísticos (seed); a atribuição e os aspectos individuais são de exemplo.*

---

## 7. Fluxo: Filtros (D11 · B15) e dispensa de coleta (D9 · B7)

### Barra de filtros (D11)

A barra é **busca + um botão "Filtro"** (padrão Linear/Vercel), não uma fileira de dropdowns. O botão
abre um menu de duas camadas (faceta → valores); o que está ligado vira **chip removível**. Facetas de
lista (empresa, DC, tipo, DM, status, formato, classificação) são **multi-seleção**; data de coleta é
intervalo (single) e dispensadas é toggle.

- **Front:** o mockup filtra no cliente. Só a **empresa** e a **data de coleta** eram ausentes na v1.
- **B15 · Backend (quando o volume exigir filtragem no servidor):** as consultas de projeto e de
  resposta precisam aceitar **listas** por faceta (`empresa[]`, `dc[]`, `tipo[]`, `dm[]`, `status[]`,
  `formato[]`, `classe[]`) e um intervalo de data. Sem migration.
- *Aberto (PRD §11-4):* filtros na querystring da rota; com multi-seleção cada faceta vira lista na URL.

### Dispensar coleta (D9 · B7)

Dentro do NPS **não existe projeto encerrado**; há um estado só, **coleta dispensada**, marcado à mão e
**reversível**. Nada é ocultado por `ProjectStatus` nem por data.

**Hoje não existe.** `ProjectStatus` tem `InProgress/Scheduled/Closed`; usar `Closed` é errado (responde
"o projeto acabou", não "a coleta não se aplica") e nem resolve (`IsOverdue` roda sobre todos, então um
`Closed` ainda conta em "vencidos").

**B7 · Mudança de backend:** flag por projeto independente do status, `Project.NpsCollectionOptOut` (bool)
ou enum `NpsCollection { Ativa, Dispensada }`, **+ motivo opcional**. Kanban, `GetOverdueProjectIdsAsync` e
dashboard passam a **excluir** os dispensados das colunas de cobrança e da contagem de vencidos. Precisa
ser **reversível**, com o mesmo endpoint aceitando a volta. **Requer migration.** Na UI: "Dispensar coleta"
no menu do card; o card dispensado exibe o botão **"Reativar coleta"**, e o toggle "Mostrar dispensados"
traz de volta.

---

## 8. Rotas: cada subpágina é uma rota (D5, front)

Coleta, Resultados e Respostas são subpáginas distintas, cada uma com sua rota (no mockup: `#/coleta`,
`#/resultados`, `#/respostas`, com deep link e histórico). No cliente Blazor viram **três páginas
roteadas** (`/nps/coleta`, `/nps/resultados`, `/nps/respostas`, com `/nps` redirecionando para a padrão),
compartilhando o layout (header, barra de filtros) em vez de estado de aba local numa rota única.

---

## Resumo: mudanças de backend por capacidade

| B | Mudança | Arquivo(s) | Migração | Classe |
|---|---|---|---|---|
| B1 | Índice único parcial (`contact_id IS NOT NULL`) | `SurveyResponseConfiguration.cs` | **Sim** | **Obrigatória** |
| B2 | Submit ignora "já respondido" em target genérico | `SubmitNpsPublicResponse.cs` | Não | **Obrigatória** |
| B3 | `AlreadyAnswered` falso para genérico | `NpsRepository.GetPublicSurveyAsync` | Não | **Obrigatória** |
| B4 | Antiabuso p/ link aberto (navegador/email/IP) | submit + infra | Não | **Obrigatória** |
| B12 | Validade de 20 dias + estado expirado + submit recusa vencido | `Dispatch`, submit, view pública | **Sim** | **Obrigatória** |
| B13 | Quarto aspecto "Valor" (renomear `Scope` ou campo novo) | Domain/EF | **Sim** | **Obrigatória** |
| B14 | Faixas NPS 1 a 10 e aspectos 1 a 5 + histórico | regras de domínio, agregações | **Sim** | **Obrigatória** |
| B5 | Expor `Format` na resposta | `NpsResponseView` + contract | Não | Recomendada |
| B6 | Rota JSON de respostas por projeto | `NpsController` | Não | Recomendada |
| B7 | Flag "coleta dispensada" (+motivo) reversível, excluída de vencidos | `Project`, `GetOverdueProjectIdsAsync`, use case | **Sim** | Recomendada |
| B15 | Filtros aceitando múltiplos valores (empresa/DC/tipo/DM/status/formato/classe + data) | consultas de projeto/resposta | Não | Recomendada |
| B11 | Agregar médias por aspecto (só Completo, 1 a 5) | novo use case/repo | Não | Opcional |
| B8 | `ExpectedRecipients` p/ taxa aproximada | `Dispatch` | **Sim** | Opcional |
| B9 | `LastRemindedAt`/`ReminderCount` (registro de cobrança) | `Dispatch` | **Sim** | Opcional |
| B10 | "Responsável pela coleta" distinto do DM | `Project`/`Dispatch` | **Sim** | Opcional |

**Conclusão.** O bloqueio de partida continua sendo o **uso único do token genérico** (B1 a B4). O que
subiu para obrigatório junto na Fase 1 é regra de domínio, não acabamento de tela: **validade do link**
(B12), **faixa das notas** (B14) e o **quarto aspecto** (B13). O restante (Respostas em tabela, filtros,
dispensa) se apoia em ajustes recomendados (B5, B6, B7, B15) ou fica por demanda (B8 a B11).
