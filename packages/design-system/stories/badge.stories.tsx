import type { Meta, StoryObj } from '@storybook/react-vite';
import { Badge } from '../src';

const meta: Meta<typeof Badge> = {
  title: 'Componentes/Badge',
  component: Badge,
  parameters: { layout: 'centered' },
};
export default meta;
type Story = StoryObj<typeof Badge>;

export const Variantes: Story = {
  render: () => (
    <div className="flex flex-wrap items-center gap-2">
      <Badge>Default</Badge>
      <Badge variant="secondary">Secondary</Badge>
      <Badge variant="success">Ativo</Badge>
      <Badge variant="destructive">Eliminado</Badge>
      <Badge variant="muted">Inativo</Badge>
      <Badge variant="outline">Outline</Badge>
    </div>
  ),
};
