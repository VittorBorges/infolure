'use client';

import { useRouter, useSearchParams } from 'next/navigation';
import type { CatalogFacets } from '../../lib/catalog';

// Painel de filtros com estado sincronizado na URL (US-01). As opções vêm dos facets
// devolvidos pela própria listagem (Typesense) — sem endpoints separados de marcas/espécies.
export function FilterPanel({ facets }: { facets: CatalogFacets }) {
  const router = useRouter();
  const params = useSearchParams();

  function setParam(key: string, value: string | null) {
    const next = new URLSearchParams(params.toString());
    if (value) next.set(key, value);
    else next.delete(key);
    next.delete('page'); // novo filtro volta à página 1
    router.push(`/iscas?${next.toString()}`);
  }

  const group = (label: string, key: string, values: { value: string; count: number }[]) => (
    <fieldset style={{ border: 'none', padding: 0, marginBottom: '1rem' }}>
      <legend style={{ fontWeight: 600, marginBottom: '0.25rem' }}>{label}</legend>
      {values.length === 0 && <p style={{ color: '#999', fontSize: '0.85rem' }}>—</p>}
      {values.map((v) => {
        const active = params.get(key) === v.value;
        return (
          <label key={v.value} style={{ display: 'block', fontSize: '0.9rem' }}>
            <input
              type="checkbox"
              checked={active}
              onChange={() => setParam(key, active ? null : v.value)}
            />{' '}
            {v.value} ({v.count})
          </label>
        );
      })}
    </fieldset>
  );

  return (
    <aside aria-label="Filtros" style={{ minWidth: 200 }}>
      {group('Tipo', 'lure_type', facets.lure_types)}
      {group('Água', 'water_type', facets.water_types)}
      {group('Marca', 'brand', facets.brands)}
      {group('Espécie', 'species', facets.species)}
    </aside>
  );
}
