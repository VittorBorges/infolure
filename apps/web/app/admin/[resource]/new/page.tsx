import Link from 'next/link';
import { notFound } from 'next/navigation';

import { ADMIN_RESOURCES, type AdminResource } from '../../../../lib/admin';
import { LureForm } from '../../../../components/admin/LureForm';
import { BrandForm } from '../../../../components/admin/BrandForm';
import { SpeciesForm } from '../../../../components/admin/SpeciesForm';
import { Button, Card, CardContent } from '@infolure/design-system';

const NEW_LABEL: Record<string, string> = { lures: 'isca', brands: 'marca', species: 'espécie' };

export const dynamic = 'force-dynamic';

function isResource(r: string): r is AdminResource {
  return (ADMIN_RESOURCES as readonly string[]).includes(r);
}

export default async function AdminNewPage({ params }: { params: Promise<{ resource: string }> }) {
  const { resource } = await params;
  if (!isResource(resource)) notFound();

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold capitalize tracking-tight">Nova {NEW_LABEL[resource] ?? resource}</h1>
        <Button variant="outline" size="sm" asChild>
          <Link href={`/admin/${resource}`}>← Voltar</Link>
        </Button>
      </div>

      {resource === 'lures' ? (
        <LureForm mode="create" />
      ) : resource === 'brands' ? (
        <BrandForm mode="create" />
      ) : resource === 'species' ? (
        <SpeciesForm mode="create" />
      ) : (
        <Card className="max-w-xl">
          <CardContent className="pt-6 text-sm text-muted-foreground">
            O registo detalhado está disponível para iscas, marcas e espécies.
          </CardContent>
        </Card>
      )}
    </div>
  );
}
