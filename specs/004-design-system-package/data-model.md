# Data Model — Feature 004: Design System Partilhado + Storybook

**Não aplicável.** Esta feature é de **arquitetura de frontend e ferramentas** (extração de um pacote,
build, workspaces, Storybook). **Não introduz, altera ou remove** entidades de dados, schema de base
de dados, migrations EF Core ou contratos de API.

- O backend (`apps/api`, .NET/PostgreSQL) não é tocado.
- Os modelos de dados das features 001/002 mantêm-se intactos.

O único "contrato" novo é a **API pública do pacote** (componentes exportados + tokens), de natureza
interna ao frontend — documentado em [contracts/package-api.md](contracts/package-api.md).
