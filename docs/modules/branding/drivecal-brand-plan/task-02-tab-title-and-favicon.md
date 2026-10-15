# Task 2: Tab title and favicon

Part of [DriveCal branding](README.md). Read the README's Global Constraints first.

**Files:**
- Modify: `client\src\app\core\brand-assets.spec.ts` (title and SVG tests)
- Modify: `client\src\app\core\language.service.spec.ts:7-10`, `:85`, `:140` (fake names)
- Modify: `client\src\index.html:5` (first-paint title)
- Replace: `client\public\favicon.svg`
- Replace (binary): `client\public\favicon.ico`
- Throwaway (session scratchpad, never committed): `favicon-render.html`, `favicon-{32,48,256}.html`, `pack-ico.cjs`, `favicon-32.png`, `favicon-48.png`, `favicon-256.png`, `edge-profile\`

**Interfaces:**
- Consumes: `brand.appName` = `"DriveCal"` / `"דרייבקל"` from task 1. `LanguageService` already sets `document.title` from `brand.appName` (`DOCUMENT_TITLE_KEY`), so its code doesn't change.
- Produces: `/favicon.svg` and `/favicon.ico` served from `public\` with the Solid calendar; `index.html` first-paint title `דרייבקל`. Task 3 checks them in the browser.

- [ ] **Step 1: Point the asset specs at DriveCal**

In `client\src\app\core\brand-assets.spec.ts`, replace the first test:

```typescript
    it('names the browser tab DriveCal in Hebrew before the app starts', () => {
        //when
        const page = readFileSync('src/index.html', 'utf8');

        //then
        expect(page).toContain('<title>דרייבקל</title>');
    });
```

and replace `draws the lane tile in the svg favicon` with:

```typescript
    it('draws the solid calendar in the svg favicon, reversed on dark tab bars', () => {
        //when
        const icon = readFileSync('public/favicon.svg', 'utf8');

        //then
        expect(icon).toContain('viewBox="0 0 40 40"');
        expect(icon).toContain('x="1" y="5.5" width="38" height="33.5" rx="10"');
        expect(icon).toContain('d="M12.5 1.8v7M27.5 1.8v7"');
        expect(icon).toContain('cx="12.5" cy="9.5" r="1.6"');
        expect(icon).toContain('cx="27.5" cy="9.5" r="1.6"');
        expect(icon).toContain('d="M20 15.5v4M20 23.5v4M20 31.5v3"');
        expect(icon).toContain('@media (prefers-color-scheme: dark)');
        expect(icon).not.toContain('M20 9v5');
    });
```

Leave `links the svg favicon with an ico fallback`, `ships an ico fallback that embeds png renders` and task 1's `spells the wordmark halves` as they are.

- [ ] **Step 2: Rename the fake product name in the language spec**

In `client\src\app\core\language.service.spec.ts`, replace the `TRANSLATIONS` constant (lines 7-10):

```typescript
const TRANSLATIONS = {
    en: { brand: { appName: 'DriveCal' } },
    he: { brand: { appName: 'דרייבקל' } },
};
```

Line 85 becomes:

```typescript
        expect(document.title).toBe('DriveCal');
```

Line 140 becomes:

```typescript
        expect([hebrewTitle, document.title]).toEqual(['דרייבקל', 'DriveCal']);
```

This is a rename of fake data, not new behavior: the spec stays green before and after.

- [ ] **Step 3: Run both specs to verify the asset tests fail**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/brand-assets.spec.ts --include src/app/core/language.service.spec.ts
```

Expected: FAIL in `brand-assets` only.
- `names the browser tab DriveCal in Hebrew` fails: `index.html` still has `<title>סלוטליין</title>`.
- `draws the solid calendar in the svg favicon` fails: `favicon.svg` is still the Lane tile.
- The ICO, link and wordmark tests pass. `language.service` passes.

- [ ] **Step 4: Rename the first-paint title**

`client\src\index.html:5`:

```html
  <title>דרייבקל</title>
```

Nothing else in `index.html` changes; the two `<link rel="icon">` lines stay (`sizes="32x32"` on the ICO keeps Chromium preferring the SVG).

- [ ] **Step 5: Write the SVG favicon**

Replace `client\public\favicon.svg` with (sky page, white knockouts; reversed under a dark tab bar, as the design's `dBr`):

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40">
  <style>
    .page { fill: #0057FF; }
    .rings { fill: none; stroke: #0057FF; }
    .hole { fill: #FFFFFF; }
    .lane { fill: none; stroke: #FFFFFF; }
    @media (prefers-color-scheme: dark) {
      .page { fill: #FFFFFF; }
      .rings { stroke: #FFFFFF; }
      .hole { fill: #0057FF; }
      .lane { stroke: #0057FF; }
    }
  </style>
  <rect class="page" x="1" y="5.5" width="38" height="33.5" rx="10"/>
  <path class="rings" d="M12.5 1.8v7M27.5 1.8v7" stroke-width="3.4" stroke-linecap="round"/>
  <circle class="hole" cx="12.5" cy="9.5" r="1.6"/>
  <circle class="hole" cx="27.5" cy="9.5" r="1.6"/>
  <path class="lane" d="M20 15.5v4M20 23.5v4M20 31.5v3" stroke-width="3.2" stroke-linecap="round"/>
</svg>
```

- [ ] **Step 6: Render the ICO fallback PNGs**

Use the session scratchpad directory (`$s` below). Create `$s\favicon-render.html`:

```html
<!doctype html>
<html>
<body style="margin:0;background:transparent">
<svg xmlns="http://www.w3.org/2000/svg" width="SIZE" height="SIZE" viewBox="0 0 40 40" style="display:block">
  <rect x="1" y="5.5" width="38" height="33.5" rx="10" fill="#0057FF"/>
  <path d="M12.5 1.8v7M27.5 1.8v7" fill="none" stroke="#0057FF" stroke-width="3.4" stroke-linecap="round"/>
  <circle cx="12.5" cy="9.5" r="1.6" fill="#FFFFFF"/>
  <circle cx="27.5" cy="9.5" r="1.6" fill="#FFFFFF"/>
  <path d="M20 15.5v4M20 23.5v4M20 31.5v3" fill="none" stroke="#FFFFFF" stroke-width="3.2" stroke-linecap="round"/>
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

Open `favicon-256.png` and `favicon-32.png` with the Read tool (Review Focus 5). Pass: a solid blue calendar with two blue binding rings on top, two white holes, and a white dashed lane down the middle, on a transparent background; at 32 px the lane and holes are still visible.

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

Overwrite the Slotline favicon, then confirm the size changed from 8,327 bytes:

```powershell
node "$s\pack-ico.cjs" "V:\DrivingLessonsBooking\client\public\favicon.ico" "$s\favicon-32.png" "$s\favicon-48.png" "$s\favicon-256.png"
(Get-Item "V:\DrivingLessonsBooking\client\public\favicon.ico").Length
```

- [ ] **Step 8: Run both specs to verify they pass**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/brand-assets.spec.ts --include src/app/core/language.service.spec.ts
```

Expected: PASS, all 5 `brand-assets` tests and every `language.service` test.

- [ ] **Step 9: Run the full client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: tests PASS; build succeeds, and `dist\client\browser` contains the new `favicon.svg` and `favicon.ico`.

- [ ] **Step 10: Commit**

```bash
git add client/src/app/core/brand-assets.spec.ts client/src/app/core/language.service.spec.ts client/src/index.html client/public/favicon.svg client/public/favicon.ico
git commit -m "feat(brand): DriveCal tab title and the solid-calendar favicon"
```

End the commit message with the attribution line from the session's attribution reminder.
