---
id: RFC-0011
title: Starlight Documentation Integration
status: Implemented
category: documentation, infrastructure
created: 2025-10-02
updated: 2025-10-02
---

# RFC-0011: Starlight Documentation Integration

**Status:** ✅ Implemented  
**Date:** 2025-10-02  
**Completed:** 2025-10-02  
**Author:** Development Team  
**Category:** documentation, infrastructure  
**Priority:** MEDIUM (P2)  
**Actual Effort:** 3 hours  

---

## Summary

Integrate Astro Starlight as the documentation theme for the existing Astro site, organizing 54+ markdown files across RFCs, guides, ADRs, and design documents into a searchable, navigable documentation site. Maintain separation between the Terminal.Gui interactive demo and documentation content through proper routing.

---

## Motivation

### Current Problems

1. **Documentation is scattered** - 54 markdown files across multiple directories
2. **No navigation structure** - hard to find related documents
3. **No search** - can't search across documentation
4. **No discoverability** - users don't know what docs exist
5. **Inconsistent presentation** - raw markdown files, no styling
6. **Demo and docs mixed** - Terminal.Gui demo on `/` with no clear separation

### Current Documentation Structure

```
docs/
├── adr/              # Architecture Decision Records (6 files)
├── design/           # Design documents (4 files)
├── development/      # Development guides (4 files)
├── guides/           # User guides (4 files)
├── implementation/   # Implementation reports (1 file)
├── recordings/       # Asciinema recordings (4 .cast files)
├── rfcs/             # RFCs (10 files)
└── verification/     # Verification reports (2 files)
```

### Why Starlight?

**Evaluated alternatives:**
- **Docusaurus**: Requires React, separate tech stack, separate dev server
- **VitePress**: Vue-based, separate tech stack
- **Just**: Simple but no search or navigation
- **Starlight**: Official Astro theme, seamless integration, built for technical docs

**Key advantages:**
- Same tech stack (Astro) - no additional dependencies
- Single dev server - docs and demo together
- Built-in search with Pagefind
- Auto-generated sidebar navigation
- Dark mode, mobile responsive out of box
- Can coexist with custom pages (Terminal.Gui demo)

---

## Proposal

### 1. Route Structure

**Separate demo and documentation with clear routing:**

```
http://localhost:4321/
├── /                    # Landing page (redirect to /demo or /docs)
├── /demo/               # Terminal.Gui interactive demo
│   ├── /demo/pty/       # PTY-based Terminal.Gui
│   └── /demo/legacy/    # Legacy WebSocket demo
├── /docs/               # Documentation (Starlight)
│   ├── /docs/           # Documentation homepage
│   ├── /docs/rfcs/      # RFCs
│   ├── /docs/guides/    # Guides
│   ├── /docs/adr/       # ADRs
│   ├── /docs/design/    # Design documents
│   └── /docs/api/       # API reference (future)
└── /recordings/         # Static asciinema recordings
```

**Key decisions:**
1. **Demo gets its own route** (`/demo/`) - not just `/`
2. **Documentation is primary** (`/docs/`) - Starlight theme
3. **Landing page** (`/`) - can redirect or show overview
4. **Recordings accessible** (`/recordings/`) - for embedding in docs

### 2. Starlight Configuration

**Install Starlight:**
```bash
cd development/nodejs/sites/docs
pnpm astro add starlight
```

**Configure routing and sidebar:**
```javascript
// astro.config.mjs
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

export default defineConfig({
  integrations: [
    starlight({
      title: 'Winged Bean',
      description: 'Multi-tier plugin architecture for Unity/Godot games',
      
      // Social links
      social: {
        github: 'https://github.com/GiantCroissant-Lunar/winged-bean',
      },
      
      // Sidebar navigation
      sidebar: [
        {
          label: 'Getting Started',
          items: [
            { label: 'Introduction', link: '/docs/' },
            { label: 'Quick Start', link: '/docs/quick-start/' },
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
            { label: 'ADRs', autogenerate: { directory: 'adr' } },
            { label: 'Design', autogenerate: { directory: 'design' } },
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
              attrs: { class: 'demo-link' }
            },
          ],
        },
      ],
      
      // Custom CSS
      customCss: [
        './src/styles/custom.css',
      ],
    }),
  ],
});
```

