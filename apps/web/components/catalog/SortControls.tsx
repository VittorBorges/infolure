'use client';

import { useRouter, useSearchParams } from 'next/navigation';

// Controles de ordenação (US-01): popularidade, preço asc/desc, mais recentes. Estado na URL.
const OPTIONS = [
  { value: 'popularity', label: 'Popularidade' },
  { value: 'price_asc', label: 'Preço ↑' },
  { value: 'price_desc', label: 'Preço ↓' },
  { value: 'newest', label: 'Mais recentes' },
];

export function SortControls() {
  const router = useRouter();
  const params = useSearchParams();
  const current = params.get('sort') ?? 'popularity';

  function onChange(value: string) {
    const next = new URLSearchParams(params.toString());
    next.set('sort', value);
    router.push(`/iscas?${next.toString()}`);
  }

  return (
    <label style={{ fontSize: '0.9rem' }}>
      Ordenar:{' '}
      <select value={current} onChange={(e) => onChange(e.target.value)}>
        {OPTIONS.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
    </label>
  );
}
