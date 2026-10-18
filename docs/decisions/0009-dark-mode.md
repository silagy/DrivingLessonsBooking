# ADR 0009: Dark Mode by Class, With an Explicit Dark Value for Every Colour

**Status:** Accepted
**Date:** 18 October 2026

## Context

Administrators and Teachers asked for a dark appearance. The Claude Design file `Admin Mobile.html` draws every phone screen in dark (frames D01 to D08c), the desktop user menu in dark (D10) and a token map (D09). Its rules: a Light / Dark / System choice in the drawer and the user menu, System by default, saved per device and applied before the first paint; only the `--app-*` tokens change; layers are told apart by lighter surfaces, not shadows; primary buttons keep the Sky gradient, and the logo is unchanged.

Before this change the client had one light colour scheme: PrimeNG's `darkModeSelector` was `false`, `_tokens.scss` aliased every `--app-*` colour to a `--p-surface-*` or palette step, and about 30 component stylesheets used palette steps (`--p-sky-50`, `--p-red-600`, ...) directly.

## Decision

Switch schemes with a class on `<html>`, give every colour token an explicit dark value, and keep light exactly as it rendered.

- **Switch.** `core\theme.service.ts` holds the mode (`light` | `dark` | `system`) in a signal, saved in `localStorage['drivecal.theme']`, and resolves System with `matchMedia('(prefers-color-scheme: dark)')`, following its `change` events. One effect sets the `app-dark` class, `color-scheme` and the `theme-color` meta. `providePrimeNG` uses `darkModeSelector: '.app-dark'`, so the preset's `colorScheme.dark` applies under the same class.
- **No flash.** `index.html` repeats the resolution in an inline `<script>` in `<head>` before Angular boots. An inline `<style>` paints `html.app-dark` with the dark page colour, because the production build loads `styles.css` without blocking and its inlined critical CSS can't include `:root.app-dark`. There is no CSP, so inline code runs.
- **Tokens.** `:root.app-dark` in `_tokens.scss` sets every colour token to a hex value from D09, not through `--p-surface-*`, whose direction doesn't flip. New tokens cover the colours components used to take from palette steps: tags (`--app-tag-sky/ocean/green-*`), the new-Student and Inactive rows, selected fills and borders, the tappable count-cell border, the error, success and info families, and `--app-danger` (Plum, formerly `--app-plum`). Where the request's light hex differed from what the app rendered, the light value is what it rendered.
- **Preset.** `colorScheme.dark` sets the D09 surfaces, primary `{sky.400}`, cards, menus and overlays at `surface.800` (`#1B1D22`) with `surface.600` (`#2C2F37`) borders and the deep shadow. Aura's dark defaults that failed 4.5:1 on those surfaces are replaced: muted text, form-field hints, secondary buttons, and error and info messages. Preset tokens may hold `var(--app-*)`, so tag and shadow colours live only in `_tokens.scss`.
- **Brand mark.** The mark keeps its light colours in both schemes through `--brand-mark-*` custom properties on `brand-logo`'s host; the wordmark lifts with `--app-brand-deep` / `--app-brand`.
- **Control.** `shared\appearance-toggle\` is a PrimeNG `SelectButton` bound to the service, in the drawer footer and the user menu's `#start` template. Arrow keys move focus (mirrored in Hebrew); Enter or Space selects. Sign-in and the Student form have no control and follow the device's saved choice or its system setting.

## Alternatives Considered

| Alternative | Verdict |
|---|---|
| `darkModeSelector: 'system'` (PrimeNG's media-query mode) | No way to choose Light or Dark against the OS |
| Flip `--p-surface-*` and keep the aliases | The D09 values aren't a reversed ramp, and every alias would change meaning by scheme |
| Save the choice on the User (server) | A per-device setting needs no API, and sign-in and the Student form have no User |
| No boot script, apply in Angular | A white frame on every reload in dark |

## Consequences

- Two specs in `client\src\app\theme\` guard this: `dark-tokens.spec.ts` (every colour token has a dark value; dark text, tag, button, form-field and message pairs meet 4.5:1 or 3:1; the boot background matches the dark page token) and `no-hardcoded-colours.spec.ts` (no hex, `rgb(a)(` or palette step in component SCSS; `--brand-mark-*` declarations are the one exception).
- Every new colour needs a token with a light and a dark value; a component never reads a palette step.
- `index.html` and `theme.service.ts` repeat the same rule and the same two status-bar colours; a change to one needs the other.
- The installed app's manifest `theme_color` and `offline.html` stay light: the manifest can't follow a runtime choice, and the offline page has no access to it.