### 3. File Structure

```
development/nodejs/sites/docs/
├── astro.config.mjs           # Starlight integration
├── src/
│   ├── pages/
│   │   ├── index.astro        # Landing page (redirect or overview)
│   │   └── demo/
│   │       ├── index.astro    # Terminal.Gui demo (current content)
│   │       └── legacy.astro   # Legacy WebSocket demo
│   │
│   ├── content/
│   │   └── docs/              # Starlight documentation
│   │       ├── index.mdx      # Docs homepage
│   │       ├── rfcs/          # Copied from ../../../../docs/rfcs/
│   │       ├── guides/        # Copied from ../../../../docs/guides/
│   │       ├── adr/           # Copied from ../../../../docs/adr/
│   │       ├── design/        # Copied from ../../../../docs/design/
│   │       └── verification/  # Copied from ../../../../docs/verification/
│   │
│   ├── components/
│   │   ├── XTerm.astro        # Existing component
│   │   └── AsciinemaPlayer.astro  # Existing component
│   │
│   └── styles/
│       └── custom.css         # Custom Starlight styling
│
└── public/
    ├── recordings/            # Symlink to ../../../../docs/recordings/
    └── node_modules/
```

### 4. Content Migration Strategy

**Option A: Copy Documentation (Recommended)**
```bash
# Copy docs to Astro content directory
cp -r ../../../../docs/rfcs/*.md src/content/docs/rfcs/
cp -r ../../../../docs/guides/*.md src/content/docs/guides/
# etc...
```

**Pros:**
- ✅ Docs are part of Astro build
- ✅ Fast access, no symlink issues
- ✅ Can add frontmatter for Starlight

**Cons:**
- ❌ Docs exist in two places
- ❌ Need to sync changes

**Option B: Symlink Documentation**
```bash
# Symlink docs to Astro content directory
ln -s ../../../../docs/rfcs src/content/docs/rfcs
ln -s ../../../../docs/guides src/content/docs/guides
# etc...
```

**Pros:**
- ✅ Single source of truth
- ✅ No sync needed

**Cons:**
- ❌ Symlinks can be fragile
- ❌ May not work on all platforms

**Recommendation:** Start with **Option A (Copy)**, add sync script later if needed.

### 5. Landing Page Strategy

**Option 1: Redirect to Docs**
```astro
---
// src/pages/index.astro
return Astro.redirect('/docs/');
---
```

**Option 2: Overview Page**
```astro
---
// src/pages/index.astro
---
<html>
  <head><title>Winged Bean</title></head>
  <body>
    <h1>Winged Bean</h1>
    <p>Multi-tier plugin architecture for Unity/Godot games</p>
    
    <div class="links">
      <a href="/docs/">📚 Documentation</a>
      <a href="/demo/">🎮 Live Demo</a>
    </div>
  </body>
</html>
```

**Option 3: Docs as Default**
```javascript
// astro.config.mjs
export default defineConfig({
  integrations: [
    starlight({
      // Starlight handles / as docs homepage
    }),
  ],
});
```

**Recommendation:** **Option 2 (Overview Page)** - gives users clear choice.

---

## Implementation Plan

### Phase 1: Install Starlight (30 minutes)

1. Install Starlight integration
   ```bash
   cd development/nodejs/sites/docs
   pnpm astro add starlight
   ```

2. Configure basic sidebar
3. Test that Starlight works

### Phase 2: Migrate Documentation (1 hour)

1. Create content directory structure
   ```bash
   mkdir -p src/content/docs/{rfcs,guides,adr,design,verification}
   ```

2. Copy documentation files
   ```bash
   cp ../../../../docs/rfcs/*.md src/content/docs/rfcs/
   cp ../../../../docs/guides/*.md src/content/docs/guides/
   cp ../../../../docs/adr/*.md src/content/docs/adr/
   cp ../../../../docs/design/*.md src/content/docs/design/
   cp ../../../../docs/verification/*.md src/content/docs/verification/
   ```

