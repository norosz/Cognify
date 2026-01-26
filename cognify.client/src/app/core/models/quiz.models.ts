export interface QuestionDto {
    id?: string;
    prompt: string;
    type: string; // 'MultipleChoice', 'TrueFalse', 'OpenEnded'
    options: string[];
    correctAnswer: string;
    explanation: string;
}

export interface QuestionSetDto {
    id: string;
    noteId: string;
    questions: QuestionDto[];
    createdAt: string;
}

export interface CreateQuestionSetDto {
    noteId: string;
    questions: QuestionDto[];
}

export interface SubmitAttemptDto {
    questionSetId: string;
    answers: { [key: string]: string }; // questionId (index/guid) -> answer
}

export interface AttemptDto {
    id: string;
    questionSetId: string;
    userId: string;
    score: number;
    answers: { [key: string]: string };
    createdAt: string;
}
