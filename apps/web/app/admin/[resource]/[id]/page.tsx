import Link from 'next/link';
import { notFound } from 'next/navigation';

import { ADMIN_RESOURCES, type AdminResource } from '../../../../lib/admin';
import { LureEditForm } from '../../../../components/admin/LureEditForm';
import { Button } from '../../../../components/ui/button';
import { Card, CardContent } from '../../../../components/ui/card';

export const dynamic = 'force-dynamic';

type SP = { status?: string; name?: string; slug?: string };

function isResource(r: string): r is AdminResource {
  return (ADMIN_RESOURCES as readonly string[]).includes(r);
}

export default async function AdminEditPage({
  params,
  searchParams,
}: {
  params: Promise<{ resource: string; id: string }>;
  searchParams: Promise<SP>;
}) {
  const { resource, id } = await params;
  if (!isResource(resource)) notFound();
  const sp = await searchParams;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold capitalize tracking-tight">Editar {resource}</h1>
        <Button variant="outline" size="sm" asChild>
          <Link href={`/admin/${resource}`}>← Voltar</Link>
        </Button>
      </div>

      {resource === 'lures' ? (
        <LureEditForm id={id} name={sp.name} slug={sp.slug} initialStatus={sp.status} />
      ) : (
        <Card className="max-w-xl">
          <CardContent className="pt-6 text-sm text-muted-foreground">
            A edição detalhada está disponível apenas para iscas. Para os outros recursos, use as ações de
            ativar/desativar, eliminar e restaurar na listagem.
          </CardContent>
        </Card>
      )}
    </div>
  );
}
