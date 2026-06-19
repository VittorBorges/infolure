import Link from 'next/link';
import { notFound } from 'next/navigation';

import { ADMIN_RESOURCES, adminFetch, type AdminResource } from '../../../../lib/admin';
import { LureForm, type LureInitial } from '../../../../components/admin/LureForm';
import { BrandForm, type BrandInitial } from '../../../../components/admin/BrandForm';
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

  let lure: LureInitial | null = null;
  let brand: BrandInitial | null = null;
  if (resource === 'lures') {
    const r = await adminFetch<LureInitial>(`/v1/admin/lures/${id}`);
    if (!r.ok) { if (r.status === 404) notFound(); } else { lure = r.data; }
  } else if (resource === 'brands') {
    const r = await adminFetch<BrandInitial>(`/v1/admin/brands/${id}`);
    if (!r.ok) { if (r.status === 404) notFound(); } else { brand = r.data; }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold capitalize tracking-tight">
          Editar {resource === 'lures' ? 'isca' : resource === 'brands' ? 'marca' : resource}
        </h1>
        <Button variant="outline" size="sm" asChild>
          <Link href={`/admin/${resource}`}>← Voltar</Link>
        </Button>
      </div>

      {resource === 'lures' && lure ? (
        <LureForm mode="edit" initial={lure} />
      ) : resource === 'brands' && brand ? (
        <BrandForm mode="edit" initial={brand} />
      ) : (
        <Card className="max-w-xl">
          <CardContent className="pt-6 text-sm text-muted-foreground">
            {resource === 'lures' || resource === 'brands'
              ? 'Não foi possível carregar o registo (sessão admin necessária).'
              : 'A edição detalhada está disponível para iscas e marcas. Para os outros recursos, use as ações na listagem.'}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
