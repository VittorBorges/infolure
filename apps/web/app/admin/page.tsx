import { adminFetch } from '../../lib/admin';

export const dynamic = 'force-dynamic';

interface Dashboard {
  users: { total: number; new_7d: number; new_30d: number };
  lures: { by_status: Record<string, number>; by_source: Record<string, number>; active: number; inactive: number };
  reviews: { pending: number };
  favorites: { total: number };
  inventory: { total: number };
}

function Card({ title, value }: { title: string; value: React.ReactNode }) {
  return (
    <div style={{ border: '1px solid #eee', borderRadius: 8, padding: '1rem', minWidth: 140 }}>
      <div style={{ fontSize: '0.8rem', color: '#666' }}>{title}</div>
      <div style={{ fontSize: '1.6rem', fontWeight: 600 }}>{value}</div>
    </div>
  );
}

export default async function AdminDashboardPage() {
  const r = await adminFetch<Dashboard>('/v1/admin/dashboard');
  if (!r.ok) {
    const msg = r.status === 403 ? 'Sem acesso — é necessária a função de administrador.'
      : 'Não foi possível carregar o dashboard.';
    return <p style={{ color: '#a00' }}>{msg}</p>;
  }
  const d = r.data;

  return (
    <div style={{ display: 'grid', gap: '1.5rem' }}>
      <h1>Dashboard</h1>

      <section>
        <h2 style={{ fontSize: '1rem' }}>Cadastros de utilizadores</h2>
        <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
          <Card title="Total" value={d.users.total} />
          <Card title="Novos (7d)" value={d.users.new_7d} />
          <Card title="Novos (30d)" value={d.users.new_30d} />
        </div>
      </section>

      <section>
        <h2 style={{ fontSize: '1rem' }}>Iscas</h2>
        <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
          <Card title="Ativas" value={d.lures.active} />
          <Card title="Inativas" value={d.lures.inactive} />
          <Card title="Por estado" value={
            <span style={{ fontSize: '0.85rem', fontWeight: 400 }}>
              {Object.entries(d.lures.by_status).map(([k, v]) => `${k}: ${v}`).join(' · ') || '—'}
            </span>
          } />
          <Card title="Por origem" value={
            <span style={{ fontSize: '0.85rem', fontWeight: 400 }}>
              {Object.entries(d.lures.by_source).map(([k, v]) => `${k}: ${v}`).join(' · ') || '—'}
            </span>
          } />
        </div>
      </section>

      <section>
        <h2 style={{ fontSize: '1rem' }}>Conteúdo e coleções</h2>
        <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
          <Card title="Reviews por moderar" value={d.reviews.pending} />
          <Card title="Favoritos" value={d.favorites.total} />
          <Card title="Inventário" value={d.inventory.total} />
        </div>
      </section>
    </div>
  );
}
