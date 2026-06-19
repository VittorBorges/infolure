import { notFound } from 'next/navigation';
import Link from 'next/link';
import { adminFetch, ADMIN_RESOURCES, PERSONAL_RESOURCES, type AdminResource, type PagedResponse } from '../../../lib/admin';
import { RowActions } from '../../../components/admin/RowActions';
import {
  Button,
  Input,
  Badge,
  Card,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@infolure/design-system';

export const dynamic = 'force-dynamic';

type SP = { q?: string; include?: string; page?: string };

function isResource(r: string): r is AdminResource {
  return (ADMIN_RESOURCES as readonly string[]).includes(r);
}

const STATUS_VARIANT: Record<string, 'success' | 'secondary' | 'muted'> = {
  published: 'success',
  draft: 'secondary',
  archived: 'muted',
};

// Renderização de cada célula com semântica de cor nos campos de estado (FR-007/SC-005).
function renderCell(column: string, value: unknown): React.ReactNode {
  if (column === 'is_active') {
    return value === true ? <Badge variant="success">ativo</Badge> : <Badge variant="muted">inativo</Badge>;
  }
  if (column === 'deleted_at') {
    return value != null ? <Badge variant="destructive">eliminado</Badge> : <span className="text-muted-foreground">—</span>;
  }
  if (column === 'status') {
    const v = String(value ?? '');
    return v ? <Badge variant={STATUS_VARIANT[v] ?? 'secondary'}>{v}</Badge> : <span className="text-muted-foreground">—</span>;
  }
  if (value === null || value === undefined) return <span className="text-muted-foreground">—</span>;
  if (typeof value === 'boolean') return value ? 'sim' : 'não';
  return String(value);
}

export default async function AdminResourcePage({
  params,
  searchParams,
}: {
  params: Promise<{ resource: string }>;
  searchParams: Promise<SP>;
}) {
  const { resource } = await params;
  if (!isResource(resource)) notFound();
  const sp = await searchParams;

  const page = Math.max(1, Number(sp.page ?? '1') || 1);
  const qs = new URLSearchParams();
  if (sp.q) qs.set('q', sp.q);
  if (sp.include) qs.set('include', sp.include);
  qs.set('page', String(page));
  qs.set('per_page', '20');

  const r = await adminFetch<PagedResponse>(`/v1/admin/${resource}?${qs.toString()}`);
  if (!r.ok) {
    const msg = r.status === 403 ? 'Sem acesso — é necessária a função de administrador.' : 'Não foi possível carregar os registos.';
    return (
      <Card className="border-destructive/40 p-6 text-sm text-destructive">{msg}</Card>
    );
  }

  const { data, meta } = r.data;
  const personal = PERSONAL_RESOURCES.has(resource);
  const columns = data.length > 0 ? Object.keys(data[0]).filter((k) => k !== 'id') : [];
  const totalPages = Math.max(1, Math.ceil(meta.total / meta.per_page));

  const pageHref = (p: number) => {
    const u = new URLSearchParams();
    if (sp.q) u.set('q', sp.q);
    if (sp.include) u.set('include', sp.include);
    u.set('page', String(p));
    return `/admin/${resource}?${u.toString()}`;
  };

  // Formulários de edição carregam por id (iscas e marcas). Sem query params legados.
  const editHref = (row: Record<string, unknown>) => `/admin/${resource}/${String(row.id)}`;
  const editable = resource === 'lures' || resource === 'brands' || resource === 'species';
  const newLabel: Record<string, string> = { lures: '+ Nova isca', brands: '+ Nova marca', species: '+ Nova espécie' };

  return (
    <div className="space-y-6">
      <header className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold capitalize tracking-tight">{resource}</h1>
          <p className="text-sm text-muted-foreground">{meta.total} registos</p>
        </div>
        {editable && (
          <Button asChild size="sm">
            <Link href={`/admin/${resource}/new`}>{newLabel[resource] ?? '+ Novo'}</Link>
          </Button>
        )}
      </header>

      <form method="get" action={`/admin/${resource}`} className="flex flex-wrap items-center gap-2">
        <Input name="q" defaultValue={sp.q ?? ''} placeholder="Pesquisar…" aria-label="Pesquisar registos" className="h-9 w-56" />
        <select
          name="include"
          defaultValue={sp.include ?? 'default'}
          aria-label="Filtrar por estado"
          className="flex h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          <option value="default">Só ativos/vivos</option>
          <option value="inactive">Inativos</option>
          <option value="deleted">Eliminados</option>
          <option value="all">Todos</option>
        </select>
        <Button type="submit">Filtrar</Button>
      </form>

      {data.length === 0 ? (
        <Card className="p-8 text-center text-sm text-muted-foreground">Sem registos.</Card>
      ) : (
        <Card className="overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                {columns.map((c) => (
                  <TableHead key={c}>{c}</TableHead>
                ))}
                <TableHead className="text-right">ações</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.map((row) => {
                const id = String(row.id);
                return (
                  <TableRow key={id}>
                    {columns.map((c) => (
                      <TableCell key={c}>{renderCell(c, row[c])}</TableCell>
                    ))}
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        {editable && row.deleted_at == null && (
                          <Button variant="outline" size="sm" asChild>
                            <Link href={editHref(row)}>Editar</Link>
                          </Button>
                        )}
                        <RowActions
                          resource={resource}
                          id={id}
                          isActive={row.is_active === true}
                          deleted={row.deleted_at != null}
                          personal={personal}
                        />
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </Card>
      )}

      <nav className="flex items-center gap-4">
        {page > 1 && (
          <Button variant="outline" size="sm" asChild>
            <Link href={pageHref(page - 1)}>← Anterior</Link>
          </Button>
        )}
        <span className="text-sm text-muted-foreground">
          Página {page} de {totalPages}
        </span>
        {page < totalPages && (
          <Button variant="outline" size="sm" asChild>
            <Link href={pageHref(page + 1)}>Seguinte →</Link>
          </Button>
        )}
      </nav>
    </div>
  );
}
