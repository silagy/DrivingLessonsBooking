import { SessionType } from './session-type.enum';
import { rankOf, removePick, SlotPick, upsertPick } from './slot-pick';

function pick(slotId: string, sessionType: SessionType, constraint: string | null): SlotPick {
    return { slotId, sessionType, constraint };
}

describe('slot picks', () => {
    it('appends new picks in selection order', () => {
        const picks = [pick('sunday-noon', SessionType.single, null)];

        const next = upsertPick(picks, pick('monday-evening', SessionType.double, null));

        expect(next.map(x => x.slotId)).toEqual(['sunday-noon', 'monday-evening']);
    });

    it('changes a picked slot in place, keeping its rank', () => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
        ];

        const next = upsertPick(picks, pick('sunday-noon', SessionType.double, 'only after 16:00'));

        expect(next).toEqual([
            pick('sunday-noon', SessionType.double, 'only after 16:00'),
            pick('monday-evening', SessionType.single, null),
        ]);
    });

    it('removes a pick and closes the gap in the ranking', () => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, null),
        ];

        const next = removePick(picks, 'sunday-noon');

        expect(rankOf(next, 'monday-evening')).toBe(1);
        expect(rankOf(next, 'friday-morning')).toBe(2);
        expect(rankOf(next, 'sunday-noon')).toBeNull();
    });

    it('never mutates the list it was given', () => {
        const picks = [pick('sunday-noon', SessionType.single, null)];

        upsertPick(picks, pick('monday-evening', SessionType.single, null));
        removePick(picks, 'sunday-noon');

        expect(picks).toEqual([pick('sunday-noon', SessionType.single, null)]);
    });
});
