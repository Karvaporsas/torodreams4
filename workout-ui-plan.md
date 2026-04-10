# ToroFitDreaming4 — Modern UI Plan

## Overview

Replace the current minimal/unstyled frontend with a modern, slick fitness app UI.
The design direction is a **dark-mode-first** dashboard aesthetic with a strong accent
colour (`#42b883` green), smooth transitions, and a mobile-friendly layout.

No external CSS framework is added — the design system is implemented as global CSS
custom properties so the project stays dependency-light.

---

## Design Principles

- **Dark, premium feel** — deep charcoal backgrounds (`#0f1117`, `#1a1d27`) with
  elevated card surfaces.
- **Accent colour** — `#42b883` (existing brand green) for CTAs, highlights, and
  active states.
- **Smooth motion** — CSS transitions on hover/focus states, page enter/leave
  transitions via Vue `<Transition>`.
- **Clear hierarchy** — generous whitespace, strong typographic scale (Inter font).
- **Mobile-first** — everything works at 375 px; desktop gets a sidebar nav.

---

## Epics & Tasks

---

### Epic 1 — Design System & Global Styles

Establish the visual foundation used by all other epics.

**Tasks**

| ID | Task |
|----|------|
| E1-T1 | Add Inter font via Google Fonts import in `index.html` |
| E1-T2 | Create `src/assets/design-system.css` with CSS custom properties: colour tokens (`--color-bg`, `--color-surface`, `--color-surface-raised`, `--color-accent`, `--color-text`, `--color-text-muted`, `--color-danger`, `--color-border`), spacing scale (`--space-1` … `--space-8`), radius scale, shadow tokens, transition duration |
| E1-T3 | Create `src/assets/base.css` — global resets, `body` dark background, default font, scrollbar styling, `*` `box-sizing: border-box` |
| E1-T4 | Create `src/assets/components.css` — reusable utility classes: `.btn`, `.btn-primary`, `.btn-secondary`, `.btn-danger`, `.btn-ghost`, `.card`, `.badge`, `.input`, `.form-row`, `.divider` |
| E1-T5 | Import all three CSS files in `src/main.ts` (replace existing asset imports) |
| E1-T6 | Create `src/assets/transitions.css` — Vue transition classes for page fade-slide (`page-enter-active`, `page-leave-active`, etc.) |

---

### Epic 2 — App Shell & Navigation

Wrap all authenticated views in a persistent shell with navigation.

**Tasks**

| ID | Task |
|----|------|
| E2-T1 | Create `src/components/AppShell.vue` — top navbar on mobile, left sidebar on desktop (≥768 px). Contains logo/brand, nav links (Home, Workouts), Admin link (conditional), and Logout button |
| E2-T2 | Add active-link highlighting using `router-link-active` / `router-link-exact-active` CSS hooks |
| E2-T3 | Add mobile hamburger toggle (CSS-only drawer, no JS library) |
| E2-T4 | Update `src/App.vue` to wrap all non-login routes in `<AppShell>` using a layout slot, keeping `<LoginView>` full-screen without the shell |
| E2-T5 | Remove inline toolbar from `HomeView.vue` (navigation moves to shell) |
| E2-T6 | Add page transition `<Transition name="page">` in `App.vue` around `<RouterView>` |

---

### Epic 3 — Home Page ✅ DONE

Replace the placeholder HelloWorld component with a real dashboard home.

**Tasks**

| ID | Task |
|----|------|
| E3-T1 | ✅ Remove `HelloWorld.vue`, `TheWelcome.vue`, `WelcomeItem.vue`, and icon components that are no longer needed |
| E3-T2 | ✅ Redesign `HomeView.vue` — hero section: greeting ("Welcome back"), large CTA button "Start New Workout" (or "Continue Active Workout" if one exists), and a compact recent-workouts panel (last 3 entries as small cards) |
| E3-T3 | ✅ Add quick-stats strip: total workouts count, total duration this week, streak badge |
| E3-T4 | ✅ Animate the hero CTA button with a subtle pulse when there's no active workout |

---

### Epic 4 — Workouts History Page

Replace table with a card-based, visually rich history list.

**Tasks**

