'use client';

import { useState } from 'react';
import { getSupabaseBrowserClient } from '../../../lib/supabase/client';

// US-04 (T049/T050) — sign-in com Google, Microsoft (Azure/MSA) e email+senha.
// O fluxo OAuth do Supabase gera e valida o parâmetro `state` (anti-CSRF) automaticamente.
// O redirect volta para /auth/callback, que troca o code pela sessão.
export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);

  const redirectTo =
    typeof window !== 'undefined' ? `${window.location.origin}/auth/callback` : undefined;

  async function oauth(provider: 'google' | 'azure') {
    setError(null);
    try {
      const supabase = getSupabaseBrowserClient();
      await supabase.auth.signInWithOAuth({ provider, options: { redirectTo } });
    } catch (e) {
      setError((e as Error).message);
    }
  }

  async function emailPassword(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const supabase = getSupabaseBrowserClient();
      const { error } = await supabase.auth.signInWithPassword({ email, password });
      if (error) setError(error.message);
      else window.location.href = '/iscas';
    } catch (e) {
      setError((e as Error).message);
    }
  }

  return (
    <div style={{ maxWidth: 360, margin: '3rem auto', padding: '0 1rem' }}>
      <h1>Entrar</h1>

      <div style={{ display: 'grid', gap: '0.5rem', marginBottom: '1.5rem' }}>
        <button type="button" onClick={() => oauth('google')}>Continuar com Google</button>
        <button type="button" onClick={() => oauth('azure')}>Continuar com Microsoft</button>
      </div>

      <form onSubmit={emailPassword} style={{ display: 'grid', gap: '0.5rem' }}>
        <input type="email" required placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} />
        <input type="password" required placeholder="Palavra-passe" value={password} onChange={(e) => setPassword(e.target.value)} />
        <button type="submit">Entrar com email</button>
      </form>

      <p style={{ marginTop: '1rem', fontSize: '0.9rem' }}>
        Não tem conta? <a href="/registar">Registar</a>
      </p>

      {error && <p role="alert" style={{ color: '#b00', marginTop: '1rem' }}>{error}</p>}
    </div>
  );
}
