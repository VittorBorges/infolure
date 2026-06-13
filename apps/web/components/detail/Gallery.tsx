import type { LureDetail } from '../../lib/catalog';

// Galeria de imagens + swatches de cores (US-03).
export function Gallery({ lure }: { lure: LureDetail }) {
  const primary = lure.images.find((i) => i.is_primary) ?? lure.images[0];
  return (
    <div>
      {primary ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={primary.url} alt={lure.name} style={{ width: '100%', maxWidth: 480, borderRadius: 8 }} />
      ) : (
        <div style={{ width: '100%', maxWidth: 480, height: 280, background: '#f4f4f4', borderRadius: 8 }} aria-hidden />
      )}

      {lure.images.length > 1 && (
        <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.5rem', flexWrap: 'wrap' }}>
          {lure.images.map((img, i) => (
            // eslint-disable-next-line @next/next/no-img-element
            <img key={i} src={img.url} alt="" style={{ width: 64, height: 64, objectFit: 'cover', borderRadius: 4 }} />
          ))}
        </div>
      )}

      {lure.colors.length > 0 && (
        <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.75rem', alignItems: 'center', flexWrap: 'wrap' }}>
          {lure.colors.map((c) => (
            <span key={c.id} title={c.name} style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontSize: '0.85rem' }}>
              <span aria-hidden style={{ width: 16, height: 16, borderRadius: '50%', background: c.hex_primary ?? '#ccc', border: '1px solid #ddd', display: 'inline-block' }} />
              {c.name}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}