| ID | Task |
|----|------|
| E4-T1 | Replace the `<table>` in `WorkoutsView.vue` with a vertical list of `.workout-card` components — each card shows: formatted date, duration pill, exercise count badge, status indicator ("Completed" / "In Progress") |
| E4-T2 | Create `src/components/WorkoutCard.vue` — reusable card for both WorkoutsView and the home recent-workouts panel |
| E4-T3 | Add "Start New Workout" / "Continue" FAB (floating action button) fixed at bottom-right on mobile; inline header button on desktop |
| E4-T4 | Add skeleton loading state (CSS animated shimmer cards) while fetch is in progress |
| E4-T5 | Add illustrated empty state ("No workouts yet — start your first one!") using an inline SVG icon |

---

### Epic 5 — Active Workout Page

Make the workout logger feel like a native app.

**Tasks**

| ID | Task |
|----|------|
| E5-T1 | Redesign `WorkoutActiveView.vue` header — sticky top bar showing the live timer (large monospace font, accent-coloured), "Complete Workout" button (top-right), and back-to-workouts breadcrumb |
| E5-T2 | Redesign exercise cards — dark `.card` surface, exercise name prominent, musclegroup badge (if available), set rows styled as a clean horizontal row (set #, weight, reps, ✓ checkbox) |
| E5-T3 | Animate set rows — new set slides in from below, completed sets get accent checkmark + faded style without strikethrough (cleaner than current) |
| E5-T4 | Replace raw `<select>` for exercise picker with a styled dropdown/combobox (still native `<select>`, custom-styled via CSS) |
| E5-T5 | Add per-exercise "collapse" toggle so users can hide sets of already-done exercises |
| E5-T6 | Replace `confirm()` dialogs with inline confirmation (small inline button row: "Confirm remove?" → Yes / Cancel) |

---

### Epic 6 — Workout Detail Page

Polished read-only summary of a completed workout.

**Tasks**

| ID | Task |
|----|------|
| E6-T1 | Redesign `WorkoutDetailView.vue` header — workout date as `<h1>`, duration + exercise count as stat pills in a flex row |
| E6-T2 | Redesign exercise cards to match active-view style (read-only — no inputs, no remove buttons) |
| E6-T3 | Add a summary banner at the top: total sets, total reps, total volume (kg × reps summed across all sets) |
| E6-T4 | Style the back-to-history link as a proper icon-button (`← History`) |

---

### Epic 7 — Login Page Polish

The login page is full-screen (no shell) — give it a matching premium look.

**Tasks**

| ID | Task |
|----|------|
| E7-T1 | Center the login form on a dark full-screen background with the app logo/name above |
| E7-T2 | Style form inputs with design system tokens — label, input, error message |
| E7-T3 | Add a loading spinner state on the submit button |

---

## Dependency Order

```
Epic 1 (design system)
  └─► Epic 2 (app shell)
        ├─► Epic 3 (home)
        ├─► Epic 4 (workouts list)
        ├─► Epic 5 (active workout)
        ├─► Epic 6 (workout detail)
        └─► Epic 7 (login — can be done independently)
```

Epics 3–7 can be implemented in parallel once Epics 1 and 2 are complete.

---

## Files to Create

```
frontend/src/assets/design-system.css
frontend/src/assets/base.css
frontend/src/assets/components.css
frontend/src/assets/transitions.css
frontend/src/components/AppShell.vue
frontend/src/components/WorkoutCard.vue
```

## Files to Modify

```
frontend/index.html                      (add Inter font)
frontend/src/main.ts                     (import new CSS files)
frontend/src/App.vue                     (add shell + page transitions)
frontend/src/views/HomeView.vue          (full redesign)
frontend/src/views/WorkoutsView.vue      (card layout)
frontend/src/views/WorkoutActiveView.vue (full redesign)
frontend/src/views/WorkoutDetailView.vue (full redesign)
frontend/src/views/LoginView.vue         (polish)
```

## Files to Delete

```
frontend/src/components/HelloWorld.vue
frontend/src/components/TheWelcome.vue
frontend/src/components/WelcomeItem.vue
frontend/src/components/icons/           (entire folder)
```
