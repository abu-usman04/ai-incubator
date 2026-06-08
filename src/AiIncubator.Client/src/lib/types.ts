export type DocumentStatus = 'Pending' | 'Processing' | 'Indexed' | 'Failed';

export interface DocumentItem {
  id: string;
  fileName: string;
  contentType: string;
  moduleId?: string | null;
  status: DocumentStatus;
  chunkCount: number;
  sizeBytes: number;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export type ModuleStatus = 'Draft' | 'Syncing' | 'Ready';

export interface KnowledgeModule {
  id: string;
  name: string;
  description?: string | null;
  documentCount: number;
  status: ModuleStatus;
  createdAt: string;
}

export type ChatRole = 'User' | 'Assistant';

export interface RetrievedChunk {
  documentId: string;
  content: string;
  score: number;
  metadata: Record<string, string>;
}

export interface ChatMessage {
  id: string;
  role: ChatRole;
  content: string;
  sources: RetrievedChunk[];
  createdAt: string;
}

export interface ChatSession {
  id: string;
  title: string;
  messages: ChatMessage[];
  createdAt: string;
}

export type QuestionType = 'MultipleChoice' | 'ShortAnswer';
export type AttemptStatus = 'InProgress' | 'Completed';
export type QuizStatus = 'Ready';

export interface QuizQuestion {
  id: string;
  type: QuestionType;
  prompt: string;
  options: string[];
  correctOptionIndex?: number | null;
  expectedAnswer?: string | null;
  points: number;
}

export interface Quiz {
  id: string;
  title: string;
  topic: string;
  moduleId?: string | null;
  status: QuizStatus;
  questions: QuizQuestion[];
  createdAt: string;
}

export interface QuizAnswer {
  questionId: string;
  selectedOptionIndex?: number | null;
  text?: string | null;
  isCorrect: boolean;
  awardedPoints: number;
  feedback?: string | null;
}

export interface QuizAttempt {
  id: string;
  quizId: string;
  status: AttemptStatus;
  answers: QuizAnswer[];
  score: number;
  maxScore: number;
  startedAt: string;
  completedAt?: string | null;
}

export interface GenerateQuizInput {
  topic?: string;
  moduleId?: string;
  questionCount?: number;
  includeShortAnswer?: boolean;
}

export interface SubmitAnswerInput {
  questionId: string;
  selectedOptionIndex?: number;
  text?: string;
}

export type PlanTaskType = 'Read' | 'Quiz' | 'Chat';

export interface PlanTask {
  id: string;
  type: PlanTaskType;
  title: string;
  description: string;
  referenceId?: string | null;
  isComplete: boolean;
  dueAt?: string | null;
}

export interface PlanWeek {
  weekNumber: number;
  theme: string;
  tasks: PlanTask[];
}

export interface LearningPlan {
  id: string;
  goal: string;
  summary?: string | null;
  userId?: string | null;
  weeks: PlanWeek[];
  createdAt: string;
}

export interface GeneratePlanInput {
  goal: string;
  weekCount?: number;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: { code: string; message: string };
}

export interface ApiClient {
  health(): Promise<boolean>;
  listDocuments(page?: number, pageSize?: number): Promise<PagedResult<DocumentItem>>;
  uploadDocument(file: File, moduleId?: string): Promise<DocumentItem>;
  deleteDocument(id: string): Promise<void>;
  listModules(): Promise<KnowledgeModule[]>;
  createModule(name: string, description?: string): Promise<KnowledgeModule>;
  syncModule(id: string): Promise<KnowledgeModule>;
  listSessions(): Promise<ChatSession[]>;
  createSession(title?: string): Promise<ChatSession>;
  getSession(id: string): Promise<ChatSession>;
  sendMessage(sessionId: string, message: string, topK?: number): Promise<ChatMessage>;
  generateQuiz(input: GenerateQuizInput): Promise<Quiz>;
  listQuizzes(page?: number, pageSize?: number): Promise<PagedResult<Quiz>>;
  getQuiz(id: string): Promise<Quiz>;
  startAttempt(quizId: string): Promise<QuizAttempt>;
  submitAnswer(attemptId: string, input: SubmitAnswerInput): Promise<QuizAttempt>;
  getAttempt(attemptId: string): Promise<QuizAttempt>;
  generatePlan(input: GeneratePlanInput): Promise<LearningPlan>;
  listPlans(page?: number, pageSize?: number): Promise<PagedResult<LearningPlan>>;
  getPlan(id: string): Promise<LearningPlan>;
  completeTask(planId: string, taskId: string): Promise<LearningPlan>;
}
