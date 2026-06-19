import { adminFetch } from '../../../lib/admin';
import { GlobalIndexingToggle } from '../../../components/admin/GlobalIndexingToggle';
import { Card, CardContent } from '@infolure/design-system';

export const dynamic = 'force-dynamic';

// Feature 006 (US1) — definições do backoffice: indexação SEO global.
export default async function AdminSettingsPage() {
  const r = await adminFetch<{ enabled: boolean }>(`/v1/admin/settings/indexing`);

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">Definições</h1>
        <p className="text-sm text-muted-foreground">Configurações globais do catálogo.</p>
      </header>

      {r.ok ? (
        <GlobalIndexingToggle initialEnabled={r.data.enabled} />
      ) : (
        <Card className="max-w-xl">
          <CardContent className="pt-6 text-sm text-muted-foreground">
            Não foi possível carregar as definições (sessão admin necessária).
          </CardContent>
        </Card>
      )}
    </div>
  );
}
