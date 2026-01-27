export enum QuestionType {
  MultipleChoice = 'MultipleChoice',
  TrueFalse = 'TrueFalse',
  OpenText = 'OpenText',
  Matching = 'Matching',
  Ordering = 'Ordering'
}

export interface GeneratedQuestion {
  text: string;
  type: QuestionType;
  options?: string[]; // For MC/TF
  correctAnswer?: string;
  pairs?: string[]; // "Term:Def" for Matching
  explanation?: string;
  difficultyLevel?: number;
}

export interface GenerateQuestionsRequest {
  noteId: string;
  type: QuestionType;
  difficulty: number;
  count: number;
}

export interface GradeAnswerRequest {
  question: string;
  answer: string;
  context: string;
}

export interface GradingResult {
  analysis: string; // "Score: 80\nFeedback: ..."
}

export interface TextExtractionResult {
  text: string;
}
