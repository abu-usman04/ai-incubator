import { useEffect, useRef, useState } from 'react';
import { useApi } from '../lib/ApiProvider';
import { Card } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Spinner } from '../components/ui/Spinner';
import type { ChatMessage, ChatSession } from '../lib/types';

export function Chat() {
  const api = useApi();
  const [sessions, setSessions] = useState<ChatSession[]>([]);
  const [active, setActive] = useState<ChatSession | null>(null);
  const [draft, setDraft] = useState('');
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    void api.listSessions().then(setSessions).catch(() => undefined);
  }, [api]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [active?.messages.length]);

  const openSession = async (id: string) => {
    try {
      const session = await api.getSession(id);
      setActive(session);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to open session');
    }
  };

  const newSession = async () => {
    const session = await api.createSession();
    setSessions((current) => [session, ...current]);
    setActive(session);
  };

  const send = async () => {
    const message = draft.trim();
    if (!message) return;

    let session = active;
    if (!session) {
      session = await api.createSession();
      setSessions((current) => [session as ChatSession, ...current]);
    }

    const optimistic: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'User',
      content: message,
      sources: [],
      createdAt: new Date().toISOString(),
    };
    const withUser: ChatSession = { ...session, messages: [...session.messages, optimistic] };
    setActive(withUser);
    setDraft('');
    setSending(true);
    setError(null);

    try {
      const reply = await api.sendMessage(session.id, message);
      setActive((current) =>
        current ? { ...current, messages: [...current.messages, reply] } : current,
      );
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to send message');
    } finally {
      setSending(false);
    }
  };

  return (
    <div className="flex h-[calc(100vh-7rem)] gap-4">
      <Card className="flex w-64 shrink-0 flex-col p-3">
        <Button className="mb-3" onClick={newSession}>
          New chat
        </Button>
        <div className="flex-1 space-y-1 overflow-auto">
          {sessions.length === 0 ? (
            <p className="px-2 py-4 text-sm text-slate-400">No conversations yet.</p>
          ) : (
            sessions.map((session) => (
              <button
                key={session.id}
                onClick={() => openSession(session.id)}
                className={`w-full truncate rounded-lg px-3 py-2 text-left text-sm transition-colors ${
                  active?.id === session.id ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100'
                }`}
              >
                {session.title}
              </button>
            ))
          )}
        </div>
      </Card>

      <Card className="flex flex-1 flex-col overflow-hidden">
        <div className="flex-1 space-y-4 overflow-auto p-5">
          {!active || active.messages.length === 0 ? (
            <div className="flex h-full items-center justify-center text-center text-slate-400">
              <p>Ask a question about your documents to get started.</p>
            </div>
          ) : (
            active.messages.map((message) => (
              <div
                key={message.id}
                className={`flex ${message.role === 'User' ? 'justify-end' : 'justify-start'}`}
              >
                <div
                  className={`max-w-[80%] rounded-2xl px-4 py-2.5 text-sm ${
                    message.role === 'User'
                      ? 'bg-brand-600 text-white'
                      : 'bg-slate-100 text-slate-800'
                  }`}
                >
                  <p className="whitespace-pre-wrap">{message.content}</p>
                  {message.sources.length > 0 && (
                    <div className="mt-2 border-t border-slate-200 pt-2 text-xs text-slate-500">
                      <p className="font-medium">Sources</p>
                      <ul className="mt-1 space-y-1">
                        {message.sources.map((source, index) => (
                          <li key={`${source.documentId}-${index}`} className="truncate">
                            {source.metadata.file_name ?? source.documentId}
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              </div>
            ))
          )}
          {sending && (
            <div className="flex items-center gap-2 text-sm text-slate-400">
              <Spinner /> Thinking…
            </div>
          )}
          <div ref={bottomRef} />
        </div>

        {error && <p className="px-5 pb-2 text-sm text-red-600">{error}</p>}

        <div className="flex items-center gap-2 border-t border-slate-200 p-3">
          <Input
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                void send();
              }
            }}
            placeholder="Ask a question…"
            disabled={sending}
          />
          <Button onClick={() => void send()} disabled={sending || draft.trim().length === 0}>
            Send
          </Button>
        </div>
      </Card>
    </div>
  );
}
