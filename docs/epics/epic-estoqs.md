# Epic — Manage Estoqs

**Goal:** full lifecycle management of Estoqs records at `/estoqs`.

## Stories

- `story-estoqs-crud` — list, create, view, edit, delete Estoqs.

## Business rules affecting this entity

- Tem certeza que deseja excluir este código?
- Tem certeza que deseja excluir este código?
- Tem certeza que deseja excluir este código?
- Transicao de estado: campo = '^FO510,40^A0R,150,36^FD'+estoq.Descri+'^FS' (STATE_TRANSITION)
- Transicao de estado: campo = '^FO420,360^A0R,100,50^FD'+Alltrim(estoq.embalag)+' c/ '+Trans(estoq.embalqt)+'^FS' (STATE_TRANSITION)

## Definition of done

- CRUD works end to end; required fields validated; relations resolve; business rules enforced.
