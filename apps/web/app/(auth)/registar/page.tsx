'use client';

import { useState } from 'react';
import { getSupabaseBrowserClient } from '../../../lib/supabase/client';

// US-04 (T050) — registo com email + palavra-passe.
export default function RegisterPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setMessage(null);
    try {
      const supabase = getSupabaseBrowserClient();
      const { error } = await supabase.auth.signUp({
        email,
        password,
        options: {
          emailRedirectTo:
            typeof window !== 'undefined' ? `${window.location.origin}/auth/callback` : undefined,
        },
      });
      if (error) setError(error.message);
      else setMessage('Verifique o seu email para confirmar a conta.');
    } catch (e) {
      setError((e as Error).message);
    }
  }

  return (
    <div style={{ maxWidth: 360, margin: '3rem auto', padding: '0 1rem' }}>
      <h1>Registar</h1>
      <form onSubmit={submit} style={{ display: 'grid', gap: '0.5rem' }}>
        <input type="email" required placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} />
        <input type="password" required minLength={8} placeholder="Palavra-passe (mín. 8)" value={password} onChange={(e) => setPassword(e.target.value)} />
        <button type="submit">Criar conta</button>
      </form>
      <p style={{ marginTop: '1rem', fontSize: '0.9rem' }}>
        Já tem conta? <a href="/login">Entrar</a>
      </p>
      {message && <p style={{ color: '#070', marginTop: '1rem' }}>{message}</p>}
      {error && <p role="alert" style={{ color: '#b00', marginTop: '1rem' }}>{error}</p>}
    </div>
  );
}
