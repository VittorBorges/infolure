'use client';

import { useRouter } from 'next/navigation';

// Estado de "sem resultados" (US-01/US-02 — Princípio V). Mostra a query (se houver)
// e um CTA para limpar os filtros.
export function NoResults({ query }: { query?: string }) {
  const router = useRouter();
  return (
    <div role="status" style={{ padding: '3rem 1rem', textAlign: 'center' }}>
      <p style={{ marginBottom: '0.5rem' }}>
        {query ? <>Sem resultados para “{query}”.</> : 'Sem resultados para os filtros aplicados.'}
      </p>
      <p style={{ color: '#666', marginBottom: '1rem' }}>Tente alargar a pesquisa ou remover filtros.</p>
      <button type="button" onClick={() => router.push('/iscas')}>
        Limpar filtros
      </button>
    </div>
  );
}
