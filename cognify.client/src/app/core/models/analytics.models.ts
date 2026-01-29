export interface LearningAnalyticsSummaryDto {
    totalTopics: number;
    averageMastery: number;
    averageForgettingRisk: number;
    weakTopicsCount: number;
    totalAttempts: number;
    accuracyRate: number;
    examReadinessScore: number;
    learningVelocity: number;
    lastActivityAt?: string | null;
}

export interface PerformanceTrendPointDto {
    periodStart: string;
    periodEnd: string;
    attemptCount: number;
    averageScore: number;
}

export interface PerformanceTrendsDto {
    from: string;
    to: string;
    bucketDays: number;
    points: PerformanceTrendPointDto[];
}

export interface TopicInsightDto {
    topic: string;
    sourceNoteId?: string | null;
    masteryScore: number;
    forgettingRisk: number;
    lastReviewedAt?: string | null;
    nextReviewAt?: string | null;
    attemptCount: number;
    accuracyRate: number;
    weaknessScore: number;
}

export interface TopicDistributionDto {
    topics: TopicInsightDto[];
    weakestTopics: TopicInsightDto[];
}

export interface RetentionHeatmapPointDto {
    topic: string;
    masteryScore: number;
    forgettingRisk: number;
}

export interface DecayForecastPointDto {
    date: string;
    forgettingRisk: number;
}

export interface DecayForecastTopicDto {
    topic: string;
    points: DecayForecastPointDto[];
}

export interface DecayForecastDto {
    days: number;
    stepDays: number;
    topics: DecayForecastTopicDto[];
}
