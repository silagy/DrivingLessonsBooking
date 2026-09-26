import { PublicationState } from '../../../shared/models/publication-state.enum';
import { StudentFormView } from './student-form-view.enum';

const VIEW_BY_STATE: Record<PublicationState, StudentFormView> = {
    [PublicationState.draft]: StudentFormView.invalidLink,
    [PublicationState.published]: StudentFormView.notYetOpen,
    [PublicationState.open]: StudentFormView.open,
    [PublicationState.closed]: StudentFormView.closed,
};

export function viewForPublicationState(state: PublicationState): StudentFormView {
    return VIEW_BY_STATE[state];
}
