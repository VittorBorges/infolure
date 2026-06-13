import Link from 'next/link';
import type { LureCard as LureCardModel } from '../../lib/catalog';
import { FavoriteButton } from './FavoriteButton';

// Card de isca (US-01). Mostra imagem, marca, tipo, peso, espécies, preço médio
// e a contagem GLOBAL de favoritos (C1) + botão de favorito (US-05).
export function LureCard({ lure }: { lure: LureCardModel }) {
  return (
    <div style={{ position: 'relative', border: '1px solid #eaeaea', borderRadius: 8 }}>
      <div style={{ position: 'absolute', top: 8, right: 8, zIndex: 1 }}>
        <FavoriteButton
          lureId={lure.id}
          initialFavorited={lure.is_favorited ?? false}
          initialCount={lure.favorites_count}
        />
      </div>

      <Link
        href={`/iscas/${lure.slug}`}
        style={{ display: 'block', padding: '0.75rem', textDecoration: 'none', color: 'inherit' }}
      >
        {lure.primary_image_url ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={lure.primary_image_url} alt={lure.name} style={{ width: '100%', height: 140, objectFit: 'cover', borderRadius: 4 }} />
        ) : (
          <div style={{ width: '100%', height: 140, background: '#f4f4f4', borderRadius: 4 }} aria-hidden />
        )}
        <h3 style={{ margin: '0.5rem 0 0.25rem', fontSize: '1rem' }}>{lure.name}</h3>
        <p style={{ margin: 0, fontSize: '0.85rem', color: '#666' }}>
          {lure.brand} · {lure.lure_type}
          {lure.weight_g != null && ` · ${lure.weight_g} g`}
        </p>
        <div style={{ marginTop: '0.5rem', fontSize: '0.85rem' }}>
          {lure.price_avg_eur != null ? `${lure.price_avg_eur.toFixed(2)} €` : '—'}
        </div>
      </Link>
    </div>
  );
}
