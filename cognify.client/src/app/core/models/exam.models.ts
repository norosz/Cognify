export interface FinalExamDto {
    moduleId: string;
    currentQuizId?: string | null;
    currentQuizTitle?: string | null;
    currentQuizCreatedAt?: string | null;
}

export interface RegenerateFinalExamRequest {
    questionCount: number;
    difficulty: string;
    questionType: string;
    title: string;
}

export interface FinalExamSaveResultDto {
    quizId: string;
}

export interface ExamAttemptDto {
    id: string;
    moduleId: string;
    quizId: string;
    userId: string;
    score: number;
    answers: { [key: string]: string };
    timeSpentSeconds?: number | null;
    difficulty?: string | null;
    createdAt: string;
}

export interface SubmitExamAttemptDto {
    moduleId: string;
    quizId: string;
    answers: { [key: string]: string };
    timeSpentSeconds?: number;
    difficulty?: string;
}
