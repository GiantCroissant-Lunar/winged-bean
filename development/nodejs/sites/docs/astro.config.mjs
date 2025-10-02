// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

// https://astro.build/config
export default defineConfig({
  integrations: [
    starlight({
      title: 'Winged Bean',
      description: 'Multi-tier plugin architecture for Unity/Godot games',

      // Starlight v0.33+ requires social to be an array of link items
      social: [
        { icon: 'github', label: 'GitHub', href: 'https://github.com/GiantCroissant-Lunar/winged-bean' },
      ],

      sidebar: [
        {
          label: '🚀 Getting Started',
          items: [
            { label: 'Introduction', link: '/' },
          ],
        },
        {
          label: '📄 RFCs',
          collapsed: false,
          badge: 'Reference',
          items: [
            { label: 'RFC-0001: Asciinema Recording', link: '/rfcs/0001-asciinema-recording-for-pty-sessions/' },
            { label: 'RFC-0002: Service Platform Core', link: '/rfcs/0002-service-platform-core-4-tier-architecture/' },
            { label: 'RFC-0003: Plugin Architecture', link: '/rfcs/0003-plugin-architecture-foundation/' },
            { label: 'RFC-0004: Project Organization', link: '/rfcs/0004-project-organization-and-folder-structure/' },
            { label: 'RFC-0005: Target Framework Compliance', link: '/rfcs/0005-target-framework-compliance/' },
            { label: 'RFC-0006: Dynamic Plugin Loading', link: '/rfcs/0006-dynamic-plugin-loading/' },
            { label: 'RFC-0007: Arch ECS Integration', link: '/rfcs/0007-arch-ecs-integration/' },
            { label: 'RFC-0008: Playwright Testing', link: '/rfcs/0008-playwright-and-asciinema-testing-strategy/' },
            { label: 'RFC-0009: Dynamic Recording', link: '/rfcs/0009-dynamic-asciinema-recording-in-pty/' },
            { label: 'RFC-0010: Build Orchestration', link: '/rfcs/0010-multi-language-build-orchestration-with-task/' },
            { label: 'RFC-0011: Starlight Integration', link: '/rfcs/0011-starlight-documentation-integration/' },
          ],
        },
        {
          label: '🧭 Guides',
          badge: 'How-To',
          items: [
            { label: 'Architecture Overview', link: '/guides/architecture-overview/' },
            { label: 'Framework Targeting', link: '/guides/framework-targeting-guide/' },
            { label: 'Playwright Quickstart', link: '/guides/playwright_asciinema_quickstart/' },
            { label: 'Source Generator Usage', link: '/guides/source-generator-usage/' },
          ],
        },
        {
          label: '🏗️ Architecture',
          badge: 'Design',
          items: [
            {
              label: 'ADRs',
              items: [
                { label: 'ADR-0001: Astro + Asciinema', link: '/adr/0001-use-astro-with-asciinema-player/' },
                { label: 'ADR-0002: Native Pre-commit', link: '/adr/0002-use-native-tools-for-pre-commit-hooks/' },
                { label: 'ADR-0003: Security Hooks', link: '/adr/0003-implement-security-and-quality-pre-commit-hooks/' },
                { label: 'ADR-0004: Act for GitHub Actions', link: '/adr/0004-adopt-act-for-local-github-actions-testing/' },
                { label: 'ADR-0005: PTY Integration', link: '/adr/0005-use-pty-for-terminal-gui-web-integration/' },
                { label: 'ADR-0006: PM2 Development', link: '/adr/0006-use-pm2-for-local-development/' },
              ],
            },
            {
              label: 'Design',
              items: [
                { label: 'Console MVP Migration', link: '/design/console-mvp-migration-plan/' },
                { label: 'Dungeon Crawler Roadmap', link: '/design/dungeon-crawler-ecs-roadmap/' },
                { label: 'WingedBean Host Analysis', link: '/design/existing-wingedbean-host-analysis/' },
                { label: 'Tier 1 Core Contracts', link: '/design/tier1-core-contracts/' },
              ],
            },
          ],
        },
        {
          label: '🧪 Verification',
          badge: 'QA',
          items: [
            { label: 'ConsoleDungeon Host', link: '/verification/rfc-0005-phase5-wave5.2-consoledungeon-host-verification/' },
            { label: 'xterm.js Integration', link: '/verification/rfc-0005-phase5-wave5.3-xterm-integration-verification/' },
            { label: 'Terminal.GUI PTY', link: '/verification/terminal_gui_pty_verification_report/' },
          ],
        },
        {
          label: '🎮 Demo',
          badge: 'Live',
          items: [
            { label: '🎮 Terminal.Gui Live Demo', link: '/demo/' },
          ],
        },
      ],
    }),
  ],
});
