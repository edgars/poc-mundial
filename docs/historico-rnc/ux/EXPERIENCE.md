# UX — Experience & Flows

Every managed entity follows the same flow:

1. **List** — the user opens `/entity` and sees a table of records (only the *list column* fields), with search and pagination.
2. **Create** — the user clicks *New*, fills the form (all fields, in order), saves.
3. **Edit** — the user clicks a row, edits the prefilled form, saves or deletes.

## Per-entity routes

| Entity | List | Create | Edit |
|---|---|---|---|
| Conferencias | `/conferencias` | `/conferencias/new` | `/conferencias/[id]/edit` |
| Estoqs | `/estoqs` | `/estoqs/new` | `/estoqs/[id]/edit` |

