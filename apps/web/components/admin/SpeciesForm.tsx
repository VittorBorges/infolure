'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';
import Link from 'next/link';
import { Button, Input, Label, Card, CardContent, CardHeader, CardTitle } from '@infolure/design-system';
import { createSpeciesAction, updateSpeciesAction } from '../../lib/admin-actions';

export interface SpeciesInitial {
  id: string;
  slug: string;
  common_name: string;
  water_type?: string | null;
  family?: string | null;
}

interface Props {
  mode: 'create' | 'edit';
  initial?: SpeciesInitial;
}

const WATER_TYPES = ['', 'freshwater', 'saltwater', 'both'] as const;

// Feature 006 — criar/editar espécie.
export function SpeciesForm({ mode, initial }: Props) {
  const router = useRouter();
  const [pending, start] = useTransition();
  const [slug, setSlug] = useState(initial?.slug ?? '');
  const [name, setName] = useState(initial?.common_name ?? '');
  const [waterType, setWaterType] = useState(initial?.water_type ?? '');
  const [family, setFamily] = useState(initial?.family ?? '');
  const [msg, setMsg] = useState<{ kind: 'ok' | 'err'; text: string } | null>(null);

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setMsg(null);
    if (!name.trim()) { setMsg({ kind: 'err', text: 'O nome comum é obrigatório.' }); return; }
    if (mode === 'create' && !slug.trim()) { setMsg({ kind: 'err', text: 'O slug é obrigatório.' }); return; }
    const body = {
      common_name: name.trim(),
      water_type: waterType || null,
      family: family.trim() || null,
    };
    start(async () => {
      const r = mode === 'create'
        ? await createSpeciesAction({ slug: slug.trim(), ...body })
        : await updateSpeciesAction(initial!.id, { slug: slug.trim() || undefined, ...body });
      if (!r.ok) {
        setMsg({ kind: 'err', text: r.status === 409 ? 'Já existe uma espécie com este slug.' : `Falha ao guardar (${r.status}).` });
        return;
      }
      setMsg({ kind: 'ok', text: mode === 'create' ? 'Espécie criada.' : 'Alterações guardadas.' });
      if (mode === 'create' && 'data' in r && r.data?.id) router.push(`/admin/species/${r.data.id}`);
      else router.refresh();
    });
  }

  return (
    <Card className="max-w-xl">
      <CardHeader><CardTitle>{mode === 'create' ? 'Nova espécie' : 'Editar espécie'}</CardTitle></CardHeader>
      <CardContent>
        <form onSubmit={onSubmit} className="space-y-4">
          <div className="space-y-1">
            <Label htmlFor="species-name">Nome comum *</Label>
            <Input id="species-name" value={name} onChange={(e) => setName(e.target.value)} placeholder="Robalo" />
          </div>
          <div className="space-y-1">
            <Label htmlFor="species-slug">Slug {mode === 'create' ? '*' : ''}</Label>
            <Input id="species-slug" value={slug} onChange={(e) => setSlug(e.target.value)} placeholder="robalo" />
          </div>
          <div className="space-y-1">
            <Label htmlFor="species-water">Tipo de água</Label>
            <select
              id="species-water"
              value={waterType}
              onChange={(e) => setWaterType(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              {WATER_TYPES.map((o) => (<option key={o} value={o}>{o === '' ? '—' : o}</option>))}
            </select>
          </div>
          <div className="space-y-1">
            <Label htmlFor="species-family">Família</Label>
            <Input id="species-family" value={family} onChange={(e) => setFamily(e.target.value)} placeholder="Moronidae" />
          </div>
          {msg && <p className={msg.kind === 'ok' ? 'text-sm text-success' : 'text-sm text-destructive'} role="alert">{msg.text}</p>}
          <div className="flex items-center gap-3">
            <Button type="submit" disabled={pending}>{pending ? 'A guardar…' : mode === 'create' ? 'Criar espécie' : 'Guardar'}</Button>
            <Button type="button" variant="outline" asChild><Link href="/admin/species">Cancelar</Link></Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
