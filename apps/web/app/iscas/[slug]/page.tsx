import type { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { fetchLureDetail, type LureDetail } from '../../../lib/catalog';
import { ApiError } from '../../../lib/api';
import { Gallery } from '../../../components/detail/Gallery';
import { PricingSection } from '../../../components/detail/PricingSection';
import { FavoriteButton } from '../../../components/catalog/FavoriteButton';
import { AddToInventory } from '../../../components/inventory/AddToInventoryModal';
import { Reviews } from '../../../components/detail/Reviews';

export const dynamic = 'force-dynamic';

async function load(slug: string): Promise<LureDetail | null> {
  try {
    return await fetchLureDetail(slug);
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) return null;
    throw e;
  }
}

// US-03 (T044): indexável se o interruptor global estiver ligado E a isca constar do sitemap
// (published+active+indexable+marca ativa). Caso contrário → noindex.
const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5191';
async function isIndexable(slug: string): Promise<boolean> {
  try {
    const res = await fetch(`${API_BASE}/v1/seo`, { cache: 'no-store' });
    if (!res.ok) return false;
    const seo = (await res.json()) as { indexing_enabled: boolean; sitemap: { slug: string }[] };
    return seo.indexing_enabled && seo.sitemap.some((s) => s.slug === slug);
  } catch {
    return false;
  }
}

// SEO (US-03): título, descrição, Open Graph e canonical por slug.
export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const lure = await load(slug);
  if (!lure) return { title: 'Isca não encontrada — Infolure' };

  const title = `${lure.name}${lure.brand ? ` · ${lure.brand}` : ''} — Infolure`;
  const description = lure.description ?? `Ficha técnica da isca ${lure.name}.`;
  const indexable = await isIndexable(lure.slug);
  return {
    title,
    description,
    robots: { index: indexable, follow: indexable },
    alternates: { canonical: `/iscas/${lure.slug}` },
    openGraph: {
      title,
      description,
      type: 'website',
      images: lure.primary_image_url ? [{ url: lure.primary_image_url }] : undefined,
    },
  };
}

export default async function LureDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const lure = await load(slug);
  if (!lure) notFound();

  // Dados estruturados Product (schema.org) para indexação rica.
  const jsonLd = {
    '@context': 'https://schema.org',
    '@type': 'Product',
    name: lure.name,
    brand: lure.brand,
    category: lure.lure_type,
    ...(lure.pricing?.avg_eur != null && {
      offers: { '@type': 'AggregateOffer', priceCurrency: 'EUR', lowPrice: lure.pricing.min_eur, highPrice: lure.pricing.max_eur },
    }),
    ...(lure.avg_rating != null && lure.reviews_count > 0 && {
      aggregateRating: { '@type': 'AggregateRating', ratingValue: lure.avg_rating, reviewCount: lure.reviews_count },
    }),
  };

  return (
    <article style={{ padding: '1.5rem', maxWidth: 900, margin: '0 auto' }}>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />

      {/* Breadcrumb: Home > [tipo] > [marca] > [isca] */}
      <nav aria-label="Breadcrumb" style={{ fontSize: '0.85rem', color: '#666', marginBottom: '1rem' }}>
        <Link href="/iscas">Catálogo</Link> › <span>{lure.lure_type}</span>
        {lure.brand && <> › <span>{lure.brand}</span></>} › <span>{lure.name}</span>
      </nav>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '2rem' }}>
        <Gallery lure={lure} />

        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', gap: '1rem' }}>
            <h1 style={{ marginTop: 0 }}>{lure.name}</h1>
            <FavoriteButton lureId={lure.id} initialFavorited={lure.is_favorited ?? false} initialCount={lure.favorites_count} />
          </div>
          <p style={{ color: '#666' }}>{lure.brand} · {lure.lure_type}</p>
          {lure.description && <p>{lure.description}</p>}

          <dl style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '0.25rem 1rem', fontSize: '0.9rem' }}>
            {lure.weight_g != null && (<><dt>Peso</dt><dd>{lure.weight_g} g</dd></>)}
            {lure.length_mm != null && (<><dt>Comprimento</dt><dd>{lure.length_mm} mm</dd></>)}
            {(lure.depth_min_m != null || lure.depth_max_m != null) && (
              <><dt>Profundidade</dt><dd>{lure.depth_min_m ?? '?'}–{lure.depth_max_m ?? '?'} m</dd></>
            )}
            {lure.hook_size && (<><dt>Anzol</dt><dd>{lure.hook_size} ({lure.hook_count ?? '?'}×)</dd></>)}
            {lure.material && (<><dt>Material</dt><dd>{lure.material}</dd></>)}
            {lure.water_type && (<><dt>Água</dt><dd>{lure.water_type}</dd></>)}
          </dl>

          {lure.target_species_detail.length > 0 && (
            <p style={{ fontSize: '0.9rem' }}>
              <strong>Espécies-alvo:</strong>{' '}
              {lure.target_species_detail.map((s) => s.common_name).join(', ')}
            </p>
          )}

          <AddToInventory
            lureId={lure.id}
            colors={lure.colors.map((c) => ({ id: c.id, name: c.name }))}
          />
        </div>
      </div>

      <PricingSection pricing={lure.pricing} />

      <Reviews slug={lure.slug} />
    </article>
  );
}
