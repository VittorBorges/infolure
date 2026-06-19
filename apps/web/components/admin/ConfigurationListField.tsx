'use client';

import { Button, Input } from '@infolure/design-system';

// Feature 006 — "Configuração da isca" (antes "Tamanho"): dimensão + peso + anzol por configuração.
export interface ConfigurationRow {
  code: string;
  label: string;
  length_mm: string;
  weight_g: string;
  hook_size: string;
  hook_type: string;
  hook_count: string;
}

interface Props {
  value: ConfigurationRow[];
  onChange: (next: ConfigurationRow[]) => void;
}

export const emptyConfiguration = (): ConfigurationRow => ({
  code: '', label: '', length_mm: '', weight_g: '', hook_size: '', hook_type: '', hook_count: '',
});

export function ConfigurationListField({ value, onChange }: Props) {
  const update = (i: number, patch: Partial<ConfigurationRow>) =>
    onChange(value.map((r, idx) => (idx === i ? { ...r, ...patch } : r)));
  const add = () => onChange([...value, emptyConfiguration()]);
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i));

  return (
    <div className="space-y-3">
      <span className="text-sm font-medium">Configurações *</span>
      {value.map((row, i) => (
        <div key={i} className="space-y-2 rounded-md border p-3">
          <div className="grid grid-cols-[1fr_1fr_1fr_1fr_auto] items-end gap-2">
            <Field label="Código">
              <Input aria-label={`Código ${i + 1}`} placeholder="S1" value={row.code} onChange={(e) => update(i, { code: e.target.value })} />
            </Field>
            <Field label="Rótulo *">
              <Input aria-label={`Rótulo ${i + 1}`} placeholder="100SP" value={row.label} onChange={(e) => update(i, { label: e.target.value })} />
            </Field>
            <Field label="Comp. (mm)">
              <Input aria-label={`Comprimento ${i + 1}`} type="number" step="0.1" value={row.length_mm} onChange={(e) => update(i, { length_mm: e.target.value })} />
            </Field>
            <Field label="Peso (g) *">
              <Input aria-label={`Peso ${i + 1}`} type="number" step="0.1" value={row.weight_g} onChange={(e) => update(i, { weight_g: e.target.value })} />
            </Field>
            <Button type="button" variant="ghost" size="sm" onClick={() => remove(i)} disabled={value.length === 1} aria-label={`Remover configuração ${i + 1}`}>
              ✕
            </Button>
          </div>
          <div className="grid grid-cols-3 gap-2">
            <Field label="Anzol — tamanho">
              <Input aria-label={`Anzol tamanho ${i + 1}`} placeholder="#4" value={row.hook_size} onChange={(e) => update(i, { hook_size: e.target.value })} />
            </Field>
            <Field label="Anzol — tipo">
              <Input aria-label={`Anzol tipo ${i + 1}`} placeholder="treble, single…" value={row.hook_type} onChange={(e) => update(i, { hook_type: e.target.value })} />
            </Field>
            <Field label="Anzol — nº">
              <Input aria-label={`Anzol número ${i + 1}`} type="number" value={row.hook_count} onChange={(e) => update(i, { hook_count: e.target.value })} />
            </Field>
          </div>
        </div>
      ))}
      <Button type="button" variant="outline" size="sm" onClick={add}>
        + Adicionar configuração
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
