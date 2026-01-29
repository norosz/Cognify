export interface UserKnowledgeStateDto {
    id: string;
    topic: string;
    sourceNoteId?: string | null;
    masteryScore: number;
    confidenceScore: number;
    forgettingRisk: number;
    nextReviewAt?: string | null;
    lastReviewedAt?: string | null;
    updatedAt: string;
}

export interface ReviewQueueItemDto {
    topic: string;
    sourceNoteId?: string | null;
    masteryScore: number;
    forgettingRisk: number;
    nextReviewAt?: string | null;
}

export type AdaptiveQuizMode = 'Review' | 'Weakness' | 'Note';

export interface AdaptiveQuizRequest {
    mode: AdaptiveQuizMode;
    questionCount: number;
    maxTopics?: number;
    questionType?: string;
    noteId?: string;
    title?: string;
}

export interface AdaptiveQuizResponse {
    pendingQuiz: {
        id: string;
        noteId: string;
        moduleId: string;
        title: string;
        noteName: string;
        moduleName: string;
        difficulty: string;
        questionType: string;
        questionCount: number;
        status: string;
        errorMessage?: string;
        createdAt: string;
    };
    selectedTopic?: string | null;
    masteryScore?: number | null;
    forgettingRisk?: number | null;
}
