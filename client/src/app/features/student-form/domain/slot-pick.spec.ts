import { SessionType } from './session-type.enum';
import { movePick, rankOf, removePick, SlotPick, upsertPick } from './slot-pick';

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

    it('promotes a later pick and renumbers the ranks to the new order', () => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, null),
        ];

        const next = movePick(picks, { slotId: 'friday-morning', toIndex: 0 });

        expect(next.map(x => x.slotId)).toEqual(['friday-morning', 'sunday-noon', 'monday-evening']);
        expect(rankOf(next, 'friday-morning')).toBe(1);
        expect(rankOf(next, 'sunday-noon')).toBe(2);
        expect(rankOf(next, 'monday-evening')).toBe(3);
    });

    it('demotes a pick below the ones after it', () => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, null),
        ];

        const next = movePick(picks, { slotId: 'sunday-noon', toIndex: 1 });

        expect(next.map(x => x.slotId)).toEqual(['monday-evening', 'sunday-noon', 'friday-morning']);
    });

    it('keeps the session type and constraint with the moved pick', () => {
        const picks = [
            pick('sunday-noon', SessionType.double, 'only after 16:00'),
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, 'pick me up from work'),
        ];

        const next = movePick(picks, { slotId: 'sunday-noon', toIndex: 2 });

        expect(next).toEqual([
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, 'pick me up from work'),
            pick('sunday-noon', SessionType.double, 'only after 16:00'),
        ]);
    });

    it.each([
        { toIndex: -1, order: ['monday-evening', 'sunday-noon', 'friday-morning'] },
        { toIndex: 9, order: ['sunday-noon', 'friday-morning', 'monday-evening'] },
    ])('clamps a move to $toIndex to the nearest end of the list', ({ toIndex, order }) => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, null),
        ];

        const next = movePick(picks, { slotId: 'monday-evening', toIndex });

        expect(next.map(x => x.slotId)).toEqual(order);
    });

    it('leaves the order alone for a slot that is not picked, without mutating the list', () => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
        ];

        const unknown = movePick(picks, { slotId: 'thursday-evening', toIndex: 0 });
        const moved = movePick(picks, { slotId: 'monday-evening', toIndex: 0 });

        expect(unknown).toEqual(picks);
        expect(unknown).not.toBe(picks);
        expect(moved).not.toBe(picks);
        expect(picks.map(x => x.slotId)).toEqual(['sunday-noon', 'monday-evening']);
    });
});
