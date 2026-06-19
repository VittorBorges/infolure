'use client';

import { useEffect, useRef, useState } from 'react';
import { Button, Input, Label } from '@infolure/design-system';
import { searchBrandsAction } from '../../lib/admin-actions';

interface Brand { id: string; name: string; slug: string }

interface Props {
  value: string;             // brand_id selecionado ("" = nenhuma)
  initialName?: string;      // nome pré-selecionado (edição)
  onChange: (brandId: string, brandName: string) => void;
}

// Feature 006 (US3) — autocomplete de marca por nome. Nunca expõe o UUID ao editor.
export function BrandPicker({ value, initialName, onChange }: Props) {
  const [query, setQuery] = useState(initialName ?? '');
  const [results, setResults] = useState<Brand[]>([]);
  const [open, setOpen] = useState(false);
  const [selectedName, setSelectedName] = useState(initialName ?? '');
  const debounce = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (!open) return;
    const q = query.trim();
    if (debounce.current) clearTimeout(debounce.current);
    debounce.current = setTimeout(async () => {
      if (q.length < 2) { setResults([]); return; }
      const r = await searchBrandsAction(q);
      if (r.ok) setResults(r.data);
    }, 250);
    return () => { if (debounce.current) clearTimeout(debounce.current); };
  }, [query, open]);

  function select(b: Brand) {
    setSelectedName(b.name);
    setQuery(b.name);
    setOpen(false);
    onChange(b.id, b.name);
  }

  function clear() {
    setSelectedName('');
    setQuery('');
    onChange('', '');
  }

  return (
    <div className="space-y-1">
      <Label>Marca</Label>
      <div className="relative">
        <Input
          placeholder="Procurar marca pelo nome…"
          value={query}
          onChange={(e) => { setQuery(e.target.value); setOpen(true); }}
          onFocus={() => setOpen(true)}
          aria-label="Marca (busca por nome)"
        />
        {open && results.length > 0 && (
          <ul className="absolute z-10 mt-1 max-h-56 w-full overflow-auto rounded-md border bg-background shadow-md">
            {results.map((b) => (
              <li key={b.id}>
                <button
                  type="button"
                  className="flex w-full px-3 py-2 text-left text-sm hover:bg-muted"
                  onClick={() => select(b)}
                >
                  {b.name}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
      {value && selectedName && (
        <p className="flex items-center gap-2 text-xs text-muted-foreground">
          Selecionada: <strong>{selectedName}</strong>
          <Button type="button" variant="ghost" size="sm" onClick={clear}>limpar</Button>
        </p>
      )}
    </div>
  );
}
