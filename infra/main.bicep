// Esqueleto de infraestrutura Azure (West Europe — RGPD) para o Infolure.
// Recursos detalhados a preencher na fase de launch (tasks T088).
// Deploy: az deployment group create --resource-group <rg> --template-file main.bicep

@description('Região dos recursos (residência de dados na UE).')
param location string = 'westeurope'

@description('Prefixo de nomes dos recursos.')
param namePrefix string = 'infolure'

@description('Ambiente (dev | staging | prod).')
@allowed([ 'dev', 'staging', 'prod' ])
param environmentName string = 'dev'

// TODO(T088): módulos a implementar
//   - module postgres   'modules/postgres.bicep'   (Azure Database for PostgreSQL Flexible Server)
//   - module redis      'modules/redis.bicep'      (Azure Cache for Redis)
//   - module blob       'modules/storage.bicep'    (Blob Storage + Front Door/CDN)
//   - module api        'modules/containerapp.bicep'(Container App — Infolure.Api)
//   - module web        'modules/containerapp.bicep'(Container App — Next.js)

output resourcePrefix string = '${namePrefix}-${environmentName}'
output location string = location
