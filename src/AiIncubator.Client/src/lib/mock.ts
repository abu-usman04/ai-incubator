import type {
  ApiClient,
  ChatMessage,
  ChatSession,
  DocumentItem,
  GeneratePlanInput,
  GenerateQuizInput,
  KnowledgeModule,
  LearningPlan,
  Quiz,
  QuizAnswer,
  QuizAttempt,
  SubmitAnswerInput,
} from './types';

const id = () => crypto.randomUUID().replace(/-/g, '');
const now = () => new Date().toISOString();

/**
 * In-memory client used when no backend is configured. Lets the UI be demoed offline.
 */
export function createMockClient(): ApiClient {
  const documents: DocumentItem[] = [
    {
      id: id(),
      fileName: 'company-handbook.md',
      contentType: 'text/markdown',
      status: 'Indexed',
      chunkCount: 12,
      sizeBytes: 18420,
      createdAt: now(),
    },
    {
      id: id(),
      fileName: 'product-faq.txt',
      contentType: 'text/plain',
      status: 'Indexed',
      chunkCount: 5,
      sizeBytes: 6240,
      createdAt: now(),
    },
  ];

  const modules: KnowledgeModule[] = [
    {
      id: id(),
      name: 'Onboarding',
      description: 'Everything a new hire needs.',
      documentCount: 2,
      status: 'Ready',
      createdAt: now(),
    },
  ];

  const sessions: ChatSession[] = [];

  const quizzes: Quiz[] = [
    {
      id: id(),
      title: 'Onboarding basics',
      topic: 'onboarding',
      status: 'Ready',
      createdAt: now(),
      questions: [
        {
          id: id(),
          type: 'MultipleChoice',
          prompt: 'Where do new hires find the company handbook?',
          options: ['The intranet', 'A printed binder', 'Nowhere'],
          correctOptionIndex: 0,
          points: 1,
        },
        {
          id: id(),
          type: 'ShortAnswer',
          prompt: 'Name one onboarding task for week one.',
          options: [],
          expectedAnswer: 'Read the handbook',
          points: 1,
        },
      ],
    },
  ];

  const attempts: QuizAttempt[] = [];

  const plans: LearningPlan[] = [
    {
      id: id(),
      goal: 'Get productive with the onboarding material',
      summary: 'A two-week starter plan.',
      createdAt: now(),
      weeks: [
        {
          weekNumber: 1,
          theme: 'Orientation',
          tasks: [
            { id: id(), type: 'Read', title: 'Read the company handbook', description: '', isComplete: false },
            { id: id(), type: 'Quiz', title: 'Take the onboarding quiz', description: '', isComplete: false },
          ],
        },
        {
          weekNumber: 2,
          theme: 'Practice',
          tasks: [
            { id: id(), type: 'Chat', title: 'Ask the assistant about support runbooks', description: '', isComplete: false },
          ],
        },
      ],
    },
  ];

  return {
    async health() {
      return true;
    },

    async listDocuments(page = 1, pageSize = 20) {
      const start = (page - 1) * pageSize;
      return {
        items: documents.slice(start, start + pageSize),
        page,
        pageSize,
        totalCount: documents.length,
      };
    },

    async uploadDocument(file, moduleId) {
      const doc: DocumentItem = {
        id: id(),
        fileName: file.name,
        contentType: file.type || 'text/plain',
        moduleId: moduleId ?? null,
        status: 'Indexed',
        chunkCount: Math.max(1, Math.round(file.size / 800)),
        sizeBytes: file.size,
        createdAt: now(),
      };
      documents.unshift(doc);
      return doc;
    },

    async deleteDocument(documentId) {
      const index = documents.findIndex((d) => d.id === documentId);
      if (index >= 0) {
        documents.splice(index, 1);
      }
    },

    async listModules() {
      return [...modules];
    },

    async createModule(name, description) {
      const module: KnowledgeModule = {
        id: id(),
        name,
        description: description ?? null,
        documentCount: 0,
        status: 'Draft',
        createdAt: now(),
      };
      modules.unshift(module);
      return module;
    },

    async syncModule(moduleId) {
      const module = modules.find((m) => m.id === moduleId);
      if (!module) {
        throw new Error('Module not found');
      }
      module.status = 'Ready';
      return module;
    },

    async listSessions() {
      return [...sessions];
    },

    async createSession(title) {
      const session: ChatSession = {
        id: id(),
        title: title?.trim() || 'New conversation',
        messages: [],
        createdAt: now(),
      };
      sessions.unshift(session);
      return session;
    },

    async getSession(sessionId) {
      const session = sessions.find((s) => s.id === sessionId);
      if (!session) {
        throw new Error('Session not found');
      }
      return session;
    },

    async sendMessage(sessionId, message) {
      const session = sessions.find((s) => s.id === sessionId);
      if (!session) {
        throw new Error('Session not found');
      }

      session.messages.push({
        id: id(),
        role: 'User',
        content: message,
        sources: [],
        createdAt: now(),
      });

      const reply: ChatMessage = {
        id: id(),
        role: 'Assistant',
        content:
          'This is a mock answer. Configure VITE_API_URL and run the backend to get real ' +
          'retrieval-augmented responses grounded in your uploaded documents.',
        sources: [
          {
            documentId: documents[0]?.id ?? 'doc',
            content: 'Sample retrieved context from your knowledge base.',
            score: 0.83,
            metadata: { file_name: documents[0]?.fileName ?? 'document.md' },
          },
        ],
        createdAt: now(),
      };
      session.messages.push(reply);
      return reply;
    },

    async generateQuiz(input: GenerateQuizInput) {
      const quiz: Quiz = {
        id: id(),
        title: input.topic ?? 'Generated quiz',
        topic: input.topic ?? 'general',
        moduleId: input.moduleId ?? null,
        status: 'Ready',
        createdAt: now(),
        questions: quizzes[0].questions.map((q) => ({ ...q, id: id() })),
      };
      quizzes.unshift(quiz);
      return quiz;
    },

    async listQuizzes(page = 1, pageSize = 20) {
      const start = (page - 1) * pageSize;
      return {
        items: quizzes.slice(start, start + pageSize),
        page,
        pageSize,
        totalCount: quizzes.length,
      };
    },

    async getQuiz(quizId) {
      const quiz = quizzes.find((q) => q.id === quizId);
      if (!quiz) throw new Error('Quiz not found');
      return quiz;
    },

    async startAttempt(quizId) {
      const quiz = quizzes.find((q) => q.id === quizId);
      if (!quiz) throw new Error('Quiz not found');
      const attempt: QuizAttempt = {
        id: id(),
        quizId,
        status: 'InProgress',
        answers: [],
        score: 0,
        maxScore: quiz.questions.reduce((sum, q) => sum + q.points, 0),
        startedAt: now(),
      };
      attempts.unshift(attempt);
      return attempt;
    },

    async submitAnswer(attemptId, input: SubmitAnswerInput) {
      const attempt = attempts.find((a) => a.id === attemptId);
      if (!attempt) throw new Error('Attempt not found');
      const quiz = quizzes.find((q) => q.id === attempt.quizId);
      const question = quiz?.questions.find((q) => q.id === input.questionId);
      if (!quiz || !question) throw new Error('Question not found');

      const correct =
        question.type === 'MultipleChoice'
          ? input.selectedOptionIndex === question.correctOptionIndex
          : (input.text ?? '').trim().length > 0;
      const answer: QuizAnswer = {
        questionId: question.id,
        selectedOptionIndex: input.selectedOptionIndex ?? null,
        text: input.text ?? null,
        isCorrect: correct,
        awardedPoints: correct ? question.points : 0,
        feedback: correct ? 'Correct.' : 'Not quite.',
      };
      attempt.answers.push(answer);
      attempt.score += answer.awardedPoints;
      if (attempt.answers.length >= quiz.questions.length) {
        attempt.status = 'Completed';
        attempt.completedAt = now();
      }
      return attempt;
    },

    async getAttempt(attemptId) {
      const attempt = attempts.find((a) => a.id === attemptId);
      if (!attempt) throw new Error('Attempt not found');
      return attempt;
    },

    async generatePlan(input: GeneratePlanInput) {
      const template = plans[plans.length - 1];
      const plan: LearningPlan = {
        id: id(),
        goal: input.goal,
        summary: 'A mock plan. Configure the backend for an agent-generated plan.',
        createdAt: now(),
        weeks: template.weeks.slice(0, input.weekCount ?? template.weeks.length).map((week) => ({
          ...week,
          tasks: week.tasks.map((task) => ({ ...task, id: id(), isComplete: false })),
        })),
      };
      plans.unshift(plan);
      return plan;
    },

    async listPlans(page = 1, pageSize = 20) {
      const start = (page - 1) * pageSize;
      return {
        items: plans.slice(start, start + pageSize),
        page,
        pageSize,
        totalCount: plans.length,
      };
    },

    async getPlan(planId) {
      const plan = plans.find((p) => p.id === planId);
      if (!plan) throw new Error('Plan not found');
      return plan;
    },

    async completeTask(planId, taskId) {
      const plan = plans.find((p) => p.id === planId);
      if (!plan) throw new Error('Plan not found');
      for (const week of plan.weeks) {
        const task = week.tasks.find((t) => t.id === taskId);
        if (task) task.isComplete = true;
      }
      return plan;
    },
  };
}
