import { NavLink } from 'react-router-dom';

const links = [
  { to: '/app', label: 'Dashboard', end: true },
  { to: '/app/documents', label: 'Documents', end: false },
  { to: '/app/chat', label: 'Chat', end: false },
];

export function Sidebar() {
  return (
    <aside className="flex w-60 shrink-0 flex-col border-r border-slate-200 bg-white">
      <div className="flex h-16 items-center gap-2 border-b border-slate-200 px-5">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-600 text-sm font-bold text-white">
          I
        </div>
        <span className="font-semibold text-slate-800">Ilm AI</span>
      </div>
      <nav className="flex flex-1 flex-col gap-1 p-3">
        {links.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            end={link.end}
            className={({ isActive }) =>
              `rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                isActive ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100'
              }`
            }
          >
            {link.label}
          </NavLink>
        ))}
      </nav>
      <div className="p-4 text-xs text-slate-400">Knowledge Base Platform</div>
    </aside>
  );
}
