import Link from 'next/link';
import { adminFetch } from '../../../lib/admin';

export const dynamic = 'force-dynamic';

const ACTIONS = ['create', 'update', 'activate', 'deactivate', 'delete', 'restore', 'moderate', 'settings_update'];

interface AuditEntry {
  id: string;
  actor_user_id: string | null;
  action: string;
  entity_type: string;
  entity_id: string;
  is_personal_data: boolean;
  changes: string | null;
  created_at: string;
}
interface AuditResponse {
  data: AuditEntry[];
  meta: { total: number; page: number; per_page: number };
}

type SP = { action?: string; page?: string };

export default async function AuditPage({ searchParams }: { searchParams: Promise<SP> }) {
  const sp = await searchParams;
  const page = Math.max(1, Number(sp.page ?? '1') || 1);

  const qs = new URLSearchParams();
  if (sp.action) qs.set('action', sp.action);
  qs.set('page', String(page));
  qs.set('per_page', '25');

  const r = await adminFetch<AuditResponse>(`/v1/admin/audit?${qs.toString()}`);
  if (!r.ok) {
    return <p style={{ color: '#a00' }}>{r.status === 403 ? 'Sem acesso — função de administrador necessária.' : 'Não foi possível carregar a auditoria.'}</p>;
  }
  const { data, meta } = r.data;
  const totalPages = Math.max(1, Math.ceil(meta.total / meta.per_page));
  const pageHref = (p: number) => {
    const u = new URLSearchParams();
    if (sp.action) u.set('action', sp.action);
    u.set('page', String(p));
    return `/admin/audit?${u.toString()}`;
  };

  return (
    <div style={{ display: 'grid', gap: '1rem' }}>
      <h1>Auditoria</h1>

      <form method="get" action="/admin/audit" style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
        <select name="action" defaultValue={sp.action ?? ''}>
          <option value="">Todas as ações</option>
          {ACTIONS.map((a) => <option key={a} value={a}>{a}</option>)}
        </select>
        <button type="submit">Filtrar</button>
      </form>

      <p style={{ color: '#666', fontSize: '0.85rem' }}>{meta.total} entradas</p>

      {data.length === 0 ? (
        <p>Sem entradas.</p>
      ) : (
        <table style={{ borderCollapse: 'collapse', width: '100%', fontSize: '0.8rem' }}>
          <thead>
            <tr>
              {['data', 'ação', 'entidade', 'registo', 'pessoal', 'alterações'].map((h) => (
                <th key={h} style={{ textAlign: 'left', borderBottom: '2px solid #eee', padding: '0.4rem' }}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {data.map((e) => (
              <tr key={e.id}>
                <td style={{ borderBottom: '1px solid #f0f0f0', padding: '0.4rem', whiteSpace: 'nowrap' }}>
                  {new Date(e.created_at).toLocaleString('pt-PT')}
                </td>
                <td style={{ borderBottom: '1px solid #f0f0f0', padding: '0.4rem' }}>{e.action}</td>
                <td style={{ borderBottom: '1px solid #f0f0f0', padding: '0.4rem' }}>{e.entity_type}</td>
                <td style={{ borderBottom: '1px solid #f0f0f0', padding: '0.4rem', fontFamily: 'monospace' }}>{e.entity_id}</td>
                <td style={{ borderBottom: '1px solid #f0f0f0', padding: '0.4rem' }}>{e.is_personal_data ? 'sim' : '—'}</td>
                <td style={{ borderBottom: '1px solid #f0f0f0', padding: '0.4rem', maxWidth: 280, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {e.changes ?? '—'}
                </td>
              </tr>
            ))}
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
