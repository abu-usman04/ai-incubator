import { API_URL } from './config';
import type {
  ApiClient,
  ApiResponse,
  ChatMessage,
  ChatSession,
  DocumentItem,
  GeneratePlanInput,
  GenerateQuizInput,
  KnowledgeModule,
  LearningPlan,
  PagedResult,
  Quiz,
  QuizAttempt,
  SubmitAnswerInput,
} from './types';

type TokenGetter = () => Promise<string | null>;

async function request<T>(path: string, getToken: TokenGetter, init?: RequestInit): Promise<T> {
  const token = await getToken();
  const headers = new Headers(init?.headers);
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${API_URL}${path}`, { ...init, headers });
  const body = (await response.json().catch(() => null)) as ApiResponse<T> | null;

  if (!response.ok || !body || !body.success) {
    throw new Error(body?.error?.message ?? `Request failed (${response.status})`);
  }

  return body.data as T;
}

export function createHttpClient(getToken: TokenGetter): ApiClient {
  return {
    async health() {
      try {
        await request('/api/health', getToken);
        return true;
      } catch {
        return false;
      }
    },

    listDocuments(page = 1, pageSize = 20) {
      return request<PagedResult<DocumentItem>>(
        `/api/documents?page=${page}&pageSize=${pageSize}`,
        getToken,
      );
    },

    async uploadDocument(file, moduleId) {
      const form = new FormData();
      form.append('file', file);
      if (moduleId) {
        form.append('moduleId', moduleId);
      }

      const token = await getToken();
      const headers = new Headers();
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }

      const response = await fetch(`${API_URL}/api/documents`, {
        method: 'POST',
        body: form,
        headers,
      });
      const body = (await response.json().catch(() => null)) as ApiResponse<DocumentItem> | null;
      if (!response.ok || !body?.success) {
        throw new Error(body?.error?.message ?? `Upload failed (${response.status})`);
      }

      return body.data as DocumentItem;
    },

    async deleteDocument(id) {
      await request(`/api/documents/${id}`, getToken, { method: 'DELETE' });
    },

    listModules() {
      return request<KnowledgeModule[]>('/api/modules', getToken);
    },

    createModule(name, description) {
      return request<KnowledgeModule>('/api/modules', getToken, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, description }),
      });
    },

    syncModule(id) {
      return request<KnowledgeModule>(`/api/modules/${id}/sync`, getToken, { method: 'POST' });
    },

    listSessions() {
      return request<ChatSession[]>('/api/chat/sessions', getToken);
    },

    createSession(title) {
      return request<ChatSession>('/api/chat/sessions', getToken, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title }),
      });
    },

    getSession(id) {
      return request<ChatSession>(`/api/chat/sessions/${id}`, getToken);
    },

    sendMessage(sessionId, message, topK = 4) {
      return request<ChatMessage>(`/api/chat/sessions/${sessionId}/messages`, getToken, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message, topK }),
      });
    },

    generateQuiz(input: GenerateQuizInput) {
      return request<Quiz>('/api/quizzes', getToken, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(input),
      });
    },

    listQuizzes(page = 1, pageSize = 20) {
      return request<PagedResult<Quiz>>(`/api/quizzes?page=${page}&pageSize=${pageSize}`, getToken);
    },

    getQuiz(id: string) {
      return request<Quiz>(`/api/quizzes/${id}`, getToken);
    },

    startAttempt(quizId: string) {
      return request<QuizAttempt>(`/api/quizzes/${quizId}/attempts`, getToken, { method: 'POST' });
    },

    submitAnswer(attemptId: string, input: SubmitAnswerInput) {
      return request<QuizAttempt>(`/api/quizzes/attempts/${attemptId}/answers`, getToken, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(input),
      });
    },

    getAttempt(attemptId: string) {
      return request<QuizAttempt>(`/api/quizzes/attempts/${attemptId}`, getToken);
    },

    generatePlan(input: GeneratePlanInput) {
      return request<LearningPlan>('/api/learning-plans', getToken, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(input),
      });
    },

    listPlans(page = 1, pageSize = 20) {
      return request<PagedResult<LearningPlan>>(
        `/api/learning-plans?page=${page}&pageSize=${pageSize}`,
        getToken,
      );
    },

    getPlan(id: string) {
      return request<LearningPlan>(`/api/learning-plans/${id}`, getToken);
    },

    completeTask(planId: string, taskId: string) {
      return request<LearningPlan>(`/api/learning-plans/${planId}/tasks/${taskId}/complete`, getToken, {
        method: 'POST',
      });
    },
  };
}
