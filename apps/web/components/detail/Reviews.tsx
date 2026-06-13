'use client';

import { useCallback, useEffect, useState } from 'react';
import { getSupabaseBrowserClient } from '../../lib/supabase/client';
import { RatingSummary, type Aggregate } from './RatingSummary';

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';

interface Review {
  id: string;
  rating: number;
  body?: string;
  helpful_count: number;
  is_helpful?: boolean | null;
  created_at: string;
  author: { username?: string; avatar_url?: string };
}
interface ReviewsResponse {
  data: Review[];
  aggregate: Aggregate;
}

// US-08 (T071) — lista de avaliações + formulário (uma por utilizador/isca) na página de detalhe.
export function Reviews({ slug }: { slug: string }) {
  const [resp, setResp] = useState<ReviewsResponse | null>(null);
  const [rating, setRating] = useState(5);
  const [body, setBody] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function token(): Promise<string | undefined> {
    try {
      const supabase = getSupabaseBrowserClient();
      const { data } = await supabase.auth.getSession();
      return data.session?.access_token;
    } catch {
      return undefined;
    }
  }

  const load = useCallback(async () => {
    const t = await token();
    const res = await fetch(`${API_BASE}/v1/lures/${slug}/reviews?sort=recent`, {
      headers: t ? { Authorization: `Bearer ${t}` } : undefined,
      cache: 'no-store',
    });
    if (res.ok) setResp(await res.json());
  }, [slug]);

  useEffect(() => {
    load();
  }, [load]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const t = await token();
      if (!t) {
        window.location.href = `/login?returnUrl=${encodeURIComponent(`/iscas/${slug}`)}`;
        return;
      }
      const res = await fetch(`${API_BASE}/v1/lures/${slug}/reviews`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${t}` },
        body: JSON.stringify({ rating, body: body || undefined }),
      });
      if (res.status === 409) setError('Já avaliou esta isca.');
      else if (!res.ok) setError('Não foi possível submeter a avaliação.');
      else {
        setBody('');
        await load();
      }
    } finally {
      setBusy(false);
    }
  }

  async function toggleHelpful(reviewId: string) {
    const t = await token();
    if (!t) {
      window.location.href = `/login?returnUrl=${encodeURIComponent(`/iscas/${slug}`)}`;
      return;
    }
    await fetch(`${API_BASE}/v1/reviews/${reviewId}/helpful`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${t}` },
    });
    await load();
  }

  return (
    <section aria-label="Avaliações" style={{ marginTop: '2rem' }}>
      <h2 style={{ fontSize: '1.2rem' }}>Avaliações</h2>

      {resp && <RatingSummary aggregate={resp.aggregate} />}

      <form onSubmit={submit} style={{ display: 'grid', gap: '0.5rem', maxWidth: 420, marginBottom: '1.5rem' }}>
        <label>
          Classificação
          <select value={rating} onChange={(e) => setRating(Number(e.target.value))}>
            {[5, 4, 3, 2, 1].map((r) => <option key={r} value={r}>{r} ★</option>)}
          </select>
        </label>
        <textarea
          maxLength={1000}
          placeholder="O que achou desta isca? (opcional)"
          value={body}
          onChange={(e) => setBody(e.target.value)}
          rows={3}
        />
        <button type="submit" disabled={busy}>{busy ? 'A enviar…' : 'Publicar avaliação'}</button>
        {error && <p role="alert" style={{ color: '#b00' }}>{error}</p>}
      </form>

      <ul style={{ listStyle: 'none', padding: 0 }}>
        {resp?.data.map((r) => (
          <li key={r.id} style={{ borderTop: '1px solid #f0f0f0', padding: '0.75rem 0' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <strong>{r.author.username ?? 'Utilizador'}</strong>
              <span>{'★'.repeat(r.rating)}{'☆'.repeat(5 - r.rating)}</span>
            </div>
            {r.body && <p style={{ margin: '0.25rem 0' }}>{r.body}</p>}
            <button type="button" onClick={() => toggleHelpful(r.id)} style={{ fontSize: '0.8rem' }}>
              {r.is_helpful ? '✓ Útil' : 'Útil'} ({r.helpful_count})
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
