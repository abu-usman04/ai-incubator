import { useEffect, useState } from 'react';
import { useApi } from '../lib/ApiProvider';
import { Card } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Badge } from '../components/ui/Badge';
import { Spinner } from '../components/ui/Spinner';
import type { LearningPlan as PlanModel, PlanTaskType } from '../lib/types';

const taskTone: Record<PlanTaskType, 'neutral' | 'info' | 'success'> = {
  Read: 'neutral',
  Quiz: 'info',
  Chat: 'success',
};

function progress(plan: PlanModel): { done: number; total: number } {
  const tasks = plan.weeks.flatMap((week) => week.tasks);
  return { done: tasks.filter((task) => task.isComplete).length, total: tasks.length };
}

export function LearningPlan() {
  const api = useApi();
  const [plans, setPlans] = useState<PlanModel[]>([]);
  const [active, setActive] = useState<PlanModel | null>(null);
  const [goal, setGoal] = useState('');
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const refresh = async () => {
    try {
      const result = await api.listPlans(1, 50);
      setPlans(result.items);
      if (!active && result.items.length > 0) {
        setActive(result.items[0]);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load plans');
    }
  };

  useEffect(() => {
    void refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api]);

  const generate = async () => {
    if (!goal.trim()) return;
    setGenerating(true);
    setError(null);
    try {
      const plan = await api.generatePlan({ goal: goal.trim(), weekCount: 4 });
      setGoal('');
      setActive(plan);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to generate plan');
    } finally {
      setGenerating(false);
    }
  };

  const completeTask = async (taskId: string) => {
    if (!active) return;
    try {
      const updated = await api.completeTask(active.id, taskId);
      setActive(updated);
      setPlans((current) => current.map((plan) => (plan.id === updated.id ? updated : plan)));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to update task');
    }
  };

  return (
    <div className="space-y-6">
      <Card className="flex items-end gap-3 p-5">
        <div className="flex-1">
          <h2 className="font-semibold text-slate-800">Generate a learning plan</h2>
          <p className="mb-2 mt-1 text-sm text-slate-500">
            The planning agent builds weeks of tasks from your knowledge base.
          </p>
          <Input
            value={goal}
            onChange={(e) => setGoal(e.target.value)}
            placeholder="Your goal, e.g. ramp up on support runbooks"
            onKeyDown={(e) => e.key === 'Enter' && void generate()}
          />
        </div>
        <Button disabled={generating || !goal.trim()} onClick={() => void generate()}>
          {generating ? <Spinner className="border-white/40 border-t-white" /> : null}
          {generating ? 'Planning…' : 'Generate'}
        </Button>
      </Card>

      {error && <Card className="p-4 text-sm text-red-600">{error}</Card>}

      <div className="flex gap-4">
        <Card className="w-64 shrink-0 p-3">
          <h3 className="px-2 pb-2 text-sm font-semibold text-slate-700">Plans</h3>
          {plans.length === 0 ? (
            <p className="px-2 py-2 text-sm text-slate-400">No plans yet.</p>
          ) : (
            plans.map((plan) => {
              const { done, total } = progress(plan);
              return (
                <button
                  key={plan.id}
                  onClick={() => setActive(plan)}
                  className={`w-full rounded-lg px-3 py-2 text-left text-sm transition-colors ${
                    active?.id === plan.id ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100'
                  }`}
                >
                  <span className="block truncate font-medium">{plan.goal}</span>
                  <span className="text-xs text-slate-400">
                    {done}/{total} done
                  </span>
                </button>
              );
            })
          )}
        </Card>

        <div className="flex-1">
          {!active ? (
            <Card className="p-6 text-sm text-slate-500">Select or generate a plan to see its weeks.</Card>
          ) : (
            <div className="space-y-4">
              {active.summary && <Card className="p-4 text-sm text-slate-600">{active.summary}</Card>}
              {active.weeks.map((week) => (
                <Card key={week.weekNumber} className="p-5">
                  <div className="mb-3 flex items-center gap-2">
                    <Badge tone="info">Week {week.weekNumber}</Badge>
                    <h3 className="font-semibold text-slate-800">{week.theme}</h3>
                  </div>
                  <ul className="space-y-2">
                    {week.tasks.map((task) => (
                      <li
                        key={task.id}
                        className="flex items-center justify-between rounded-lg border border-slate-100 px-3 py-2"
                      >
                        <div className="flex items-center gap-2">
                          <Badge tone={taskTone[task.type]}>{task.type}</Badge>
                          <span
                            className={`text-sm ${
                              task.isComplete ? 'text-slate-400 line-through' : 'text-slate-700'
                            }`}
                          >
                            {task.title}
                          </span>
                        </div>
                        {!task.isComplete && (
                          <Button variant="ghost" onClick={() => void completeTask(task.id)}>
                            Mark done
                          </Button>
                        )}
                      </li>
                    ))}
                  </ul>
                </Card>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
