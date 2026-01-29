export interface LearningAnalyticsTrendPointDto {
  date: string;
  attempts: number;
  averageScore: number;
}

export interface TopicDistributionDto {
  topic: string;
  masteryScore: number;
  forgettingRisk: number;
  lastReviewedAt?: string | null;
  nextReviewAt?: string | null;
}

export interface LearningAnalyticsSummaryDto {
  from: string;
  to: string;
  totalAttempts: number;
  averageScore: number;
  totalInteractions: number;
  correctRate: number;
  activeTopics: number;
  averageMastery: number;
  averageForgettingRisk: number;
  trends: LearningAnalyticsTrendPointDto[];
  topics: TopicDistributionDto[];
}
