'use client';

import { useEffect, useState } from 'react';
import { getSupabaseBrowserClient } from '../../lib/supabase/client';

// US-04 (T052) — linking multi-provedor. Consumido pela página de settings (T076).
// Permite vincular Google/Microsoft a uma conta existente e desvincular.
type Identity = { identity_id?: string; id?: string; provider: string };

const LINKABLE: { provider: 'google' | 'azure'; label: string }[] = [
  { provider: 'google', label: 'Google' },
  { provider: 'azure', label: 'Microsoft' },
];

export function AuthProviders() {
  const [identities, setIdentities] = useState<Identity[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        const supabase = getSupabaseBrowserClient();
        const { data } = await supabase.auth.getUserIdentities();
        setIdentities((data?.identities as Identity[]) ?? []);
      } catch (e) {
        setError((e as Error).message);
      }
    })();
  }, []);

  async function link(provider: 'google' | 'azure') {
    setError(null);
    try {
      const supabase = getSupabaseBrowserClient();
      await supabase.auth.linkIdentity({ provider });
    } catch (e) {
      setError((e as Error).message);
    }
  }

  async function unlink(identity: Identity) {
    setError(null);
    try {
      const supabase = getSupabaseBrowserClient();
      // @ts-expect-error — a tipagem de unlinkIdentity aceita o objeto identity do utilizador
      await supabase.auth.unlinkIdentity(identity);
      setIdentities((prev) => prev.filter((i) => i.provider !== identity.provider));
    } catch (e) {
      setError((e as Error).message);
    }
  }

  const linkedProviders = new Set(identities.map((i) => i.provider));

  return (
    <section aria-label="Provedores de login">
      <h2 style={{ fontSize: '1.1rem' }}>Métodos de login</h2>
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {LINKABLE.map(({ provider, label }) => {
          const linked = linkedProviders.has(provider);
          const identity = identities.find((i) => i.provider === provider);
          return (
            <li key={provider} style={{ display: 'flex', justifyContent: 'space-between', maxWidth: 320, padding: '0.4rem 0' }}>
              <span>{label} {linked && <span style={{ color: '#070' }}>· ligado</span>}</span>
              {linked ? (
                <button type="button" onClick={() => identity && unlink(identity)} disabled={identities.length <= 1}>
                  Desvincular
                </button>
              ) : (
                <button type="button" onClick={() => link(provider)}>Vincular</button>
              )}
            </li>
          );
        })}
      </ul>
      {error && <p role="alert" style={{ color: '#b00' }}>{error}</p>}
    </section>
  );
}
