import { readFileSync } from 'node:fs';

const NOT_COLOURS = [
    '--app-font-display',
    '--app-font-text',
    '--app-font-wordmark',
    '--app-wordmark-style',
    '--app-wordmark-tracking',
    '--app-radius-card',
    '--app-radius-field',
    '--app-caps-tracking',
    '--app-grad-sky',
];

const BODY_TEXT = 4.5;
const LARGE_TEXT = 3;

function block(selector: string): Map<string, string> {
    const source = readFileSync('src/styles/_tokens.scss', 'utf8');
    const escaped = selector.replace(/[.[\]]/g, '\\$&');
    const body = source.match(new RegExp(`(?:^|\\n)${escaped}\\s*\\{([\\s\\S]*?)\\n\\}`));

    if (!body) {
        return new Map();
    }

    return new Map([...body[1].matchAll(/(--app-[\w-]+)\s*:\s*([^;]+);/g)].map((m) => [m[1], m[2].trim()]));
}

function luminance(hex: string): number {
    const channels = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255);
    const linear = channels.map((c) => (c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4));

    return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
}

function contrast(foreground: string, background: string): number {
    const [lighter, darker] = [luminance(foreground), luminance(background)].sort((a, b) => b - a);

    return (lighter + 0.05) / (darker + 0.05);
}

function failingPairs(pairs: [string, string][], minimum: number): string[] {
    const dark = block(':root.app-dark');

    return pairs
        .map(([fg, bg]) => ({ fg, bg, ratio: contrast(dark.get(fg) ?? '#000000', dark.get(bg) ?? '#000000') }))
        .filter(({ ratio }) => ratio < minimum)
        .map(({ fg, bg, ratio }) => `${fg} on ${bg}: ${ratio.toFixed(2)}`);
}

describe('dark tokens', () => {
    it('gives every colour token a dark value', () => {
        //given
        const light = block(':root');
        const dark = block(':root.app-dark');

        //when
        const missing = [...light.keys()].filter((token) => !NOT_COLOURS.includes(token) && !dark.has(token));

        //then
        expect(missing).toEqual([]);
    });

    it('keeps dark body text readable', () => {
        //given
        const pairs: [string, string][] = [
            ...['--app-ink', '--app-text-secondary', '--app-text-muted', '--app-brand', '--app-danger'].flatMap(
                (fg): [string, string][] => [
                    [fg, '--app-bg-page'],
                    [fg, '--app-bg-card'],
                ],
            ),
            ['--app-text-muted', '--app-bg-muted'],
            ['--app-brand-deep', '--app-selected-bg'],
            ['--app-brand-deep', '--app-row-new-bg'],
            ['--app-whale', '--app-steel-light'],
        ];

        //when
        const failing = failingPairs(pairs, BODY_TEXT);

        //then
        expect(failing).toEqual([]);
    });

    it('keeps dark tags readable', () => {
        //given
        const pairs: [string, string][] = [
            ['--app-tag-sky-fg', '--app-tag-sky-bg'],
            ['--app-tag-ocean-fg', '--app-tag-ocean-bg'],
            ['--app-tag-green-fg', '--app-tag-green-bg'],
        ];

        //when
        const failing = failingPairs(pairs, LARGE_TEXT);

        //then
        expect(failing).toEqual([]);
    });

    it('paints the boot background with the dark page colour', () => {
        //given
        const page = readFileSync('src/index.html', 'utf8');

        //when
        const boot = page.match(/html\.app-dark\s*\{[^}]*background:\s*(#[0-9a-fA-F]{6})/)?.[1];

        //then
        expect(boot?.toLowerCase()).toBe(block(':root.app-dark').get('--app-bg-page')?.toLowerCase());
    });

    it('no longer has a Plum alias', () => {
        //when
        const light = block(':root');

        //then
        expect([light.has('--app-plum'), light.get('--app-danger')]).toEqual([false, 'var(--p-plum-500)']);
    });
});
