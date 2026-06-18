# Research — Feature 005: Formulário de Registo e Edição de Iscas

Decisões de design tomadas na Fase 0. Cada uma resolve uma incógnita do Technical Context.

---

## D1 — Modelo de cor: lista de hex em coluna JSONB na própria `lure_colors`

**Decision**: A cor fica só em `lure_colors` (decisão do utilizador). Substituir
`hex_primary`/`hex_secondary` por uma coluna `hex_codes JSONB` que guarda um array ordenado de
`{ hex, label }` — cada `hex` é um código HTML válido (`#RGB`/`#RRGGBB`) e `label` é a cor de base
opcional (ex.: "verde"). Uma variante "verde e amarelo" são dois itens do array. `lure_colors`
mantém `name_pt`/`name_en`/`pattern`. **Sem tabela filha.**

**Rationale**: O pedido descreve "cores de base" (verde+amarelo) **e** "uma lista de códigos html";
um array de `{hex, label}` cobre ambos com um só conceito. Mantê-lo numa coluna JSONB na própria
`lure_colors` (em vez de tabela filha) atende ao pedido explícito do utilizador de "a tabela de cor
pode ser só lure_colors" e simplifica a escrita (a cor inteira grava-se numa linha). A ordenação é a
do array. Como não há cores reais, a remoção do par fixo não exige migração.

**Alternatives considered**:
- Tabela filha `lure_color_hex_codes` (1→N) — preterida a pedido do utilizador; daria ordenação/
  validação por linha mas adiciona uma tabela e joins desnecessários para este âmbito.
- Manter `hex_primary`/`hex_secondary` — rejeitado antes (duplica o conceito; catálogo sem dados).
- Validação: como o conteúdo é JSONB, o formato do hex valida-se no `LureWriteValidator` (não por
  check-constraint de coluna).

---

## D2 — Lista de tamanhos como fonte única (remove escalares)

**Decision**: Nova tabela `lure_sizes (id, lure_id, code, label, length_mm, weight_g, sort_order)`,
**fonte única** de peso/comprimento (`code` = código curto/SKU opcional; `label` = designação do
fabricante). As colunas escalares `lures.weight_g`/`length_mm` e o índice `idx_lures_weight` são
**removidos**. A busca/listagem da feature 001 passa a indexar os pesos derivados de `lure_sizes`
(ex.: min/max ou array por isca) no `LureIndexer`, e o filtro de peso do catálogo público é ajustado
em conformidade — sem alterar o parâmetro público do contrato.

**Rationale**: Decisão do utilizador — não existem dados reais, logo não há migração nem necessidade
de compatibilidade com as colunas antigas. Um modelo de fonte única é mais limpo e elimina a
duplicação (Princípio I). O custo é tocar no indexador/catálogo da 001, justificado por evitar manter
dois locais de verdade para o peso.

**Alternatives considered**:
- Denormalizar um tamanho representativo em `lures.weight_g` (manter escalares sincronizados) —
  preterido pelo utilizador: mantinha duplicação só para não tocar na busca; sem dados, não compensa.
- Manter escalares e a lista em paralelo — rejeitado: duas fontes de verdade.

---

## D3 — Upload de foto por cor via Azure Blob

**Decision**: Implementar `BlobUploadService` sobre `Azure.Storage.Blobs` (já no `.csproj`),
exposto por `POST /v1/admin/media` (multipart) que valida tipo/tamanho, grava no container de fotos
(West Europe) e devolve a URL pública. A URL é persistida em `lure_images` com `color_id` definido.
A connection string vem de configuração/user-secrets (`Azure:Blob:ConnectionString`); em
dev/local pode apontar para Azurite.

**Rationale**: O pedido quer **anexar** uma foto, não só referenciar uma URL. A dependência Blob já
existe e a infra do projeto é Azure West Europe (CLAUDE.md). Reutiliza-se `lure_images` (já tem
`color_id`/`is_primary`), evitando nova tabela.

**Alternatives considered**:
- Só URL externa — rejeitado: UX fraca, não verificável.
- Supabase Storage — considerado (o projeto usa Supabase Auth) mas preterido: a stack de
  armazenamento alvo é Azure e a dependência Blob já está presente. Continua viável trocar o
  `BlobUploadService` por um adaptador Supabase sem mexer no contrato.

---

## D4 — Escrita transacional via serviço dedicado

**Decision**: Criar `LureWriteService` que, numa transação, faz upsert da isca + `lure_translations`
(descrição) + `lure_sizes` + `lure_colors` + `lure_color_hex_codes`, com estratégia
replace-children na edição (apaga e recria as filhas a partir do payload). Reindexa via `LureIndexer`
no fim (best-effort, como no código atual).

**Rationale**: O CRUD genérico (`AdminResourceService`) não cobre coleções aninhadas. Um serviço
dedicado mantém o controller fino e a operação atómica (Princípio IV — testável; consistência).
Replace-children é o mais simples e correto para um formulário que envia o estado completo.

**Alternatives considered**:
- Diff incremental das filhas — rejeitado por complexidade desnecessária (YAGNI); o formulário
  submete o conjunto completo.
- Endpoints separados por coleção — rejeitado: pior atomicidade e UX (várias chamadas por gravação).

---

## D5 — Validação de hex (backend + frontend)

**Decision**: Backend valida com FluentValidation (`LureWriteValidator`): cada `hex` casa
`^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$`, normalizado para minúsculas; obrigatórios (slug, nome,
lure_type, ≥1 tamanho com peso) verificados; slug único. Frontend reusa a mesma regex num helper
`lib/hex.ts` para feedback imediato antes de submeter.

**Rationale**: Validar nas duas pontas dá feedback rápido (UX) sem confiar no cliente para
integridade (segurança). A regex é a forma HTML usual (`#RGB`/`#RRGGBB`).

**Alternatives considered**:
- Validar só no cliente — rejeitado (integridade). Só no servidor — rejeitado (UX pior).

---

## D6 — Biblioteca de formulário: manter padrão nativo

**Decision**: Não adicionar `react-hook-form`/`zod`. As listas dinâmicas (tamanhos, cores, hex por
cor) gerem-se com `useState` + atualizações imutáveis e subcomponentes controlados, seguindo o padrão
de `LureEditForm.tsx`/server actions já no projeto.

**Rationale**: Princípio I (YAGNI) e consistência com o existente. O contrato é OpenAPI (tipos via
`openapi-typescript`), não zod; introduzir uma stack de forms não se justifica para este âmbito.

**Alternatives considered**:
- `react-hook-form` + `zod` — rejeitado: nova dependência/padrão sem necessidade comprovada; a
  complexidade do form é gerível com estado nativo + um helper de validação de hex.
