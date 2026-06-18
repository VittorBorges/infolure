'use client';

import { Button, Input, Label } from '@infolure/design-system';
import { isValidHex } from '../../lib/hex';

export interface HexRow {
  hex: string;
  label: string;
}

interface Props {
  value: HexRow[];
  onChange: (next: HexRow[]) => void;
}

// Feature 005 (US3) — lista de códigos HTML (hex) de uma cor. Duplicados permitidos.
export function HexCodeListField({ value, onChange }: Props) {
  const update = (i: number, patch: Partial<HexRow>) =>
    onChange(value.map((r, idx) => (idx === i ? { ...r, ...patch } : r)));
  const add = () => onChange([...value, { hex: '', label: '' }]);
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i));

  return (
    <div className="space-y-2">
      <Label>Códigos HTML (hex)</Label>
      {value.length === 0 && <p className="text-xs text-muted-foreground">Sem códigos. Adicione pelo menos um.</p>}
      {value.map((row, i) => {
        const invalid = row.hex.trim() !== '' && !isValidHex(row.hex);
        return (
          <div key={i} className="flex items-center gap-2">
            <span
              aria-hidden
              className="inline-block size-6 shrink-0 rounded border"
              style={{ background: isValidHex(row.hex) ? row.hex : 'transparent' }}
            />
            <div className="flex-1">
              <Input
                aria-label={`Código hex ${i + 1}`}
                placeholder="#00ff00"
                value={row.hex}
                onChange={(e) => update(i, { hex: e.target.value })}
                aria-invalid={invalid}
              />
              {invalid && <p className="mt-1 text-xs text-destructive">Hex inválido (use #RGB ou #RRGGBB).</p>}
            </div>
            <Input
              aria-label={`Cor de base ${i + 1}`}
              placeholder="verde (opcional)"
              value={row.label}
              onChange={(e) => update(i, { label: e.target.value })}
              className="flex-1"
            />
            <Button type="button" variant="ghost" size="sm" onClick={() => remove(i)} aria-label={`Remover hex ${i + 1}`}>
              ✕
            </Button>
          </div>
        );
      })}
      <Button type="button" variant="outline" size="sm" onClick={add}>
        + Adicionar cor/hex
      </Button>
    </div>
  );
}
