'use server';

import { revalidatePath } from 'next/cache';
import { adminFetch, type AdminResult } from './admin';

// Server Actions do backoffice (US-02): mutações de ciclo de vida com o JWT da sessão.
// Auditadas automaticamente no backend (AdminAuditInterceptor).

export async function setActiveAction(
  resource: string, id: string, isActive: boolean,
): Promise<AdminResult<void>> {
  const r = await adminFetch<void>(`/v1/admin/${resource}/${id}/active`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ is_active: isActive }),
  });
  revalidatePath(`/admin/${resource}`);
  return r;
}

export async function softDeleteAction(resource: string, id: string): Promise<AdminResult<void>> {
  const r = await adminFetch<void>(`/v1/admin/${resource}/${id}`, { method: 'DELETE' });
  revalidatePath(`/admin/${resource}`);
  return r;
}

export async function restoreAction(resource: string, id: string): Promise<AdminResult<void>> {
  const r = await adminFetch<void>(`/v1/admin/${resource}/${id}/restore`, { method: 'POST' });
  revalidatePath(`/admin/${resource}`);
  return r;
}

// T036b / FR-012a: eliminação RGPD efetiva e irreversível de um utilizador.
export async function eraseUserAction(id: string): Promise<AdminResult<void>> {
  const r = await adminFetch<void>(`/v1/admin/users/${id}/erase`, { method: 'POST' });
  revalidatePath('/admin/users');
  return r;
}

// US-03 (T046): indexabilidade SEO por isca.
export async function setIndexableAction(id: string, isIndexable: boolean): Promise<AdminResult<void>> {
  const r = await adminFetch<void>(`/v1/admin/lures/${id}/indexable`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ is_indexable: isIndexable }),
  });
  revalidatePath('/admin/lures');
  return r;
}
