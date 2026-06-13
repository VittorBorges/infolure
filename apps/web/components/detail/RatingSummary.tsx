// US-08 (T072) — agregado de avaliações: média + distribuição por estrelas.
export interface Aggregate {
  avg_rating?: number | null;
  total_reviews: number;
  distribution?: Record<string, number>;
}

export function RatingSummary({ aggregate }: { aggregate: Aggregate }) {
  const { avg_rating, total_reviews, distribution } = aggregate;
  if (total_reviews === 0) {
    return <p style={{ color: '#666' }}>Ainda sem avaliações.</p>;
  }

  return (
    <div style={{ marginBottom: '1rem' }}>
      <p style={{ fontSize: '1.4rem', margin: 0 }}>
        ★ {avg_rating?.toFixed(1)} <span style={{ fontSize: '0.9rem', color: '#666' }}>({total_reviews})</span>
      </p>
      {distribution && (
        <ul style={{ listStyle: 'none', padding: 0, margin: '0.5rem 0', maxWidth: 240 }}>
          {[5, 4, 3, 2, 1].map((star) => {
            const count = distribution[String(star)] ?? 0;
            const pct = total_reviews > 0 ? Math.round((count / total_reviews) * 100) : 0;
            return (
              <li key={star} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.8rem' }}>
                <span style={{ width: 28 }}>{star}★</span>
                <span style={{ flex: 1, background: '#f0f0f0', height: 8, borderRadius: 4 }}>
                  <span style={{ display: 'block', width: `${pct}%`, height: 8, background: '#f5a623', borderRadius: 4 }} />
                </span>
                <span style={{ width: 24, textAlign: 'right' }}>{count}</span>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
