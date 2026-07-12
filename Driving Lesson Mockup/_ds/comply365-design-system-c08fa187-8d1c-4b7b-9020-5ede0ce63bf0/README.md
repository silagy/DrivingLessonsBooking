# Comply365 Design System

A design system distilled from the **Comply365 brand guidelines** and the **Comply365 Deck Template (L1R4)**. Comply365 is a compliance-technology platform for the highly regulated industries of **aviation, defense, rail and space** — providing operational content management, safety management, training management, and AI across a single connected platform. The company serves 450+ organizations worldwide and is headquartered in Beloit, WI.

## Products represented

The brand is a family — the parent Comply365 mark covers sub-products. Per the April 2025 brand refresh, the current product nomenclature is:

- **Content Manager 365** *(formerly DocuNet / ContentManager365)* — operational content management
- **Safety Manager 365** *(formerly SafetyNet / AQD / iQSMS)* — safety, risk & compliance management
- **Training Manager 365** *(formerly Fox / Qualtero / TQMS)* — training & qualification management
- **CoAnalyst** — data & analytics *(legacy: PureIntel)*
- **CoAuthor** — authoring platform *(legacy: ProAuthor)*
- **CoTrainer** — training assistant

Legacy product wordmarks (DocuNet, SafetyNet, Qualtero, Fox, ProAuthor, iQSMS, AQD) may still appear on older product-portfolio slides during transition. Horizontal SVG lockups for the current names are supplied in `assets/brand/products/`.

This design system focuses on the **parent Comply365** brand (color, type, deck, marketing) rather than any one sub-product UI — no product UI code or Figma was provided. Product-specific UI kits can be added later when source material is available.

## Sources

All material was extracted from uploads provided to this project:

- `uploads/COM_Comply365 Deck Template_L1R4.pdf` — full 57-page deck template (title, dividers, 1/2/3-column, sidebars, quotes, stats, palette, iconography)
- `uploads/02.pdf`–`34.pdf` — individual pages of the original **Comply365 Brand Guidelines**: logo usage (04–06), colour (08–12), typography (13–18), buttons & toggles (30–32), product logos (34)
- `uploads/brand-guidelines-2025.pdf` — **April 2025 Brand Guidelines & Usage refresh** (67 pages). Updates: Arial as open-source font substitute (was Inter); new product nomenclature (CoAnalyst, CoAuthor, CoTrainer, *Manager 365* line); Plum sidebar template; logo meaning officially stated.
- `uploads/Comply365 Color Palette 2025.pdf` — confirms the color values used in `colors_and_type.css`.
- Supplied **SVG logo kit** (April 2025): horizontal, stacked, and icon lockups in color + mono + reversed → extracted to `assets/brand/`. Product horizontal lockups + CoAgents → `assets/brand/products/`. Rocket element → `assets/brand/rocket.svg`.
- `uploads/Teams Comply BG1…BG8.png` — 8 branded Teams / slide backgrounds
- `uploads/Comply AI .png` — Comply AI marketing background

Additional context came from public web copy at **comply365.com** (messaging / voice samples). No codebase or Figma was supplied.

---

## Index

| File / folder | What it is |
|---|---|
| `README.md` | This file — brand context, content + visual foundations |
| `colors_and_type.css` | All CSS variables (colors, gradients, type scale, radii, shadows, motion) and semantic element styles |
| `SKILL.md` | Cross-compatible skill manifest (for use in Claude Code, etc.) |
| `assets/brand/` | **Official SVG logo kit** (April 2025): horizontal + stacked + icon-only lockups in color / mono / reversed. **Always use these SVGs; do not re-typeset the wordmark.** |
| `assets/brand/products/` | Product horizontal lockups (Content Manager 365, Safety Manager 365, Training Manager 365) and CoAgents (CoAuthor, CoAnalyst, CoTrainer). |
| `assets/brand/rocket.svg` | Standalone rocket / vertical-ascender element. |
| `assets/logo-mark.png` | Legacy chevron mark PNG cropped from BG1 (kept for back-compat with older files that reference it). Prefer the SVGs in `assets/brand/` for new work. |
| `assets/icons/` | 34 brand icons (84 px, flat color, from deck iconography page) |
| `assets/icons-large/` | 4 larger 112 px versions pulled from two-column-icon slides |
| `assets/backgrounds/` | 8 Teams / video-call virtual backgrounds (logo is baked in — **do not use as slide backgrounds**) plus 1 Comply AI hero image |
| `preview/` | Design-system tab cards (colors, type, components, etc.) |
| `slides/` | Sample slides + `slides/index.html` grid preview |
| `uploads/` | Original source files (do not modify) |

