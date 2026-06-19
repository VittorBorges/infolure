'use client';

import { Button, Input, Label, Card, CardContent } from '@infolure/design-system';
import { HexCodeListField, type HexRow } from './HexCodeListField';
import { ColorPhotosField } from './ColorPhotosField';

export interface ColorRow {
  name_pt: string;
  pattern: string;
  photos: string[];
  hex: HexRow[];
}

interface Props {
  value: ColorRow[];
  onChange: (next: ColorRow[]) => void;
}

export const emptyColor = (): ColorRow => ({ name_pt: '', pattern: '', photos: [], hex: [] });

// Feature 005 (US3) — lista de cores. Cada cor: nome, padrão, foto opcional e lista de hex.
export function ColorListField({ value, onChange }: Props) {
  const update = (i: number, patch: Partial<ColorRow>) =>
    onChange(value.map((r, idx) => (idx === i ? { ...r, ...patch } : r)));
  const add = () => onChange([...value, emptyColor()]);
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i));

  return (
    <div className="space-y-3">
      <Label>Cores</Label>
      {value.length === 0 && <p className="text-xs text-muted-foreground">Sem cores (opcional).</p>}
      {value.map((row, i) => (
        <Card key={i}>
          <CardContent className="space-y-4 pt-6">
            <div className="flex items-start gap-2">
              <div className="grid flex-1 grid-cols-2 gap-2">
                <div className="space-y-1">
                  <span className="text-xs text-muted-foreground">Nome</span>
                  <Input aria-label={`Nome da cor ${i + 1}`} placeholder="Tiger" value={row.name_pt} onChange={(e) => update(i, { name_pt: e.target.value })} />
                </div>
                <div className="space-y-1">
                  <span className="text-xs text-muted-foreground">Padrão</span>
                  <Input aria-label={`Padrão da cor ${i + 1}`} placeholder="listas, glitter…" value={row.pattern} onChange={(e) => update(i, { pattern: e.target.value })} />
                </div>
              </div>
              <Button type="button" variant="ghost" size="sm" onClick={() => remove(i)} aria-label={`Remover cor ${i + 1}`}>
                ✕ Remover
              </Button>
            </div>
            <HexCodeListField value={row.hex} onChange={(hex) => update(i, { hex })} />
            <ColorPhotosField value={row.photos} onChange={(photos) => update(i, { photos })} />
          </CardContent>
        </Card>
      ))}
      <Button type="button" variant="outline" size="sm" onClick={add}>
        + Adicionar cor
      </Button>
    </div>
  );
}
