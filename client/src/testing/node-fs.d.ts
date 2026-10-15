declare module 'node:fs' {
    export function existsSync(path: string): boolean;
    export function readdirSync(path: string, options: { recursive: true }): string[];
    export function readFileSync(path: string, encoding: 'utf8'): string;
    export function readFileSync(path: string): Uint8Array;
}
