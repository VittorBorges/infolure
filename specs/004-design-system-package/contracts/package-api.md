# Contrato — API pública de `@infolure/design-system`

Contrato interno do frontend: o que o pacote **exporta** e como os consumidores o usam. Não é um
contrato de rede (o contrato de API mantém-se nas features 001/002). Define a superfície estável que
o admin e o público (piloto) passam a depender.

## 1. Entradas do `exports` (package.json)

| Subpath | Conteúdo | Consumidor |
|---|---|---|
| `@infolure/design-system` | Barrel JS (componentes + `cn`) + tipos `.d.ts` | Código React (admin, público) |
| `@infolure/design-system/tokens.css` | `@theme` com os tokens (fonte única) | CSS de entrada de cada app (`@import`) |
| `@infolure/design-system/styles.css` | (opcional) CSS base/preflight-friendly do DS, se necessário | CSS de entrada de cada app |

> O JS dos componentes é o artefacto construído (`dist`, ESM + `.d.ts`), com as diretivas `'use client'`
> preservadas. As utilities Tailwind **não** são embutidas no pacote — são geradas por cada app via
> `@source` (ver research §3).

## 2. Componentes exportados (barrel)

Migrados da feature 003, sem alteração de API:

| Export | Notas |
|---|---|
| `Button`, `buttonVariants` | variantes: default, success, destructive, outline, secondary, ghost, link |
| `Card`, `CardHeader`, `CardTitle`, `CardDescription`, `CardContent`, `CardFooter` | |
| `Table`, `TableHeader`, `TableBody`, `TableRow`, `TableHead`, `TableCell` | |
| `Input` | |
| `Select`, `SelectTrigger`, `SelectContent`, `SelectItem`, `SelectValue`, … | client (Radix) |
| `Label` | client (Radix) |
| `Dialog`, `DialogContent`, `DialogHeader`, `DialogTitle`, `DialogDescription`, `DialogFooter`, … | client (Radix) |
| `Badge`, `badgeVariants` | variantes: default, secondary, success, destructive, outline, muted |
| `cn` | helper de composição de classes (clsx + tailwind-merge) |

**Invariante**: a API (nomes, props, variantes) é **idêntica** à da 003 — a migração do admin é só
troca de origem do import, sem mudanças de uso (suporta SC-002, paridade).

## 3. Tokens (fonte única) — `tokens.css`

`@theme` com a paleta "Azul SaaS + Verde fresco" da 003 (valores em
`specs/003-admin-design-system/contracts/design-tokens.md` §1): `--background #FFFFFF`,
`--foreground #0F172A`, `--primary #2563EB`, `--success #16A34A`, `--destructive #DC2626`,
`--border #E2E8F0`, `--ring #2563EB`, `--radius 0.625rem`, etc. **Tema claro fixo** (sem `.dark`,
sem `@media prefers-color-scheme`).

## 4. Como um consumidor usa o pacote

```text
# package.json da app
"dependencies": { "@infolure/design-system": "*" }

# next.config.ts
transpilePackages: ['@infolure/design-system']

# CSS de entrada da app (ex.: app/admin/admin.css)
@import 'tailwindcss';
@import '@infolure/design-system/tokens.css';
@source '../../../packages/design-system/src';   /* gera as utilities usadas pelo pacote */

# componente
import { Button, Card, Badge, cn } from '@infolure/design-system';
```

## 5. Invariantes (o que NÃO pode mudar)

- A API pública dos componentes é a da 003 (sem regressões — SC-002).
- Os tokens são **uma única fonte** no pacote; nenhuma app redefine os seus próprios (FR-003).
- O pacote **não** embute utilities Tailwind (evita duplicação — FR-009/SC-006).
- Backend e contratos de API: inalterados.
