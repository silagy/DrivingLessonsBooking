import { rtlPopoverPlacement } from './rtl-popover-placement';

const CONTAINER_RIGHT = 707;
const PANEL_WIDTH = 266;

describe('rtlPopoverPlacement', () => {
    it('lines the panel up with the trigger\'s inline-start edge', () => {
        //given
        const triggerRight = 378;

        //when
        const placement = rtlPopoverPlacement(triggerRight, PANEL_WIDTH, CONTAINER_RIGHT);

        //then
        expect(placement).toEqual({ insetInlineStart: 329, arrowInset: 0 });
    });

    it('keeps the panel inside the page when the trigger is near the edge', () => {
        //given
        const triggerRight = 200;

        //when
        const placement = rtlPopoverPlacement(triggerRight, PANEL_WIDTH, CONTAINER_RIGHT);

        //then
        expect(placement).toEqual({ insetInlineStart: 441, arrowInset: 66 });
    });

    it('never pushes the panel past the inline-start edge of the page', () => {
        //given
        const triggerRight = 720;

        //when
        const placement = rtlPopoverPlacement(triggerRight, PANEL_WIDTH, CONTAINER_RIGHT);

        //then
        expect(placement).toEqual({ insetInlineStart: 0, arrowInset: 0 });
    });
});
