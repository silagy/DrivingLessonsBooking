# Task 2: Tab title and favicon

Part of [Slotline branding](README.md). Read the README's Global Constraints first.

**Files:**
- Create: `client\src\app\core\brand-assets.spec.ts`
- Create: `client\public\favicon.svg`
- Replace (binary): `client\public\favicon.ico`
- Modify: `client\src\index.html:5` (title) and its `<head>` (icon links)
- Modify: `client\src\app\core\language.service.ts:13` (`DOCUMENT_TITLE_KEY`)
- Modify: `client\src\app\core\language.service.spec.ts:7-10`, `:85`, `:140`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (delete `shell.title`)
- Throwaway (scratchpad, never committed): `favicon-render.html`, `pack-ico.cjs`, `favicon-32.png`, `favicon-48.png`, `favicon-256.png`

**Interfaces:**
- Consumes: `brand.appName` from task 1 (`"Slotline"` / `"סלוטליין"`).
- Produces: `LanguageService` sets `document.title` from `brand.appName`; the `shell.title` key no longer exists; `/favicon.svg` and `/favicon.ico` are served from `public\`.

- [ ] **Step 1: Write the failing asset spec**

Create `client\src\app\core\brand-assets.spec.ts` (Vitest runs from `client\`, like `source-text.spec.ts`):

```typescript
import { readFileSync } from 'node:fs';

const PNG_SIGNATURE = [0x89, 0x50, 0x4e, 0x47];

function icoImages(icon: Uint8Array): { size: number; head: number[] }[] {
    const view = new DataView(icon.buffer, icon.byteOffset, icon.byteLength);

    return Array.from({ length: view.getUint16(4, true) }, (_, index) => {
        const entry = 6 + index * 16;
        const offset = view.getUint32(entry + 12, true);

        return { size: icon[entry] || 256, head: Array.from(icon.subarray(offset, offset + 4)) };
    });
}

describe('brand assets', () => {
    it('names the browser tab Slotline in Hebrew before the app starts', () => {
        //when
        const page = readFileSync('src/index.html', 'utf8');

        //then
        expect(page).toContain('<title>סלוטליין</title>');
    });

    it('links the svg favicon with an ico fallback', () => {
        //when
        const page = readFileSync('src/index.html', 'utf8');

        //then
        expect(page).toContain('<link rel="icon" href="favicon.ico" sizes="32x32">');
        expect(page).toContain('<link rel="icon" href="favicon.svg" type="image/svg+xml">');
    });

    it('draws the lane tile in the svg favicon', () => {
        //when
        const icon = readFileSync('public/favicon.svg', 'utf8');

        //then
        expect(icon).toContain('viewBox="0 0 40 40"');
        expect(icon).toContain('rx="10"');
        expect(icon).toContain('d="M20 9v5M20 17.5v5M20 26v5"');
    });

    it('ships an ico fallback that embeds png renders', () => {
        //when
        const images = icoImages(readFileSync('public/favicon.ico'));

        //then
        expect(images.map(image => image.size)).toEqual([32, 48, 256]);
        expect(images.every(image => image.head.join() === PNG_SIGNATURE.join())).toBe(true);
    });
});
```

- [ ] **Step 2: Update the title spec**

In `client\src\app\core\language.service.spec.ts`, replace the `TRANSLATIONS` constant (lines 7-10):

```typescript
const TRANSLATIONS = {
    en: { brand: { appName: 'Slotline' } },
    he: { brand: { appName: 'סלוטליין' } },
};
```

Line 85 becomes:

```typescript
        expect(document.title).toBe('Slotline');
```

Line 140 becomes:

```typescript
        expect([hebrewTitle, document.title]).toEqual(['סלוטליין', 'Slotline']);
```

- [ ] **Step 3: Run both specs to verify they fail**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/brand-assets.spec.ts --include src/app/core/language.service.spec.ts
```

Expected: FAIL.
- `brand-assets`: all four fail. The title is still "מערכת תכנון שיעורי נהיגה", there are no icon links, `public/favicon.svg` doesn't exist (ENOENT), and the current ICO holds 3 BMP entries (sizes 48/32/16, head `40,0,0,0`).
- `language.service`: `starts in the saved language` and `names the browser tab in the selected language` fail because `LanguageService` still reads `shell.title`, which the fake translations no longer have, so the tab never reads "Slotline" / "סלוטליין".

- [ ] **Step 4: Read the title from `brand.appName`**

`client\src\app\core\language.service.ts:13`:

```typescript
const DOCUMENT_TITLE_KEY = 'brand.appName';
```

Delete the `"title"` line from the `shell` block in both files:
- `client\public\i18n\en.json`: `"title": "Driving Lessons Planner",`
- `client\public\i18n\he.json`: `"title": "מערכת תכנון שיעורי נהיגה",`

Then confirm nothing else reads it (expect no output):

```powershell
git grep -n "shell.title" -- client/src
```

- [ ] **Step 5: Write the SVG favicon**

