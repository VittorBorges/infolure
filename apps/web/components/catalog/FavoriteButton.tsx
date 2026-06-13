'use client';

import { useState } from 'react';
import { getSupabaseBrowserClient } from '../../lib/supabase/client';

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';

// US-05 (T058) — botão de favorito otimista e auth-gated.
// Anónimo → redireciona para /login preservando a return URL.
export function FavoriteButton({
  lureId,
  initialFavorited = false,
  initialCount = 0,
}: {
  lureId: string;
  initialFavorited?: boolean;
  initialCount?: number;
}) {
  const [favorited, setFavorited] = useState(initialFavorited);
  const [count, setCount] = useState(initialCount);
  const [busy, setBusy] = useState(false);

  async function toggle(e: React.MouseEvent) {
    e.preventDefault(); // evita navegar quando dentro de um card
    e.stopPropagation();
    if (busy) return;
    setBusy(true);

    let token: string | undefined;
    try {
      const supabase = getSupabaseBrowserClient();
      const { data } = await supabase.auth.getSession();
      token = data.session?.access_token;
    } catch {
      // Supabase não configurado → trata como anónimo
    }

    if (!token) {
      const returnUrl = typeof window !== 'undefined' ? window.location.pathname + window.location.search : '/';
      window.location.href = `/login?returnUrl=${encodeURIComponent(returnUrl)}`;
      return;
    }

    // otimista
    const next = !favorited;
    setFavorited(next);
    setCount((c) => c + (next ? 1 : -1));

    try {
      const res = await fetch(`${API_BASE}/v1/me/favorites/${lureId}`, {
        method: next ? 'POST' : 'DELETE',
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error('falhou');
    } catch {
      // reverte em caso de erro
      setFavorited(!next);
      setCount((c) => c + (next ? -1 : 1));
    } finally {
      setBusy(false);
    }
  }

  return (
    <button
      type="button"
      onClick={toggle}
      aria-pressed={favorited}
      aria-label={favorited ? 'Remover dos favoritos' : 'Adicionar aos favoritos'}
      title="Favoritos"
      style={{ border: 'none', background: 'rgba(255,255,255,0.85)', borderRadius: 999, padding: '0.2rem 0.5rem', cursor: 'pointer' }}
    >
      {favorited ? '♥' : '♡'} {count}
    </button>
  );
}