---

## Content fundamentals

**Audience.** Highly regulated operators — airline flight & tech ops, rail network managers, defense organizations, safety & compliance officers. Senior stakeholders in safety-critical environments.

**Tone.** Trusted · technical · confident · plainspoken. Not playful; not jargon-stuffed either. The brand voice treats compliance as something to be **transformed and simplified**, not feared — the recurring promise is that complexity is navigable. Think "seasoned domain expert in the room, not a marketing team trying too hard."

**Person.** Predominantly **third-person corporate** ("Comply365 empowers…", "Our platform delivers…"). Switches to **second-person** ("help you gain a deeper understanding") in services / support / training copy where the reader is the subject. First-person plural ("we continue to lead the way") appears in About / leadership copy.

**Casing.** Sentence case for headings and body. Button labels are ALL CAPS (`REQUEST A DEMO`), with a subtle type-design quirk: the guidelines render the brand's button as `REQuEST A DEMO` — lowercase "u" inside otherwise all-caps — because the sample word contains a lowercase letter in the templated asset. Treat this as a **template artifact, not a rule** — use standard all-caps `REQUEST A DEMO`.

**Vocabulary.** Favors operational verbs: *streamline, empower, transform, digitize, accelerate, unify, elevate*. Nouns cluster around: *platform, content, operations, compliance, workflows, data, domains, crews, assets*. Products use the `365` suffix or their own wordmark (DocuNet, SafetyNet, Qualtero, Fox).

**Emoji.** Never. The brand does not use emoji in any surfaced material.

**Punctuation.** Em dashes for asides. Ampersands accepted in headings and short phrases (*"Safety, Risk & Compliance"*). Smart quotes in long-form copy.

**Specimen lines** (actual and in-style):

- "One AI-Powered Platform. One Trusted Partner. One shared vision for a safer, smarter, and more connected world."
- "Powering Operational Excellence for Airlines."
- "Designed to streamline complex workflows, enhance collaboration, and accelerate continuous improvement."
- "The right information to the right people, at the right time, all the time."

---

## Visual foundations

**Palette.** Primary is **Dark Gray `#121418` + White `#FFFFFF`**. Five secondary hues — **Sky** (bright royal blue, primary accent), **Ocean** (teal), **Plum** (magenta), **Whale** (deep navy), **Steel** (desaturated blue-grey) — each with light and dark variants. Cool-neutral grayscale spans #000 → #FBFBFF. Industry assignments: Aviation = Sky, Rail = Plum, Defense = Ocean.

