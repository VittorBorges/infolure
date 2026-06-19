'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';
import Link from 'next/link';
import {
  Button, Input, Label, Card, CardContent, CardHeader, CardTitle, CardDescription,
} from '@infolure/design-system';

import { createLureAction, updateLureAction, type LureWritePayload } from '../../lib/admin-actions';
import { isValidHex, normalizeHex } from '../../lib/hex';
import { ConfigurationListField, emptyConfiguration, type ConfigurationRow } from './ConfigurationListField';
import { ColorListField, type ColorRow } from './ColorListField';
import { BrandPicker } from './BrandPicker';
import { TargetSpeciesField, type TargetSpeciesRow } from './TargetSpeciesField';

const STATUSES = ['draft', 'published', 'archived'] as const;
const WATER_TYPES = ['', 'freshwater', 'saltwater', 'both'] as const;

export interface LureInitial {
  id: string;
  slug: string;
  name: string;
  description?: string | null;
  brand_id?: string | null;
  brand_name?: string | null;
  lure_type: string;
  water_type?: string | null;
  model_ref?: string | null;
  material?: string | null;
  depth_min_m?: number | null;
  depth_max_m?: number | null;
  status: string;
  configurations: {
    code?: string | null; label: string; length_mm?: number | null; weight_g?: number | null;
    hook_size?: string | null; hook_type?: string | null; hook_count?: number | null;
  }[];
  colors: { name_pt: string; pattern?: string | null; photo_urls?: string[]; hex_codes: { hex: string; label?: string | null }[] }[];
  target_species?: { species_id: string; name: string; confidence?: string | null }[];
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
    material: str(initial?.material),
    depth_min_m: str(initial?.depth_min_m),
    depth_max_m: str(initial?.depth_max_m),
    status: initial?.status ?? 'draft',
  });
  const set = (patch: Partial<typeof f>) => setF((prev) => ({ ...prev, ...patch }));

  const [configs, setConfigs] = useState<ConfigurationRow[]>(
    initial && initial.configurations.length > 0
      ? initial.configurations.map((c) => ({
          code: str(c.code), label: c.label, length_mm: str(c.length_mm), weight_g: str(c.weight_g),
          hook_size: str(c.hook_size), hook_type: str(c.hook_type), hook_count: str(c.hook_count),
        }))
      : [emptyConfiguration()],
  );
  const [colors, setColors] = useState<ColorRow[]>(
    (initial?.colors ?? []).map((c) => ({
      name_pt: c.name_pt ?? '',
      pattern: str(c.pattern),
      photos: c.photo_urls ?? [],
      hex: c.hex_codes.map((h) => ({ hex: h.hex, label: str(h.label) })),
    })),
  );
  const [species, setSpecies] = useState<TargetSpeciesRow[]>(
    (initial?.target_species ?? []).map((s) => ({
      species_id: s.species_id, name: s.name, confidence: str(s.confidence),
    })),
  );

  function validate(): string | null {
    if (!f.slug.trim()) return 'O slug é obrigatório.';
    if (!f.name.trim()) return 'O nome é obrigatório.';
    if (!f.lure_type.trim()) return 'O tipo de isca é obrigatório.';
    if (configs.length === 0) return 'Adicione pelo menos uma configuração.';
    for (const c of configs) {
      if (!c.label.trim()) return 'Cada configuração precisa de um rótulo.';
      // Peso opcional; quando preenchido, deve ser um número > 0.
      if (c.weight_g.trim() !== '') {
        const w = Number(c.weight_g);
        if (Number.isNaN(w) || w <= 0) return 'O peso da configuração, quando indicado, deve ser > 0.';
      }
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
      material: f.material.trim() || null,
      depth_min_m: numOrNull(f.depth_min_m),
      depth_max_m: numOrNull(f.depth_max_m),
      status: f.status,
      configurations: configs.map((c, i) => ({
        code: c.code.trim() || undefined,
        label: c.label.trim(),
        length_mm: numOrNull(c.length_mm),
        weight_g: c.weight_g.trim() === '' ? null : Number(c.weight_g),
        hook_size: c.hook_size.trim() || null,
        hook_type: c.hook_type.trim() || null,
        hook_count: c.hook_count.trim() === '' ? null : Number(c.hook_count),
        sort_order: i,
      })),
      colors: colors.map((c) => ({
        name_pt: c.name_pt.trim() || undefined,
        pattern: c.pattern.trim() || null,
        photo_urls: c.photos.map((p) => p.trim()).filter((p) => p !== ''),
        hex_codes: c.hex
          .filter((h) => h.hex.trim() !== '')
          .map((h, i) => ({ hex: normalizeHex(h.hex), label: h.label.trim() || null, sort_order: i })),
      })),
      target_species: species.map((s) => ({ species_id: s.species_id, confidence: s.confidence || null })),
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
          <CardDescription>Campos da isca. Os marcados com * são obrigatórios. O anzol é por configuração.</CardDescription>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <Text label="Slug *" value={f.slug} onChange={(v) => set({ slug: v })} />
          <Text label="Nome *" value={f.name} onChange={(v) => set({ name: v })} />
          <Text label="Tipo *" value={f.lure_type} onChange={(v) => set({ lure_type: v })} placeholder="jig, crankbait…" />
          <Select label="Tipo de água" value={f.water_type} onChange={(v) => set({ water_type: v })} options={WATER_TYPES} />
          <BrandPicker
            value={f.brand_id}
            initialName={initial?.brand_name ?? undefined}
            onChange={(brandId) => set({ brand_id: brandId })}
          />
          <Text label="Model ref" value={f.model_ref} onChange={(v) => set({ model_ref: v })} />
          <Text label="Material" value={f.material} onChange={(v) => set({ material: v })} />
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
          <ConfigurationListField value={configs} onChange={setConfigs} />
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-6">
          <ColorListField value={colors} onChange={setColors} />
        </CardContent>
      </Card>

      <Card>
        <CardContent className="pt-6">
          <TargetSpeciesField value={species} onChange={setSpecies} />
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
