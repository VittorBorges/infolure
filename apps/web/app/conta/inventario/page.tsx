import { redirect } from 'next/navigation';
import Link from 'next/link';
import { getSupabaseServerClient } from '../../../lib/supabase/server';
import { EmptyState } from '../../../components/States';
import type { LureCard } from '../../../lib/catalog';

export const dynamic = 'force-dynamic';
export const metadata = { title: 'Meu Inventário — Infolure' };

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';

interface InventoryEntry {
  id: string;
  lure: LureCard;
  color?: { id: string; name: string } | null;
  quantity: number;
  condition?: string | null;
  notes?: string | null;
}
interface InventoryResponse {
  data: InventoryEntry[];
  total_unique_lures: number;
}

const CONDITION_LABEL: Record<string, string> = {
  new: 'Nova',
  good: 'Boa',
  used: 'Usada',
  lost: 'Perdida',
};

// US-06 (T065) — inventário agrupado por tipo de isca.
export default async function InventoryPage() {
  const supabase = await getSupabaseServerClient();
  const { data } = await supabase.auth.getSession();
  const token = data.session?.access_token;
  if (!token) redirect('/login?returnUrl=/conta/inventario');

  const res = await fetch(`${API_BASE}/v1/me/inventory`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: 'no-store',
  });
  if (!res.ok) return <div style={{ padding: '1.5rem' }}>Não foi possível carregar o inventário.</div>;
  const inventory = (await res.json()) as InventoryResponse;

  // agrupar por tipo
  const groups = new Map<string, InventoryEntry[]>();
  for (const entry of inventory.data) {
    const key = entry.lure.lure_type;
    (groups.get(key) ?? groups.set(key, []).get(key)!).push(entry);
  }

  return (
    <div style={{ padding: '1.5rem' }}>
      <h1>Meu Inventário</h1>
      <p style={{ color: '#666' }}>{inventory.total_unique_lures} iscas únicas</p>

      {inventory.data.length === 0 ? (
        <EmptyState title="O seu inventário está vazio." />
      ) : (
        [...groups.entries()].map(([type, entries]) => (
          <section key={type} style={{ marginTop: '1.5rem' }}>
            <h2 style={{ fontSize: '1.1rem', textTransform: 'capitalize' }}>{type}</h2>
            <ul style={{ listStyle: 'none', padding: 0 }}>
              {entries.map((e) => (
                <li key={e.id} style={{ display: 'flex', justifyContent: 'space-between', maxWidth: 520, padding: '0.4rem 0', borderBottom: '1px solid #f0f0f0' }}>
                  <span>
                    <Link href={`/iscas/${e.lure.slug}`}>{e.lure.name}</Link>
                    {e.color && <span style={{ color: '#888' }}> · {e.color.name}</span>}
                    {e.notes && <span style={{ color: '#aaa' }}> — {e.notes}</span>}
                  </span>
                  <span>
                    {e.quantity}× {e.condition && <em style={{ color: '#666' }}>({CONDITION_LABEL[e.condition] ?? e.condition})</em>}
                  </span>
                </li>
              ))}
            </ul>
          </section>
        ))
      )}
    </div>
  );
}
