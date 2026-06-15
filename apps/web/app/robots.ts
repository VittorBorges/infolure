import type { MetadataRoute } from 'next';

export const dynamic = 'force-dynamic';

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';
const SITE = process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000';

// US-03 (T042): permite/proíbe indexação conforme o interruptor global (/v1/seo).
export default async function robots(): Promise<MetadataRoute.Robots> {
  let enabled = false;
  try {
    const res = await fetch(`${API_BASE}/v1/seo`, { cache: 'no-store' });
    if (res.ok) enabled = ((await res.json()) as { indexing_enabled: boolean }).indexing_enabled === true;
  } catch {
    enabled = false;
  }

  if (!enabled) {
    return { rules: [{ userAgent: '*', disallow: '/' }] };
  }
  return {
    rules: [{ userAgent: '*', allow: '/', disallow: ['/admin', '/conta'] }],
    sitemap: `${SITE}/sitemap.xml`,
  };
}
