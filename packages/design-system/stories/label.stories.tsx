import type { Meta, StoryObj } from '@storybook/react-vite';
import { Label, Input } from '../src';

const meta: Meta<typeof Label> = {
  title: 'Componentes/Label',
  component: Label,
  parameters: { layout: 'centered' },
};
export default meta;
type Story = StoryObj<typeof Label>;

export const ComCampo: Story = {
  render: () => (
    <div className="grid w-64 gap-2">
      <Label htmlFor="peso">Peso (g)</Label>
      <Input id="peso" type="number" placeholder="12.5" />
    </div>
  ),
};
