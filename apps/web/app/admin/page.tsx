import { adminFetch } from '../../lib/admin';
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card';
import { Badge } from '../../components/ui/badge';

export const dynamic = 'force-dynamic';

interface Dashboard {
  users: { total: number; new_7d: number; new_30d: number };
  lures: { by_status: Record<string, number>; by_source: Record<string, number>; active: number; inactive: number };
  reviews: { pending: number };
  favorites: { total: number };
  inventory: { total: number };
}

function Metric({ title, value }: { title: string; value: React.ReactNode }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="text-3xl font-semibold tracking-tight">{value}</div>
      </CardContent>
    </Card>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="space-y-3">
      <h2 className="text-sm font-medium text-muted-foreground">{title}</h2>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">{children}</div>
    </section>
  );
}

function Breakdown({ data }: { data: Record<string, number> }) {
  const entries = Object.entries(data);
  if (entries.length === 0) return <span className="text-sm text-muted-foreground">—</span>;
  return (
    <div className="flex flex-wrap gap-1.5">
      {entries.map(([k, v]) => (
        <Badge key={k} variant="muted">
          {k}: {v}
        </Badge>
      ))}
    </div>
  );
}

export default async function AdminDashboardPage() {
  const r = await adminFetch<Dashboard>('/v1/admin/dashboard');
  if (!r.ok) {
    const msg =
      r.status === 403
        ? 'Sem acesso — é necessária a função de administrador.'
        : 'Não foi possível carregar o dashboard.';
    return (
      <Card className="border-destructive/40">
        <CardContent className="pt-6 text-sm text-destructive">{msg}</CardContent>
      </Card>
    );
  }
  const d = r.data;

  return (
    <div className="space-y-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">Dashboard</h1>
        <p className="text-sm text-muted-foreground">Visão geral do catálogo e da comunidade.</p>
      </header>

      <Section title="Cadastros de utilizadores">
        <Metric title="Total" value={d.users.total} />
        <Metric title="Novos (7d)" value={d.users.new_7d} />
        <Metric title="Novos (30d)" value={d.users.new_30d} />
      </Section>

      <Section title="Iscas">
        <Metric
          title="Ativas"
          value={
            <span className="flex items-center gap-2">
              {d.lures.active}
              <Badge variant="success">ativas</Badge>
            </span>
          }
        />
        <Metric title="Inativas" value={d.lures.inactive} />
        <Metric title="Por estado" value={<Breakdown data={d.lures.by_status} />} />
        <Metric title="Por origem" value={<Breakdown data={d.lures.by_source} />} />
      </Section>

      <Section title="Comunidade">
        <Metric
          title="Reviews pendentes"
          value={
            <span className="flex items-center gap-2">
              {d.reviews.pending}
              {d.reviews.pending > 0 && <Badge variant="secondary">moderar</Badge>}
            </span>
          }
        />
        <Metric title="Favoritos" value={d.favorites.total} />
        <Metric title="Inventário" value={d.inventory.total} />
      </Section>
    </div>
  );
}
