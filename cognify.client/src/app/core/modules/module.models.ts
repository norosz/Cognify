export interface ModuleDto {
    id: string;
    title: string;
    description?: string;
    createdAt: string;
    documentsCount: number;
    notesCount: number;
    quizzesCount: number;
    categoryLabel?: string;
    categorySource?: string;
}

export interface ModuleWeakTopicDto {
    topic: string;
    masteryScore: number;
    forgettingRisk: number;
    nextReviewAt?: string;
}

export interface ModuleStatsDto {
    moduleId: string;
    totalDocuments: number;
    totalNotes: number;
    totalQuizzes: number;
    averageMastery: number;
    averageForgettingRisk: number;
    practiceAttemptCount: number;
    practiceAverageScore: number;
    practiceBestScore: number;
    lastPracticeAttemptAt?: string;
    examAttemptCount: number;
    examAverageScore: number;
    examBestScore: number;
    lastExamAttemptAt?: string;
    weakTopics: ModuleWeakTopicDto[];
}

export interface CreateModuleDto {
    title: string;
    description?: string;
}

export interface UpdateModuleDto {
    title: string;
    description?: string;
}
