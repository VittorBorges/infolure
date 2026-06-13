import type { Metadata } from 'next';
import { notFound } from 'next/navigation';

export const dynamic = 'force-dynamic';

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';

interface PublicProfile {
  username: string;
  avatar_url?: string;
  member_since: string;
  favorites_count: number;
  inventory_count: number;
  reviews_count: number;
}

async function load(username: string): Promise<PublicProfile | null> {
  const res = await fetch(`${API_BASE}/v1/users/${encodeURIComponent(username)}`, { cache: 'no-store' });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error('falha ao carregar perfil');
  return res.json();
}

export async function generateMetadata({
  params,
}: {
  params: Promise<{ username: string }>;
}): Promise<Metadata> {
  const { username } = await params;
  return { title: `@${username} — Infolure` };
}

// US-07 (T075) — perfil público: username, avatar, membro desde, contagens. Sem PII.
export default async function ProfilePage({ params }: { params: Promise<{ username: string }> }) {
  const { username } = await params;
  const profile = await load(username);
  if (!profile) notFound();

  const memberSince = new Date(profile.member_since).toLocaleDateString('pt-PT', {
    year: 'numeric',
    month: 'long',
  });

  return (
    <div style={{ padding: '1.5rem', maxWidth: 480, margin: '0 auto' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
        {profile.avatar_url ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={profile.avatar_url} alt="" style={{ width: 64, height: 64, borderRadius: '50%' }} />
        ) : (
          <div style={{ width: 64, height: 64, borderRadius: '50%', background: '#eee' }} aria-hidden />
        )}
        <div>
          <h1 style={{ margin: 0 }}>@{profile.username}</h1>
          <p style={{ margin: 0, color: '#666', fontSize: '0.85rem' }}>Membro desde {memberSince}</p>
        </div>
      </div>

      <ul style={{ display: 'flex', gap: '2rem', listStyle: 'none', padding: 0, marginTop: '1.5rem' }}>
        <li><strong>{profile.favorites_count}</strong> favoritos</li>
        <li><strong>{profile.inventory_count}</strong> no inventário</li>
        <li><strong>{profile.reviews_count}</strong> avaliações</li>
      </ul>
    </div>
  );
}
