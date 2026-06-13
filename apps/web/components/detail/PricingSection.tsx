import type { LureDetail } from '../../lib/catalog';

// Secção de preços (US-03): média 6m, min/max e até 3 retalhistas.
// Oculta-se por completo se não houver dados de preço (regra da spec).
export function PricingSection({ pricing }: { pricing: LureDetail['pricing'] }) {
  if (!pricing) return null;

  return (
    <section aria-label="Preços" style={{ marginTop: '1.5rem' }}>
      <h2 style={{ fontSize: '1.1rem' }}>Preços</h2>
      <p>
        Média (6 meses): <strong>{pricing.avg_eur != null ? `${pricing.avg_eur.toFixed(2)} €` : '—'}</strong>
        {pricing.min_eur != null && pricing.max_eur != null && (
          <span style={{ color: '#666' }}> ({pricing.min_eur.toFixed(2)} € – {pricing.max_eur.toFixed(2)} €)</span>
        )}
      </p>
      {pricing.retailers.length > 0 && (
        <ul style={{ listStyle: 'none', padding: 0 }}>
          {pricing.retailers.map((r, i) => (
            <li key={i} style={{ display: 'flex', justifyContent: 'space-between', maxWidth: 360, padding: '0.25rem 0' }}>
              <span>
                {r.url ? <a href={r.url} rel="nofollow noopener" target="_blank">{r.retailer}</a> : r.retailer}
                {!r.in_stock && <span style={{ color: '#999' }}> (esgotado)</span>}
              </span>
              <span>{r.price_eur.toFixed(2)} €</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
