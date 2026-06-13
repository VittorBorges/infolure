'use client';

import { useEffect, useRef, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { apiGet } from '../../lib/api';
import type { SuggestResponse } from '../../lib/catalog';

// Barra de busca com autocomplete (US-02): aparece após ≥2 chars, debounce 250ms.
// Submeter aplica `q` à listagem (estado na URL).
export function SearchBar() {
  const router = useRouter();
  const params = useSearchParams();
  const [value, setValue] = useState(params.get('q') ?? '');
  const [suggestions, setSuggestions] = useState<SuggestResponse['suggestions']>([]);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (timer.current) clearTimeout(timer.current);
    if (value.trim().length < 2) {
      setSuggestions([]);
      return;
    }
    timer.current = setTimeout(async () => {
      try {
        const res = await apiGet<SuggestResponse>(
          `/v1/lures/suggest?q=${encodeURIComponent(value.trim())}`,
        );
        setSuggestions(res.suggestions);
      } catch {
        setSuggestions([]);
      }
    }, 250);
    return () => {
      if (timer.current) clearTimeout(timer.current);
    };
  }, [value]);

  function submit(q: string) {
    const next = new URLSearchParams(params.toString());
    if (q.trim()) next.set('q', q.trim());
    else next.delete('q');
    next.delete('page');
    setSuggestions([]);
    router.push(`/iscas?${next.toString()}`);
  }

  return (
    <div style={{ position: 'relative', maxWidth: 420 }}>
      <input
        type="search"
        value={value}
        placeholder="Procurar iscas, marcas ou modelos…"
        aria-label="Procurar iscas"
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={(e) => e.key === 'Enter' && submit(value)}
        style={{ width: '100%', padding: '0.5rem' }}
      />
      {suggestions.length > 0 && (
        <ul
          role="listbox"
          style={{ position: 'absolute', zIndex: 10, background: '#fff', border: '1px solid #eaeaea', width: '100%', listStyle: 'none', margin: 0, padding: 0 }}
        >
          {suggestions.map((s) => (
            <li key={s.slug}>
              <button
                type="button"
                onClick={() => router.push(`/iscas/${s.slug}`)}
                style={{ display: 'block', width: '100%', textAlign: 'left', padding: '0.4rem 0.5rem', border: 'none', background: 'none', cursor: 'pointer' }}
              >
                {s.name} <span style={{ color: '#888' }}>· {s.brand}</span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
