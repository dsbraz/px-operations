# Repository Guidelines

## How to Work Here

**Rule 1 — Think Before Coding.** No silent assumptions. State what you're assuming. Surface tradeoffs. Ask before guessing. Push back when a simpler approach exists.

**Rule 2 — Simplicity First.** Minimum code that solves the problem. No speculative features. No abstractions for single-use code. If a senior engineer would call it overcomplicated — simplify.

**Rule 3 — Surgical Changes.** Touch only what you must. Don't "improve" adjacent code, comments, or formatting. Don't refactor what isn't broken. Match existing style.

**Rule 4 — Goal-Driven Execution.** Define success criteria. Loop until verified. Success is stated as an outcome, not as a list of steps.

**Rule 5 — Test-First.** Red→Green→Refactor, every new behavior, no exceptions: write the failing test, write the minimum code that passes it, then refactor. The test is written before the implementation, not reconstructed after it — a test that never failed proves nothing. This applies to domain rules, use cases, endpoints, and Blazor components alike. See *Testing Guidelines* for the tools and *Quality Gates → Proof & Reuse* for what each kind of change must prove.

The quality gates below apply to **the diff you are producing**, per Rule 3 — they are not a mandate to refactor code you are only passing through. Where the existing base already fails a gate, see *Known Deviations*.

## Project Structure & Module Organization

- `src/Server` — backend in Clean Architecture:
  - `PxOperations.Api`: ASP.NET Core host, controllers, HTTP setup.
  - `PxOperations.Application`: use cases, queries, views, and ports.
  - `PxOperations.Domain`: DDD building blocks and business rules.
  - `PxOperations.Infrastructure`: EF Core, persistence, external integrations.
- `src/Client`:
  - `PxOperations.BlazorWasm`: the Blazor WebAssembly application.
  - `PxOperations.Ui`: shared design-system components.
- `tests/Server`: `PxOperations.ArchitectureTests`, `PxOperations.Domain.UnitTests`, `PxOperations.Api.IntegrationTests`.
- `tests/Client`: `PxOperations.BlazorWasm.Tests`, `PxOperations.Ui.Tests`.
- `specs/openapi`: the exported API contract (`PxOperations.Api.json`).
- `design`: visual references used to build features.
- `scripts`: GCP bootstrap/deploy and OpenTelemetry collector config.

## Build, Test, and Development Commands

- `dotnet build PX-Operations.sln`: build the full solution.
- `dotnet test PX-Operations.sln`: run all tests.
- `dotnet run --project src/Server/PxOperations.Api`: run the API locally.
- `dotnet run --project src/Client/PxOperations.BlazorWasm`: run the client locally.
- `docker compose up`: PostgreSQL, API, and client with `dotnet watch`.

Use targeted test runs while developing — the inner TDD loop should be seconds, not minutes:

```bash
dotnet test tests/Server/PxOperations.Domain.UnitTests
dotnet test tests/Client/PxOperations.BlazorWasm.Tests
```

## Coding Style & Naming Conventions

- 4-space indentation, UTF-8 files.
- Technical names in English: `Api`, `Application`, `Domain`, `Infrastructure`.
- PascalCase for types and public members, camelCase for locals and parameters.
- Test classes named after the subject under test, e.g. `HealthEndpointsTests`.
- Routes are `/api/<plural-resource>`. There is no version segment today — do not introduce `/api/v1` for a single endpoint; version the whole surface or none of it.

## Quality Gates

A change is rejected if any "Rejected if" cell matches the diff.

### Clean Architecture

| Do | Rejected if |
| --- | --- |
| Each layer knows only the one inside it. New API code talks to views and commands, never to an aggregate. | A new `using` crosses the line, or new API code imports a Domain entity. |
| GET → query + mapping. A use case only when there is orchestration (does it exist? does it close the previous one? does it persist?). The controller calls an Application query interface that returns a View. | `GetXUseCase` / `ListXUseCase` that only delegates, or a controller that maps an aggregate. |
| A command returns an id or a view. | A use case returns the aggregate and the API maps it to a response. |
| Input (format, enum, length) is validated at the edge. Invariants live in the Domain. The two never repeat the same message. | FluentValidation reimplements a domain rule, or the API constructs the aggregate. |

### DDD

| Do | Rejected if |
| --- | --- |
| Business policy (is it allowed, stage, delay, score, deadline) lives in the Domain — aggregate or pure calculation. | That `if` was born in a Repository, a Controller, or Mappings. |
| One aggregate, one transaction. Another aggregate enters by id, not to be mutated through navigation. | A use case mutates two aggregates "by accident", or an invariant is spread across both. |
| A type, interface, or `*Rule` exists only with a production caller. No second real implementation, no new interface. No MediatR, `IRepository<T>`, Specification, `IDomainService`, or DomainEvent without a handler. | A new abstraction with no caller, a `*Rule` nothing in the aggregate calls, or any of the above in the diff. |
| **Exempt from the rule above:** Application→Infrastructure ports (`IProjectRepository`, `INpsQueries`, `IUnitOfWork`). One implementation is the design, not speculation. | A port declared in Infrastructure, or a generic `IRepository<T>` dressed up as a port. |

### Clean Code

