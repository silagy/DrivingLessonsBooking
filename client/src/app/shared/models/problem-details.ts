export interface ProblemDetails {
    status: number;
    title: string;
    detail?: string;
    code?: string;
    params?: Record<string, string>;
}
