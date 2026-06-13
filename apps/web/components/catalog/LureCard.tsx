import Link from 'next/link';
import type { LureCard as LureCardModel } from '../../lib/catalog';

// Card de isca (US-01). Mostra imagem, marca, tipo, peso, espécies, preço médio
// e a contagem GLOBAL de favoritos (C1). O botão de favorito chega na US-05.
export function LureCard({ lure }: { lure: LureCardModel }) {
  return (
    <Link
      href={`/iscas/${lure.slug}`}
      style={{ display: 'block', border: '1px solid #eaeaea', borderRadius: 8, padding: '0.75rem', textDecoration: 'none', color: 'inherit' }}
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
      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '0.5rem', fontSize: '0.85rem' }}>
        <span>{lure.price_avg_eur != null ? `${lure.price_avg_eur.toFixed(2)} €` : '—'}</span>
        <span title="Favoritos (global)" aria-label={`${lure.favorites_count} favoritos`}>
          ♥ {lure.favorites_count}
        </span>
      </div>
    </Link>
  );
}
