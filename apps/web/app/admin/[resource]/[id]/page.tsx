import Link from 'next/link';
import { notFound } from 'next/navigation';

import { ADMIN_RESOURCES, adminFetch, type AdminResource } from '../../../../lib/admin';
import { LureForm, type LureInitial } from '../../../../components/admin/LureForm';
import { Button, Card, CardContent } from '@infolure/design-system';

export const dynamic = 'force-dynamic';

function isResource(r: string): r is AdminResource {
  return (ADMIN_RESOURCES as readonly string[]).includes(r);
}

export default async function AdminEditPage({
  params,
}: {
  params: Promise<{ resource: string; id: string }>;
}) {
  const { resource, id } = await params;
  if (!isResource(resource)) notFound();

  let initial: LureInitial | null = null;
  if (resource === 'lures') {
    const r = await adminFetch<LureInitial>(`/v1/admin/lures/${id}`);
    if (!r.ok) {
      if (r.status === 404) notFound();
    } else {
      initial = r.data;
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold capitalize tracking-tight">Editar {resource === 'lures' ? 'isca' : resource}</h1>
        <Button variant="outline" size="sm" asChild>
          <Link href={`/admin/${resource}`}>← Voltar</Link>
        </Button>
      </div>

      {resource === 'lures' && initial ? (
        <LureForm mode="edit" initial={initial} />
      ) : (
        <Card className="max-w-xl">
          <CardContent className="pt-6 text-sm text-muted-foreground">
            {resource === 'lures'
              ? 'Não foi possível carregar a isca (sessão admin necessária).'
              : 'A edição detalhada está disponível apenas para iscas. Para os outros recursos, use as ações na listagem.'}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
