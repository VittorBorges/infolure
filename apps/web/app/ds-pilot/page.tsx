import './pilot.css';
import Link from 'next/link';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, Button, Badge } from '@infolure/design-system';

export const metadata = { title: 'Design System — piloto' };

// US3 — adoção-piloto: uma página pública a consumir o design system partilhado
// (@infolure/design-system), com os tokens e as utilities geradas via @source no pilot.css.
// O resto do frontend público não importa este CSS e mantém-se inalterado (FR-011).
export default function DesignSystemPilotPage() {
  return (
    <div className="bg-background text-foreground min-h-screen px-6 py-10">
      <div className="mx-auto max-w-2xl space-y-6">
        <header className="space-y-1">
          <h1 className="text-2xl font-semibold tracking-tight">Design System — piloto público</h1>
          <p className="text-sm text-muted-foreground">
            Esta página pública consome os componentes do pacote partilhado{' '}
            <code>@infolure/design-system</code>.
          </p>
        </header>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              Componentes partilhados <Badge variant="success">live</Badge>
            </CardTitle>
            <CardDescription>Os mesmos componentes do backoffice, agora no frontend público.</CardDescription>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-3">
            <Button>Ação primária</Button>
            <Button variant="success">Sucesso</Button>
            <Button variant="outline" asChild>
              <Link href="/">Voltar ao início</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
