'use client';

import { useState } from 'react';
import { getSupabaseBrowserClient } from '../../lib/supabase/client';

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';

// US-07 (T076/T077) — atualizar nome/avatar e apagar a conta (RGPD).
export function SettingsForm() {
  const [displayName, setDisplayName] = useState('');
  const [avatarUrl, setAvatarUrl] = useState('');
  const [status, setStatus] = useState<string | null>(null);

  async function token(): Promise<string | undefined> {
    try {
      const supabase = getSupabaseBrowserClient();
      const { data } = await supabase.auth.getSession();
      return data.session?.access_token;
    } catch {
      return undefined;
    }
  }

  async function save(e: React.FormEvent) {
    e.preventDefault();
    setStatus(null);
    const t = await token();
    if (!t) return;
    const res = await fetch(`${API_BASE}/v1/me`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${t}` },
      body: JSON.stringify({ display_name: displayName || undefined, avatar_url: avatarUrl || undefined }),
    });
    setStatus(res.ok ? 'Guardado.' : 'Não foi possível guardar.');
  }

  async function deleteAccount() {
    if (!confirm('Apagar a conta? Esta ação é irreversível (RGPD: os seus dados serão anonimizados).')) return;
    const t = await token();
    if (!t) return;
    const res = await fetch(`${API_BASE}/v1/me`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${t}` },
    });
    if (res.ok) {
      try {
        const supabase = getSupabaseBrowserClient();
        await supabase.auth.signOut();
      } catch {
        /* ignore */
      }
      window.location.href = '/';
    } else {
      setStatus('Não foi possível apagar a conta.');
    }
  }

  return (
    <section aria-label="Definições da conta" style={{ maxWidth: 420 }}>
      <h2 style={{ fontSize: '1.1rem' }}>Perfil</h2>
      <form onSubmit={save} style={{ display: 'grid', gap: '0.5rem' }}>
        <label>
          Nome a exibir
          <input type="text" maxLength={80} value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </label>
        <label>
          URL do avatar
          <input type="url" value={avatarUrl} onChange={(e) => setAvatarUrl(e.target.value)} />
        </label>
        <button type="submit">Guardar</button>
        {status && <p style={{ color: '#070' }}>{status}</p>}
      </form>

      <h2 style={{ fontSize: '1.1rem', marginTop: '2rem', color: '#b00' }}>Apagar conta</h2>
      <p style={{ fontSize: '0.85rem', color: '#666' }}>
        Remove o acesso e anonimiza os seus dados pessoais (RGPD).
      </p>
      <button type="button" onClick={deleteAccount} style={{ color: '#b00' }}>
        Apagar a minha conta
      </button>
    </section>
  );
}
