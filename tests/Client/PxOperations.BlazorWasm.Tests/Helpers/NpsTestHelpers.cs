namespace PxOperations.BlazorWasm.Tests.Helpers;

/// <summary>
/// Cargas de exemplo do NPS. Ficam aqui, e não dentro do teste da página,
/// porque cada componente extraído tem o próprio arquivo de teste e todos
/// precisam das mesmas respostas de API.
/// </summary>
internal static class NpsTestHelpers
{
    internal static string DashboardJson() => """
        {
          "officialNps":50.0,"totalResponses":4,"averageScore":8.3,"overdueProjects":1,
          "scale":{"minimum":1,"maximum":10},
          "distribution":[
            {"code":"detractor","label":"Detrator","tone":"critical","count":1,"percentage":25.0},
            {"code":"passive","label":"Neutro","tone":"warning","count":1,"percentage":25.0},
            {"code":"promoter","label":"Promotor","tone":"positive","count":2,"percentage":50.0}
          ],
          "aspectSummary":{
            "completeResponsesCount":4,"scale":{"minimum":1,"maximum":5},
            "aspects":[
              {"code":"quality","label":"Qualidade técnica","average":4.2,"responsesCount":3},
              {"code":"schedule","label":"Prazos acordados","average":3.5,"responsesCount":2},
              {"code":"communication","label":"Comunicação","average":4.0,"responsesCount":4},
              {"code":"business_value","label":"Valor para o negócio","average":null,"responsesCount":0}
            ]
          },
          "filterOptions":{
            "clients":[{"code":"Alpha","label":"Alpha"},{"code":"Beta","label":"Beta"}],
            "dcs":[{"code":"DC1","label":"DC1"}],"projectTypes":[],"deliveryManagers":[],
            "statuses":[],"formats":[],"classifications":[]
          }
        }
        """;

    internal static string FractionalDashboardJson() => """
        {
          "officialNps":0.0,"totalResponses":3,"averageScore":7.0,"overdueProjects":0,
          "scale":{"minimum":1,"maximum":10},
          "distribution":[
            {"code":"detractor","label":"Detrator","tone":"critical","count":1,"percentage":33.333},
            {"code":"passive","label":"Neutro","tone":"warning","count":1,"percentage":33.333},
            {"code":"promoter","label":"Promotor","tone":"positive","count":1,"percentage":33.334}
          ],
          "aspectSummary":{
            "completeResponsesCount":0,"scale":{"minimum":1,"maximum":5},
            "aspects":[
              {"code":"quality","label":"Qualidade técnica","average":null,"responsesCount":0},
              {"code":"schedule","label":"Prazos acordados","average":null,"responsesCount":0},
              {"code":"communication","label":"Comunicação","average":null,"responsesCount":0},
              {"code":"business_value","label":"Valor para o negócio","average":null,"responsesCount":0}
            ]
          },
          "filterOptions":{"clients":[],"dcs":[],"projectTypes":[],"deliveryManagers":[],"statuses":[],"formats":[],"classifications":[]}
        }
        """;

    internal static string DashboardWithoutCompleteResponsesJson() => """
        {
          "officialNps":100.0,"totalResponses":1,"averageScore":9.0,"overdueProjects":0,
          "scale":{"minimum":1,"maximum":10},
          "distribution":[
            {"code":"detractor","label":"Detrator","tone":"critical","count":0,"percentage":0.0},
            {"code":"passive","label":"Neutro","tone":"warning","count":0,"percentage":0.0},
            {"code":"promoter","label":"Promotor","tone":"positive","count":1,"percentage":100.0}
          ],
          "aspectSummary":{
            "completeResponsesCount":0,"scale":{"minimum":1,"maximum":5},
            "aspects":[
              {"code":"quality","label":"Qualidade técnica","average":null,"responsesCount":0},
              {"code":"schedule","label":"Prazos acordados","average":null,"responsesCount":0},
              {"code":"communication","label":"Comunicação","average":null,"responsesCount":0},
              {"code":"business_value","label":"Valor para o negócio","average":null,"responsesCount":0}
            ]
          },
          "filterOptions":{"clients":[],"dcs":[],"projectTypes":[],"deliveryManagers":[],"statuses":[],"formats":[],"classifications":[]}
        }
        """;

