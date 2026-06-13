// Primitivos de estado de UI (Princípio V — estados de loading/empty/error explícitos
// em cada ecrã com I/O). Componentes apresentacionais e reutilizáveis.

export function LoadingState({ label = 'A carregar…' }: { label?: string }) {
  return (
    <div role="status" aria-live="polite" style={{ padding: '2rem', textAlign: 'center' }}>
      <span>{label}</span>
    </div>
  );
}

export function EmptyState({
  title = 'Sem resultados',
  ctaLabel,
  onCta,
}: {
  title?: string;
  ctaLabel?: string;
  onCta?: () => void;
}) {
  return (
    <div style={{ padding: '2rem', textAlign: 'center' }}>
      <p>{title}</p>
      {ctaLabel && onCta && (
        <button type="button" onClick={onCta}>
          {ctaLabel}
        </button>
      )}
    </div>
  );
}

export function ErrorState({
  title = 'Algo correu mal',
  retryLabel,
  onRetry,
}: {
  title?: string;
  retryLabel?: string;
  onRetry?: () => void;
}) {
  return (
    <div role="alert" style={{ padding: '2rem', textAlign: 'center' }}>
      <p>{title}</p>
      {retryLabel && onRetry && (
        <button type="button" onClick={onRetry}>
          {retryLabel}
        </button>
      )}
    </div>
  );
}
