// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

// https://astro.build/config
export default defineConfig({
  integrations: [
    starlight({
      title: 'Winged Bean',
      description: 'Multi-tier plugin architecture for Unity/Godot games',

      sidebar: [
        {
          label: 'Getting Started',
          items: [
            { label: 'Introduction', link: '/docs/' },
          ],
        },
        {
          label: 'RFCs',
          autogenerate: { directory: 'rfcs' },
          collapsed: false,
        },
        {
          label: 'Guides',
          autogenerate: { directory: 'guides' },
        },
        {
          label: 'Architecture',
          items: [
            {
              label: 'ADRs',
              autogenerate: { directory: 'adr' },
            },
            {
              label: 'Design',
              autogenerate: { directory: 'design' },
            },
          ],
        },
        {
          label: 'Verification',
          autogenerate: { directory: 'verification' },
        },
        {
          label: 'Demo',
          items: [
            {
              label: '🎮 Terminal.Gui Live Demo',
              link: '/demo/',
            },
          ],
        },
      ],

      // Disabled autogenerate due to missing frontmatter in markdown files
      // Will need to add frontmatter to all docs or manually list them
      // TODO: Add proper Starlight frontmatter to all markdown files
    }),
  ],
});
