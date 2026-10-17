import { holdBackInstallPromptOnStudentForm } from './hold-back-install-prompt';

function promptAt(path: string): Event {
    window.history.pushState({}, '', path);
    const prompt = new Event('beforeinstallprompt', { cancelable: true });
    window.dispatchEvent(prompt);

    return prompt;
}

describe('holdBackInstallPromptOnStudentForm', () => {
    beforeAll(() => {
        holdBackInstallPromptOnStudentForm(window);
    });

    afterEach(() => {
        window.history.pushState({}, '', '/');
    });

    it('holds back the install prompt on the student form', () => {
        //when
        const prompt = promptAt('/s/3f9c2a7d');

        //then
        expect(prompt.defaultPrevented).toBe(true);
    });

    it.each(['/', '/login', '/students', '/publications/history'])(
        'leaves the install prompt to the browser outside the student form (%s)',
        path => {
            //when
            const prompt = promptAt(path);

            //then
            expect(prompt.defaultPrevented).toBe(false);
        },
    );
});
