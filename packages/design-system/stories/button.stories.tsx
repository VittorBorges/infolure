import type { Meta, StoryObj } from '@storybook/react-vite';
import { Button } from '../src';

const meta: Meta<typeof Button> = {
  title: 'Componentes/Button',
  component: Button,
  parameters: { layout: 'centered' },
};
export default meta;
type Story = StoryObj<typeof Button>;

export const Variantes: Story = {
  render: () => (
    <div className="flex flex-wrap items-center gap-3">
      <Button>Default</Button>
      <Button variant="success">Success</Button>
      <Button variant="destructive">Destructive</Button>
      <Button variant="outline">Outline</Button>
      <Button variant="secondary">Secondary</Button>
      <Button variant="ghost">Ghost</Button>
      <Button variant="link">Link</Button>
    </div>
  ),
};

export const Tamanhos: Story = {
  render: () => (
    <div className="flex flex-wrap items-center gap-3">
      <Button size="sm">Small</Button>
      <Button size="default">Default</Button>
      <Button size="lg">Large</Button>
    </div>
  ),
};

export const Desativado: Story = { args: { children: 'Desativado', disabled: true } };
