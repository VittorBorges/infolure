import Link from 'next/link';
import { adminFetch } from '../../../lib/admin';
import { Button } from '../../../components/ui/button';
import { Badge } from '../../../components/ui/badge';
import { Card } from '../../../components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../../components/ui/table';

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
    const msg = r.status === 403 ? 'Sem acesso — função de administrador necessária.' : 'Não foi possível carregar a auditoria.';
    return <Card className="border-destructive/40 p-6 text-sm text-destructive">{msg}</Card>;
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
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">Auditoria</h1>
        <p className="text-sm text-muted-foreground">{meta.total} entradas</p>
      </header>

      <form method="get" action="/admin/audit" className="flex flex-wrap items-center gap-2">
        <select
          name="action"
          defaultValue={sp.action ?? ''}
          aria-label="Filtrar por ação"
          className="flex h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          <option value="">Todas as ações</option>
          {ACTIONS.map((a) => (
            <option key={a} value={a}>
              {a}
            </option>
          ))}
        </select>
        <Button type="submit">Filtrar</Button>
      </form>

      {data.length === 0 ? (
        <Card className="p-8 text-center text-sm text-muted-foreground">Sem entradas.</Card>
      ) : (
        <Card className="overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                {['data', 'ação', 'entidade', 'registo', 'pessoal', 'alterações'].map((h) => (
                  <TableHead key={h}>{h}</TableHead>
                ))}
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.map((e) => (
                <TableRow key={e.id}>
                  <TableCell className="whitespace-nowrap">{new Date(e.created_at).toLocaleString('pt-PT')}</TableCell>
                  <TableCell>
                    <Badge variant="secondary">{e.action}</Badge>
                  </TableCell>
                  <TableCell>{e.entity_type}</TableCell>
                  <TableCell className="font-mono text-xs">{e.entity_id}</TableCell>
                  <TableCell>{e.is_personal_data ? <Badge variant="muted">PII</Badge> : <span className="text-muted-foreground">—</span>}</TableCell>
                  <TableCell className="max-w-[280px] truncate">{e.changes ?? '—'}</TableCell>
                </TableRow>
              ))}
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
