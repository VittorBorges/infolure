'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';
import { setActiveAction, softDeleteAction, restoreAction, eraseUserAction } from '../../lib/admin-actions';
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@infolure/design-system';

interface Props {
  resource: string;
  id: string;
  isActive: boolean;
  deleted: boolean;
  personal: boolean;
}

export function RowActions({ resource, id, isActive, deleted, personal }: Props) {
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
      setConfirmDelete(false);
      router.refresh();
    });
  }

  if (deleted) {
    return (
      <div className="flex items-center justify-end gap-2">
        <Button variant="outline" size="sm" disabled={pending} onClick={() => run(() => restoreAction(resource, id))}>
          Restaurar
        </Button>
        {err && <span className="text-xs text-destructive">{err}</span>}
      </div>
    );
  }

  return (
    <div className="flex items-center justify-end gap-2">
      <Button
        variant={isActive ? 'outline' : 'success'}
        size="sm"
        disabled={pending}
        onClick={() => run(() => setActiveAction(resource, id, !isActive))}
      >
        {isActive ? 'Desativar' : 'Ativar'}
      </Button>

      <Button
        variant="destructive"
        size="sm"
        disabled={pending}
        onClick={() => (personal ? setConfirmDelete(true) : run(() => softDeleteAction(resource, id)))}
      >
        Eliminar
      </Button>

      {err && <span className="text-xs text-destructive">{err}</span>}

      {/* Aviso RGPD (FR-012a): distingue soft-delete reversível de eliminação irreversível. */}
      <Dialog open={confirmDelete} onOpenChange={setConfirmDelete}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Eliminar dados pessoais</DialogTitle>
            <DialogDescription>
              Este registo contém dados pessoais. O <strong>soft-delete</strong> é reversível e não cumpre o
              direito ao esquecimento; a <strong>eliminação RGPD</strong> anonimiza a PII e revoga o acesso de
              forma <strong>irreversível</strong>.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="gap-2 sm:gap-2">
            <Button variant="outline" disabled={pending} onClick={() => setConfirmDelete(false)}>
              Cancelar
            </Button>
            <Button variant="secondary" disabled={pending} onClick={() => run(() => softDeleteAction(resource, id))}>
              Soft-delete (reversível)
            </Button>
            {resource === 'users' && (
              <Button variant="destructive" disabled={pending} onClick={() => run(() => eraseUserAction(id))}>
                Eliminar RGPD (irreversível)
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
