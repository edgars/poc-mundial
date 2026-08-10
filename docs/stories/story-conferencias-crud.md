# Story — Conferencias CRUD

**As an** operator, **I want** to manage Conferencias records, **so that** the data stays current.

## Context

- Entity: `Conferencia` (table `conferencia`)
- Routes: list `/conferencias`, create `/conferencias/new`, edit `/conferencias/[id]/edit`
- API base: `/api/conferencias`

## Fields (render in order)

| Field | Label | Component | Required | List column |
|---|---|---|---|---|
| descri | Descri | input | no | yes |
| acesso | Acesso | input | no | yes |
| contem | Contém | input | no | yes |
| qtde | Qtde | input | no | yes |
| timer1 | Timer1 | input | no | yes |
| dataenvironment | Dataenvironment | date | no | no |
| doca | Doca | input | no | no |
| dun14 | Dun14 | input | no | no |
| ean13 | Ean13 | input | no | no |
| embalqt | Embalqt | input | no | no |
| listnf | Listnf | select | no | no |
| listprod | Listprod | select | no | no |

## Acceptance criteria

- [ ] `GET /api/conferencias` returns a paginated list.
- [ ] `POST /api/conferencias` creates a record after validating the body.
- [ ] `GET /api/conferencias/:id` returns one record.
- [ ] `PUT /api/conferencias/:id` updates a record.
- [ ] `DELETE /api/conferencias/:id` deletes a record.
- [ ] List page shows only *list column* fields, with search + pagination.
- [ ] Create and edit forms render all fields in order; required fields validated.
