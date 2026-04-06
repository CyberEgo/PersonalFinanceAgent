import { Outlet, NavLink, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Wallet,
  ArrowLeftRight,
  MessageSquare,
  Bot,
  ChevronRight,
} from 'lucide-react';
import { cn } from '../lib/utils';
import FloatingChat from './FloatingChat';

const navItems = [
  { to: '/', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/accounts', icon: Wallet, label: 'Accounts' },
  { to: '/transactions', icon: ArrowLeftRight, label: 'Transactions' },
  { to: '/chat', icon: MessageSquare, label: 'AI Assistant' },
];

export default function Layout() {
  const location = useLocation();
  const isChatPage = location.pathname === '/chat';

  return (
    <div className="flex h-screen overflow-hidden">
      {/* Sidebar */}
      <aside className="glass w-64 flex flex-col shrink-0">
        {/* Logo */}
        <div className="p-6 border-b border-[var(--color-border)]">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-[var(--color-primary)] to-purple-500 flex items-center justify-center">
              <Bot className="w-6 h-6 text-white" />
            </div>
            <div>
              <h1 className="text-lg font-bold gradient-text">PersonalFinance</h1>
              <p className="text-xs text-[var(--color-text-muted)]">AI-Powered Finance</p>
            </div>
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 p-4 space-y-1">
          {navItems.map(({ to, icon: Icon, label }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all duration-200',
                  isActive
                    ? 'bg-[var(--color-primary)]/10 text-[var(--color-primary-light)] glow'
                    : 'text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-surface-hover)]',
                )
              }
            >
              <Icon className="w-5 h-5" />
              {label}
              <ChevronRight className="w-4 h-4 ml-auto opacity-0 group-hover:opacity-100 transition-opacity" />
            </NavLink>
          ))}
        </nav>

        {/* User info */}
        <div className="p-4 border-t border-[var(--color-border)]">
          <div className="flex items-center gap-3 px-3 py-2">
            <img src="/profile.jpg" alt="Arman Haeri" className="w-8 h-8 rounded-full object-cover" />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium truncate">Arman Haeri</p>
              <p className="text-xs text-[var(--color-text-muted)] truncate">arman.haeri@example.com</p>
            </div>
          </div>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-auto p-8">
        <Outlet />
      </main>

      {/* Floating chat – hidden on the dedicated chat page */}
      {!isChatPage && <FloatingChat />}
    </div>
  );
}
