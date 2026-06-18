'use client';

import { useState, useTransition } from 'react';
import { Input, Label } from '@infolure/design-system';
import { uploadMediaAction } from '../../lib/admin-actions';

interface Props {
  value: string;
  onChange: (url: string) => void;
}

// Feature 005 (US3) — foto opcional de uma cor. Upload via /v1/admin/media; se o armazenamento
// não estiver configurado (503), permite colar uma URL manualmente. A ausência de foto é válida.
export function ColorPhotoField({ value, onChange }: Props) {
  const [pending, start] = useTransition();
  const [err, setErr] = useState<string | null>(null);

  function onFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setErr(null);
    const fd = new FormData();
    fd.append('file', file);
    start(async () => {
      const r = await uploadMediaAction(fd);
      if (r.ok) onChange(r.data.url);
      else if (r.status === 503) setErr('Armazenamento de fotos não configurado — cole uma URL.');
      else if (r.status === 415) setErr('Tipo de ficheiro não suportado (use JPEG/PNG/WebP).');
      else if (r.status === 413) setErr('Ficheiro demasiado grande (máx. 5 MB).');
      else setErr(`Falha no upload (${r.status}).`);
    });
  }

  return (
    <div className="space-y-2">
      <Label>Foto (opcional)</Label>
      <div className="flex items-center gap-3">
        {value && (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={value} alt="Pré-visualização da cor" className="size-12 rounded border object-cover" />
        )}
        <input type="file" accept="image/png,image/jpeg,image/webp" onChange={onFile} disabled={pending} aria-label="Carregar foto" />
        {pending && <span className="text-xs text-muted-foreground">A carregar…</span>}
      </div>
      <Input placeholder="ou cole uma URL de imagem" value={value} onChange={(e) => onChange(e.target.value)} aria-label="URL da foto" />
      {err && <p className="text-xs text-destructive">{err}</p>}
    </div>
  );
}
