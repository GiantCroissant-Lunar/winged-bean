---
title: ADR-0001: Use Astro.js with asciinema player
---

# ADR-0001: Use Astro.js with asciinema player

## Status

Accepted

## Context

We need to choose a web framework and terminal recording solution for our project. The requirements include:

- Static site generation capabilities for optimal performance
- Support for embedding terminal recordings/demos
- Modern development experience with good tooling
- SEO-friendly output
- Fast build times and excellent developer experience

## Decision

We will use Astro.js as our web framework combined with asciinema player for terminal recordings.

## Rationale

### Astro.js Benefits

- **Islands Architecture**: Allows us to ship minimal JavaScript while still supporting interactive components where needed
- **Framework Agnostic**: Can integrate components from React, Vue, Svelte, or vanilla JS as needed
- **Static-First**: Generates static HTML by default, ensuring fast loading times and good SEO
- **Modern Tooling**: Built-in TypeScript support, hot module replacement, and excellent developer experience
- **Performance**: Zero JavaScript by default unless explicitly opted in

### asciinema player Benefits

- **Lightweight**: Small bundle size for embedding terminal recordings
- **High Quality**: Captures actual terminal output, not just video recordings
- **Interactive**: Users can copy text from recordings, pause, and control playback
- **Accessible**: Text-based recordings are more accessible than video
- **Easy Integration**: Simple to embed in web pages with minimal configuration

## Consequences

### Positive

- Fast, SEO-friendly static site generation
- Minimal JavaScript bundle sizes
- High-quality, interactive terminal demonstrations
- Excellent developer experience
- Future-proof architecture that can evolve with project needs

### Negative

- Learning curve for team members unfamiliar with Astro
- asciinema requires users to install recording tools for content creation
- Static-first approach may require additional consideration for dynamic features

## Implementation Notes

- Use Astro's component system to create reusable asciinema player components
- Leverage Astro's content collections for organizing terminal recordings
- Consider using Astro's partial hydration for any interactive UI components
- Set up automated build and deployment pipeline compatible with static site hosting

## Date

2025-09-29
