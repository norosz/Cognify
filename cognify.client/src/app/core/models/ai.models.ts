export enum QuestionType {
  Mixed = 'Mixed',
  MultipleChoice = 'MultipleChoice',
  TrueFalse = 'TrueFalse',
  OpenText = 'OpenText',
  Matching = 'Matching',
  Ordering = 'Ordering',
  MultipleSelect = 'MultipleSelect'
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
  extractedContentId: string;
  status: string;
}

export interface ExplainMistakeRequest {
  questionPrompt: string;
  userAnswer: string;
  correctAnswer: string;
  detectedMistakes?: string[];
  conceptLabel?: string;
  noteContext?: string;
  moduleContext?: string;
}

export interface ExplainMistakeResponse {
  explanationMarkdown: string;
  keyTakeaways: string[];
  nextSteps: string[];
}
