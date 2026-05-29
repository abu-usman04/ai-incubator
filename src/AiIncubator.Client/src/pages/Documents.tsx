import { useEffect, useRef, useState } from 'react';
import { useApi } from '../lib/ApiProvider';
import { Card } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { Badge } from '../components/ui/Badge';
import { Spinner } from '../components/ui/Spinner';
import type { DocumentItem, DocumentStatus } from '../lib/types';

const statusTone: Record<DocumentStatus, 'success' | 'warning' | 'danger' | 'neutral'> = {
  Indexed: 'success',
  Processing: 'warning',
  Pending: 'neutral',
  Failed: 'danger',
};

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function Documents() {
  const api = useApi();
  const fileRef = useRef<HTMLInputElement>(null);
  const [documents, setDocuments] = useState<DocumentItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refresh = async () => {
    setError(null);
    try {
      const result = await api.listDocuments(1, 50);
      setDocuments(result.items);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load documents');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api]);

  const onFileSelected = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;
    setUploading(true);
    setError(null);
    try {
      await api.uploadDocument(file);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Upload failed');
    } finally {
      setUploading(false);
      if (fileRef.current) fileRef.current.value = '';
    }
  };

  const onDelete = async (id: string) => {
    try {
      await api.deleteDocument(id);
      setDocuments((current) => current.filter((d) => d.id !== id));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Delete failed');
    }
  };

  return (
    <div className="space-y-6">
      <Card className="flex items-center justify-between p-5">
        <div>
          <h2 className="font-semibold text-slate-800">Upload a document</h2>
          <p className="mt-1 text-sm text-slate-500">Supported formats: .txt, .md</p>
        </div>
        <div>
          <input
            ref={fileRef}
            type="file"
            accept=".txt,.md"
            className="hidden"
            onChange={onFileSelected}
          />
          <Button disabled={uploading} onClick={() => fileRef.current?.click()}>
            {uploading ? <Spinner className="border-white/40 border-t-white" /> : null}
            {uploading ? 'Uploading…' : 'Upload'}
          </Button>
        </div>
      </Card>

      {error && <Card className="p-4 text-sm text-red-600">{error}</Card>}

      <Card className="p-5">
        <h2 className="mb-4 font-semibold text-slate-800">Your documents</h2>
        {loading ? (
          <div className="flex items-center gap-2 text-slate-500">
            <Spinner /> Loading…
          </div>
        ) : documents.length === 0 ? (
          <p className="text-sm text-slate-500">No documents yet.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-400">
                <th className="pb-2 font-medium">Name</th>
                <th className="pb-2 font-medium">Status</th>
                <th className="pb-2 font-medium">Chunks</th>
                <th className="pb-2 font-medium">Size</th>
                <th className="pb-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {documents.map((doc) => (
                <tr key={doc.id}>
                  <td className="py-3 font-medium text-slate-700">{doc.fileName}</td>
                  <td className="py-3">
                    <Badge tone={statusTone[doc.status]}>{doc.status}</Badge>
                  </td>
                  <td className="py-3 text-slate-500">{doc.chunkCount}</td>
                  <td className="py-3 text-slate-500">{formatSize(doc.sizeBytes)}</td>
                  <td className="py-3 text-right">
                    <Button variant="danger" onClick={() => onDelete(doc.id)}>
                      Delete
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}
