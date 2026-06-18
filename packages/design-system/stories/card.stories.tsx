import type { Meta, StoryObj } from '@storybook/react-vite';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, Badge } from '../src';

const meta: Meta<typeof Card> = { title: 'Componentes/Card', component: Card };
export default meta;
type Story = StoryObj<typeof Card>;

export const Metrica: Story = {
  render: () => (
    <Card className="w-64">
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">Iscas ativas</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="flex items-center gap-2 text-3xl font-semibold">
          128 <Badge variant="success">ativas</Badge>
        </div>
      </CardContent>
    </Card>
  ),
};

export const ComDescricao: Story = {
  render: () => (
    <Card className="w-80">
      <CardHeader>
        <CardTitle>Editar isca</CardTitle>
        <CardDescription>Rapala X-Rap · rapala-x-rap</CardDescription>
      </CardHeader>
      <CardContent className="text-sm text-muted-foreground">Conteúdo do cartão.</CardContent>
    </Card>
  ),
};
