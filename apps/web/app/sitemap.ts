import type { MetadataRoute } from 'next';

export const dynamic = 'force-dynamic';

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';
const SITE = process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000';

// US-03 (T043): só lista iscas elegíveis quando a indexação global está ligada; senão vazio.
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  try {
    const res = await fetch(`${API_BASE}/v1/seo`, { cache: 'no-store' });
    if (!res.ok) return [];
    const { sitemap: entries } = (await res.json()) as {
      sitemap: { slug: string; updated_at: string }[];
    };
    return (entries ?? []).map((e) => ({
      url: `${SITE}/iscas/${e.slug}`,
      lastModified: e.updated_at,
    }));
  } catch {
    return [];
  }
}
