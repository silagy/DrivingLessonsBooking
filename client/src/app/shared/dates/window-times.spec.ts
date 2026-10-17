import { SlotWindow } from '../models/slot-window.enum';
import { compactWindowTime, windowTimesOf } from './window-times';

describe('windowTimesOf', () => {
    it('labels each window once with its hour-minute range', () => {
        //given
        const slots = [
            { window: SlotWindow.morning, startLocal: '07:00:00', endLocal: '12:00:00' },
            { window: SlotWindow.morning, startLocal: '07:00:00', endLocal: '12:00:00' },
            { window: SlotWindow.evening, startLocal: '18:00:00', endLocal: '22:00:00' },
        ];

        //when
        const times = windowTimesOf(slots);

        //then
        expect(times).toEqual({
            [SlotWindow.morning]: '07:00-12:00',
            [SlotWindow.evening]: '18:00-22:00',
        });
    });
});

describe('compactWindowTime', () => {
    it('drops whole-hour minutes', () => {
        //given
        const time = '07:00-12:00';

        //when
        const compact = compactWindowTime(time);

        //then
        expect(compact).toBe('07-12');
    });

    it('keeps minutes that are not on the hour', () => {
        //given
        const time = '07:30-12:00';

        //when
        const compact = compactWindowTime(time);

        //then
        expect(compact).toBe('07:30-12');
    });
});
