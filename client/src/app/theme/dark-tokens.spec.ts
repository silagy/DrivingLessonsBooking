import { readFileSync } from 'node:fs';
import { AppPreset } from './app-preset';

interface DarkButtonPreset {
    semantic: { colorScheme: { dark: { surface: Record<number, string> } } };
    components: {
        button: { colorScheme: { dark: { outlined: { secondary: { color: string } }; text: { secondary: { color: string } } } } };
    };
}

interface DarkFormFieldPreset {
    semantic: {
        colorScheme: {
            dark: {
                surface: Record<number, string>;
                formField: Record<
                    'background' | 'disabledBackground' | 'placeholderColor' | 'floatLabelColor' | 'iconColor' | 'disabledColor',
                    string
                >;
            };
        };
    };
}

interface DarkMessagePreset {
    primitive: Record<string, Record<number, string>>;
    semantic: { colorScheme: { dark: { surface: Record<number, string> } } };
    components: { message: { colorScheme: { dark: Record<'error' | 'info' | 'warn' | 'success', { color: string }> } } };
}

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
const MESSAGE_TINT = 0.16;

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

function mix(top: string, bottom: string, share: number): string {
    const channel = (hex: string, i: number) => parseInt(hex.slice(i, i + 2), 16);
    const mixed = [1, 3, 5].map((i) => Math.round(channel(top, i) * share + channel(bottom, i) * (1 - share)));

    return `#${mixed.map((c) => c.toString(16).padStart(2, '0')).join('')}`;
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

    it('keeps secondary buttons readable on a dark card', () => {
        //given
        const preset = AppPreset as unknown as DarkButtonPreset;
        const surface = preset.semantic.colorScheme.dark.surface;
        const resolve = (token: string) => surface[Number(token.match(/\{surface\.(\d+)\}/)![1])];
        const buttons = preset.components.button.colorScheme.dark;

        //when
        const ratios = [buttons.outlined.secondary.color, buttons.text.secondary.color].map((color) =>
            contrast(resolve(color), resolve('{surface.800}')),
        );

        //then
        expect(ratios.every((ratio) => ratio >= BODY_TEXT)).toBe(true);
    });

    it('keeps form field hints readable in dark', () => {
        //given
        const preset = AppPreset as unknown as DarkFormFieldPreset;
        const surface = preset.semantic.colorScheme.dark.surface;
        const resolve = (token: string) => surface[Number(token.match(/\{surface\.(\d+)\}/)![1])];
        const field = preset.semantic.colorScheme.dark.formField;

        //when
        const ratios = {
            placeholder: contrast(resolve(field.placeholderColor), resolve(field.background)),
            floatLabel: contrast(resolve(field.floatLabelColor), resolve(field.background)),
            icon: contrast(resolve(field.iconColor), resolve(field.background)),
            disabled: contrast(resolve(field.disabledColor), resolve(field.disabledBackground)),
        };

        //then
        expect(Object.entries(ratios).filter(([, ratio]) => ratio < BODY_TEXT)).toEqual([]);
    });

    it('keeps messages readable on a dark page', () => {
        //given
        const preset = AppPreset as unknown as DarkMessagePreset;
        const resolve = (token: string) => {
            const [, palette, step] = token.match(/\{(\w+)\.(\d+)\}/)!;
            return palette === 'surface' ? preset.semantic.colorScheme.dark.surface[Number(step)] : preset.primitive[palette][Number(step)];
        };
        const messages = preset.components.message.colorScheme.dark;
        const tintedPage = (token: string) =>
            mix(resolve(`{${token.match(/\{(\w+)\./)![1]}.500}`), resolve('{surface.900}'), MESSAGE_TINT);

        //when
        const failing = (['error', 'info', 'warn', 'success'] as const)
            .map((severity) => {
                const color = messages[severity].color;
                return { severity, ratio: contrast(resolve(color), tintedPage(color)) };
            })
            .filter(({ ratio }) => ratio < BODY_TEXT)
            .map(({ severity, ratio }) => `${severity}: ${ratio.toFixed(2)}`);

        //then
        expect(failing).toEqual([]);
    });

    it('no longer has a Plum alias', () => {
        //when
        const light = block(':root');

        //then
        expect([light.has('--app-plum'), light.get('--app-danger')]).toEqual([false, 'var(--p-plum-500)']);
    });
});
