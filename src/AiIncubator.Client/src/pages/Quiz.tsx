import { useEffect, useState } from 'react';
import { useApi } from '../lib/ApiProvider';
import { Card } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Badge } from '../components/ui/Badge';
import { Spinner } from '../components/ui/Spinner';
import type { Quiz as QuizModel, QuizAttempt, SubmitAnswerInput } from '../lib/types';

type Mode = 'browse' | 'attempt' | 'result';

export function Quiz() {
  const api = useApi();
  const [quizzes, setQuizzes] = useState<QuizModel[]>([]);
  const [topic, setTopic] = useState('');
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [mode, setMode] = useState<Mode>('browse');
  const [activeQuiz, setActiveQuiz] = useState<QuizModel | null>(null);
  const [attempt, setAttempt] = useState<QuizAttempt | null>(null);
  const [index, setIndex] = useState(0);
  const [selected, setSelected] = useState<number | null>(null);
  const [textDraft, setTextDraft] = useState('');
  const [feedback, setFeedback] = useState<string | null>(null);

  const refresh = async () => {
    try {
      const result = await api.listQuizzes(1, 50);
      setQuizzes(result.items);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load quizzes');
    }
  };

  useEffect(() => {
    void refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api]);

  const generate = async () => {
    if (!topic.trim()) return;
    setGenerating(true);
    setError(null);
    try {
      await api.generateQuiz({ topic: topic.trim(), questionCount: 5 });
      setTopic('');
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to generate quiz');
    } finally {
      setGenerating(false);
    }
  };

  const start = async (quiz: QuizModel) => {
    setError(null);
    try {
      const loaded = await api.getQuiz(quiz.id);
      const started = await api.startAttempt(quiz.id);
      setActiveQuiz(loaded);
      setAttempt(started);
      setIndex(0);
      setSelected(null);
      setTextDraft('');
      setFeedback(null);
      setMode('attempt');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to start quiz');
    }
  };

  const submit = async () => {
    if (!activeQuiz || !attempt) return;
    const question = activeQuiz.questions[index];
    if (question.type === 'MultipleChoice' && selected === null) return;
    if (question.type === 'ShortAnswer' && !textDraft.trim()) return;

    const input: SubmitAnswerInput =
      question.type === 'MultipleChoice'
        ? { questionId: question.id, selectedOptionIndex: selected ?? undefined }
        : { questionId: question.id, text: textDraft.trim() };

    try {
      const updated = await api.submitAnswer(attempt.id, input);
      setAttempt(updated);
      setFeedback(updated.answers[updated.answers.length - 1]?.feedback ?? null);
      if (updated.status === 'Completed') {
        setMode('result');
        return;
      }
      setIndex((current) => current + 1);
      setSelected(null);
      setTextDraft('');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to submit answer');
    }
  };

  if (mode === 'attempt' && activeQuiz && attempt) {
    const question = activeQuiz.questions[index];
    return (
      <div className="mx-auto max-w-2xl space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="font-semibold text-slate-800">{activeQuiz.title}</h2>
          <Badge tone="info">
            Score {attempt.score}/{attempt.maxScore}
          </Badge>
        </div>
        {feedback && <Card className="p-3 text-sm text-slate-600">{feedback}</Card>}
        <Card className="space-y-4 p-5">
          <p className="text-xs text-slate-400">
            Question {index + 1} of {activeQuiz.questions.length}
          </p>
          <p className="font-medium text-slate-800">{question.prompt}</p>
          {question.type === 'MultipleChoice' ? (
            <div className="space-y-2">
              {question.options.map((option, optionIndex) => (
                <button
                  key={option}
                  onClick={() => setSelected(optionIndex)}
                  className={`w-full rounded-lg border px-3 py-2 text-left text-sm transition-colors ${
                    selected === optionIndex
                      ? 'border-brand-500 bg-brand-50 text-brand-700'
                      : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                  }`}
                >
                  {option}
                </button>
              ))}
            </div>
          ) : (
            <Input
              value={textDraft}
              onChange={(e) => setTextDraft(e.target.value)}
              placeholder="Type your answer…"
            />
          )}
          <Button onClick={() => void submit()}>Submit answer</Button>
        </Card>
      </div>
    );
  }

  if (mode === 'result' && activeQuiz && attempt) {
    return (
      <div className="mx-auto max-w-2xl space-y-4">
        <Card className="space-y-3 p-6 text-center">
          <h2 className="text-lg font-semibold text-slate-800">Quiz complete</h2>
          <p className="text-3xl font-bold text-brand-600">
            {attempt.score}/{attempt.maxScore}
          </p>
          <Button variant="secondary" onClick={() => setMode('browse')}>
            Back to quizzes
          </Button>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <Card className="flex items-end gap-3 p-5">
        <div className="flex-1">
          <h2 className="font-semibold text-slate-800">Generate a quiz</h2>
          <p className="mb-2 mt-1 text-sm text-slate-500">Grounded in your indexed documents.</p>
          <Input
            value={topic}
            onChange={(e) => setTopic(e.target.value)}
            placeholder="Topic, e.g. onboarding"
            onKeyDown={(e) => e.key === 'Enter' && void generate()}
          />
        </div>
        <Button disabled={generating || !topic.trim()} onClick={() => void generate()}>
          {generating ? <Spinner className="border-white/40 border-t-white" /> : null}
          {generating ? 'Generating…' : 'Generate'}
        </Button>
      </Card>

      {error && <Card className="p-4 text-sm text-red-600">{error}</Card>}

      <Card className="p-5">
        <h2 className="mb-4 font-semibold text-slate-800">Your quizzes</h2>
        {quizzes.length === 0 ? (
          <p className="text-sm text-slate-500">No quizzes yet. Generate one above.</p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {quizzes.map((quiz) => (
              <li key={quiz.id} className="flex items-center justify-between py-3">
                <div>
                  <p className="font-medium text-slate-700">{quiz.title}</p>
                  <p className="text-xs text-slate-400">{quiz.questions.length} questions</p>
                </div>
                <Button variant="secondary" onClick={() => void start(quiz)}>
                  Start
                </Button>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
