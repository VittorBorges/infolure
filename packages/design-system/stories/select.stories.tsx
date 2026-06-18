import type { Meta, StoryObj } from '@storybook/react-vite';
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from '../src';

const meta: Meta<typeof Select> = { title: 'Componentes/Select', component: Select, parameters: { layout: 'centered' } };
export default meta;
type Story = StoryObj<typeof Select>;

export const EstadoEditorial: Story = {
  render: () => (
    <Select defaultValue="published">
      <SelectTrigger className="w-56">
        <SelectValue placeholder="Selecionar estado" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="draft">draft</SelectItem>
        <SelectItem value="published">published</SelectItem>
        <SelectItem value="archived">archived</SelectItem>
      </SelectContent>
    </Select>
  ),
};
