'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';
import Link from 'next/link';

import { updateLureAction } from '../../lib/admin-actions';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '../ui/card';

const STATUSES = ['draft', 'published', 'archived'] as const;

interface Props {
  id: string;
  name?: string;
  slug?: string;
  initialStatus?: string;
}

export function LureEditForm({ id, name, slug, initialStatus }: Props) {
  const router = useRouter();
  const [pending, start] = useTransition();
  const [status, setStatus] = useState(initialStatus ?? 'draft');
  const [weight, setWeight] = useState('');
  const [msg, setMsg] = useState<{ kind: 'ok' | 'err'; text: string } | null>(null);

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setMsg(null);
    const weightNum = weight.trim() === '' ? undefined : Number(weight);
    if (weightNum !== undefined && Number.isNaN(weightNum)) {
      setMsg({ kind: 'err', text: 'O peso deve ser um número (em gramas).' });
      return;
    }
    start(async () => {
      const r = await updateLureAction(id, { status, weight_g: weightNum });
      if (!r.ok) {
        setMsg({ kind: 'err', text: r.status === 403 ? 'Sem permissão.' : `Falha ao guardar (${r.status}).` });
        return;
      }
      setMsg({ kind: 'ok', text: 'Alterações guardadas.' });
      router.refresh();
    });
  }

  return (
    <Card className="max-w-xl">
      <CardHeader>
        <CardTitle>Editar isca</CardTitle>
        <CardDescription>
          {name ?? slug ?? id}
          {slug && name ? ` · ${slug}` : ''}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={onSubmit} className="space-y-5">
          <div className="space-y-2">
            <Label htmlFor="status">Estado editorial</Label>
            <select
              id="status"
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              {STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="weight">Peso (g)</Label>
            <Input
              id="weight"
              type="number"
              step="0.1"
              inputMode="decimal"
              placeholder="deixar vazio para manter"
              value={weight}
              onChange={(e) => setWeight(e.target.value)}
            />
            <p className="text-xs text-muted-foreground">
              Em branco mantém o valor atual (não há leitura do peso na listagem).
            </p>
          </div>

          {msg && (
            <p className={msg.kind === 'ok' ? 'text-sm text-success' : 'text-sm text-destructive'}>{msg.text}</p>
          )}

          <div className="flex items-center gap-3 pt-1">
            <Button type="submit" disabled={pending}>
              {pending ? 'A guardar…' : 'Guardar alterações'}
            </Button>
            <Button type="button" variant="outline" asChild>
              <Link href="/admin/lures">Cancelar</Link>
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
