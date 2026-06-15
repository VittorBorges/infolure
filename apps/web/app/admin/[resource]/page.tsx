import { notFound } from 'next/navigation';
import Link from 'next/link';
import { adminFetch, ADMIN_RESOURCES, PERSONAL_RESOURCES, type AdminResource, type PagedResponse } from '../../../lib/admin';
import { RowActions } from '../../../components/admin/RowActions';

export const dynamic = 'force-dynamic';

type SP = { q?: string; include?: string; page?: string };

function isResource(r: string): r is AdminResource {
  return (ADMIN_RESOURCES as readonly string[]).includes(r);
}

function cell(value: unknown): string {
  if (value === null || value === undefined) return '—';
  if (typeof value === 'boolean') return value ? 'sim' : 'não';
  return String(value);
}

export default async function AdminResourcePage({
  params, searchParams,
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
    const msg = r.status === 403 ? 'Sem acesso — é necessária a função de administrador.'
      : 'Não foi possível carregar os registos.';
    return <p style={{ color: '#a00' }}>{msg}</p>;
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

  return (
    <div style={{ display: 'grid', gap: '1rem' }}>
      <h1 style={{ textTransform: 'capitalize' }}>{resource}</h1>

      <form method="get" action={`/admin/${resource}`} style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
        <input name="q" defaultValue={sp.q ?? ''} placeholder="Pesquisar…" style={{ padding: '0.3rem 0.5rem' }} />
        <select name="include" defaultValue={sp.include ?? 'default'}>
          <option value="default">Só ativos/vivos</option>
          <option value="inactive">Inativos</option>
          <option value="deleted">Eliminados</option>
          <option value="all">Todos</option>
        </select>
        <button type="submit">Filtrar</button>
      </form>

      <p style={{ color: '#666', fontSize: '0.85rem' }}>{meta.total} registos</p>

      {data.length === 0 ? (
        <p>Sem registos.</p>
      ) : (
        <table style={{ borderCollapse: 'collapse', width: '100%', fontSize: '0.85rem' }}>
          <thead>
            <tr>
              {columns.map((c) => (
                <th key={c} style={{ textAlign: 'left', borderBottom: '2px solid #eee', padding: '0.4rem' }}>{c}</th>
              ))}
              <th style={{ textAlign: 'left', borderBottom: '2px solid #eee', padding: '0.4rem' }}>ações</th>
            </tr>
          </thead>
          <tbody>
            {data.map((row) => {
              const id = String(row.id);
              return (
                <tr key={id}>
                  {columns.map((c) => (
                    <td key={c} style={{ borderBottom: '1px solid #f0f0f0', padding: '0.4rem' }}>{cell(row[c])}</td>
                  ))}
                  <td style={{ borderBottom: '1px solid #f0f0f0', padding: '0.4rem' }}>
                    <RowActions
                      resource={resource}
                      id={id}
                      isActive={row.is_active === true}
                      deleted={row.deleted_at != null}
                      personal={personal}
                      indexable={resource === 'lures' ? row.is_indexable === true : undefined}
                    />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}

      <nav style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
        {page > 1 && <Link href={pageHref(page - 1)}>← Anterior</Link>}
        <span style={{ fontSize: '0.85rem', color: '#666' }}>Página {page} de {totalPages}</span>
        {page < totalPages && <Link href={pageHref(page + 1)}>Seguinte →</Link>}
      </nav>
    </div>
  );
}
