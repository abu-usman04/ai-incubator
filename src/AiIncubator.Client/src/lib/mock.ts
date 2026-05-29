import type {
  ApiClient,
  ChatMessage,
  ChatSession,
  DocumentItem,
  KnowledgeModule,
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
  };
}
