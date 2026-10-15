# Task 2: Wheel favicon

Part of [DriveCal logo, Option E](README.md). Read the README's Global Constraints and Decisions 9 and 10 first.

**Files:**
- Modify: `client\src\app\core\brand-assets.spec.ts` (SVG and ICO tests)
- Replace: `client\public\favicon.svg`
- Replace (binary): `client\public\favicon.ico`
- Throwaway (session scratchpad, never committed): `wheel-render.html`, `tile-render.html`, `favicon-{16,32,48,256}.html`, `favicon-{16,32,48,256}.png`, `pack-ico.cjs`, `edge-profile\`

**Interfaces:**
- Consumes: the Option E geometry from the README's Global Constraints (task 1 uses the same numbers in the component).
- Produces: `/favicon.svg` (the wheel) and `/favicon.ico` (16/32 wheel, 48/256 app tile) served from `public\`. `index.html` doesn't change: its two `<link rel="icon">` lines and `<title>דרייבקל</title>` stay. Task 3 checks the served files.

- [ ] **Step 1: Write the failing asset tests**

In `client\src\app\core\brand-assets.spec.ts`, replace `draws the solid calendar in the svg favicon, reversed on dark tab bars` with:

```typescript
    it('draws the plain wheel in the svg favicon, reversed on dark tab bars', () => {
        //when
        const icon = readFileSync('public/favicon.svg', 'utf8');

        //then
        expect(icon).toContain('viewBox="0 0 40 40"');
        expect(icon).toContain('cx="20" cy="20" r="19"');
        expect(icon).toContain('d="M6.06 16A14.5 14.5 0 0 1 33.94 16C29 16.5 25 12 20 12S11 16.5 6.06 16Z"');
        expect(icon).toContain('d="M5.9 23.5C10 23.5 14 25.5 15.5 30L16 34A14.5 14.5 0 0 1 5.9 23.5Z"');
        expect(icon).toContain('d="M34.1 23.5C30 23.5 26 25.5 24.5 30L24 34A14.5 14.5 0 0 0 34.1 23.5Z"');
        expect(icon).toContain('cx="20" cy="21.5" r="4.6"');
        expect(icon).toContain('@media (prefers-color-scheme: dark)');
        expect(icon).not.toContain('<rect');
    });
```

and replace `ships an ico fallback that embeds png renders` with:

```typescript
    it('ships an ico fallback that embeds png renders at 16, 32, 48 and 256', () => {
        //when
        const images = icoImages(readFileSync('public/favicon.ico'));

        //then
        expect(images.map(image => image.size)).toEqual([16, 32, 48, 256]);
        expect(images.every(image => image.head.join() === PNG_SIGNATURE.join())).toBe(true);
    });
```

`not.toContain('<rect')` is the guard for Review Focus 5: the full mark has rectangles (the calendar), the wheel has none.

- [ ] **Step 2: Run the spec to verify it fails**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/brand-assets.spec.ts
```

Expected: FAIL in exactly two tests.
- `draws the plain wheel in the svg favicon` fails: `favicon.svg` is still the Solid calendar (no `r="19"` circle, and it has a `<rect`).
- `ships an ico fallback that embeds png renders at 16, 32, 48 and 256` fails: the sizes are `[32, 48, 256]`.
- The other five tests pass.

- [ ] **Step 3: Write the SVG favicon**

