'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';
import Link from 'next/link';
import { Button, Input, Label, Card, CardContent, CardHeader, CardTitle } from '@infolure/design-system';
import { createBrandAction, updateBrandAction } from '../../lib/admin-actions';

export interface BrandInitial { id: string; slug: string; name: string }

interface Props {
  mode: 'create' | 'edit';
  initial?: BrandInitial;
}

// Feature 006 (US2) — criar/editar marca.
export function BrandForm({ mode, initial }: Props) {
  const router = useRouter();
  const [pending, start] = useTransition();
  const [slug, setSlug] = useState(initial?.slug ?? '');
  const [name, setName] = useState(initial?.name ?? '');
  const [msg, setMsg] = useState<{ kind: 'ok' | 'err'; text: string } | null>(null);

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setMsg(null);
    if (!name.trim()) { setMsg({ kind: 'err', text: 'O nome é obrigatório.' }); return; }
    if (mode === 'create' && !slug.trim()) { setMsg({ kind: 'err', text: 'O slug é obrigatório.' }); return; }
    start(async () => {
      const r = mode === 'create'
        ? await createBrandAction({ slug: slug.trim(), name: name.trim() })
        : await updateBrandAction(initial!.id, { slug: slug.trim() || undefined, name: name.trim() });
      if (!r.ok) {
        setMsg({ kind: 'err', text: r.status === 409 ? 'Já existe uma marca com este slug.' : `Falha ao guardar (${r.status}).` });
        return;
      }
      setMsg({ kind: 'ok', text: mode === 'create' ? 'Marca criada.' : 'Alterações guardadas.' });
      if (mode === 'create' && 'data' in r && r.data?.id) router.push(`/admin/brands/${r.data.id}`);
      else router.refresh();
    });
  }

  return (
    <Card className="max-w-xl">
      <CardHeader><CardTitle>{mode === 'create' ? 'Nova marca' : 'Editar marca'}</CardTitle></CardHeader>
      <CardContent>
        <form onSubmit={onSubmit} className="space-y-4">
          <div className="space-y-1">
            <Label htmlFor="brand-name">Nome *</Label>
            <Input id="brand-name" value={name} onChange={(e) => setName(e.target.value)} placeholder="Rapala" />
          </div>
          <div className="space-y-1">
            <Label htmlFor="brand-slug">Slug {mode === 'create' ? '*' : ''}</Label>
            <Input id="brand-slug" value={slug} onChange={(e) => setSlug(e.target.value)} placeholder="rapala" />
          </div>
          {msg && <p className={msg.kind === 'ok' ? 'text-sm text-success' : 'text-sm text-destructive'} role="alert">{msg.text}</p>}
          <div className="flex items-center gap-3">
            <Button type="submit" disabled={pending}>{pending ? 'A guardar…' : mode === 'create' ? 'Criar marca' : 'Guardar'}</Button>
            <Button type="button" variant="outline" asChild><Link href="/admin/brands">Cancelar</Link></Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
