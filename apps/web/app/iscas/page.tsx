import Link from 'next/link';
import { fetchLures } from '../../lib/catalog';
import { LureCard } from '../../components/catalog/LureCard';
import { FilterPanel } from '../../components/catalog/FilterPanel';
import { SortControls } from '../../components/catalog/SortControls';
import { NoResults } from '../../components/catalog/NoResults';
import { SearchBar } from '../../components/search/SearchBar';
import { ErrorState } from '../../components/States';

export const dynamic = 'force-dynamic';

export const metadata = {
  title: 'Catálogo de Iscas — Infolure',
  description: 'Descubra iscas de pesca por tipo, espécie, peso e marca.',
};

type SearchParams = Record<string, string | string[] | undefined>;

function toQueryString(sp: SearchParams): string {
  const params = new URLSearchParams();
  for (const [k, v] of Object.entries(sp)) {
    if (typeof v === 'string' && v) params.set(k, v);
  }
  if (!params.has('per_page')) params.set('per_page', '20');
  return params.toString();
}

export default async function CatalogPage({
  searchParams,
}: {
  searchParams: Promise<SearchParams>;
}) {
  const sp = await searchParams;
  const query = toQueryString(sp);

  let result;
  try {
    result = await fetchLures(query);
  } catch {
    return <ErrorState title="Não foi possível carregar o catálogo." />;
  }

  const { data, meta } = result;
  const page = meta.page;
  const totalPages = Math.max(1, Math.ceil(meta.total / meta.per_page));

  function pageHref(p: number) {
    const params = new URLSearchParams(query);
    params.set('page', String(p));
    return `/iscas?${params.toString()}`;
  }

  return (
    <div style={{ padding: '1.5rem', display: 'grid', gridTemplateColumns: '220px 1fr', gap: '1.5rem' }}>
      <FilterPanel facets={meta.facets} />

      <section>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', gap: '1rem', flexWrap: 'wrap' }}>
          <SearchBar />
          <SortControls />
        </div>

        {data.length === 0 ? (
          <NoResults query={typeof sp.q === 'string' ? sp.q : undefined} />
        ) : (
          <>
            <p style={{ color: '#666', fontSize: '0.85rem' }}>{meta.total} iscas</p>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: '1rem' }}>
              {data.map((lure) => (
                <LureCard key={lure.id} lure={lure} />
              ))}
            </div>

            <nav aria-label="Paginação" style={{ display: 'flex', gap: '1rem', justifyContent: 'center', marginTop: '1.5rem' }}>
              {page > 1 && <Link href={pageHref(page - 1)}>← Anterior</Link>}
              <span style={{ color: '#666' }}>Página {page} de {totalPages}</span>
              {page < totalPages && <Link href={pageHref(page + 1)}>Seguinte →</Link>}
            </nav>
          </>
        )}
      </section>
    </div>
  );
}
