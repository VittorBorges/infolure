import type { Meta, StoryObj } from '@storybook/react-vite';
import { Input } from '../src';

const meta: Meta<typeof Input> = {
  title: 'Componentes/Input',
  component: Input,
  parameters: { layout: 'centered' },
};
export default meta;
type Story = StoryObj<typeof Input>;

export const Default: Story = { args: { placeholder: 'Pesquisar…' }, render: (a) => <Input {...a} className="w-64" /> };
export const Preenchido: Story = { args: { defaultValue: 'Crankbait', className: 'w-64' } };
export const Desativado: Story = { args: { placeholder: 'Desativado', disabled: true, className: 'w-64' } };