3. Add frontmatter to docs if needed
   ```markdown
   ---
   title: RFC-0009 Dynamic Asciinema Recording
   description: Add dynamic recording to PTY service
   ---
   ```

### Phase 3: Restructure Routes (30 minutes)

1. Move current `/` content to `/demo/`
   ```bash
   mkdir -p src/pages/demo
   mv src/pages/index.astro src/pages/demo/index.astro
   ```

2. Create new landing page at `/`
3. Update links in demo to point to `/docs/`

### Phase 4: Polish & Test (30 minutes)

1. Add custom CSS for branding
2. Test all routes work
3. Test search functionality
4. Test dark mode
5. Verify mobile responsive

---

## Benefits

### 1. Discoverability ✅
- Sidebar navigation shows all available docs
- Search across all documentation
- Clear categorization (RFCs, Guides, ADRs, etc.)

### 2. Professional Presentation ✅
- Consistent styling across all docs
- Dark mode support
- Mobile responsive
- Syntax highlighting for code blocks

### 3. Better Organization ✅
- Clear separation: demo vs documentation
- Logical routing structure
- Easy to find related documents

### 4. Maintainability ✅
- Single tech stack (Astro)
- Auto-generated navigation
- Easy to add new docs (just drop in markdown)

### 5. User Experience ✅
- Fast search with Pagefind
- Keyboard navigation
- Breadcrumbs
- Table of contents for long docs

---

## Risks & Mitigation

### Risk 1: Documentation Duplication
**Risk:** Docs exist in `docs/` and `src/content/docs/`  
**Mitigation:** Create sync script or use symlinks

### Risk 2: Breaking Existing Links
**Risk:** Moving demo from `/` to `/demo/` breaks bookmarks  
**Mitigation:** Add redirect from `/` to `/demo/` or overview page

### Risk 3: Build Time Increase
**Risk:** Starlight adds build time  
**Mitigation:** Starlight is optimized, minimal impact expected

---

## Future Enhancements

### Phase 2 (Future)
1. **API Documentation** - Auto-generate from .NET XML comments
2. **Versioned Docs** - Support multiple versions (v1.0, v2.0, etc.)
3. **i18n** - Multi-language support
4. **Interactive Examples** - Embed Terminal.Gui demos in docs
5. **Changelog** - Auto-generated from git commits

---

## Success Criteria

- All 30 markdown files accessible via Starlight (RFCs, Guides, ADRs, Design, Verification)
- Search works across all documentation (Pagefind integrated)
- Sidebar navigation auto-generated from file structure
- Demo accessible at `/demo/`
- Documentation accessible at `/docs/`
- Landing page at `/` with clear navigation
- Dark mode works (Starlight default)
- Mobile responsive (Starlight default)

## Implementation Status

### ✅ Phase 1: Install Starlight (COMPLETE)
- Installed @astrojs/starlight integration
- Configured sidebar with RFCs, Guides, ADRs, Design, Verification
- Created documentation homepage at /docs/

### ✅ Phase 2: Migrate Documentation (COMPLETE)
- Copied 11 RFCs to src/content/docs/rfcs/
- Copied 4 guides to src/content/docs/guides/
- Copied 6 ADRs to src/content/docs/adr/
- Copied 4 design docs to src/content/docs/design/
- Copied 2 verification reports to src/content/docs/verification/
- Total: 30 files (including index.mdx) migrated

### ✅ Phase 3: Restructure Routes (COMPLETE)
- Moved Terminal.Gui demo from / to /demo/
- Created new landing page at / with gradient design
- Clear separation between demo and documentation

### ✅ Phase 4: Testing (COMPLETE)
- All routes verified working
- PM2 services restarted successfully
- Documentation accessible and searchable

---

## References

- [Astro Starlight Documentation](https://starlight.astro.build/)
- [Astro Content Collections](https://docs.astro.build/en/guides/content-collections/)
- [Pagefind Search](https://pagefind.app/)
- RFC-0008: Playwright and Asciinema Testing Strategy
- RFC-0009: Dynamic Asciinema Recording in PTY
