import './tailwind.css';
import type { Preview } from '@storybook/react-vite';

const preview: Preview = {
  parameters: {
    controls: { matchers: { color: /(background|color)$/i, date: /Date$/i } },
    backgrounds: {
      default: 'app',
      values: [{ name: 'app', value: '#ffffff' }],
    },
  },
};

export default preview;
