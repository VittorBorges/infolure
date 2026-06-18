import { redirect } from 'next/navigation';
import { getSupabaseServerClient } from '../../../lib/supabase/server';
import { LureCard } from '../../../components/catalog/LureCard';
import { EmptyState } from '../../../components/States';
import type { LureListResponse } from '../../../lib/catalog';

export const dynamic = 'force-dynamic';

export const metadata = { title: 'Meus Favoritos — Infolure' };

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';

// US-05 (T059) — lista os favoritos do utilizador autenticado.
export default async function FavoritesPage() {
  const supabase = await getSupabaseServerClient();
  const { data } = await supabase.auth.getSession();
  const token = data.session?.access_token;
  if (!token) redirect('/login?returnUrl=/conta/favoritos');

  const res = await fetch(`${API_BASE}/v1/me/favorites`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: 'no-store',
  });
  if (!res.ok) {
    return <div style={{ padding: '1.5rem' }}>Não foi possível carregar os favoritos.</div>;
  }
  const { data: favorites } = (await res.json()) as LureListResponse;

  return (
    <div style={{ padding: '1.5rem' }}>
      <h1>Meus Favoritos</h1>
      {favorites.length === 0 ? (
        <EmptyState title="Ainda não tem favoritos." />
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: '1rem' }}>
          {favorites.map((lure) => (
            <LureCard key={lure.id} lure={lure} />
          ))}
        </div>
      )}
    </div>
  );
}
