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

// Edição da isca (FR-009): o backend só aceita `status` e `weight_g` (PATCH /v1/admin/lures/{id}).
export async function updateLureAction(
  id: string,
  patch: { status?: string; weight_g?: number },
): Promise<AdminResult<void>> {
  const body: Record<string, unknown> = {};
  if (patch.status) body.status = patch.status;
  if (patch.weight_g !== undefined && !Number.isNaN(patch.weight_g)) body.weight_g = patch.weight_g;

  const r = await adminFetch<void>(`/v1/admin/lures/${id}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  revalidatePath('/admin/lures');
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
