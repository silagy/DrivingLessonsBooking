import { PublicationState } from '../../../shared/models/publication-state.enum';

export interface HistoryWeek {
    publicationId: string;
    weekStart: string;
    state: PublicationState;
}

export type HistoryRow<T extends HistoryWeek> =
    | { kind: 'teacher'; key: string; item: T }
    | { kind: 'allTeachers'; key: string; week: HistoryWeek };

export function historyRowsOf<T extends HistoryWeek & { teacherId: string }>(
    items: readonly T[],
    withAllTeachers: boolean,
): HistoryRow<T>[] {
    const rows: HistoryRow<T>[] = [];
    const weeksShown = new Set<string>();

    for (const item of items) {
        const isFirstOfClosedWeek = item.state === PublicationState.closed && !weeksShown.has(item.publicationId);

        if (withAllTeachers && isFirstOfClosedWeek) {
            weeksShown.add(item.publicationId);
            rows.push({
                kind: 'allTeachers',
                key: `${item.publicationId}-all`,
                week: { publicationId: item.publicationId, weekStart: item.weekStart, state: item.state },
            });
        }

        rows.push({ kind: 'teacher', key: `${item.publicationId}-${item.teacherId}`, item });
    }

    return rows;
}