**Gradients** are brand-critical. Four established styles (Soft, Shine, Dark Angular, Light Angular) × four hues = 16 canonical gradient fills. The logo mark itself is a **Soft Gradient Sky** (#0057FF → #00BBC7). Primary CTA on light backgrounds fills with **Soft Gradient Sky**; on dark, with **Soft Gradient Plum**. Never recolor or alter gradients.

**Type.** **Helvetica Now Display** for headings (Medium at H1–H3, Bold at H4+, XBold for Eyebrow) and **Helvetica Now Text** for body/UI (Regular, Medium for quotes/buttons). Eyebrow labels are small-caps-style: XBold + UPPERCASE + 10% tracking. Paragraph rhythm: 150 % line height, 15 px paragraph spacing. **Font substitute per April 2025 guidelines: Arial** — a system font available on virtually every OS, so no webfont import is required. Comply365 holds a Helvetica Now license for its website and digital touchpoints; for any public-facing or printed material, check with marketing before using Helvetica Now.

**Backgrounds.** Three modes co-exist and should be chosen deliberately per slide:
1. **White / near-white** (`#FFFFFF`, `#F5F6FF`, `#FBFBFF`) — default for content-heavy decks.
2. **Environmental photography** — cool, natural-light scenes (cloud + wing, office interiors with negative space on one side for the logo). Warm tones and candid photography are **avoided**.
3. **Flood gradients** or **deep `#121418` + digital imagery** for dividers and hero moments.

**Corner radii.** 3 px / 6 px / 14 px (cards) / **999 px (pills)**. Buttons always pill. Cards crisp at 14 px.

**Borders / cards.** Thin (1 pt) borders in `#E6E8F6` or `#E4E5ED`. Cards are **flat with a faint shadow**, not heavy drop shadows. Quiet elevation: `0 1px 2px rgba(18,20,24,0.06), 0 2px 8px rgba(18,20,24,0.06)`.

**Hover & press.** Buttons change **fill** on hover (Soft Gradient Sky → solid `#0057FF`). The primary/secondary CTA has a distinctive motion detail: **the arrow icon grows from 20 → 22 px wide on hover** — a quiet, disciplined signal. No bounces, no scale-ups on press. Arrows (icon buttons) darken fill on hover (`#F5F6FF → #E6E8F6`).

**Motion.** Subtle, functional, never decorative. `200 ms` with `cubic-bezier(0.22, 0.61, 0.36, 1)` out-ease for most state changes; `120 ms` for instantaneous feedback.

**Layout rules.** Logo fixed top-left with ~48 px margin. Page number fixed bottom-right. Slide content respects a generous ~128 px side margin. Grids of 2 / 3 / 4 columns for content; stat slides reserve large numbers as a visual anchor.

**Transparency & blur.** Used sparingly — only for legibility overlays on photographic backgrounds (linear gradient white-to-transparent for logo/title areas) or sticky nav on dark scenes. No glass / glassmorphism.

**Imagery vibe.** Cool, corporate, architectural. Offices with abundant daylight; aviation interiors; sky. Never people in close-up, never warm sunset colors, never textured or grainy. The brand image is "quiet competence".

---

## Iconography

**Source set.** The brand ships a **34-icon flat-color PNG set** at 84 × 84 px (and 112 × 112 px), extracted from the deck template's iconography page. All 34 are available in `assets/icons/`. They cover: document, aircraft, shield, checklist, data-chart, training, audit, lock, globe, operations, lifecycle, alert, and industry-specific marks (airplane, jet, satellite).

**Style.** Single-color or two-tone flat fills — predominantly Sky (`#0057FF`), Ocean (`#00BBC7`), Plum (`#BA0081`), and Whale (`#005389`). Chunky, rounded geometry. No outlines/strokes. No gradients inside icons.

**Usage.** Three-column icon slides show icons at **112 px** flush with the top of their column. Inline icons in running copy should be 20–24 px. For digital UI where icons must stroke-match a text grid, substitute with **Lucide** (linked from CDN) at stroke 1.75 — flag this as a substitute for production use.

**Emoji.** Never used.

**Unicode icon chars.** Not used. Arrow glyphs in buttons are custom-drawn (CSS pseudo-elements with a 2 pt hairline — not `→`).

**Logo as favicon.** For applications under 50 px, the Comply365 mark (just the chevron parallelogram, no wordmark) is used as the symbol.

---

## Component micro-rules (for reference)

**Primary button (light).** `16 / 26 / 10` px padding (H/W/Interior). Fill: **Soft Gradient Sky**. Text: `#FFFFFF`, font-weight 500, 16 px. Corner radius: `999 px`. Outline: linear gradient, 2 pt. **Hover:** solid `#0057FF` fill, `#0057FF` outline, arrow widens 20 → 22 px.

**Primary button (dark).** Same geometry; fill **Soft Gradient Sky** (Sky is the primary accent in both modes). Hover: solid `#0057FF`.

**Secondary button (light).** Transparent fill, `#121418` text, 2 pt `#121418` outline, pill. **Hover:** `#F5F6FF` fill.

**Tertiary (link-like).** Text + arrow only, arrow `#0057FF` (light) or `#FFA3E3` (dark). Hover recolors text to match arrow.

**Arrow icon button.** 40 px pill. Light: `#F5F6FF` fill, `#E6E8F6` 1 pt outline, `#121418` glyph. Hover: `#E6E8F6` fill.

**Toggle.** 50 × 25 px. Active: `#001939` bg + `#289E49` thumb. Inactive: `#B6C6DA` bg + `#001939` thumb.

---

## Caveats & substitutions

- **Helvetica Now Display / Text** are licensed — per the April 2025 guidelines, **Arial** is the approved open-source substitute (system font on all major OSes). Replace with licensed Helvetica Now files + `@font-face` on Comply365 digital properties where the license is held.
- **No product UI** was shipped with this project (no codebase or Figma). UI kits for DocuNet, SafetyNet, Qualtero etc. are stubbed out of scope — add when source material is provided.
- **Colour accessibility page (11.pdf)** listed dozens of BACKGROUND/FOREGROUND combinations as "LG/UI" accessible (large text / UI icon) — the detailed color-combination matrix was not reproduced here; refer back to that page for the authoritative table.
- **Buttons' "linear-gradient outline"** on primary default is rendered with a CSS mask trick; it reads cleanly but is not a byte-exact reproduction of the brand spec.

