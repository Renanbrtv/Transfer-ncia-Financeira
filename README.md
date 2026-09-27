# Transferências Financeiras — API .NET 8 + React

API REST e interface web para **realizar, agendar, consultar e cancelar transferências** entre contas,
com cheque especial, limites por hora (diurno/noturno), processamento de agendamentos em background e
tratamento correto de concorrência no SQL Server.

---

## Sumário

- [Tecnologias](#tecnologias)
- [Como executar](#como-executar)
  - [Opção 1: Docker (tudo de uma vez)](#opção-1-docker-tudo-de-uma-vez)
  - [Opção 2: execução local](#opção-2-execução-local)
  - [Migrations](#migrations)
  - [Testes](#testes)
- [Swagger e exemplos de requisição](#swagger-e-exemplos-de-requisição)
- [Arquitetura](#arquitetura)
- [Regras de negócio](#regras-de-negócio)
- [Período diurno e noturno](#período-diurno-e-noturno)
- [Concorrência e consistência](#concorrência-e-consistência)
- [Idempotência](#idempotência)
- [Modelo de dados](#modelo-de-dados)
- [Códigos HTTP e erros](#códigos-http-e-erros)
- [Decisões arquiteturais e trade-offs](#decisões-arquiteturais-e-trade-offs)
- [Diferenciais implementados](#diferenciais-implementados)

---

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Backend | C# 12, .NET 8, ASP.NET Core Web API (controllers) |
| Persistência | Entity Framework Core 8, SQL Server 2022, migrations |
| Documentação | Swagger / OpenAPI (Swashbuckle) com comentários XML |
| Background | `BackgroundService` + `PeriodicTimer` |
| Testes | xUnit, `WebApplicationFactory`, Testcontainers (SQL Server real), `FakeTimeProvider` |
| Frontend | React 18 + TypeScript + Vite (sem biblioteca de UI) |
| Infra | Docker, docker-compose, Nginx, GitHub Actions |

## Como executar

### Pré-requisitos

- [.NET SDK 8](https://dotnet.microsoft.com/download) (ou superior)
- [Node.js 20+](https://nodejs.org/) para o frontend
- [Docker](https://www.docker.com/) para o docker-compose e para os **testes de integração**
- SQL Server: via Docker (recomendado) ou LocalDB no Windows

### Opção 1: Docker (tudo de uma vez)

```bash
cp .env.example .env        # opcional: define a senha do SQL Server
docker compose up --build
```

| Serviço | URL |
|---|---|
| Frontend | http://localhost:3000 |
| Swagger | http://localhost:5080/swagger |
| Health check | http://localhost:5080/health |

A API aplica as migrations e os dados iniciais automaticamente ao subir. No container do frontend,
o Nginx encaminha `/api` para a API, então não há CORS nem URL fixa no bundle.

### Opção 2: execução local

**1. Banco de dados.** Suba só o SQL Server do compose:

```bash
docker compose up -d sqlserver
```

**2. Connection string.** Nenhuma senha fica versionada. Configure com *user-secrets* (ou com a
variável de ambiente `ConnectionStrings__TransfersDb`):

```bash
dotnet user-secrets set "ConnectionStrings:TransfersDb" \
  "Server=localhost,1433;Database=TransfersDb;User Id=sa;Password=Transfers@Local2026;TrustServerCertificate=True" \
  --project src/Transfers.Api
```

> No Windows com LocalDB não é preciso configurar nada: o `appsettings.Development.json` já aponta para
> `(localdb)\mssqllocaldb`.

**3. Backend:**

```bash
dotnet run --project src/Transfers.Api
# Swagger: http://localhost:5080/swagger
```

**4. Frontend** (em outro terminal):

```bash
cd frontend
npm install
npm run dev
# http://localhost:5173  (o Vite encaminha /api para http://localhost:5080)
```

### Migrations

As migrations ficam em `src/Transfers.Infrastructure/Persistence/Migrations` e são aplicadas na subida
da API quando `Database:ApplyMigrationsOnStartup = true` (padrão). Para aplicar ou criar manualmente:

```bash
dotnet tool install --global dotnet-ef --version 8.*

# aplicar
dotnet ef database update --project src/Transfers.Infrastructure --startup-project src/Transfers.Api

# criar uma nova
dotnet ef migrations add NomeDaMigration --project src/Transfers.Infrastructure \
  --startup-project src/Transfers.Api --output-dir Persistence/Migrations
```

A migration inicial cria as tabelas, os índices, as *check constraints* e as contas de exemplo:

| Id | Titular | Saldo | Cheque especial | Status |
|---|---|---|---|---|
| 1 | João | 5.000 | 1.000 | Active |
| 2 | Maria | 2.000 | 500 | Active |
| 3 | Carlos | 500 | 1.000 | Active |
| 4 | Conta Bloqueada | 5.000 | 1.000 | Blocked |

### Testes

```bash
dotnet test                                   # tudo
dotnet test tests/Transfers.UnitTests         # só unitários (não precisam de Docker)
dotnet test tests/Transfers.IntegrationTests  # integração (precisam do Docker em execução)
```

- **Unitários** (`Transfers.UnitTests`): regras do domínio puro, sem mocks. Cobrem saldo, cheque
  especial, limites de valor e de tentativas, contas bloqueadas e inativas, origem igual ao destino, valor
  inválido, máquina de estados, cancelamento, agendamento e período dia/noite.
- **Integração** (`Transfers.IntegrationTests`): a API sobe em memória com `WebApplicationFactory`
  contra um **SQL Server real** criado pelo Testcontainers. O relógio é um `FakeTimeProvider`, então os
  testes de janela de uma hora, período noturno e agendamento são determinísticos. Os 15 cenários do
  enunciado estão cobertos. Há também testes de concorrência: duas transferências simultâneas de R$ 800
  com saldo de R$ 1.000, rajada de 10 transferências, limite por hora sob concorrência, transferências
  cruzadas A→B e B→A sem deadlock, e requisições simultâneas com a mesma chave de idempotência.

> Por que não SQLite ou InMemory nos testes de integração? Eles não suportam `UPDLOCK` nem o modelo de
> locks do SQL Server. O teste de concorrência passaria sem provar nada.

O workflow `.github/workflows/ci.yml` roda build e testes (backend e frontend) a cada push.

## Swagger e exemplos de requisição

Todos os endpoints estão documentados no Swagger (http://localhost:5080/swagger), com requests,
responses, códigos HTTP e exemplos.

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/transfers` | Transferência imediata |
| `POST` | `/api/transfers/scheduled` | Agendar transferência |
| `POST` | `/api/transfers/{id}/cancel` | Cancelar transferência agendada |
| `GET` | `/api/transfers/{id}` | Consultar transferência |
| `GET` | `/api/accounts/{id}` | Consultar conta |
| `GET` | `/api/accounts` | Listar contas (dashboard) |
| `GET` | `/api/accounts/{id}/transfers?take=20` | Transferências enviadas/recebidas pela conta |

**Transferência imediata**

```bash
curl -i -X POST http://localhost:5080/api/transfers \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: 7f1d8a52-3c2e-4b8e-9a51-0c6f7e1b2a90" \
  -d '{ "sourceAccountId": 1, "destinationAccountId": 2, "amount": 150.00 }'
```

```http
HTTP/1.1 201 Created
Location: http://localhost:5080/api/transfers/3f0c7c1e-8d0a-4a51-9b39-2f5f1b7f6a11
```

```json
{
  "id": "3f0c7c1e-8d0a-4a51-9b39-2f5f1b7f6a11",
  "sourceAccountId": 1,
  "destinationAccountId": 2,
  "amount": 150.00,
  "type": "Immediate",
  "status": "Completed",
  "createdAt": "2026-01-15T13:00:00+00:00",
  "scheduledFor": null,
  "processedAt": "2026-01-15T13:00:00+00:00",
  "cancelledAt": null,
  "failureCode": null,
  "failureMessage": null
}
```

**Transferência rejeitada por regra de negócio** (fica registrada como `Failed`):

```http
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/problem+json
```

```json
{
  "title": "Transferência rejeitada",
  "status": 422,
  "detail": "Saldo + cheque especial insuficiente.",
  "code": "InsufficientFunds",
  "transferId": "b7a0e0d4-3c1f-4f55-8a0e-4d1f8f0f2c77",
  "transfer": { "id": "b7a0e0d4-…", "status": "Failed", "failureCode": "InsufficientFunds", "...": "..." },
  "traceId": "00-…"
}
```

**Agendamento**

```bash
curl -X POST http://localhost:5080/api/transfers/scheduled \
  -H "Content-Type: application/json" \
  -d '{ "sourceAccountId": 1, "destinationAccountId": 3, "amount": 250.00, "scheduledFor": "2030-01-15T14:30:00-03:00" }'
```

**Cancelamento, consultas**

```bash
curl -X POST http://localhost:5080/api/transfers/{id}/cancel
curl http://localhost:5080/api/transfers/{id}
curl http://localhost:5080/api/accounts/1
```

## Arquitetura

Quatro projetos com dependências apontando para dentro, sem MediatR, AutoMapper, CQRS ou repositório genérico:

```
Transfers.Api ─────────► Transfers.Application ─────► Transfers.Domain
      │                          ▲
      └──► Transfers.Infrastructure (implementa as interfaces da Application)
```

| Projeto | Responsabilidade |
|---|---|
| **Domain** | Entidades com comportamento (`Account`, `Transfer`, `TransferAttempt`), máquina de estados, regras de execução, `TransferLimitPolicy`, exceções de domínio. Não depende de nada. |
| **Application** | Casos de uso (`TransferService`, `ScheduledTransferProcessor`, `AccountService`), fluxo compartilhado de execução (`TransferExecutor`), DTOs, interfaces de persistência, options. |
| **Infrastructure** | `DbContext` (também é o Unit of Work), mapeamentos, migrations, seed, repositórios com os locks de SQL Server. |
| **Api** | Controllers finos, tratamento global de erros (ProblemDetails), Swagger, DI, CORS e o `BackgroundService` de agendamentos. |

```
transfers-challenge/
├── src/
│   ├── Transfers.Domain/          Accounts/ · Transfers/ · Limits/ · Exceptions/ · Money.cs
│   ├── Transfers.Application/     Abstractions/ · Accounts/ · Transfers/ · Options/ · Exceptions/
│   ├── Transfers.Infrastructure/  Persistence/ (DbContext, Configurations, Migrations) · Repositories/
│   └── Transfers.Api/             Controllers/ · Middleware/ · Workers/ · Extensions/ · Program.cs
├── tests/
│   ├── Transfers.UnitTests/
│   └── Transfers.IntegrationTests/
├── frontend/                      React + TypeScript (Vite), Dockerfile, nginx.conf
├── docker-compose.yml
└── .github/workflows/ci.yml
```

**Onde ficam as regras:** a entidade `Transfer` decide tudo sobre a execução (`Execute`, `Cancel`,
`StartProcessing`). A Application só busca os dados, já com os locks (contas e uso da última hora),
chama o domínio e persiste. Os controllers só traduzem o resultado para HTTP.

## Regras de negócio

**Conta**
- Tem Id, titular, saldo, limite de cheque especial e status (`Active`, `Blocked`, `Inactive`).
- **Saldo disponível = saldo + cheque especial.** O saldo pode ficar negativo até o limite.
- Não existe separação artificial entre "saldo" e "cheque especial usado": um saldo negativo é
  exatamente o valor usado do limite. Por isso `-800 + 1.000 = 200`, e o valor recebido cobre primeiro o
  cheque especial, como pede o enunciado.
- Conta bloqueada ou inativa não envia nem recebe transferências.
- Defesa em profundidade: o banco tem as *check constraints* `Balance >= -OverdraftLimit` e `OverdraftLimit >= 0`.

**Transferência** (imediata ou agendada, na execução)
1. Validação de entrada, que responde **400** e não registra nada: contas com Id positivo, origem diferente do
   destino, valor maior que zero com no máximo 2 casas decimais e, no agendamento, data futura.
2. Contas precisam existir. Caso contrário, **404**.
3. Regras avaliadas na execução, nesta ordem. A primeira que falhar define o motivo:
   `SourceAccountNotActive` → `DestinationAccountNotActive` → `HourlyAttemptLimitExceeded` →
   `HourlyAmountLimitExceeded` → `InsufficientFunds`.
4. Se todas passam, débito e crédito acontecem na **mesma transação**, a transferência vai para `Completed`
   e uma `TransferAttempt` bem-sucedida é gravada.
5. Se alguma falha, a transferência vai para `Failed` com o motivo e uma `TransferAttempt` rejeitada é
   gravada. **A rejeição é confirmada no banco**, porque tentativas rejeitadas contam para o limite.

**Limites por hora** (por conta de origem, configuráveis em `appsettings.json`, seção `TransferLimits`)

| Período | Valor máximo por hora | Tentativas por hora |
|---|---|---|
| Dia | R$ 5.000 | 5 |
| Noite | R$ 1.000 | 3 |

- **Janela móvel**: os 60 minutos anteriores ao instante da tentativa. É mais justa que a "hora cheia",
  onde era possível fazer o dobro do limite entre 10:59 e 11:00.
- **Limite de valor**: soma apenas as transferências **concluídas**. Rejeições não consomem valor.
- **Limite de tentativas**: conta **todas** as tentativas, inclusive as rejeitadas. Com o limite de 5, a 6ª
  tentativa na janela é rejeitada, e essa rejeição também conta. A conta volta a operar quando as
  tentativas antigas saem da janela.
- **Qual limite vale**: o do período do instante da tentativa. Uma tentativa às 22:10 usa o limite noturno,
  somando o que foi transferido às 21:30.
- Erros de validação e contas inexistentes (400/404) **não** contam como tentativa: não chegam a ser uma
  transferência de uma conta válida.

**Agendamento**
- A criação valida os dados, a data futura e a existência das contas, e nasce com status `Scheduled`.
- Saldo, status e limites **não** são verificados na criação, e sim no momento da execução, pelo
  **mesmo fluxo** da transferência imediata (`TransferExecutor`). Se falhar, termina como `Failed`.
- Pode ser cancelada enquanto estiver `Scheduled`.

**Máquina de estados** (encapsulada em `Transfer`)

```
Imediata:   Processing ──► Completed | Failed
Agendada:   Scheduled ──► Processing ──► Completed | Failed
            Scheduled ──► Cancelled
```

Qualquer outra transição, como cancelar `Completed`, `Failed`, `Cancelled` ou `Processing`, lança
`InvalidTransferStateException` e resulta em **409**. A transferência imediata nasce em `Processing`
porque é executada na mesma requisição.

**Worker de agendamentos** (`ScheduledTransfersWorker`)
- `BackgroundService` com `PeriodicTimer`. O intervalo e o tamanho do lote ficam em `ScheduledTransfers`
  no appsettings, com padrão de 10 s e 50 itens.
- Cada transferência vencida é processada em **escopo de DI e transação próprios**. A falha de uma não
  afeta as outras.
- A linha é travada com `UPDLOCK, READPAST` e só se ainda estiver `Scheduled`. Isso permite **várias
  instâncias da API** sem processar a mesma transferência duas vezes.
- Se o processo cair no meio, o rollback devolve a transferência para `Scheduled` e ela é retomada no
  próximo ciclo. Não sobram registros órfãos em `Processing`.

## Período diurno e noturno

| Período | Horário (fuso `America/Sao_Paulo`) |
|---|---|
| **Diurno** | das **06:00:00** até **21:59:59** |
| **Noturno** | das **22:00:00** até **05:59:59** |

- Todas as datas são gravadas em UTC (`datetimeoffset`). O período é calculado convertendo o instante da
  tentativa para o fuso configurado.
- Os horários e o fuso são configuráveis (`TransferLimits:DayStartsAt`, `NightStartsAt`, `TimeZoneId`). A
  aplicação valida a configuração na subida e não inicia se ela for inválida.
- O relógio é injetado via `TimeProvider` (nativo do .NET 8), o que torna todos os testes de horário
  determinísticos.

## Concorrência e consistência

**Problema.** Conta com R$ 1.000 recebe duas transferências simultâneas de R$ 800. Sem controle, as duas
leem saldo 1.000, as duas passam na validação e a conta termina em -600, fora do limite. O mesmo vale para
o **limite por hora**: as duas leriam a mesma soma horária e juntas ultrapassariam o limite.

**Solução: lock pessimista por linha (`UPDLOCK, ROWLOCK`) dentro de uma transação `READ COMMITTED`.**

```sql
SELECT * FROM [Accounts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = @id
```

Fluxo de uma transferência, em uma única transação:

1. Trava as duas contas **sempre em ordem crescente de Id**.
2. Lê o uso da última hora da conta de origem (tentativas e valor).
3. Aplica as regras do domínio; debita e credita, ou marca como `Failed`.
4. Grava `Transfer` e `TransferAttempt` e faz commit, o que libera os locks.

No exemplo das duas transferências de R$ 800, A trava a conta e conclui. B **espera** o lock, relê o saldo
já atualizado (R$ 200) e é rejeitada por `InsufficientFunds`. O teste
`TwoSimultaneousTransfers_UsingTheSameBalance_OnlyOneCompletes` prova isso contra um SQL Server real.

**Por que lock pessimista, e não só `RowVersion` (concorrência otimista)?**
- O `RowVersion` protege a linha da conta, mas **não protege a soma horária**, que vem de outra tabela.
  Duas transferências poderiam ler a mesma soma e ambas passar do limite. Com o lock na conta de origem,
  tudo que depende dela (saldo, tentativas e valor da hora) fica serializado.
- Evita lógica de retry: em alta disputa, a abordagem otimista gera muitas falhas e reprocessamento. Aqui
  a segunda requisição apenas espera alguns milissegundos.
- `UPDLOCK` não bloqueia leituras comuns (consultas de saldo continuam livres) e o `ROWLOCK` restringe o
  lock a uma linha: só serializa operações **da mesma conta**.
- `SERIALIZABLE` também resolveria, mas com *range locks* e muito mais deadlocks. O lock explícito é mais
  previsível.

**Deadlocks.** Transferências cruzadas (A→B e B→A ao mesmo tempo) são o cenário clássico. Travar sempre
na mesma ordem (menor Id primeiro) elimina o ciclo. Há teste para isso.

**Atomicidade.** Débito, crédito, transferência e tentativa são gravados no mesmo `SaveChanges`, dentro
da transação explícita. Qualquer exceção inesperada faz rollback de tudo: não existe transferência parcial.

**`RowVersion` continua no modelo** como rede de segurança (concorrência otimista do EF) nas tabelas
`Accounts` e `Transfers`. Se algum fluxo futuro esquecer o lock, o conflito vira `409` em vez de
corromper dados.

**Cancelamento vs. worker.** Os dois travam a linha da transferência. Se o worker já está executando, o
cancelamento espera e depois recebe `409`, porque a transferência não está mais `Scheduled`. Se o
cancelamento chegou antes, o worker pula a linha (`READPAST`) e depois não a encontra mais como `Scheduled`.

## Idempotência

Os `POST` de transferência e agendamento aceitam o header opcional **`Idempotency-Key`**, uma chave de até
100 caracteres gerada pelo cliente. O frontend gera um UUID por envio.

- A chave é gravada na transferência, com **índice único filtrado** (`WHERE IdempotencyKey IS NOT NULL`).
- Repetição com a **mesma chave e os mesmos dados**: devolve a transferência original com o header
  `Idempotency-Replayed: true`. Status `200` se concluída ou agendada, `422` se tinha falhado. Nada é
  executado de novo.
- Mesma chave com **dados diferentes**: `409 Conflict`.
- Requisições simultâneas com a mesma chave: na transferência imediata, a verificação acontece **depois**
  do lock da conta de origem, então elas se serializam e só uma executa (há teste). No agendamento, que
  não trava contas, o índice único barra a segunda, que recebe `409`.

Cenário que isso resolve: o cliente envia a transferência, a conexão cai antes da resposta e ele reenvia.
Sem idempotência, o dinheiro sairia duas vezes.

## Modelo de dados

| Tabela | Colunas principais | Índices e constraints |
|---|---|---|
| `Accounts` | `Id` (identity), `HolderName`, `Balance`, `OverdraftLimit`, `Status`, `RowVersion` | CK saldo ≥ −limite; CK limite ≥ 0 |
| `Transfers` | `Id` (GUID), `SourceAccountId`, `DestinationAccountId`, `Amount`, `Type`, `Status`, `CreatedAt`, `ScheduledFor`, `ProcessedAt`, `CancelledAt`, `FailureReason`, `IdempotencyKey`, `RowVersion` | `(Status, ScheduledFor)` para o worker; `(SourceAccountId, CreatedAt)` e `(DestinationAccountId, CreatedAt)` para o extrato; único filtrado em `IdempotencyKey`; CK `Amount > 0`; CK origem ≠ destino |
| `TransferAttempts` | `Id` (bigint identity), `AccountId`, `TransferId`, `Amount`, `Succeeded`, `RejectionReason`, `AttemptedAt` | `(AccountId, AttemptedAt) INCLUDE (Amount, Succeeded)`: índice de cobertura da consulta de limites |

- Dinheiro é sempre `decimal` no C# e `DECIMAL(18,2)` no banco, definido por convenção no `DbContext`.
  Não há `float` ou `double` em nenhum lugar.
- Enums são gravados como texto (`'Completed'`, `'Blocked'`), o que facilita a leitura direta no banco.
- As FKs usam `ON DELETE NO ACTION`: histórico financeiro não é apagado em cascata.

## Códigos HTTP e erros

Todos os erros seguem o formato **ProblemDetails (RFC 7807)**, com `code` estável e `traceId`.

| Status | Quando |
|---|---|
| `201 Created` | Transferência concluída ou agendada (com header `Location`) |
| `200 OK` | Consultas, cancelamento, repetição idempotente |
| `400 Bad Request` | Valor ≤ 0, mais de 2 casas decimais, origem = destino, data no passado, JSON inválido ou campos faltando |
| `404 Not Found` | Conta ou transferência inexistente |
| `409 Conflict` | Transição de estado inválida (ex.: cancelar `Completed`), conflito de concorrência, `Idempotency-Key` reutilizada com outros dados |
| `422 Unprocessable Entity` | Transferência rejeitada por regra de negócio (saldo, limites, conta bloqueada). Inclui `transferId` e o registro `Failed` |
| `500` | Erro inesperado: mensagem genérica, detalhes só no log |

**Por que 422 e não 400 nas rejeições?** A requisição é válida e a transferência **foi registrada**
(status `Failed`, consultável pelo Id). O 400 fica para dados que nunca poderiam formar uma transferência.
Separar os dois ajuda o cliente: 400 significa "corrija a requisição"; 422 significa "a operação foi
avaliada e recusada".

## Decisões arquiteturais e trade-offs

| Decisão | Motivo | Trade-off |
|---|---|---|
| Lock pessimista (`UPDLOCK`) na conta | Protege saldo **e** limite horário; sem retry | Operações da mesma conta são serializadas. Aceitável: são raras e curtas. |
| SQL específico de SQL Server nos repositórios | Controle explícito do lock, isolado em 3 métodos | Trocar de banco exige reescrever esses métodos |
| Rejeição confirmada como `Failed` | Tentativas rejeitadas precisam contar e ser auditáveis | A tabela cresce com rejeições; em produção caberia uma política de retenção |
| Janela móvel de 60 min | Mais justa e simples de testar | Consulta por intervalo em vez de agrupamento por hora (coberta por índice) |
| Worker em `BackgroundService` + polling | Simples, sem infraestrutura extra, seguro com várias instâncias (`READPAST`) | Latência de até um intervalo de polling (10 s). Com volume alto, caberia fila ou Hangfire/Quartz. |
| Sem MediatR, AutoMapper, CQRS, repositório genérico | O domínio é pequeno; indireção sem ganho | — |
| Sem Domain Events | Não há consumidores reais (notificação, integração). Seriam código sem uso. | Se surgirem integrações, o ponto natural é `Transfer.Execute`, com outbox |
| Sem mensageria | O enunciado não exige e o worker resolve o agendamento | — |
| `TransferExecutor` compartilhado | Imediata e agendada passam exatamente pelas mesmas regras | — |
| DbContext como Unit of Work | Evita uma abstração extra sobre algo que o EF já faz | A Application depende da interface `IUnitOfWork`, não do EF |
| Migrations aplicadas na subida (configurável) | Facilita a avaliação e o docker-compose | Em produção, o ideal é aplicar no pipeline (`dotnet ef migrations bundle`) |
| Ids de conta `int`, de transferência `Guid` | Contas fáceis de digitar no Swagger; transferência gerada no domínio antes do insert | GUID aleatório fragmenta o índice clusterizado. Com volume alto, um GUID sequencial resolve. |
| Sem autenticação | Fora do escopo do enunciado | Em produção, a conta de origem viria do usuário autenticado |

## Diferenciais implementados

- ✅ **Docker e docker-compose**: SQL Server, API e frontend com `docker compose up --build`
- ✅ **Testes de integração** com SQL Server real (Testcontainers), incluindo concorrência
- ✅ **Background worker** para agendamentos, seguro para múltiplas instâncias
- ✅ **Idempotência** via header `Idempotency-Key`
- ✅ **CI** no GitHub Actions (build e testes do backend, type-check e build do frontend)
- ✅ Check constraints no banco, health check (`/health`), ProblemDetails com `traceId`, logs estruturados
- ❌ **Domain Events** e **mensageria**: deixados de fora de propósito (ver trade-offs)

## Frontend (TransferFlow)

React 18 + TypeScript + Vite, **sem biblioteca de UI**: um design system pequeno e próprio (tokens em
`styles/global.css`, componentes com CSS Modules e ícones SVG desenhados para o projeto).

**Identidade.** "TransferFlow: Gestão de Transferências Financeiras". A paleta é sóbria: esmeralda como
única cor de marca, neutros quentes e cores de status só em badges e mensagens. Tipografia IBM Plex Sans /
Plex Mono, com números tabulares para os valores ficarem alinhados em coluna.

| Tela | Destaques |
|---|---|
| **Dashboard** | Posição consolidada (disponível, saldo, cheque especial em uso, contas ativas), indicadores (concluídas, agendadas, recusadas, tentativas na última hora), transferências recentes e tabela de contas |
| **Nova transferência** | Campo monetário no padrão bancário (digita-se centavos), resumo lateral com saldo antes/depois e avisos, comprovante de sucesso ou de recusa |
| **Agendar transferência** | Data e hora com validação de futuro, "data programada" por extenso no resumo |
| **Consultar transferência** | Detalhes (só os campos que existem), linha do tempo derivada das datas da API, cancelamento com diálogo de confirmação |
| **Consultar conta** | Saldo (negativo destacado), limite com medidor de uso do cheque especial, disponível para transferência e extrato com entradas/saídas |

**Decisões de UX**
- Estados de carregamento (skeleton), vazio e erro em todas as telas. Botões ficam desabilitados durante as requisições.
- Validação no cliente só para o que certamente seria 400 (campos vazios, valor zero, data passada).
  Saldo, limites e status continuam sendo decididos pela API, e a recusa (422) vira um comprovante de recusa.
- **Idempotência no cliente**: cada envio usa uma `Idempotency-Key`. Se a rede falhar e o usuário tentar de
  novo com os mesmos dados, a chave é reaproveitada e a operação não é duplicada.
- Falha de conexão (sem resposta, 502/503/504) mostra "Não foi possível conectar ao servidor" com
  **Tentar novamente**. Status HTTP e `traceId` ficam em "Detalhes técnicos".
- Indicador de status da API no topo, via `GET /health`.
- Rotas por hash (`#/transferir`, `#/transferencias/{id}`): voltar/avançar e links diretos funcionam sem configuração de servidor.
- Responsivo: sidebar vira menu no mobile e as tabelas viram listas em telas pequenas.
- Acessibilidade: HTML semântico, labels ligados aos campos (`aria-describedby` para dicas e erros), foco visível,
  `<dialog>` nativo, "pular para o conteúdo" e respeito a `prefers-reduced-motion`.

**Métricas do dashboard.** A API não tem endpoint de estatísticas, então nada é inventado: as métricas são
calculadas a partir dos endpoints existentes (lista de contas e extrato de cada conta, até 100 transferências
por conta), e a tela informa isso.

```
frontend/src/
├── api/          cliente HTTP, erros tipados (ApiError) e um serviço por recurso
├── hooks/        useAsync, useAccounts, useDashboardData, useTransferSubmission, useRoute, useApiHealth
├── lib/          formatação, dinheiro em centavos, validação, métricas, prévia da operação (funções puras)
├── components/
│   ├── layout/   AppLayout, Sidebar, Topbar, PageHeader, Logo
│   ├── ui/       Button, Card, Alert, StatusBadge, Money, FormField, MoneyInput, TextInput,
│   │             ConfirmDialog, EmptyState, ErrorState, Skeleton, Spinner, CopyButton, Icon
│   ├── accounts/ AccountSelect, AccountsTable, OverdraftMeter
│   └── transfers/TransferTable, TransferSummary, TransferReceipt, TransferFacts, TransferTimeline
├── pages/        Dashboard, NewTransfer, ScheduleTransfer, TransferLookup, Account
└── styles/       tokens e estilos base
```

### Erro 502 no docker-compose: causa e correção

O 502 vinha do Nginx do container do frontend, que não conseguia falar com a API:
1. O Nginx resolve o nome `api` **uma única vez**, ao subir. Se a API ainda estava iniciando (aguardando o
   SQL Server e aplicando migrations) ou fosse reiniciada, ele ficava com um endereço inválido.
   **Correção:** `resolver 127.0.0.11` + upstream em variável, o que força nova resolução a cada 10 s.
2. O frontend só esperava o container da API existir, não ficar pronto.
   **Correção:** healthcheck na API (`GET /health`, que só responde depois das migrations), `depends_on:
   condition: service_healthy` no frontend e `restart: unless-stopped`.

Se a API estiver realmente fora do ar, a interface mostra o estado de "servidor indisponível" com opção de
tentar novamente, em vez de uma mensagem técnica.
