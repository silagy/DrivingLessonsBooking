import { formatWindowInstant } from '../../../shared/dates/jerusalem-time';
import { weekRangeLabel } from '../../../shared/dates/week-label';

export const REMINDER_LOCALE = 'he-IL';

const WHATSAPP_CHAT_URL = 'https://wa.me';

export interface ReminderMessageInput {
    studentName: string;
    teacherName: string;
    weekNumber: number;
    weekStart: string;
    windowEndUtc: string;
    link: string;
}

export interface ReminderMessageParams {
    name: string;
    teacherName: string;
    weekNumber: number;
    weekRange: string;
    closesAt: string;
    link: string;
}

export function reminderMessageParams(input: ReminderMessageInput): ReminderMessageParams {
    return {
        name: input.studentName,
        teacherName: input.teacherName,
        weekNumber: input.weekNumber,
        weekRange: weekRangeLabel(input.weekStart, REMINDER_LOCALE),
        closesAt: formatWindowInstant(input.windowEndUtc, REMINDER_LOCALE),
        link: input.link,
    };
}

export function whatsAppChatUrl(whatsAppNumber: string, message: string): string {
    return `${WHATSAPP_CHAT_URL}/${whatsAppNumber}?text=${encodeURIComponent(message)}`;
}
