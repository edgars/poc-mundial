# Architecture — Mundial - Conferência

## Stack

- Frontend: angular
- Backend: dotnet-core + prisma
- Database: sqlserver

## Component directives (author-supplied — follow VERBATIM; override derived values)

- **Frontend App** (frontend): O modelo de dados fidedigno ao RNC URI
- **API Service** (backend): O modelo de dados fidedigno ao RNC URI
- **Relational DB** (database): O modelo de dados fidedigno ao RNC URI

## Deployment — docker-compose

Ship a single `docker-compose.yml` at the repo root:

- One service per architecture component: frontend (`web`), backend (`api`), database (`db`, engine sqlserver).
- Use each component's port as configured on the canvas (frontend default 3000, API default 5000, database engine default). A component directive that names a port wins over these defaults.
- The database gets a named volume so data survives `docker compose down`.
- Services reference each other by service name (`api`, `db`) — never `localhost`.
- All secrets and connection strings come from `.env` (documented in `.env.example`).
- Multi-stage Dockerfiles per app service; production images run the built app, not a dev server.

## Data model

### Conferencia (`conferencia`)

| Column | Type | Nullable |
|---|---|---|
| id | Int (PK, auto) | no |
| qtdRec | Decimal? | yes |
| dataConf | DateTime? | yes |
| matrConf | String? | yes |
| dun14 | String? | yes |
| acesso | String? | yes |
| doca | Int? | yes |
| itnf | Decimal? | yes |
| dataMov | DateTime? | yes |
| qtdNf | Decimal? | yes |
| qtdUnidNf | Decimal? | yes |
| qtdUnidRec | Decimal? | yes |
| matrFec | String? | yes |
| matrLib | String? | yes |
| situacao | String? | yes |
| status | Boolean? | yes |
| fechado | Boolean? | yes |
| pendencia | Boolean? | yes |
| rAcor | Int? | yes |
| finan | String? | yes |
| dtHora | DateTime? | yes |
| pasta | Int? | yes |
| placa | String? | yes |
| cartao | Int? | yes |
| destino | Int? | yes |
| dataValid | DateTime? | yes |
| lote | String? | yes |
| media | Decimal? | yes |
| pesoBrutoCol | Decimal | no |
| balanca | Boolean | no |
| contem | String? | yes |
| qtde | String? | yes |
| ean13 | Decimal? | yes |
| listnf | String? | yes |
| listprod | String? | yes |
| dataenvironment | String? | yes |
| descri | String? | yes |
| embalqt | Decimal? | yes |
| timer1 | String? | yes |

### Estoq (`estoq`)

| Column | Type | Nullable |
|---|---|---|
| id | Int (PK, auto) | no |
| barrEmb | String? | yes |
| codigo | String? | yes |
| barrEmb2 | String? | yes |
| descri | String? | yes |
| embalag | String? | yes |
| embalqt | String? | yes |
| barrEmb3 | Decimal? | yes |
| dataenvironment | String? | yes |
| codigo1 | Decimal? | yes |

## Architecture Decision Records

### ADR-001 — Target stack

**Decision:** build on angular / dotnet-core / sqlserver.
**Why:** chosen in the RNC Architecture Canvas as a supported, modern replacement for the legacy stack.

### ADR-002 — One module per managed entity

**Decision:** each managed entity gets its own route group, API, validation schema and pages.
**Why:** mirrors the legacy screen-per-entity structure and keeps the app navigable.

### ADR-003 — Reference tables are lookups, not screens

**Decision:** a table with no legacy edit screen gets only a read-only list API and is rendered as a dropdown inside the entities that reference it; the foreign key stores the related record's id.
**Why:** reproduces the legacy lookup behavior without inventing CRUD the original app never had.

### ADR-004 — Author directives override derived values

**Decision:** any *Component directive* or *Global directive* in this pack was written by the architecture author and wins over any value the generator derived (ports, versions, libraries, wiring).
**Why:** directives carry intent the canvas form fields cannot express; they are reproduced verbatim precisely so nothing is lost in translation.
