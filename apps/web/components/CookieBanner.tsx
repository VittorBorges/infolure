'use client';

import { useEffect, useState } from 'react';

const KEY = 'infolure-cookie-consent';

// T084 — banner de cookies. Apenas analytics requer consentimento (cookies essenciais não).
export function CookieBanner() {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    try {
      if (!localStorage.getItem(KEY)) setVisible(true);
    } catch {
      /* localStorage indisponível */
    }
  }, []);

  function decide(consent: 'accepted' | 'rejected') {
    try {
      localStorage.setItem(KEY, consent);
    } catch {
      /* ignore */
    }
    setVisible(false);
  }

  if (!visible) return null;

  return (
    <div
      role="dialog"
      aria-label="Consentimento de cookies"
      style={{ position: 'fixed', bottom: 0, left: 0, right: 0, background: '#222', color: '#fff', padding: '1rem', display: 'flex', gap: '1rem', alignItems: 'center', justifyContent: 'center', flexWrap: 'wrap', zIndex: 50 }}
    >
      <span style={{ fontSize: '0.9rem' }}>
        Usamos cookies de analytics para melhorar a experiência. Os cookies essenciais são sempre usados.
      </span>
      <span style={{ display: 'flex', gap: '0.5rem' }}>
        <button type="button" onClick={() => decide('rejected')}>Rejeitar analytics</button>
        <button type="button" onClick={() => decide('accepted')}>Aceitar</button>
      </span>
    </div>
  );
}
