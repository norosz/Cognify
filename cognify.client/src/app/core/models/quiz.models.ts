export interface QuestionDto {
    id?: string;
    prompt: string;
    type: 'MultipleChoice' | 'TrueFalse' | 'OpenText' | 'Matching' | 'Ordering' | 'MultipleSelect'; // 'Mixed' used in generation only
    options?: string[]; // Made optional for OpenText
    correctAnswer: string; // or JSON for Matching
    pairs?: string[]; // For Matching
    explanation?: string;
}

export interface QuestionSetDto {
    id: string;
    noteId: string;
    title: string;
    questions: QuestionDto[];
    type: string;
    difficulty?: string;
    createdAt: string;
}

export interface CreateQuestionSetDto {
    noteId: string;
    title: string;
    questions: QuestionDto[];
    difficulty?: string;
}

export interface SubmitAttemptDto {
    questionSetId: string;
    answers: { [key: string]: string }; // questionId (index/guid) -> answer
    timeSpentSeconds?: number;
    difficulty?: string;
}

export interface AttemptDto {
    id: string;
    questionSetId: string;
    userId: string;
    score: number;
    answers: { [key: string]: string };
    timeSpentSeconds?: number;
    difficulty?: string;
    createdAt: string;
}
