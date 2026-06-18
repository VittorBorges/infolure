'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';
import Link from 'next/link';
import {
  Button, Input, Label, Card, CardContent, CardHeader, CardTitle, CardDescription,
} from '@infolure/design-system';

import { createLureAction, updateLureAction, type LureWritePayload } from '../../lib/admin-actions';
import { isValidHex, normalizeHex } from '../../lib/hex';
import { SizeListField, emptySize, type SizeRow } from './SizeListField';
import { ColorListField, type ColorRow } from './ColorListField';

const STATUSES = ['draft', 'published', 'archived'] as const;
const WATER_TYPES = ['', 'freshwater', 'saltwater', 'both'] as const;

export interface LureInitial {
  id: string;
  slug: string;
  name: string;
  description?: string | null;
  brand_id?: string | null;
  lure_type: string;
  water_type?: string | null;
  model_ref?: string | null;
  hook_size?: string | null;
  hook_type?: string | null;
  hook_count?: number | null;
  material?: string | null;
  depth_min_m?: number | null;
  depth_max_m?: number | null;
  status: string;
  sizes: { code?: string | null; label: string; length_mm?: number | null; weight_g: number }[];
  colors: { name_pt: string; pattern?: string | null; photo_url?: string | null; hex_codes: { hex: string; label?: string | null }[] }[];
}

interface Props {
  mode: 'create' | 'edit';
  initial?: LureInitial;
}

const str = (v: unknown) => (v == null ? '' : String(v));
const numOrNull = (s: string): number | null => (s.trim() === '' ? null : Number(s));

