import { readdirSync, readFileSync } from 'node:fs';

const SCANNED = ['src/app', 'src/styles/_global.scss'];

const LITERAL =
    /#[0-9a-fA-F]{3,8}\b|\brgba?\(|--p-(?:sky|ocean|steel|whale|plum|primary|surface|red|green|blue|orange|yellow|gray|zinc|slate)-\d+/;
const ALLOWED = /^\s*--brand-mark-[\w-]+\s*:/;

function styleFiles(path: string): string[] {
    if (path.endsWith('.scss')) {
        return [path];
    }

    return readdirSync(path, { recursive: true })
        .filter((file) => file.endsWith('.scss'))
        .map((file) => `${path}/${file}`);
}

function offenders(): string[] {
    return SCANNED.flatMap(styleFiles).flatMap((file) =>
        readFileSync(file, 'utf8')
            .split(/\r?\n/)
            .map((text, index) => ({ text, line: index + 1 }))
            .filter(({ text }) => LITERAL.test(text) && !ALLOWED.test(text))
            .map(({ text, line }) => `${file.replace(/\\/g, '/')}:${line}: ${text.trim()}`),
    );
}

describe('component styles', () => {
    it('keeps palette steps and literal colours out of component styles', () => {
        //when
        const found = offenders();

        //then
        expect(found).toEqual([]);
    });
});
