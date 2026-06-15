'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';
import { setActiveAction, softDeleteAction, restoreAction, setIndexableAction, eraseUserAction } from '../../lib/admin-actions';

interface Props {
  resource: string;
  id: string;
  isActive: boolean;
  deleted: boolean;
  personal: boolean;
  /** Indexabilidade SEO (apenas iscas; undefined nos restantes recursos). */
  indexable?: boolean;
}

const btn: React.CSSProperties = {
  fontSize: '0.8rem', padding: '0.2rem 0.5rem', marginRight: '0.35rem',
  border: '1px solid #ccc', borderRadius: 4, background: '#fff', cursor: 'pointer',
};

export function RowActions({ resource, id, isActive, deleted, personal, indexable }: Props) {
  const router = useRouter();
  const [pending, start] = useTransition();
  const [err, setErr] = useState<string | null>(null);
  const [confirmDelete, setConfirmDelete] = useState(false);

  function run(fn: () => Promise<{ ok: boolean; status?: number }>) {
    setErr(null);
    start(async () => {
      const r = await fn();
      if (!r.ok) {
        setErr(r.status === 409 ? 'Operação bloqueada (ex.: último admin / própria conta).' : `Falha (${r.status}).`);
        return;
      }
      router.refresh();
    });
  }

  if (deleted) {
    return (
      <span>
        <button style={btn} disabled={pending} onClick={() => run(() => restoreAction(resource, id))}>Restaurar</button>
        {err && <em style={{ color: '#a00', fontSize: '0.75rem' }}>{err}</em>}
      </span>
    );
  }

  return (
    <span>
      <button style={btn} disabled={pending} onClick={() => run(() => setActiveAction(resource, id, !isActive))}>
        {isActive ? 'Desativar' : 'Ativar'}
      </button>
      <button style={btn} disabled={pending} onClick={() => (personal ? setConfirmDelete(true) : run(() => softDeleteAction(resource, id)))}>
        Eliminar
      </button>
      {indexable !== undefined && (
        <button style={btn} disabled={pending} onClick={() => run(() => setIndexableAction(id, !indexable))}>
          {indexable ? 'Tornar não-indexável' : 'Tornar indexável'}
        </button>
      )}
      {err && <em style={{ color: '#a00', fontSize: '0.75rem' }}>{err}</em>}

      {confirmDelete && (
        <div style={{ marginTop: '0.4rem', padding: '0.5rem', border: '1px solid #e0b400', background: '#fffbe6', borderRadius: 4, fontSize: '0.78rem' }}>
          <strong>Aviso RGPD:</strong> este registo contém dados pessoais. O <em>soft-delete</em> é
          reversível e não cumpre o direito ao esquecimento; a <em>eliminação RGPD</em> anonimiza a
          PII e revoga o acesso de forma irreversível.
          <div style={{ marginTop: '0.4rem' }}>
            <button style={btn} disabled={pending} onClick={() => run(() => softDeleteAction(resource, id))}>Soft-delete (reversível)</button>
            {resource === 'users' && (
              <button style={{ ...btn, borderColor: '#a00', color: '#a00' }} disabled={pending}
                onClick={() => run(() => eraseUserAction(id))}>Eliminar RGPD (irreversível)</button>
            )}
            <button style={btn} onClick={() => setConfirmDelete(false)}>Cancelar</button>
          </div>
        </div>
      )}
    </span>
  );
}