    internal static string ProjectsJson() => """
        [
          {
            "id":1,"name":"Projeto ativo","client":"Alpha","dc":"DC1","deliveryManager":"Maria","projectType":"Squad","responsesCount":0,
            "stage":{"code":"no_link","label":"Sem link","tone":"neutral"},
            "temporal":{"label":"Nunca coletado","tone":"neutral","at":null},"waiver":null,"activeLinks":[],
            "primaryAction":{"code":"generate_link","label":"Gerar link","format":"complete","dispatchId":null,"token":null},
            "isOverdue":true,"lastDispatchClosedAt":null
          },
          {
            "id":2,"name":"Projeto dispensado","client":"Beta","dc":"DC1","deliveryManager":"João","projectType":"Squad","responsesCount":1,
            "stage":{"code":"waived","label":"Dispensado","tone":"neutral"},
            "temporal":{"label":"Dispensado em 01/08/2026","tone":"neutral","at":"2026-08-01T12:00:00Z"},
            "waiver":{"reason":"Sem pesquisa","waivedAt":"2026-08-01T12:00:00Z"},"activeLinks":[],
            "primaryAction":{"code":"reactivate","label":"Reativar","format":null,"dispatchId":null,"token":null},
            "isOverdue":false,"lastDispatchClosedAt":"2026-08-01T12:00:00Z"
          }
        ]
        """;

    internal static string DispatchJson(Guid token) => $$"""
        {
          "dispatch":{"id":10,"projectId":1,"projectName":"Projeto ativo","format":"complete","formatLabel":"Completo","language":"pt","languageLabel":"Português","status":"open","createdAt":"2026-08-01T12:00:00Z","expiresAt":"2026-08-21T12:00:00Z","closedAt":null,"targetsCount":1,"responsesCount":0,"availability":"open","availabilityLabel":"Aberto","tone":"positive"},
          "targets":[{"id":20,"dispatchId":10,"contactId":null,"contactName":null,"contactEmail":null,"token":"{{token}}","isGeneric":true,"responsesCount":0}]
        }
        """;

    internal static string DetailJson() => """
        {
          "project":{
            "id":1,"name":"Projeto ativo","client":"Alpha","dc":"DC1","deliveryManager":"Maria","projectType":"Squad","responsesCount":2,
            "stage":{"code":"awaiting_response","label":"Aguardando resposta","tone":"warning"},
            "temporal":{"label":"Enviado há 2d","tone":"warning","at":"2026-08-19T12:00:00Z"},"waiver":null,
            "activeLinks":[{"dispatchId":10,"token":"11111111-1111-1111-1111-111111111111","format":"complete","formatLabel":"Completo","expiresAt":"2026-08-21T12:00:00Z","availability":"open","availabilityLabel":"Aberto","tone":"positive"}],
            "primaryAction":{"code":"copy_link","label":"Copiar link","format":"complete","dispatchId":10,"token":"11111111-1111-1111-1111-111111111111"},
            "isOverdue":false,"lastDispatchClosedAt":null
          },
          "officialNps":50.0,"averageScore":8.5,"responsesCount":2,"promotersCount":1,
          "activeLinks":[{"dispatchId":10,"token":"11111111-1111-1111-1111-111111111111","format":"complete","formatLabel":"Completo","expiresAt":"2026-08-21T12:00:00Z","availability":"open","availabilityLabel":"Aberto","tone":"positive"}],
          "recentResponses":[{"id":1,"projectId":1,"projectName":"Projeto ativo","dispatchId":10,"targetId":20,"contactId":null,"contactName":null,"contactEmail":null,"format":"complete","formatLabel":"Completo","score":10,"classification":"promoter","classificationLabel":"Promotor","quality":5,"schedule":5,"communication":5,"businessValue":5,"comment":"Excelente parceria","respondentName":null,"respondentEmail":null,"submittedAt":"2026-08-20T12:00:00Z"}]
        }
        """;

