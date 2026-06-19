'use client';

import { useState, useTransition } from 'react';
import { Button, Card, CardContent, CardHeader, CardTitle, CardDescription } from '@infolure/design-system';
import { setGlobalIndexingAction } from '../../lib/admin-actions';

// Feature 006 (US1) — interruptor único para ligar/desligar a indexação SEO de TODO o catálogo.
export function GlobalIndexingToggle({ initialEnabled }: { initialEnabled: boolean }) {
  const [enabled, setEnabled] = useState(initialEnabled);
  const [pending, start] = useTransition();
  const [msg, setMsg] = useState<{ kind: 'ok' | 'err'; text: string } | null>(null);

  function toggle(next: boolean) {
    setMsg(null);
    start(async () => {
      const r = await setGlobalIndexingAction(next);
      if (!r.ok) { setMsg({ kind: 'err', text: `Falha ao guardar (${r.status}).` }); return; }
      setEnabled(next);
      setMsg({ kind: 'ok', text: next ? 'Indexação SEO ligada para todo o catálogo.' : 'Indexação SEO desligada para todo o catálogo.' });
    });
  }

  return (
    <Card className="max-w-xl">
      <CardHeader>
        <CardTitle>Indexação SEO (global)</CardTitle>
        <CardDescription>
          Liga ou desliga a indexação SEO de <strong>todo</strong> o catálogo de uma só vez. Não há controlo por isca.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        <p className="text-sm">
          Estado atual:{' '}
          <strong className={enabled ? 'text-success' : 'text-destructive'}>
            {enabled ? 'ligada' : 'desligada'}
          </strong>
        </p>
        <div className="flex items-center gap-3">
          <Button variant="success" disabled={pending || enabled} onClick={() => toggle(true)}>
            Ligar tudo
          </Button>
          <Button variant="destructive" disabled={pending || !enabled} onClick={() => toggle(false)}>
            Desligar tudo
          </Button>
        </div>
        {msg && <p className={msg.kind === 'ok' ? 'text-sm text-success' : 'text-sm text-destructive'} role="alert">{msg.text}</p>}
      </CardContent>
    </Card>
  );
}
