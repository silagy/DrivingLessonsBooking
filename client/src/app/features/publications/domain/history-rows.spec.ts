import { PublicationState } from '../../../shared/models/publication-state.enum';
import { historyRowsOf } from './history-rows';

function item(publicationId: string, teacherId: string, state: PublicationState) {
    return { publicationId, teacherId, weekStart: '2026-10-11', state };
}

describe('historyRowsOf', () => {
    it('puts one all-teachers row above the teacher rows of each closed week', () => {
        //given
        const items = [
            item('week-42', 'teacher-a', PublicationState.closed),
            item('week-42', 'teacher-b', PublicationState.closed),
            item('week-41', 'teacher-a', PublicationState.closed),
        ];

        //when
        const rows = historyRowsOf(items, true);

        //then
        expect(rows.map((row) => row.key)).toEqual([
            'week-42-all',
            'week-42-teacher-a',
            'week-42-teacher-b',
            'week-41-all',
            'week-41-teacher-a',
        ]);
    });

    it('adds no all-teachers row to a week that is not closed', () => {
        //given
        const items = [
            item('week-43', 'teacher-a', PublicationState.open),
            item('week-44', 'teacher-a', PublicationState.draft),
        ];

        //when
        const rows = historyRowsOf(items, true);

        //then
        expect(rows.map((row) => row.kind)).toEqual(['teacher', 'teacher']);
    });

    it('adds no all-teachers row when they are not offered', () => {
        //given
        const items = [item('week-42', 'teacher-a', PublicationState.closed)];

        //when
        const rows = historyRowsOf(items, false);

        //then
        expect(rows).toEqual([{ kind: 'teacher', key: 'week-42-teacher-a', item: items[0] }]);
    });
});
