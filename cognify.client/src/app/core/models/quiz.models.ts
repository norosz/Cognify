export interface QuizQuestionDto {
    id?: string;
    prompt: string;
    type: 'MultipleChoice' | 'TrueFalse' | 'OpenText' | 'Matching' | 'Ordering' | 'MultipleSelect'; // 'Mixed' used in generation only
    options?: string[]; // Made optional for OpenText
    correctAnswer: string; // or JSON for Matching
    pairs?: string[]; // For Matching
    explanation?: string;
}

export interface QuizDto {
    id: string;
    noteId: string;
    title: string;
    questions: QuizQuestionDto[];
    type: string;
    difficulty?: string;
    quizRubric?: string | null;
    categoryLabel?: string;
    categorySource?: string;
    createdAt: string;
}

export interface CreateQuizDto {
    noteId: string;
    title: string;
    questions: QuizQuestionDto[];
    difficulty?: string;
    quizRubric?: string | null;
}

export interface SubmitAttemptDto {
    quizId: string;
    answers: { [key: string]: string }; // questionId (index/guid) -> answer
    timeSpentSeconds?: number;
    difficulty?: string;
}

export interface AttemptDto {
    id: string;
    quizId: string;
    userId: string;
    score: number;
    answers: { [key: string]: string };
    timeSpentSeconds?: number;
    difficulty?: string;
    createdAt: string;
}

export interface QuizStatsDto {
    quizId: string;
    questionCount: number;
    attemptCount: number;
    averageScore: number;
    bestScore: number;
    lastAttemptAt?: string;
    accuracyRate: number;
}
