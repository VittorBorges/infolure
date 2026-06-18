import type { Meta, StoryObj } from '@storybook/react-vite';
import {
  Dialog,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
  Button,
} from '../src';

const meta: Meta<typeof Dialog> = { title: 'Componentes/Dialog', component: Dialog, parameters: { layout: 'centered' } };
export default meta;
type Story = StoryObj<typeof Dialog>;

export const AvisoRGPD: Story = {
  render: () => (
    <Dialog>
      <DialogTrigger asChild>
        <Button variant="destructive">Eliminar</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Eliminar dados pessoais</DialogTitle>
          <DialogDescription>
            O soft-delete é reversível; a eliminação RGPD é irreversível.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter className="gap-2 sm:gap-2">
          <Button variant="outline">Cancelar</Button>
          <Button variant="secondary">Soft-delete</Button>
          <Button variant="destructive">Eliminar RGPD</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  ),
};
