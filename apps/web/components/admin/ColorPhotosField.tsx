'use client';

import { useState, useTransition } from 'react';
import { Button, Input, Label } from '@infolure/design-system';
import { uploadMediaAction } from '../../lib/admin-actions';

interface Props {
  value: string[];
  onChange: (urls: string[]) => void;
}

// Feature 006 (US5) — lista de fotos por cor. Upload via /v1/admin/media (≤ 5 MB); se o
// armazenamento não estiver configurado (503), permite colar URLs. Cada foto pode ser removida.
export function ColorPhotosField({ value, onChange }: Props) {
  const [pending, start] = useTransition();
  const [err, setErr] = useState<string | null>(null);

  function onFiles(e: React.ChangeEvent<HTMLInputElement>) {
    const files = Array.from(e.target.files ?? []);
    if (files.length === 0) return;
    setErr(null);
    start(async () => {
      const added: string[] = [];
      for (const file of files) {
        const fd = new FormData();
        fd.append('file', file);
        const r = await uploadMediaAction(fd);
        if (r.ok) added.push(r.data.url);
        else if (r.status === 503) setErr('Armazenamento de fotos não configurado — cole uma URL.');
        else if (r.status === 415) setErr('Tipo de ficheiro não suportado (use JPEG/PNG/WebP).');
        else if (r.status === 413) setErr('Ficheiro demasiado grande (máx. 5 MB).');
        else setErr(`Falha no upload (${r.status}).`);
      }
      if (added.length) onChange([...value, ...added]);
    });
  }

  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i));
  const addUrl = () => onChange([...value, '']);
  const setUrl = (i: number, url: string) => onChange(value.map((u, idx) => (idx === i ? url : u)));

  return (
    <div className="space-y-2">
      <Label>Fotos (várias, opcionais)</Label>
      <div className="flex flex-wrap gap-3">
        {value.map((url, i) => (
          <div key={i} className="flex items-center gap-1">
            {url
              // eslint-disable-next-line @next/next/no-img-element
              ? <img src={url} alt={`Foto ${i + 1}`} className="size-12 rounded border object-cover" />
              : null}
            <Input aria-label={`URL da foto ${i + 1}`} placeholder="https://…" value={url} onChange={(e) => setUrl(i, e.target.value)} className="w-48" />
            <Button type="button" variant="ghost" size="sm" onClick={() => remove(i)} aria-label={`Remover foto ${i + 1}`}>✕</Button>
          </div>
        ))}
      </div>
      <div className="flex items-center gap-3">
        <input type="file" accept="image/png,image/jpeg,image/webp" multiple onChange={onFiles} disabled={pending} aria-label="Carregar fotos" />
        {pending && <span className="text-xs text-muted-foreground">A carregar…</span>}
        <Button type="button" variant="outline" size="sm" onClick={addUrl}>+ URL manual</Button>
      </div>
      {err && <p className="text-xs text-destructive">{err}</p>}
    </div>
  );
}
