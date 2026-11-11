# FCG — Catalog API

Microsserviço de **catálogo e compras** da FIAP Cloud Games: CRUD de jogos, **início do fluxo de compra** e manutenção da **biblioteca** do usuário. A compra é assíncrona: o pedido nasce `Pending` e só vira `Approved`/`Rejected` quando o resultado do pagamento chega por evento.

> Parte da arquitetura de microsserviços da FIAP Cloud Games. Visão geral, diagramas, decisões (ADRs) e matriz de
> requisitos no repositório [fcg-orchestration](https://github.com/Phsynnistro/fcg-orchestration).

**Stack:** .NET 10 · ASP.NET Core Minimal APIs · EF Core 10 + PostgreSQL 17 · MassTransit 8.5 + RabbitMQ (Outbox/Inbox) · OpenTelemetry · xUnit

## Responsabilidades

- CRUD de jogos (escrita restrita a Admin; exclusão é *soft delete*).
- Receber a requisição de compra ("adicionar um jogo à biblioteca") e publicar `OrderPlacedEvent` (UserId, GameId, Price + OrderId para correlação).
- Consumir `PaymentProcessedEvent`: se aprovado, adicionar o jogo à biblioteca; se recusado, registrar o motivo.
- Expor o status do pedido e a biblioteca do usuário.

## Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/games?search=&genre=&page=&pageSize=` | Anônimo | Catálogo paginado |
| `GET` | `/api/games/{id}` | Anônimo | Detalhe do jogo |
| `POST` | `/api/games` | Admin | Cadastra jogo → 201 |
| `PUT` | `/api/games/{id}` | Admin | Atualiza jogo |
| `DELETE` | `/api/games/{id}` | Admin | Remove do catálogo (soft delete) → 204 |
| `POST` | `/api/orders` | Autenticado | **Inicia a compra** → 202 Accepted + `Location` |
| `GET` | `/api/orders/{id}` | Dono ou Admin | Status do pedido (`Pending`/`Approved`/`Rejected`) |
| `GET` | `/api/orders` | Autenticado | Meus pedidos |
| `GET` | `/api/library` | Autenticado | Minha biblioteca |

Todos os serviços expõem `/health/live` (liveness), `/health/ready` (readiness) e `/health` (diagnóstico em JSON).
Erros seguem **Problem Details (RFC 9457)** com `traceId`.

Documentação interativa: http://localhost:5102/scalar (com `dotnet run`) ou http://localhost:5002/scalar (compose).

## Eventos

| Direção | Evento | Exchange | Fila deste serviço | Campos principais |
|---|---|---|---|---|
| Publica | `OrderPlacedEvent` | `fcg.order-placed` | — | `OrderId`, `UserId`, `GameId`, `Price`, `UserEmail`, `GameTitle`, `PlacedAt` |
| Consome | `PaymentProcessedEvent` | `fcg.payment-processed` | `catalog.payment-processed` | `OrderId`, `Status` (`Approved`/`Rejected`), `Reason`, … |

Contratos em `src/Fcg.Catalog.Api/Contracts/Events`, namespace **`Fcg.Contracts.Events`** — idêntico em todos os
repositórios (o MassTransit identifica a mensagem por namespace + nome). Altere-os apenas de forma **aditiva**;
o `ContractTests` falha se a identidade do contrato mudar.

## Variáveis de ambiente

Configuração .NET via variáveis de ambiente: `__` (dois underscores) equivale a `:` — `RabbitMq__Host` → `RabbitMq:Host`.

| Variável | Exemplo | Origem no K8s | Descrição |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | ConfigMap | Ambiente do ASP.NET Core. |
| `RabbitMq__Host` | `rabbitmq` | ConfigMap | Host do broker (nome do Service no cluster). |
| `RabbitMq__Port` | `5672` | ConfigMap | Porta AMQP. |
| `RabbitMq__VirtualHost` | `/` | ConfigMap | Virtual host do RabbitMQ. |
| `RabbitMq__Username` | `fcg` | **Secret** | Usuário do broker. |
| `RabbitMq__Password` | `••••` | **Secret** | Senha do broker. |
| `ConnectionStrings__Database` | `Host=catalog-db;Port=5432;Database=fcg_catalog;Username=fcg_catalog;Password=••••` | **Secret** | Connection string do PostgreSQL exclusivo do serviço. |
| `ApiDocs__Enabled` | `true` | ConfigMap | Expõe `/openapi/v1.json` e a UI Scalar em `/scalar`. |
| `Jwt__Issuer` | `fcg-users-api` | ConfigMap | Emissor esperado no token. |
| `Jwt__Audience` | `fcg` | ConfigMap | Audiência esperada no token. |
| `Jwt__MetadataAddress` | `http://users-api:80/.well-known/openid-configuration` | ConfigMap | Discovery do UsersAPI (nome do Service); a chave pública (JWKS) é baixada com retry/circuit breaker e cacheada. |
| `Jwt__RequireHttpsMetadata` | `false` | ConfigMap | `false` só dentro do cluster/compose. |
| `Messaging__Queues__PaymentProcessed` | `catalog.payment-processed` | ConfigMap | Fila deste serviço para `PaymentProcessedEvent`. |
| `Messaging__PrefetchCount` | `16` | ConfigMap | Mensagens pré-buscadas por consumidor. |
| `Messaging__ConcurrentMessageLimit` | `8` | ConfigMap | Processamento concorrente por réplica (bulkhead). |
| `Seed__SampleGames` | `true` | ConfigMap | Popula o catálogo com 6 jogos de exemplo (um acima do limite de aprovação). |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://jaeger:4317` | ConfigMap | Coletor OTLP (Jaeger). Ausente ⇒ tracing não é exportado. |

Valores sensíveis (**Secret**) nunca entram no `appsettings.json`. O `appsettings.Development.json` contém valores
**apenas de desenvolvimento local**.

## Executar localmente

```bash
# 1) infraestrutura (a partir do repositório de orquestração)
cd ../fcg-orchestration && docker compose up -d rabbitmq jaeger catalog-db users-api

# 2) o serviço
cd ../fcg-catalog-api
dotnet run --project src/Fcg.Catalog.Api        # http://localhost:5102
```

> A validação de JWT busca a chave pública no UsersAPI (`Jwt:MetadataAddress`), por isso ele sobe junto.

## Testes

```bash
dotnet test
```

Testes de domínio (regras e invariantes) e de **contrato** (identidade dos eventos).

## Docker

```bash
docker build -t fcg/catalog-api:1.0.0 .            # imagem final: runtime chiseled, não-root, porta 8080
docker build --target test .                    # (opcional) roda os testes dentro do build
```

Dockerfile **multi-stage**: `build` (SDK) → `test` (opcional) → `final` (`aspnet:10.0-noble-chiseled`).
O CI analisa a imagem com **Trivy** e falha em vulnerabilidade CRITICAL/HIGH corrigível.

## Kubernetes

| Arquivo em [`k8s/`](k8s) | Conteúdo |
|---|---|
| `00-namespace.yaml` | Namespace `fcg` (permite aplicar este repositório isoladamente) |
| `01-configmap.yaml` | ConfigMap: filas, URLs de serviços, limites |
| `02-secrets.yaml` | Secrets: credenciais, connection string e senha do banco |
| `03-database.yaml` | PostgreSQL: PersistentVolumeClaim + Deployment (`Recreate`) + Service |
| `04-deployment.yaml` | Deployment da API: probes, recursos, securityContext, anotações |
| `05-service.yaml` | Service ClusterIP `catalog-api:80` |
| `06-hpa.yaml` | HorizontalPodAutoscaler: 2–5 réplicas por CPU |

```bash
# pré-requisito: RabbitMQ e Jaeger do repositório fcg-orchestration (ou o ambiente completo dele)
cd k8s && kubectl apply -f .
kubectl -n fcg get pods -l app.kubernetes.io/name=catalog-api
```

Este `/k8s` é a **fonte da verdade** do serviço. O repositório de orquestração mantém uma cópia sincronizada
(`scripts/k8s-sync-manifests.sh`) para subir o ambiente inteiro com um único `kubectl apply -f .` (ADR-0013).

## Estrutura

```
src/Fcg.Catalog.Api/
├── Contracts/Events/                OrderPlacedEvent, PaymentProcessedEvent (cópias idênticas)
├── Domain/                          Game, Order (máquina de estados), LibraryItem
├── Application/Games/               GameService (CRUD)
├── Application/Orders/              PlaceOrderHandler, ProcessPaymentResultHandler, OrderQueries
├── Application/Library/             LibraryQueries
├── Infrastructure/Persistence/      CatalogDbContext, mapeamentos, seed de jogos
├── Infrastructure/Messaging/        MessagingOptions (nomes de fila do ConfigMap)
├── Infrastructure/Security/         validação de JWT via JWKS (HttpClient resiliente)
├── Infrastructure/Observability/    OpenTelemetry
└── Presentation/                    endpoints, consumer PaymentProcessedConsumer, health
tests/Fcg.Catalog.UnitTests/
k8s/                                 manifestos Kubernetes (00 a 06)
Dockerfile · Fcg.Catalog.sln · Directory.Build.props · Directory.Packages.props (versões centralizadas) · global.json
```

## Decisões de design

- **UserId vem do token** e **preço vem do banco** — nunca do corpo da requisição (evita IDOR e manipulação de preço).
- **202 Accepted** na compra: o resultado depende de outro serviço; o cliente acompanha `GET /api/orders/{id}` (consistência eventual).
- **Índice único parcial** `(UserId, GameId) WHERE Status = 'Pending'`: dois cliques simultâneos não geram duas cobranças.
- **Outbox + Inbox**: pedido e evento gravados atomicamente; `PaymentProcessedEvent` reentregue é descartado pelo inbox e pedido fora de `Pending` é ignorado (idempotência em camadas).
- **Resiliência** (Aula 05): retry exponencial e circuit breaker (*kill switch*) no consumo; retry/circuit breaker/timeout no download do JWKS (ADR-0012).
- **Snapshot de preço** no pedido e **soft delete** de jogos (pedidos e bibliotecas os referenciam).
- Pedido inexistente ao receber `PaymentProcessedEvent` **lança exceção** → retry → fila `_error` para investigação (engolir esconderia bug).
