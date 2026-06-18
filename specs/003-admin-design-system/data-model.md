# Data Model — Feature 003: Design System do Backoffice Admin

**Não aplicável.** Esta feature é puramente de apresentação (frontend) e **não introduz, altera ou
remove entidades de dados**, schema de base de dados, migrations EF Core ou contratos de API.

- Sem novas tabelas/colunas; o modelo de dados das features 001/002 mantém-se intacto.
- Os dados consumidos pelo painel continuam a vir dos endpoints existentes
  (`/v1/admin/*`, `/v1/admin/dashboard`, `/v1/admin/audit`), descritos em
  `specs/002-admin-indexing-audit/contracts/admin-api.yaml` e `.../data-model.md` — que permanecem a
  fonte de verdade.

O único "contrato" novo é o de **tokens de design** (interno ao frontend), documentado em
[contracts/design-tokens.md](contracts/design-tokens.md).
