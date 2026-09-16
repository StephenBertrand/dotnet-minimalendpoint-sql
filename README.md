# Feature-folder minimal APIs + App Configuration + Azure SQL

Sibling of [dotnet-minimalendpoint-appconfig](https://github.com/StephenBertrand/dotnet-minimalendpoint-appconfig). Same **.NET 10** minimal API and `Features/` layout, plus **SQL-backed Todos** and Azure-oriented identity.

| Environment | App Configuration | SQL |
| --- | --- | --- |
| Local (`aspire run` / AppHost) | Official emulator container (image `1.2.0`) | Azure SQL resource **run as a SQL Server container** (SQL auth, Aspire-generated password) |
| Publish to Azure | Real App Configuration store, **DefaultAzureCredential** / managed identity | **Azure SQL with Microsoft Entra authentication**. Aspire grants the app’s **managed identity** database access. No SQL passwords in app settings. |

The API does not branch on “local vs Azure”. It uses connection name `todos` and `appconfiguration`. Aspire injects the right shape of connection.

## What you get

| Path | Behavior |
| --- | --- |
| `web` `/` | Playground UI (separate Aspire project) |
| `web` `/api/*` | YARP forwarder → `api` via Aspire service discovery |
| `api` `GET /hello` | `Hello World` / `Hello New World` via feature flag `HelloNewWorld` |
| `api` `GET /ping` | Health-style ping |
| `api` `GET/POST/PUT/DELETE /todos` | EF Core CRUD against SQL |

## Prerequisites

- .NET 10 SDK
- Docker (App Config emulator + SQL Server container). On Apple Silicon the App Config emulator is pinned to **1.2.0**.
- Optional: Azure subscription to publish (`aspire deploy` / your pipeline) with a user that can assign roles

## Run

```bash
dotnet run --project src/AppHost
```

Open the **web** resource in the Aspire dashboard. The UI calls `/api/...` on itself; YARP forwards those requests to **api** through Aspire service discovery (no CORS, no hardcoded ports). The **api** resource remains available for curl and OpenAPI.

## Identity and secrets (the point of this sample)

- **Do not** put SQL passwords or App Configuration keys in `appsettings.json`.
- **Local container:** Aspire creates a SQL login and injects `ConnectionStrings__todos`. That password never needs to live in git.
- **Azure:** `AddAzureSqlServer` defaults to Entra-only admin. On deploy, Aspire’s SQL script creates a contained database user for the **application managed identity** and assigns data roles. The EF integration uses **DefaultAzureCredential** (`Active Directory Default`): in Azure that is the MI; on a laptop it can be `az login` against a real server.
- App Configuration in Azure should also use Entra (`App Configuration Data Reader` on the app MI), not a connection string in source.

## Add a feature

1. Folder under `src/Api/Features/<Name>/`
2. Implement `IEndpoint`
3. `AddFeatureEndpoints` registers it

SQL tables belong next to the feature (`TodosDbContext`) until a second feature needs a shared context.

## Tests

```bash
dotnet test
```

Hello and Ping use `WebApplicationFactory` only. Todos tests start a **SQL Server Testcontainer**, apply EF migrations, and run real CRUD (not the in-memory provider). Docker must be running.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Api --startup-project src/Api
```

AppHost applies pending migrations at API startup (`Database.MigrateAsync`) when the provider is SQL Server.
