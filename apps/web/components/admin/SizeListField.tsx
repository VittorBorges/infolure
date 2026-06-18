'use client';

import { Button, Input, Label } from '@infolure/design-system';

export interface SizeRow {
  code: string;
  label: string;
  length_mm: string;
  weight_g: string;
}

interface Props {
  value: SizeRow[];
  onChange: (next: SizeRow[]) => void;
}

export const emptySize = (): SizeRow => ({ code: '', label: '', length_mm: '', weight_g: '' });

// Feature 005 (US1) — lista de tamanhos da isca (rótulo + comprimento + peso). ≥1 obrigatório.
export function SizeListField({ value, onChange }: Props) {
  const update = (i: number, patch: Partial<SizeRow>) =>
    onChange(value.map((r, idx) => (idx === i ? { ...r, ...patch } : r)));
  const add = () => onChange([...value, emptySize()]);
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i));

  return (
    <div className="space-y-3">
      <Label>Tamanhos *</Label>
      {value.map((row, i) => {
        const weightInvalid = row.weight_g.trim() !== '' && Number.isNaN(Number(row.weight_g));
        return (
          <div key={i} className="grid grid-cols-[1fr_1fr_1fr_1fr_auto] items-end gap-2">
            <Field label="Código">
              <Input aria-label={`Código ${i + 1}`} placeholder="S1" value={row.code} onChange={(e) => update(i, { code: e.target.value })} />
            </Field>
            <Field label="Rótulo *">
              <Input aria-label={`Rótulo ${i + 1}`} placeholder="100SP" value={row.label} onChange={(e) => update(i, { label: e.target.value })} />
            </Field>
            <Field label="Comp. (mm)">
              <Input aria-label={`Comprimento ${i + 1}`} type="number" step="0.1" inputMode="decimal" value={row.length_mm} onChange={(e) => update(i, { length_mm: e.target.value })} />
            </Field>
            <Field label="Peso (g) *">
              <Input aria-label={`Peso ${i + 1}`} type="number" step="0.1" inputMode="decimal" value={row.weight_g} onChange={(e) => update(i, { weight_g: e.target.value })} aria-invalid={weightInvalid} />
            </Field>
            <Button type="button" variant="ghost" size="sm" onClick={() => remove(i)} disabled={value.length === 1} aria-label={`Remover tamanho ${i + 1}`}>
              ✕
            </Button>
          </div>
        );
      })}
      <Button type="button" variant="outline" size="sm" onClick={add}>
        + Adicionar tamanho
      </Button>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1">
      <span className="text-xs text-muted-foreground">{label}</span>
      {children}
    </div>
  );
}
