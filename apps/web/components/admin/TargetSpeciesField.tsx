'use client';

import { useEffect, useRef, useState } from 'react';
import { Button, Input } from '@infolure/design-system';
import { searchSpeciesAction } from '../../lib/admin-actions';

export interface TargetSpeciesRow {
  species_id: string;
  name: string;
  confidence: string; // '' | 'primary' | 'secondary'
}

interface Species { id: string; name: string; slug: string }

interface Props {
  value: TargetSpeciesRow[];
  onChange: (next: TargetSpeciesRow[]) => void;
}

const CONFIDENCES = ['', 'primary', 'secondary'] as const;

// Feature 006 — espécies-alvo da isca por nome (autocomplete multi-seleção). Nunca expõe o UUID.
export function TargetSpeciesField({ value, onChange }: Props) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<Species[]>([]);
  const [open, setOpen] = useState(false);
  const debounce = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!open) return;
    const q = query.trim();
    if (debounce.current) clearTimeout(debounce.current);
    debounce.current = setTimeout(async () => {
      if (q.length < 2) { setResults([]); return; }
      const r = await searchSpeciesAction(q);
      if (r.ok) setResults(r.data);
    }, 250);
    return () => { if (debounce.current) clearTimeout(debounce.current); };
  }, [query, open]);

  const add = (s: Species) => {
    if (!value.some((v) => v.species_id === s.id)) {
      onChange([...value, { species_id: s.id, name: s.name, confidence: '' }]);
    }
    setQuery('');
    setResults([]);
    setOpen(false);
  };
  const remove = (id: string) => onChange(value.filter((v) => v.species_id !== id));
  const setConfidence = (id: string, confidence: string) =>
    onChange(value.map((v) => (v.species_id === id ? { ...v, confidence } : v)));

  return (
    <div className="space-y-3">
      <span className="text-sm font-medium">Espécies-alvo</span>

      <div className="relative">
        <Input
          placeholder="Procurar espécie pelo nome…"
          value={query}
          onChange={(e) => { setQuery(e.target.value); setOpen(true); }}
          onFocus={() => setOpen(true)}
          aria-label="Espécie-alvo (busca por nome)"
        />
        {open && results.length > 0 && (
          <ul className="absolute z-10 mt-1 max-h-56 w-full overflow-auto rounded-md border bg-background shadow-md">
            {results.map((s) => (
              <li key={s.id}>
                <button
                  type="button"
                  className="flex w-full px-3 py-2 text-left text-sm hover:bg-muted disabled:opacity-50"
                  onClick={() => add(s)}
                  disabled={value.some((v) => v.species_id === s.id)}
                >
                  {s.name}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      {value.length > 0 && (
        <ul className="space-y-2">
          {value.map((row) => (
            <li key={row.species_id} className="flex items-center gap-2 rounded-md border px-3 py-2 text-sm">
              <span className="flex-1">{row.name}</span>
              <select
                aria-label={`Confiança ${row.name}`}
                value={row.confidence}
                onChange={(e) => setConfidence(row.species_id, e.target.value)}
                className="h-8 rounded-md border border-input bg-transparent px-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                {CONFIDENCES.map((c) => (<option key={c} value={c}>{c === '' ? 'confiança —' : c}</option>))}
              </select>
              <Button type="button" variant="ghost" size="sm" onClick={() => remove(row.species_id)} aria-label={`Remover ${row.name}`}>
                ✕
              </Button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
