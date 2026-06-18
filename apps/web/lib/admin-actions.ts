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

// Feature 005 — payload completo de escrita de iscas (POST/PUT /v1/admin/lures).
export interface LureSizeInput { code?: string; label: string; length_mm?: number | null; weight_g: number; sort_order?: number }
export interface LureHexInput { hex: string; label?: string | null; sort_order?: number }
export interface LureColorInput { name_pt?: string; name_en?: string | null; pattern?: string | null; photo_url?: string | null; hex_codes?: LureHexInput[] }
export interface LureWritePayload {
  slug: string;
  name: string;
  description?: string | null;
  brand_id?: string | null;
  lure_type: string;
  water_type?: string | null;
  model_ref?: string | null;
  hook_size?: string | null;
  hook_type?: string | null;
  hook_count?: number | null;
  material?: string | null;
  depth_min_m?: number | null;
  depth_max_m?: number | null;
  status?: string;
  sizes: LureSizeInput[];
  colors?: LureColorInput[];
  target_species?: { species_id: string; confidence?: string | null }[];
}

// US1 — criar isca completa.
export async function createLureAction(body: LureWritePayload): Promise<AdminResult<{ id: string }>> {
  const r = await adminFetch<{ id: string }>(`/v1/admin/lures`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  revalidatePath('/admin/lures');
  return r;
}

// US2 — editar isca completa (PUT; replace-children). status omisso preserva o atual.
export async function updateLureAction(id: string, body: LureWritePayload): Promise<AdminResult<void>> {
  const r = await adminFetch<void>(`/v1/admin/lures/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  revalidatePath('/admin/lures');
  revalidatePath(`/admin/lures/${id}`);
  return r;
}

// US3 — upload de foto de cor (multipart → /v1/admin/media). Devolve a URL pública.
export async function uploadMediaAction(formData: FormData): Promise<AdminResult<{ url: string }>> {
  const file = formData.get('file');
  if (!(file instanceof File) || file.size === 0) return { ok: false, status: 400 };
  const body = new FormData();
  body.append('file', file);
  return adminFetch<{ url: string }>(`/v1/admin/media`, { method: 'POST', body });
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