    internal static string FilteredResponsesJson() => """
        [
          {"id":2,"projectId":1,"projectName":"Projeto ativo","dispatchId":10,"targetId":21,"contactId":null,"contactName":null,"contactEmail":null,"format":"complete","formatLabel":"Completo","score":9,"classification":"promoter","classificationLabel":"Promotor","quality":5,"schedule":4,"communication":5,"businessValue":5,"comment":"Resposta completa filtrada","respondentName":null,"respondentEmail":null,"submittedAt":"2026-08-21T12:00:00Z"}
        ]
        """;

    internal static string ProjectResultsJson() => """
        [
          {
            "id":1,"name":"Zulu","client":"Beta","dc":"DC1","deliveryManager":"Maria","responsesCount":1,"officialNps":100.0,
            "distribution":[{"code":"detractor","label":"Detrator","tone":"critical","count":0,"percentage":0.0},{"code":"passive","label":"Neutro","tone":"warning","count":0,"percentage":0.0},{"code":"promoter","label":"Promotor","tone":"positive","count":1,"percentage":100.0}],
            "formats":[{"code":"complete","label":"Completo","count":1},{"code":"simplified","label":"Simplificado","count":0}],
            "lastResponseAt":"2026-08-21T12:00:00Z","status":{"code":"responded","label":"Respondido","tone":"positive"}
          },
          {
            "id":2,"name":"Alpha","client":"Alpha","dc":"DC2","deliveryManager":"João","responsesCount":2,"officialNps":0.0,
            "distribution":[{"code":"detractor","label":"Detrator","tone":"critical","count":1,"percentage":50.0},{"code":"passive","label":"Neutro","tone":"warning","count":0,"percentage":0.0},{"code":"promoter","label":"Promotor","tone":"positive","count":1,"percentage":50.0}],
            "formats":[{"code":"complete","label":"Completo","count":1},{"code":"simplified","label":"Simplificado","count":1}],
            "lastResponseAt":"2026-08-20T12:00:00Z","status":{"code":"responded","label":"Respondido","tone":"positive"}
          }
        ]
        """;

    internal static string FilterOptionsJson() => """
        {
          "clients":[{"code":"Alpha","label":"Alpha"}],"dcs":[{"code":"DC1","label":"DC1"}],
          "projectTypes":[{"code":"squad","label":"Squad"}],"deliveryManagers":[{"code":"Maria","label":"Maria"}],
          "statuses":[{"code":"responded","label":"Respondido"}],
          "formats":[{"code":"complete","label":"Completo"},{"code":"simplified","label":"Simplificado"}],
          "classifications":[{"code":"detractor","label":"Detrator"},{"code":"passive","label":"Neutro"},{"code":"promoter","label":"Promotor"}]
        }
        """;

    internal static string ResponsesJson() => """
        [
          {"id":1,"projectId":1,"projectName":"Projeto ativo","dispatchId":10,"targetId":20,"contactId":null,"contactName":null,"contactEmail":null,"format":"complete","formatLabel":"Completo","score":10,"classification":"promoter","classificationLabel":"Promotor","quality":5,"schedule":4,"communication":3,"businessValue":2,"comment":"Comentário completo que deve permanecer acessível","respondentName":"Pessoa Teste","respondentEmail":"pessoa@example.com","submittedAt":"2026-08-21T12:00:00Z","classificationTone":"warning"},
          {"id":2,"projectId":2,"projectName":"Projeto simplificado","dispatchId":11,"targetId":21,"contactId":null,"contactName":null,"contactEmail":null,"format":"simplified","formatLabel":"Simplificado","score":4,"classification":"detractor","classificationLabel":"Detrator","quality":null,"schedule":null,"communication":null,"businessValue":null,"comment":null,"respondentName":null,"respondentEmail":null,"submittedAt":"2026-08-20T12:00:00Z","classificationTone":"critical"}
        ]
        """;
}
