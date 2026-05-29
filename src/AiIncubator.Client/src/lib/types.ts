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
}