| Do | Rejected if |
| --- | --- |
| The name says what the business does. | `Handle`, `Process`, `Manager`, `Helper`, `Util`, `Data`, `Info`, or a method name containing `And`. |
| A class and a function do one job. | Describing it needs an "and" for two jobs (persist **and** compute stage **and** format an enum). |
| Guard at the top, early return. | Nested `if`/`foreach`/`try`, or `else` after `return`. |
| The aggregate encapsulates its invariants (private setters, factory, behavior). A use case never reaches into its internals. When a class is already large, new behavior goes in a new type. | A new public setter, an exposed mutable collection, or one more method in a file that already mixes roles. |

### REST API

| Do | Rejected if |
| --- | --- |
| Resource + HTTP verb + protocol status. No verb in the path. GET never changes state. Create → 201, delete → 204, absent → 404, business rule → 400, conflict → 409. | A verb in the path, a GET with a side effect, a create returning 200, or an invented status in the body instead of the protocol. |
| The Domain distinguishes the failure at throw time — a broken invariant and a state conflict are different exception types — and a single `IExceptionHandler` maps each to its status. Actions carry no `try/catch`. | `try/catch` inside an action, `catch (Exception)`, the same exception type mapped to 400 in one action and 409 in another, or framework exceptions (`KeyNotFoundException`, `InvalidOperationException`) used as control flow. |
| The public contract (OpenAPI / JSON) changes only on purpose. A new field does not break the client. | A rename, removal, or type change in a response that is not itself the contract feature. |

### Client

| Do | Rejected if |
| --- | --- |
| The client formats; it never re-derives a business rule. Bands, thresholds, deadlines, and labels come from the server contract. | A score band, deadline, or rule message born again in a `.razor.cs` or a `*Format.cs`. |
| Markup renders; the code-behind fetches, maps, and holds state. | A business decision expressed as an `if` in `.razor` markup. |
| Shared visuals live in `PxOperations.Ui`. | The same markup block copied into a second feature. |

### Proof & Reuse

| Do | Rejected if |
| --- | --- |
| One business fact, one place. | The same enum, deadline, or rule message is born again in the diff. |
| A new invariant has a domain test that breaks it. A new command has an integration test for success and one for the broken rule. A new component has a bUnit test for what it renders. | The new rule, endpoint, or component ships without that test. |
| The test came first and was seen failing (Rule 5). | Tests written only after the code was already passing, to backfill coverage. |
| A boundary a gate depends on is enforced by a test in `PxOperations.ArchitectureTests`, not by convention. | A new layering rule with nothing asserting it. |

## Testing Guidelines

- Backend: `xUnit`. Blazor components: `bUnit`. Integration: `WebApplicationFactory` + `Testcontainers.PostgreSql`.
- Domain unit tests are the fast loop and carry the invariants; integration tests carry the endpoint contract; architecture tests carry the layering.
- Add or update tests with every behavior change. Architecture and endpoint changes do not merge without coverage.
- A bug fix starts with the test that reproduces the bug.

## Known Deviations

The base predates these gates. New code follows the gates; the items below are known debt — do not imitate them, and do not fix them as a drive-by (Rule 3).

1. **Delegate-only use cases.** `GetProjectUseCase`, `ListMilestonesUseCase`, and ~8 siblings only forward to a repository. New reads go straight to an Application query interface.
2. **Aggregates crossing into the API.** Projects and ProjectHealth return the aggregate and the controller maps it (`ProjectsController.cs:68`); Milestones and Nps use Views (`MilestoneView`, `NpsProjectView`) — the Views are the pattern to follow. Note `DependencyRulesTests` only asserts on `.csproj` references, so it does not catch the `using PxOperations.Domain.*` in `ProjectsController.cs`.
3. **Dead abstractions.** `IDomainService` has zero references; `Specification<T>` and the `DomainEvent` machinery are exercised only by `DomainBuildingBlocksTests` (no production `RaiseDomainEvent`, and `AppDbContext` calls `Ignore<DomainEvent>()`). All three are deletable.
4. **Duplicated error mapping.** The single handler now exists: `ApiExceptionHandler` maps `ValidationException` / `BusinessRuleValidationException` / `InvalidRequestValueException` to 400, `ResourceNotFoundException` to 404 and `BusinessStateConflictException` to 409. Three controllers still catch `BusinessRuleValidationException` locally and hand-build `ProblemDetails` — Projects, Milestones and ProjectHealth. Nps carries no `try/catch`. The same three still throw `KeyNotFoundException` as control flow from their use cases.
5. **Aggregates reached by navigation.** `Project.Milestones` is a public mutable `ICollection<Milestone>`; `Milestone.Project` and `ProjectHealth.Project` expose another aggregate through navigation instead of by id. Only `NpsArchitectureTests` asserts the opposite, and only for the Nps folder.
6. **A unit test in the integration project.** `SubmitNpsPublicResponseUseCaseTests` uses fakes and needs no container, but there is no Application unit-test project to hold it. Creating one for a single file is not worth it today.

## Commit & Pull Request Guidelines

- Short imperative commit messages, e.g. `Add readiness endpoint skeleton`.
- One change set per commit.
- Never reference AI agents in commits — no co-author lines, no "Generated by", no tool attributions of any kind.
- PRs include: a brief summary, impacted paths or modules, test evidence (`dotnet test ...`), and screenshots only for UI changes.

## Security & Configuration Tips

- Never commit `.env`, secrets, or connection strings with real credentials.
- Keep local settings in `.env` and project `appsettings.*.json`.
- Treat OpenAPI as the public contract for future non-.NET clients.
