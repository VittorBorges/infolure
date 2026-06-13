'use client';

import { useState } from 'react';
import { getSupabaseBrowserClient } from '../../../lib/supabase/client';

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';

// US-04 (T051) — primeiro login OAuth: escolher um username único (3–20, alfanumérico + _).
export default function ChooseUsernamePage() {
  const [username, setUsername] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSaving(true);
    try {
      const supabase = getSupabaseBrowserClient();
      const { data } = await supabase.auth.getSession();
      const token = data.session?.access_token;
      if (!token) {
        setError('Sessão expirada. Entre novamente.');
        return;
      }
      const res = await fetch(`${API_BASE}/v1/auth/username`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({ username }),
      });
      if (res.status === 409) setError('Esse username já está em uso.');
      else if (res.status === 422) setError('Username inválido (3–20, letras/números/_).');
      else if (!res.ok) setError('Não foi possível guardar. Tente novamente.');
      else window.location.href = '/iscas';
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div style={{ maxWidth: 360, margin: '3rem auto', padding: '0 1rem' }}>
      <h1>Escolha o seu username</h1>
      <p style={{ color: '#666', fontSize: '0.9rem' }}>3–20 caracteres: letras, números e underscore.</p>
      <form onSubmit={submit} style={{ display: 'grid', gap: '0.5rem' }}>
        <input
          required
          minLength={3}
          maxLength={20}
          pattern="[A-Za-z0-9_]{3,20}"
          placeholder="username"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
        />
        <button type="submit" disabled={saving}>{saving ? 'A guardar…' : 'Continuar'}</button>
      </form>
      {error && <p role="alert" style={{ color: '#b00', marginTop: '1rem' }}>{error}</p>}
    </div>
  );
}
