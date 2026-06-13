import type { paths } from './api-types';

// Cliente de API fino e tipado a partir do contrato OpenAPI (Princípio III).
// Os tipos são gerados de specs/001-lure-catalog-mvp/contracts/api.yaml via `npm run gen:api-types`.

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';

export type Paths = paths;

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly correlationId?: string,
    message?: string,
  ) {
    super(message ?? `API error ${status}`);
  }
}

/** GET tipado simples. Lança ApiError em respostas não-2xx. */
export async function apiGet<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: { Accept: 'application/json', ...init?.headers },
  });

  if (!res.ok) {
    throw new ApiError(res.status, res.headers.get('X-Correlation-Id') ?? undefined);
  }
  return (await res.json()) as T;
}
