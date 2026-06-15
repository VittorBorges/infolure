import { getSupabaseServerClient } from './supabase/server';

// Cliente server-side do backoffice (US-02). Anexa o JWT da sessão Supabase a cada chamada
// /v1/admin/* (a autorização admin é verificada no backend, com a role da BD).

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';

export type AdminResult<T> =
  | { ok: true; data: T }
  | { ok: false; status: number };

async function token(): Promise<string | null> {
  try {
    const supabase = await getSupabaseServerClient();
    const { data } = await supabase.auth.getSession();
    return data.session?.access_token ?? null;
  } catch {
    return null; // Supabase não configurado
  }
}

export async function adminFetch<T>(path: string, init?: RequestInit): Promise<AdminResult<T>> {
  const t = await token();
  if (!t) return { ok: false, status: 401 };

  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: { Accept: 'application/json', Authorization: `Bearer ${t}`, ...init?.headers },
    cache: 'no-store',
  });

  if (!res.ok) return { ok: false, status: res.status };
  if (res.status === 204) return { ok: true, data: undefined as T };
  return { ok: true, data: (await res.json()) as T };
}

export const ADMIN_RESOURCES = ['lures', 'brands', 'species', 'users'] as const;
export type AdminResource = (typeof ADMIN_RESOURCES)[number];

/** Recursos que contêm dados pessoais (exigem aviso RGPD nas operações). */
export const PERSONAL_RESOURCES: ReadonlySet<string> = new Set(['users']);

export interface PagedResponse {
  data: Record<string, unknown>[];
  meta: { total: number; page: number; per_page: number };
}
