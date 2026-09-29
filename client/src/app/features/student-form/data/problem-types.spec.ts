import { HttpErrorResponse } from '@angular/common/http';
import { isWindowClosedProblem, SUBMISSION_WINDOW_CLOSED_PROBLEM } from './problem-types';

const HTTP_CONFLICT = 409;
const HTTP_NOT_FOUND = 404;

describe('isWindowClosedProblem', () => {
    it('recognises the window-closed conflict', () => {
        const error = new HttpErrorResponse({
            status: HTTP_CONFLICT,
            error: { type: SUBMISSION_WINDOW_CLOSED_PROBLEM, title: 'Conflict', status: HTTP_CONFLICT },
        });

        expect(isWindowClosedProblem(error)).toBe(true);
    });

    it.each([
        {
            case: 'another conflict',
            error: new HttpErrorResponse({ status: HTTP_CONFLICT, error: { title: 'Conflict' } }),
        },
        { case: 'a conflict without a body', error: new HttpErrorResponse({ status: HTTP_CONFLICT }) },
        {
            case: 'the type on another status',
            error: new HttpErrorResponse({ status: HTTP_NOT_FOUND, error: { type: SUBMISSION_WINDOW_CLOSED_PROBLEM } }),
        },
        { case: 'a network failure', error: new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') }) },
        { case: 'an error that is not HTTP', error: new Error('boom') },
    ])('ignores $case', ({ error }) => {
        expect(isWindowClosedProblem(error)).toBe(false);
    });
});
