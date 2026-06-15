# Research — Feature 002: Admin, Indexação e Base Auditável

Decisões técnicas que resolvem as incógnitas do plano. Formato: Decisão / Rationale / Alternativas.

---

## 1. Padrão de soft-delete + auditoria no EF Core (.NET 10)

**Decisão**: Interface `IAuditable { bool IsActive; string Source; DateTimeOffset? DeletedAt;
DateTimeOffset CreatedAt; DateTimeOffset UpdatedAt; }` implementada por todas as entidades. No
`AppDbContext.OnModelCreating`, após a configuração existente, iterar
`Model.GetEntityTypes()` e, para cada uma cujo CLR type implemente `IAuditable`, aplicar um **global
query filter** `e => e.DeletedAt == null`. Um `ISaveChangesInterceptor` (`AuditSaveChangesInterceptor`)
carimba `CreatedAt`/`UpdatedAt`, define `Source` (default `manual`) e converte `EntityState.Deleted`
em `Modified` com `DeletedAt = now` (soft-delete). O restauro/toggle é feito por serviços de admin
via `IgnoreQueryFilters()`.

**Rationale**: Aplicar por convenção (loop + interceptor) cobre as ~16 entidades sem editar query a
query nem entidade a entidade — exatamente o que o Pilar 0 pede e o que torna o SC-002/SC-009
sustentáveis. Centralizar no interceptor mantém DRY (Princípio I) e garante que nada escapa.

**Alternativas**:
- `Where(x => x.DeletedAt == null)` repetido em cada serviço → frágil, esquecível, polui o código.
- Shadow properties em vez de interface → menos legível e mais difícil de projetar nos DTOs do admin.
- Hard delete + tabela de histórico → contraria a decisão de soft-delete reversível (spec).

**Notas de implementação**: o interceptor deve ignorar entidades em `Deleted` que o admin queira
**purgar** (não há purga nesta fase, logo todo `Remove` vira soft-delete). `IsActive` não é tocado
pelo soft-delete (são ortogonais); o restauro repõe `DeletedAt = null` mantendo o `IsActive` anterior
(FR-004).

## 2. Query filter global vs. navegações requeridas (aviso do EF)

**Decisão**: Aplicar o mesmo filtro `DeletedAt == null` a **todas** as entidades `IAuditable`,
incluindo as dependentes (traduções, cores, imagens, etc.). Onde existir relação **requerida** com
filtro, manter os filtros consistentes para evitar o warning
`PossibleIncompleteQueryFilter`/resultados incoerentes. Para a **visibilidade pública por pai**
(FR-003a, marca→isca), aplicar a condição na query pública do catálogo/detalhe/indexer
(`l.Brand == null || (l.Brand.IsActive && l.Brand.DeletedAt == null)`), não como cascata de estado.

**Rationale**: O EF avisa quando uma entidade com filtro é navegada a partir de outra sem filtro
equivalente; manter o filtro uniforme evita incoerências. A cascata de *visibilidade* (não de estado)
é uma condição de consulta, alinhada com a clarificação Q1.

**Alternativas**:
- Cascata de estado real (propagar `DeletedAt` aos filhos) → rejeitada na clarificação (irreversível
  em massa, perde reversibilidade).
- Ocultar filhos via trigger na BD → mais opaco e fora do EF; viola observabilidade/simplicidade.

## 3. Enforcement de utilizador ativo/eliminado por requisição (FR-013a)

**Decisão**: Middleware `ActiveUserMiddleware` a correr **após** a autenticação JWT. Resolve o
utilizador local (por `provider_uid`/`sub` do JWT, como o `UserResolver` já faz) e, se
`!IsActive || DeletedAt != null`, responde **401** (sessão deixa de ser aceite). O estado é lido de
**cache Redis** (`user:active:{id}` com TTL curto, ex. 60s) e **invalidado** quando o admin
desativa/elimina o utilizador, garantindo efeito "imediato".

**Rationale**: O backend só valida a assinatura/lifetime do JWT Supabase (stateless) — não conhece o
`IsActive` local. Sem este check, "desativar utilizador" não tem efeito real (a sessão continuaria
válida até expirar). O cache evita uma ida à BD por requisição (performance).

**Alternativas**:
- Revogar tokens no Supabase → fora do nosso controlo total e mais lento a propagar; o utilizador
  pode ter um access token ainda válido. O check local é a fonte de verdade.
- Verificar só no login → não invalida sessões já abertas (viola "de imediato").

## 4. Origem da role admin: BD vs. claim do JWT

