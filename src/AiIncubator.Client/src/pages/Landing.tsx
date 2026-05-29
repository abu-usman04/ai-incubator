import { Link } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { clerkEnabled } from '../lib/config';

const features = [
  { title: 'Upload documents', body: 'Bring your text and markdown knowledge. Files are parsed and chunked automatically.' },
  { title: 'Embeddings & vectors', body: 'Each chunk is embedded and stored in a Qdrant vector index for fast semantic search.' },
  { title: 'Chat with sources', body: 'Ask questions and get clear answers grounded in your documents, with the sources shown alongside.' },
  { title: 'Knowledge modules', body: 'Group related documents into modules and sync them as your knowledge grows.' },
];

export function Landing() {
  const primaryHref = clerkEnabled ? '/sign-in' : '/app';

  return (
    <div className="min-h-full">
      <header className="mx-auto flex max-w-6xl items-center justify-between px-6 py-6">
        <div className="flex items-center gap-2">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-600 text-sm font-bold text-white">
            I
          </div>
          <span className="text-lg font-semibold text-slate-800">Ilm AI</span>
        </div>
        <Link to={primaryHref}>
          <Button variant="secondary">Sign in</Button>
        </Link>
      </header>

      <section className="mx-auto max-w-6xl px-6 pb-10 pt-12 text-center">
        <span className="inline-flex items-center rounded-full bg-brand-50 px-3 py-1 text-sm font-medium text-brand-700">
          Retrieval-Augmented Knowledge Base
        </span>
        <h1 className="mx-auto mt-6 max-w-3xl text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl">
          Turn your documents into an AI you can talk to
        </h1>
        <p className="mx-auto mt-4 max-w-2xl text-lg text-slate-600">
          Upload your knowledge, generate embeddings, and chat with an assistant that answers from
          your own content — with sources you can verify.
        </p>
        <div className="mt-8 flex items-center justify-center gap-3">
          <Link to={primaryHref}>
            <Button className="px-6 py-3 text-base">Get started</Button>
          </Link>
          <a href="https://github.com/ai-incubator-org/AI-mentorship-program" target="_blank" rel="noreferrer">
            <Button variant="ghost" className="px-6 py-3 text-base">
              Learn more
            </Button>
          </a>
        </div>
      </section>

      <section className="mx-auto grid max-w-6xl gap-4 px-6 pb-20 sm:grid-cols-2 lg:grid-cols-4">
        {features.map((feature) => (
          <Card key={feature.title} className="p-5">
            <h3 className="font-semibold text-slate-800">{feature.title}</h3>
            <p className="mt-2 text-sm text-slate-600">{feature.body}</p>
          </Card>
        ))}
      </section>
    </div>
  );
}
