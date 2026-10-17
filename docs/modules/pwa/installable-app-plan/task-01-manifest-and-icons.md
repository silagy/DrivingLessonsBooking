# Task 1: Manifest and icons

Part of [Installable DriveCal app](README.md). Read the README's Global Constraints and Decisions 5 to 9 first.

**Files:**
- Create: `client\src\app\core\pwa\pwa-assets.spec.ts`
- Create: `client\public\manifest.webmanifest`
- Create (binary): `client\public\icons\icon-192.png`, `client\public\icons\icon-512.png`, `client\public\icons\icon-maskable-512.png`, `client\public\apple-touch-icon.png`
- Modify: `client\src\index.html` (four head tags)
- Modify: `tests\DrivingLessons.Application.Test\StaticFiles\StaticFileCachingTest.cs` (`DataRow`s)
- Throwaway (session scratchpad, never committed): `icon-render.html`, `icon-*.html`, `edge-profile\`

**Interfaces:**
- Consumes: the Option E mark from `docs\modules\branding\drivecal-option-e-plan\task-02-wheel-favicon.md`, step 4 (`tile-render.html`).
- Produces: `/manifest.webmanifest` and its icons; the head tags; `pwa-assets.spec.ts` with a `describe('pwa assets', ...)` block and the helpers `publicText(path: string): string` and `pngSize(path: string): [number, number]`, which task 2 adds tests to.

- [ ] **Step 1: Write the failing asset spec**

Create `client\src\app\core\pwa\pwa-assets.spec.ts`. Helpers: `publicText(path)` reads `public/${path}` as UTF-8; `pngSize(path)` reads `public/${path}` and returns `[bytes.readUInt32BE(16), bytes.readUInt32BE(20)]` after checking the first four bytes are the PNG signature (`0x89 0x50 0x4e 0x47`, as `brand-assets.spec.ts` does). Tests:

```typescript
    it('describes the installed app in hebrew', () => {
        //when
        const manifest = JSON.parse(publicText('manifest.webmanifest'));

        //then
        expect(manifest).toEqual({
            id: '/',
            name: 'דרייבקל',
            short_name: 'דרייבקל',
            description: 'ביקוש שבועי לשיעורים, בקישור אחד',
            lang: 'he',
            dir: 'rtl',
            start_url: '/',
            scope: '/',
            display: 'standalone',
            theme_color: '#ffffff',
            background_color: '#FBFBFF',
            icons: [
                { src: 'icons/icon-192.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
                { src: 'icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
                { src: 'icons/icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
            ],
        });
    });

    it('renders every icon at the size it declares', () => {
        //given
        const manifest = JSON.parse(publicText('manifest.webmanifest'));

        //when
        const sizes = manifest.icons.map((icon: { src: string }) => pngSize(icon.src).join('x'));

        //then
        expect(sizes).toEqual(manifest.icons.map((icon: { sizes: string }) => icon.sizes));
        expect(pngSize('apple-touch-icon.png')).toEqual([180, 180]);
    });

    it('links the manifest, theme color and home screen icon from the page', () => {
        //when
        const page = readFileSync('src/index.html', 'utf8');

        //then
        expect(page).toContain('<meta name="theme-color" content="#ffffff">');
        expect(page).toContain('<link rel="manifest" href="manifest.webmanifest">');
        expect(page).toContain('<link rel="apple-touch-icon" href="apple-touch-icon.png">');
        expect(page).toContain('<meta name="apple-mobile-web-app-title" content="דרייבקל">');
    });

    it('writes the public text files with a plain hyphen and three dots', () => {
        //when
        const offending = readdirSync('public', { recursive: true, encoding: 'utf8' })
            .filter(file => /\.(webmanifest|js|html)$/.test(file))
            .filter(file => /[\u2013\u2014\u2026]/.test(publicText(file)));

        //then
        expect(offending).toEqual([]);
    });
```

- [ ] **Step 2: Run the spec to verify it fails**

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/pwa/pwa-assets.spec.ts
```

Expected: FAIL in the first three tests (`ENOENT` on `manifest.webmanifest`; `index.html` lacks the tags). The punctuation test passes (no matching files yet).

- [ ] **Step 3: Write the manifest and the head tags**

Create `client\public\manifest.webmanifest` with exactly the object the first test expects, 2-space indent.

In `client\src\index.html`, after the viewport `<meta>` and before the two icon links, add the four tags from the third test in that order. Keep every existing line.

- [ ] **Step 4: Write the render page**

In the session scratchpad (`$s`), copy `tile-render.html` from the branding plan (see Interfaces) to `$s\icon-render.html`, and change `border-radius:22%` to `border-radius:RADIUS`. `SIZE` and `MARK` stay as placeholders.

- [ ] **Step 5: Render the four PNGs**

PowerShell, not Bash (Edge needs `Start-Process -Wait` and its own `--user-data-dir`):

```powershell
$s = "<scratchpad path>"
$public = "V:\DrivingLessonsBooking\client\public"
New-Item -ItemType Directory -Force "$public\icons" | Out-Null
$edge = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
$frames = @(
  @{ name = 'icon-192'; out = "$public\icons\icon-192.png"; n = 192; mark = 150; radius = '22%' },
  @{ name = 'icon-512'; out = "$public\icons\icon-512.png"; n = 512; mark = 400; radius = '22%' },
  @{ name = 'icon-maskable-512'; out = "$public\icons\icon-maskable-512.png"; n = 512; mark = 307; radius = '0' },
  @{ name = 'apple-touch-icon'; out = "$public\apple-touch-icon.png"; n = 180; mark = 141; radius = '0' }
)
foreach ($f in $frames) {
  (Get-Content "$s\icon-render.html" -Raw) -replace 'SIZE', "$($f.n)" -replace 'MARK', "$($f.mark)" -replace 'RADIUS', $f.radius | Set-Content "$s\$($f.name).html" -Encoding utf8
  Start-Process -FilePath $edge -Wait -ArgumentList "--headless=new", "--disable-gpu", "--hide-scrollbars", "--default-background-color=00000000", "--window-size=$($f.n),$($f.n)", "--user-data-dir=$s\edge-profile", "--screenshot=$($f.out)", "file:///$($s -replace '\\','/')/$($f.name).html"
}
node -e "for (const p of process.argv.slice(1)) { const b = require('fs').readFileSync(p); console.log(p.split('\\').pop(), b.readUInt32BE(16), b.readUInt32BE(20)); }" $frames.out
```

Expected: `icon-192.png 192 192`, `icon-512.png 512 512`, `icon-maskable-512.png 512 512`, `apple-touch-icon.png 180 180`. Marks are 50/64 of the side (150, 400, 141) and 60% for the maskable (307), per Decision 8.

- [ ] **Step 6: Look at the PNGs (Review Focus 5)**

Open all four with the Read tool. Pass:
- `icon-192` and `icon-512`: a white tile with rounded corners on a transparent background, the full mark (blue calendar, navy rings, blue check, navy wheel in front of the lower-left corner) centered, touching no edge.
- `icon-maskable-512`: a white square to every edge, no rounded or transparent corners; the mark smaller, wholly inside a centered circle of diameter 410px.
- `apple-touch-icon`: a white square to every edge, the mark as large as on the tile.

If a frame is blank, cropped or transparent where it should be white, fix the replacement or the window size and rerender.

- [ ] **Step 7: Pin `no-cache` for the new files**

In `StaticFileCachingTest.A_File_Without_A_Fingerprint_Is_Revalidated`, add after the existing rows:

```csharp
    [DataRow("manifest.webmanifest")]
    [DataRow("sw.js")]
    [DataRow("offline.html")]
    [DataRow("icon-maskable-512.png")]
    [DataRow("apple-touch-icon.png")]
```

These pass without a code change (none matches the fingerprint pattern); they guard a later change to the pattern.

```powershell
dotnet test tests\DrivingLessons.Application.Test --filter "FullyQualifiedName~StaticFileCachingTest"
```

Expected: PASS, 13 tests.

- [ ] **Step 8: Run the spec to verify it passes**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/pwa/pwa-assets.spec.ts
```

Expected: PASS, 4 tests.

- [ ] **Step 9: Run the full client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: tests PASS (`brand-assets.spec.ts` still passes: its favicon link lines are unchanged); `dist\client\browser` has `manifest.webmanifest`, `apple-touch-icon.png` and `icons\`.

- [ ] **Step 10: Commit**

```bash
git add client/src/app/core/pwa/pwa-assets.spec.ts client/public/manifest.webmanifest client/public/icons client/public/apple-touch-icon.png client/src/index.html tests/DrivingLessons.Application.Test/StaticFiles/StaticFileCachingTest.cs
git commit -m "feat(pwa): web app manifest and Option E app icons"
```

End the commit message with the attribution line from the session's attribution reminder.
