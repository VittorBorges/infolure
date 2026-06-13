'use client';

import { useState } from 'react';
import { getSupabaseBrowserClient } from '../../lib/supabase/client';

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';

type Color = { id: string; name: string };

// US-06 (T064) — botão + modal "Adicionar ao inventário" (quantidade, condição, cor, notas).
export function AddToInventory({ lureId, colors = [] }: { lureId: string; colors?: Color[] }) {
  const [open, setOpen] = useState(false);
  const [quantity, setQuantity] = useState(1);
  const [condition, setCondition] = useState('good');
  const [colorId, setColorId] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSaving(true);
    try {
      const supabase = getSupabaseBrowserClient();
      const { data } = await supabase.auth.getSession();
      const token = data.session?.access_token;
      if (!token) {
        const returnUrl = typeof window !== 'undefined' ? window.location.pathname : '/';
        window.location.href = `/login?returnUrl=${encodeURIComponent(returnUrl)}`;
        return;
      }
      const res = await fetch(`${API_BASE}/v1/me/inventory`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({
          lure_id: lureId,
          color_id: colorId || undefined,
          quantity,
          condition,
          notes: notes || undefined,
        }),
      });
      if (res.status === 409) setError('Já tem esta isca (com esta cor) no inventário.');
      else if (res.status === 422) setError('Dados inválidos.');
      else if (!res.ok) setError('Não foi possível adicionar.');
      else {
        setOpen(false);
        setNotes('');
      }
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setSaving(false);
    }
  }

  if (!open) {
    return <button type="button" onClick={() => setOpen(true)}>Adicionar ao inventário</button>;
  }

  return (
    <div role="dialog" aria-label="Adicionar ao inventário" style={{ border: '1px solid #eaeaea', borderRadius: 8, padding: '1rem', marginTop: '1rem', maxWidth: 360 }}>
      <form onSubmit={submit} style={{ display: 'grid', gap: '0.5rem' }}>
        <label>
          Quantidade
          <input type="number" min={1} max={99} value={quantity} onChange={(e) => setQuantity(Number(e.target.value))} />
        </label>
        <label>
          Condição
          <select value={condition} onChange={(e) => setCondition(e.target.value)}>
            <option value="new">Nova</option>
            <option value="good">Boa</option>
            <option value="used">Usada</option>
          </select>
        </label>
        {colors.length > 0 && (
          <label>
            Cor
            <select value={colorId} onChange={(e) => setColorId(e.target.value)}>
              <option value="">(qualquer)</option>
              {colors.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </label>
        )}
        <label>
          Notas
          <input type="text" maxLength={200} value={notes} onChange={(e) => setNotes(e.target.value)} />
        </label>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button type="submit" disabled={saving}>{saving ? 'A guardar…' : 'Guardar'}</button>
          <button type="button" onClick={() => setOpen(false)}>Cancelar</button>
        </div>
      </form>
      {error && <p role="alert" style={{ color: '#b00' }}>{error}</p>}
    </div>
  );
}
