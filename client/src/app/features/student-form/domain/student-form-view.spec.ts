import { PublicationState } from '../../../shared/models/publication-state.enum';
import { viewForPublicationState } from './student-form-view';
import { StudentFormView } from './student-form-view.enum';

describe('viewForPublicationState', () => {
    it.each([
        [PublicationState.published, StudentFormView.notYetOpen],
        [PublicationState.open, StudentFormView.open],
        [PublicationState.closed, StudentFormView.closed],
        [PublicationState.draft, StudentFormView.invalidLink],
    ])('maps %s to %s', (state, view) => {
        expect(viewForPublicationState(state)).toBe(view);
    });
});
