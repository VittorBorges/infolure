import type { Meta, StoryObj } from '@storybook/react-vite';
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell, Badge } from '../src';

const meta: Meta<typeof Table> = { title: 'Componentes/Table', component: Table };
export default meta;
type Story = StoryObj<typeof Table>;

export const Listagem: Story = {
  render: () => (
    <div className="w-[36rem] rounded-xl border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>nome</TableHead>
            <TableHead>estado</TableHead>
            <TableHead>ativo</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          <TableRow>
            <TableCell>Rapala X-Rap</TableCell>
            <TableCell><Badge variant="success">published</Badge></TableCell>
            <TableCell><Badge variant="success">ativo</Badge></TableCell>
          </TableRow>
          <TableRow>
            <TableCell>Shimano World Minnow</TableCell>
            <TableCell><Badge variant="secondary">draft</Badge></TableCell>
            <TableCell><Badge variant="muted">inativo</Badge></TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>
  ),
};
