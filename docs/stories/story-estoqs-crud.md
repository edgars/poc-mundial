# Story — Estoqs CRUD

**As an** operator, **I want** to manage Estoqs records, **so that** the data stays current.

## Context

- Entity: `Estoq` (table `estoq`)
- Routes: list `/estoqs`, create `/estoqs/new`, edit `/estoqs/[id]/edit`
- API base: `/api/estoqs`

## Fields (render in order)

| Field | Label | Component | Required | List column |
|---|---|---|---|---|
| codigo1 | Codigo1 | input | no | yes |
| descri | Descri | input | no | yes |
| dataenvironment | Dataenvironment | date | no | yes |
| barrEmb2 | Barr Emb2 | input | no | yes |
| barrEmb3 | Barr Emb3 | input | no | yes |
| barrEmb | Barr Emb | input | no | no |

## Acceptance criteria

- [ ] `GET /api/estoqs` returns a paginated list.
- [ ] `POST /api/estoqs` creates a record after validating the body.
- [ ] `GET /api/estoqs/:id` returns one record.
- [ ] `PUT /api/estoqs/:id` updates a record.
- [ ] `DELETE /api/estoqs/:id` deletes a record.
- [ ] List page shows only *list column* fields, with search + pagination.
- [ ] Create and edit forms render all fields in order; required fields validated.
- [ ] Enforce: Tem certeza que deseja excluir este código?
- [ ] Enforce: Tem certeza que deseja excluir este código?
- [ ] Enforce: Tem certeza que deseja excluir este código?
- [ ] Enforce: Transicao de estado: campo = '^FO510,40^A0R,150,36^FD'+estoq.Descri+'^FS' (STATE_TRANSITION)
- [ ] Enforce: Transicao de estado: campo = '^FO420,360^A0R,100,50^FD'+Alltrim(estoq.embalag)+' c/ '+Trans(estoq.embalqt)+'^FS' (STATE_TRANSITION)
- [ ] If the RNC MCP server is connected: verified each rule above with `getRule` (see `bmad-context.md` § RNC MCP guardrail).