Replace `client\public\favicon.svg` with (the design's light wheel; under a dark tab bar the disc turns white and the windows ink, as the design's dark cell):

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40">
  <style>
    .disc { fill: #003293; }
    .window { fill: #FFFFFF; }
    .hub { fill: none; stroke: #7BAEFF; }
    @media (prefers-color-scheme: dark) {
      .disc { fill: #FFFFFF; }
      .window { fill: #121418; }
    }
  </style>
  <circle class="disc" cx="20" cy="20" r="19"/>
  <path class="window" d="M6.06 16A14.5 14.5 0 0 1 33.94 16C29 16.5 25 12 20 12S11 16.5 6.06 16Z"/>
  <path class="window" d="M5.9 23.5C10 23.5 14 25.5 15.5 30L16 34A14.5 14.5 0 0 1 5.9 23.5Z"/>
  <path class="window" d="M34.1 23.5C30 23.5 26 25.5 24.5 30L24 34A14.5 14.5 0 0 0 34.1 23.5Z"/>
  <circle class="hub" cx="20" cy="21.5" r="4.6" stroke-width="2.2"/>
</svg>
```

- [ ] **Step 4: Write the two render pages**

Use the session scratchpad directory (`$s` below). Create `$s\wheel-render.html` (the 16 and 32 frames):

```html
<!doctype html>
<html>
<body style="margin:0;background:transparent">
<svg xmlns="http://www.w3.org/2000/svg" width="SIZE" height="SIZE" viewBox="0 0 40 40" style="display:block">
  <circle cx="20" cy="20" r="19" fill="#003293"/>
  <path d="M6.06 16A14.5 14.5 0 0 1 33.94 16C29 16.5 25 12 20 12S11 16.5 6.06 16Z" fill="#FFFFFF"/>
  <path d="M5.9 23.5C10 23.5 14 25.5 15.5 30L16 34A14.5 14.5 0 0 1 5.9 23.5Z" fill="#FFFFFF"/>
  <path d="M34.1 23.5C30 23.5 26 25.5 24.5 30L24 34A14.5 14.5 0 0 0 34.1 23.5Z" fill="#FFFFFF"/>
  <circle cx="20" cy="21.5" r="4.6" fill="none" stroke="#7BAEFF" stroke-width="2.2"/>
</svg>
</body>
</html>
```

Create `$s\tile-render.html` (the 48 and 256 frames: the design's app icon, a white tile with 22% corners and the full mark at 50/64 of the tile):

```html
<!doctype html>
<html>
<body style="margin:0;background:transparent">
<div style="width:SIZEpx;height:SIZEpx;border-radius:22%;background:#FFFFFF;display:flex;align-items:center;justify-content:center">
<svg xmlns="http://www.w3.org/2000/svg" width="MARK" height="MARK" viewBox="0 0 40 40" style="display:block">
  <rect x="13" y="5" width="25" height="24" rx="5" fill="#0057FF"/>
  <rect x="15.5" y="11" width="20" height="15.5" rx="2.5" fill="#FFFFFF"/>
  <path d="M19.5 2.6v4.8M25.5 2.6v4.8M31.5 2.6v4.8" fill="none" stroke="#003293" stroke-width="2.4" stroke-linecap="round"/>
  <path d="M26.6 17.6l2.4 2.4 4.2-4.6" fill="none" stroke="#0057FF" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round"/>
  <circle cx="15" cy="25.5" r="14.2" fill="#FFFFFF"/>
  <svg x="2" y="12.5" width="26" height="26" viewBox="0 0 40 40">
    <circle cx="20" cy="20" r="19" fill="#003293"/>
    <path d="M6.06 16A14.5 14.5 0 0 1 33.94 16C29 16.5 25 12 20 12S11 16.5 6.06 16Z" fill="#FFFFFF"/>
    <path d="M5.9 23.5C10 23.5 14 25.5 15.5 30L16 34A14.5 14.5 0 0 1 5.9 23.5Z" fill="#FFFFFF"/>
    <path d="M34.1 23.5C30 23.5 26 25.5 24.5 30L24 34A14.5 14.5 0 0 0 34.1 23.5Z" fill="#FFFFFF"/>
    <circle cx="20" cy="21.5" r="4.6" fill="none" stroke="#7BAEFF" stroke-width="2.2"/>
  </svg>
</svg>
</div>
</body>
</html>
```

- [ ] **Step 5: Render the four PNGs**

Run this in PowerShell, not Bash: Edge needs its own `--user-data-dir` and `Start-Process -Wait`, otherwise it exits before writing the file.

```powershell
$s = "<scratchpad path>"
$edge = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
$frames = @(@{ n = 16; page = 'wheel-render' }, @{ n = 32; page = 'wheel-render' }, @{ n = 48; page = 'tile-render' }, @{ n = 256; page = 'tile-render' })
foreach ($f in $frames) {
  $n = $f.n
  $mark = [math]::Round($n * 50 / 64)
  (Get-Content "$s\$($f.page).html" -Raw) -replace 'SIZE', "$n" -replace 'MARK', "$mark" | Set-Content "$s\favicon-$n.html" -Encoding utf8
  Start-Process -FilePath $edge -Wait -ArgumentList "--headless=new", "--disable-gpu", "--hide-scrollbars", "--default-background-color=00000000", "--window-size=$n,$n", "--user-data-dir=$s\edge-profile", "--screenshot=$s\favicon-$n.png", "file:///$($s -replace '\\','/')/favicon-$n.html"
}
node -e "for (const n of [16,32,48,256]) { const b = require('fs').readFileSync(process.argv[1] + '/favicon-' + n + '.png'); console.log(n, b.readUInt32BE(16), b.readUInt32BE(20), b[25]); }" $s
```

Expected output: `16 16 16 6`, `32 32 32 6`, `48 48 48 6`, `256 256 256 6` (width, height, color type 6 = RGBA).

If the 16 line shows another size (Edge can refuse a 16px window), render it at 32 CSS px with a 0.5 scale factor instead, then check again:

```powershell
(Get-Content "$s\wheel-render.html" -Raw) -replace 'SIZE', '32' | Set-Content "$s\favicon-16.html" -Encoding utf8
Start-Process -FilePath $edge -Wait -ArgumentList "--headless=new", "--disable-gpu", "--hide-scrollbars", "--default-background-color=00000000", "--window-size=32,32", "--force-device-scale-factor=0.5", "--user-data-dir=$s\edge-profile", "--screenshot=$s\favicon-16.png", "file:///$($s -replace '\\','/')/favicon-16.html"
```

- [ ] **Step 6: Look at the PNGs**

Open `favicon-16.png`, `favicon-32.png` and `favicon-256.png` with the Read tool (Review Focus 5). Pass:
- 16 and 32: a navy disc with three white windows (a wide arc on top, two lower side windows) and a light blue hub ring, on a transparent background. At 16 the disc and the top window are distinct.
- 256: a white rounded tile; on it a blue calendar with three navy rings on top, a white page with a blue check at its inline end, and the navy wheel in front of the calendar's lower-left corner, separated from it by a white gap. No part of the mark touches the tile edge.

If a frame is blank or cut off, the `SIZE` / `MARK` replacement or the window size is wrong; fix and rerender before packing.

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

Overwrite the Solid-calendar favicon (6,885 bytes) and print the new size; task 3, step 5 compares the served file against it:

```powershell
node "$s\pack-ico.cjs" "V:\DrivingLessonsBooking\client\public\favicon.ico" "$s\favicon-16.png" "$s\favicon-32.png" "$s\favicon-48.png" "$s\favicon-256.png"
(Get-Item "V:\DrivingLessonsBooking\client\public\favicon.ico").Length
```

- [ ] **Step 8: Run the spec to verify it passes**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/brand-assets.spec.ts
```

Expected: PASS, all 7 `brand assets` tests.

- [ ] **Step 9: Run the full client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: tests PASS; build succeeds, and `dist\client\browser` contains the new `favicon.svg` and `favicon.ico`.

- [ ] **Step 10: Commit**

```bash
git add client/src/app/core/brand-assets.spec.ts client/public/favicon.svg client/public/favicon.ico
git commit -m "feat(brand): steering-wheel favicon with the Option E app tile in the ico"
```

End the commit message with the attribution line from the session's attribution reminder.