Create `client\public\favicon.svg` (sky `#0057FF` on light, white on dark tab bars, as the design's dark cell):

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40">
  <style>
    .lane { stroke: #0057FF; }
    @media (prefers-color-scheme: dark) { .lane { stroke: #FFFFFF; } }
  </style>
  <rect class="lane" x="2" y="2" width="36" height="36" rx="10" fill="none" stroke-width="3"/>
  <path class="lane" d="M20 9v5M20 17.5v5M20 26v5" fill="none" stroke-width="3" stroke-linecap="round"/>
</svg>
```

- [ ] **Step 6: Render the ICO fallback PNGs**

Use the session scratchpad directory (`$s` below). Create `$s\favicon-render.html`:

```html
<!doctype html>
<html>
<body style="margin:0;background:transparent">
<svg xmlns="http://www.w3.org/2000/svg" width="SIZE" height="SIZE" viewBox="0 0 40 40" style="display:block">
  <rect x="2" y="2" width="36" height="36" rx="10" fill="none" stroke="#0057FF" stroke-width="3"/>
  <path d="M20 9v5M20 17.5v5M20 26v5" fill="none" stroke="#0057FF" stroke-width="3" stroke-linecap="round"/>
</svg>
</body>
</html>
```

Render 32, 48 and 256 px with headless Edge. Run this in PowerShell, not Bash: Edge needs its own `--user-data-dir` and `Start-Process -Wait`, otherwise it exits before writing the file.

```powershell
$s = "<scratchpad path>"
$edge = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
foreach ($n in 32, 48, 256) {
  (Get-Content "$s\favicon-render.html" -Raw) -replace 'SIZE', "$n" | Set-Content "$s\favicon-$n.html" -Encoding utf8
  Start-Process -FilePath $edge -Wait -ArgumentList "--headless=new", "--disable-gpu", "--hide-scrollbars", "--default-background-color=00000000", "--window-size=$n,$n", "--user-data-dir=$s\edge-profile", "--screenshot=$s\favicon-$n.png", "file:///$($s -replace '\\','/')/favicon-$n.html"
}
```

Check each PNG is the right size with an alpha channel (expect `32 32 6`, `48 48 6`, `256 256 6`):

```powershell
node -e "for (const n of [32,48,256]) { const b = require('fs').readFileSync(process.argv[1] + '/favicon-' + n + '.png'); console.log(b.readUInt32BE(16), b.readUInt32BE(20), b[25]); }" $s
```

Open `favicon-32.png` with the Read tool and check it shows the outlined tile with three dashes on a transparent background.

- [ ] **Step 7: Pack the ICO**

Create `$s\pack-ico.cjs`:

```javascript
const { readFileSync, writeFileSync } = require('node:fs');

const [output, ...inputs] = process.argv.slice(2);
const images = inputs.map(path => readFileSync(path));
const header = Buffer.alloc(6);
header.writeUInt16LE(0, 0);
header.writeUInt16LE(1, 2);
header.writeUInt16LE(images.length, 4);

let offset = 6 + 16 * images.length;
const entries = images.map(png => {
  const entry = Buffer.alloc(16);
  const width = png.readUInt32BE(16);
  const height = png.readUInt32BE(20);
  entry[0] = width >= 256 ? 0 : width;
  entry[1] = height >= 256 ? 0 : height;
  entry.writeUInt16LE(1, 4);
  entry.writeUInt16LE(32, 6);
  entry.writeUInt32LE(png.length, 8);
  entry.writeUInt32LE(offset, 12);
  offset += png.length;
  return entry;
});

writeFileSync(output, Buffer.concat([header, ...entries, ...images]));
```

Overwrite the Angular favicon:

```powershell
node "$s\pack-ico.cjs" "V:\DrivingLessonsBooking\client\public\favicon.ico" "$s\favicon-32.png" "$s\favicon-48.png" "$s\favicon-256.png"
```

- [ ] **Step 8: Link the icons and rename the first-paint title**

`client\src\index.html`, whole file:

```html
<!doctype html>
<html lang="he" dir="rtl">
<head>
  <meta charset="utf-8">
  <title>סלוטליין</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1, interactive-widget=resizes-content">
  <link rel="icon" href="favicon.ico" sizes="32x32">
  <link rel="icon" href="favicon.svg" type="image/svg+xml">
</head>
<body>
  <app-root></app-root>
</body>
</html>
```

`sizes="32x32"` on the ICO is deliberate: without it, Chromium prefers the ICO over the SVG.

- [ ] **Step 9: Run both specs to verify they pass**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/brand-assets.spec.ts --include src/app/core/language.service.spec.ts
```

Expected: PASS, every test in both files.

- [ ] **Step 10: Run the full client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: tests PASS; build succeeds, and `dist\client\browser` contains `favicon.svg` and `favicon.ico`.

- [ ] **Step 11: Commit**

```bash
git add client/src/app/core/brand-assets.spec.ts client/src/app/core/language.service.ts client/src/app/core/language.service.spec.ts client/src/index.html client/public/favicon.svg client/public/favicon.ico client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(brand): Slotline tab title from brand.appName and the lane-tile favicon"
```
