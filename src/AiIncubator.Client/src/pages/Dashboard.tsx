import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useApi } from '../lib/ApiProvider';
import { Card } from '../components/ui/Card';
import { Badge } from '../components/ui/Badge';
import { Spinner } from '../components/ui/Spinner';
import type { DocumentItem } from '../lib/types';

interface Stats {
  documents: number;
  modules: number;
  sessions: number;
  recent: DocumentItem[];
}

export function Dashboard() {
  const api = useApi();
  const [stats, setStats] = useState<Stats | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const [docs, modules, sessions] = await Promise.all([
          api.listDocuments(1, 5),
          api.listModules(),
          api.listSessions(),
        ]);
        if (active) {
          setStats({
            documents: docs.totalCount,
            modules: modules.length,
            sessions: sessions.length,
            recent: docs.items,
          });
        }
      } catch (e) {
        if (active) {
          setError(e instanceof Error ? e.message : 'Failed to load dashboard');
        }
      }
    })();
    return () => {
      active = false;
    };
  }, [api]);

  if (error) {
    return <Card className="p-5 text-sm text-red-600">{error}</Card>;
  }

  if (!stats) {
    return (
      <div className="flex items-center gap-2 text-slate-500">
        <Spinner /> Loading…
      </div>
    );
  }

  const cards = [
    { label: 'Documents', value: stats.documents },
    { label: 'Knowledge modules', value: stats.modules },
    { label: 'Chat sessions', value: stats.sessions },
  ];

  return (
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-3">
        {cards.map((card) => (
          <Card key={card.label} className="p-5">
            <p className="text-sm text-slate-500">{card.label}</p>
            <p className="mt-2 text-3xl font-bold text-slate-900">{card.value}</p>
          </Card>
        ))}
      </div>

      <Card className="p-5">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-semibold text-slate-800">Recent documents</h2>
          <Link to="/app/documents" className="text-sm font-medium text-brand-600 hover:text-brand-700">
            View all
          </Link>
        </div>
        {stats.recent.length === 0 ? (
          <p className="text-sm text-slate-500">No documents yet. Upload one to get started.</p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {stats.recent.map((doc) => (
              <li key={doc.id} className="flex items-center justify-between py-3">
                <span className="text-sm font-medium text-slate-700">{doc.fileName}</span>
                <Badge tone={doc.status === 'Indexed' ? 'success' : 'neutral'}>{doc.status}</Badge>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