export function LureForm({ mode, initial }: Props) {
  const router = useRouter();
  const [pending, start] = useTransition();
  const [msg, setMsg] = useState<{ kind: 'ok' | 'err'; text: string } | null>(null);

  const [f, setF] = useState({
    slug: initial?.slug ?? '',
    name: initial?.name ?? '',
    description: str(initial?.description),
    brand_id: str(initial?.brand_id),
    lure_type: initial?.lure_type ?? '',
    water_type: str(initial?.water_type),
    model_ref: str(initial?.model_ref),
    hook_size: str(initial?.hook_size),
    hook_type: str(initial?.hook_type),
    hook_count: str(initial?.hook_count),
    material: str(initial?.material),
    depth_min_m: str(initial?.depth_min_m),
    depth_max_m: str(initial?.depth_max_m),
    status: initial?.status ?? 'draft',
  });
  const set = (patch: Partial<typeof f>) => setF((prev) => ({ ...prev, ...patch }));

  const [sizes, setSizes] = useState<SizeRow[]>(
    initial && initial.sizes.length > 0
      ? initial.sizes.map((s) => ({ code: str(s.code), label: s.label, length_mm: str(s.length_mm), weight_g: str(s.weight_g) }))
      : [emptySize()],
  );
  const [colors, setColors] = useState<ColorRow[]>(
    (initial?.colors ?? []).map((c) => ({
      name_pt: c.name_pt ?? '',
      pattern: str(c.pattern),
      photo_url: str(c.photo_url),
      hex: c.hex_codes.map((h) => ({ hex: h.hex, label: str(h.label) })),
    })),
  );

  function validate(): string | null {
    if (!f.slug.trim()) return 'O slug é obrigatório.';
    if (!f.name.trim()) return 'O nome é obrigatório.';
    if (!f.lure_type.trim()) return 'O tipo de isca é obrigatório.';
    if (sizes.length === 0) return 'Adicione pelo menos um tamanho.';
    for (const s of sizes) {
      if (!s.label.trim()) return 'Cada tamanho precisa de um rótulo.';
      const w = Number(s.weight_g);
      if (s.weight_g.trim() === '' || Number.isNaN(w) || w <= 0) return 'Cada tamanho precisa de um peso válido (> 0).';
    }
    for (const c of colors) {
      const hasName = c.name_pt.trim() !== '';
      const hexes = c.hex.filter((h) => h.hex.trim() !== '');
      if (!hasName && hexes.length === 0) return 'Cada cor precisa de um nome ou pelo menos um código hex.';
      for (const h of hexes) if (!isValidHex(h.hex)) return `Código hex inválido: ${h.hex}`;
    }
    return null;
  }

  function buildPayload(): LureWritePayload {
    return {
      slug: f.slug.trim(),
      name: f.name.trim(),
      description: f.description.trim() || null,
      brand_id: f.brand_id.trim() || null,
      lure_type: f.lure_type.trim(),
      water_type: f.water_type || null,
      model_ref: f.model_ref.trim() || null,
      hook_size: f.hook_size.trim() || null,
      hook_type: f.hook_type.trim() || null,
      hook_count: f.hook_count.trim() === '' ? null : Number(f.hook_count),
      material: f.material.trim() || null,
      depth_min_m: numOrNull(f.depth_min_m),
      depth_max_m: numOrNull(f.depth_max_m),
      status: f.status,
      sizes: sizes.map((s, i) => ({
        code: s.code.trim() || undefined,
        label: s.label.trim(),
        length_mm: numOrNull(s.length_mm),
        weight_g: Number(s.weight_g),
        sort_order: i,
      })),
      colors: colors.map((c) => ({
        name_pt: c.name_pt.trim() || undefined,
        pattern: c.pattern.trim() || null,
        photo_url: c.photo_url.trim() || null,
        hex_codes: c.hex
          .filter((h) => h.hex.trim() !== '')
          .map((h, i) => ({ hex: normalizeHex(h.hex), label: h.label.trim() || null, sort_order: i })),
      })),
    };
  }

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setMsg(null);
    const err = validate();
    if (err) {
      setMsg({ kind: 'err', text: err });
      return;
    }
    const payload = buildPayload();
    start(async () => {
      const r = mode === 'create' ? await createLureAction(payload) : await updateLureAction(initial!.id, payload);
      if (!r.ok) {
        const text =
          r.status === 403 ? 'Sem permissão.'
          : r.status === 409 ? 'Já existe uma isca com este slug.'
          : r.status === 422 ? 'Validação falhou no servidor — reveja os campos.'
          : `Falha ao guardar (${r.status}).`;
        setMsg({ kind: 'err', text });
        return;
      }
      setMsg({ kind: 'ok', text: mode === 'create' ? 'Isca criada.' : 'Alterações guardadas.' });
      if (mode === 'create' && 'data' in r && r.data?.id) {
        router.push(`/admin/lures/${r.data.id}`);
      } else {
        router.refresh();
      }
    });
  }

  return (
    <form onSubmit={onSubmit} className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Propriedades</CardTitle>
          <CardDescription>Campos da isca. Os marcados com * são obrigatórios.</CardDescription>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <Text label="Slug *" value={f.slug} onChange={(v) => set({ slug: v })} />
          <Text label="Nome *" value={f.name} onChange={(v) => set({ name: v })} />
          <Text label="Tipo *" value={f.lure_type} onChange={(v) => set({ lure_type: v })} placeholder="jig, crankbait…" />
          <Select label="Tipo de água" value={f.water_type} onChange={(v) => set({ water_type: v })} options={WATER_TYPES} />
          <Text label="Marca (UUID)" value={f.brand_id} onChange={(v) => set({ brand_id: v })} />
          <Text label="Model ref" value={f.model_ref} onChange={(v) => set({ model_ref: v })} />
          <Text label="Material" value={f.material} onChange={(v) => set({ material: v })} />
          <Text label="Anzol — tamanho" value={f.hook_size} onChange={(v) => set({ hook_size: v })} />
          <Text label="Anzol — tipo" value={f.hook_type} onChange={(v) => set({ hook_type: v })} />
          <Text label="Anzol — nº" type="number" value={f.hook_count} onChange={(v) => set({ hook_count: v })} />
          <Text label="Prof. mín (m)" type="number" value={f.depth_min_m} onChange={(v) => set({ depth_min_m: v })} />
          <Text label="Prof. máx (m)" type="number" value={f.depth_max_m} onChange={(v) => set({ depth_max_m: v })} />
          <Select label="Estado editorial" value={f.status} onChange={(v) => set({ status: v })} options={STATUSES} />
          <div className="col-span-2 space-y-1">
            <Label htmlFor="description">Descrição</Label>
            <textarea
              id="description"
              value={f.description}
              onChange={(e) => set({ description: e.target.value })}
              rows={3}
              className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-6">
          <SizeListField value={sizes} onChange={setSizes} />
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-6">
          <ColorListField value={colors} onChange={setColors} />
        </CardContent>
      </Card>

      {msg && <p className={msg.kind === 'ok' ? 'text-sm text-success' : 'text-sm text-destructive'} role="alert">{msg.text}</p>}

      <div className="flex items-center gap-3">
        <Button type="submit" disabled={pending}>
          {pending ? 'A guardar…' : mode === 'create' ? 'Criar isca' : 'Guardar alterações'}
        </Button>
        <Button type="button" variant="outline" asChild>
          <Link href="/admin/lures">Cancelar</Link>
        </Button>
      </div>
    </form>
  );
}

function Text({ label, value, onChange, type = 'text', placeholder }: { label: string; value: string; onChange: (v: string) => void; type?: string; placeholder?: string }) {
  return (
    <div className="space-y-1">
      <Label>{label}</Label>
      <Input type={type} value={value} placeholder={placeholder} onChange={(e) => onChange(e.target.value)} />
    </div>
  );
}

function Select({ label, value, onChange, options }: { label: string; value: string; onChange: (v: string) => void; options: readonly string[] }) {
  return (
    <div className="space-y-1">
      <Label>{label}</Label>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        {options.map((o) => (
          <option key={o} value={o}>{o === '' ? '—' : o}</option>
        ))}
      </select>
    </div>
  );
}
