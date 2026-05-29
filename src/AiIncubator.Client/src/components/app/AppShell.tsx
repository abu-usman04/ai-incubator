import { useLocation, Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Header } from './Header';

const titles: Record<string, string> = {
  '/app': 'Dashboard',
  '/app/documents': 'Documents',
  '/app/chat': 'Chat with your knowledge base',
};

export function AppShell() {
  const { pathname } = useLocation();
  const title = titles[pathname] ?? 'Ilm AI';

  return (
    <div className="flex h-full">
      <Sidebar />
      <div className="flex flex-1 flex-col overflow-hidden">
        <Header title={title} />
        <main className="flex-1 overflow-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
