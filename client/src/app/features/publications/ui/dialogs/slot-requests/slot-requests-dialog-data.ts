import { Signal } from '@angular/core';
import { SlotWindow } from '../../../../../shared/models/slot-window.enum';
import { SlotCountForGetPublicationDashboardResponse } from '../../../data/get-publication-dashboard.response';
import { ItemForFindSlotRequestsResponse } from '../../../data/item-for-find-slot-requests.response';

export interface SlotRequestsDialogData {
    slot: Signal<SlotCountForGetPublicationDashboardResponse | undefined>;
    requests: Signal<ItemForFindSlotRequestsResponse[]>;
    isLoading: Signal<boolean>;
    errorKey: Signal<string | null>;
    weekStart: Signal<string>;
    windowTimes: Signal<Partial<Record<SlotWindow, string>>>;
}
