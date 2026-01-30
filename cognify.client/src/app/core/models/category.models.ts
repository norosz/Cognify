export type CategoryHistorySource = 'AI' | 'Applied';

export interface CategorySuggestionItemDto {
    label: string;
    confidence?: number | null;
    rationale?: string | null;
}

export interface CategorySuggestionResponseDto {
    suggestions: CategorySuggestionItemDto[];
}

export interface CategoryHistoryBatchDto {
    batchId: string;
    createdAt: string;
    source: CategoryHistorySource;
    items: CategorySuggestionItemDto[];
}

export interface CategoryHistoryResponseDto {
    items: CategoryHistoryBatchDto[];
    nextCursor?: string | null;
}