**Decisão**: A `AdminPolicy` passa a basear-se na **role da BD** (`users.role == 'admin'`). O
`ActiveUserMiddleware` (que já carrega o utilizador) injeta um claim `role` derivado da BD no
`ClaimsPrincipal`; a policy continua a usar `RequireClaim("role","admin")`, mas a fonte é a BD.

**Rationale**: A verdade da role vive na BD ([users.role](apps/api/src/Infolure.Api/Infrastructure/Persistence/Entities/Users.cs#L13)).
Depender de um claim emitido pelo Supabase exigiria sincronizar `app_metadata` e atrasaria alterações
de role (o utilizador teria de renovar o token). Reaproveitar o middleware mantém uma só leitura.

**Alternativas**:
- Claim de role via Supabase `app_metadata` → duplica a verdade e atrasa mudanças; mais infra.
- Policy a consultar a BD diretamente por requisição → o middleware já o faz; evitar dupla leitura.

## 5. `robots.ts` / `sitemap.ts` dinâmicos em Next.js 16 + cache do flag

**Decisão**: Implementar `app/robots.ts` e `app/sitemap.ts` como **dinâmicos**, lendo de um endpoint
público `GET /v1/seo` (flag global + lista mínima para o sitemap). O flag global é cacheado em
**Redis** no backend com **TTL ≤ 60s** e **invalidado** quando o admin o altera, satisfazendo o
SC-005 (< 60s). `generateMetadata` do detalhe define `robots: { index: false }` quando o flag global
está off **ou** a isca tem `is_indexable = false`; páginas de utilizador são sempre `noindex`.

**Rationale**: Robots/sitemap têm de refletir estado em runtime sem novo deploy (Pilar A). O cache no
backend protege a BD e o TTL curto + invalidação garante a janela de 60s. ⚠️ Confirmar a API exata de
`robots`/`sitemap`/`generateMetadata` nesta versão do Next em `node_modules/next/dist/docs/` antes de
codar (AGENTS.md sinaliza breaking changes; `middleware`→`proxy` já deprecado).

**Alternativas**:
- Ficheiros `robots.txt`/`sitemap.xml` estáticos → não controláveis em runtime (falha o Pilar A).
- Sem cache, ler a BD a cada pedido de robots → desnecessário; robots é muito acedido por crawlers.

## 6. Migration + backfill sem perda de dados (SC-008)

**Decisão**: Uma migration EF Core que adiciona, em todas as tabelas, `is_active boolean NOT NULL
DEFAULT true`, `source text NOT NULL DEFAULT 'manual'`, `deleted_at timestamptz NULL`, e
`created_at`/`updated_at` onde ainda não existem. Cria `app_settings` (singleton, com linha inicial
`seo_indexing_enabled = true` para preservar o comportamento atual da 001), `lures.is_indexable
boolean NOT NULL DEFAULT true` e `admin_audit_log`. **Backfill**: registos pré-existentes ficam
`is_active = true`, `deleted_at = null`; a `source` é definida conforme a proveniência — os dados de
seed/automação como `'automation'`, e (se aplicável) cargas iniciais como `'import'`. Validação por
contagem antes/depois.

**Rationale**: Defaults garantem coerência imediata e não-nulidade; a linha inicial de `app_settings`
mantém a indexação ligada (não regredir SEO silenciosamente). O backfill por proveniência cumpre a
clarificação sobre origem.

**Alternativas**:
- Colunas nullable sem default → estado incoerente e queries com `IS NOT FALSE` por todo o lado.
- `seo_indexing_enabled = false` por defeito → desligaria SEO sem decisão explícita; rejeitado.

## 7. Âmbito e segurança do CRUD de dados pessoais

**Decisão**: O CRUD admin cobre todas as entidades, incluindo `users`, `user_lure_favorites` e
`user_lure_inventory`. Operações sobre estas exigem: (a) aviso RGPD no frontend antes de confirmar
(FR-012), (b) entrada de auditoria com **instantâneo antes→depois** (FR-020a), (c) o soft-delete não
substitui a eliminação RGPD efetiva (FR-012a). Bloqueios de segurança: impedir auto-desativação/
eliminação que remova o próprio acesso e impedir remover o último admin (FR-013).

**Rationale**: Concentra os controlos RGPD num ponto previsível e auditável, dado o "poder máximo"
concedido. Mantém distinta a eliminação reversível da remoção efetiva.

**Alternativas**:
- Excluir dados pessoais do CRUD → contraria a decisão explícita do utilizador (Q3 da fase de
  discussão: "Tudo, incl. dados pessoais").
