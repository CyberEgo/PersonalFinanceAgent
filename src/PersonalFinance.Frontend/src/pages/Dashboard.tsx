import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import {
  Wallet,
  ArrowUpRight,
  ArrowDownLeft,
  CreditCard,
  Loader2,
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { fetchAccounts, fetchTransactions, fetchCards } from '../lib/api';
import type { Account, Transaction, Card } from '../lib/types';
import { usePaymentEvents } from '../hooks/usePaymentEvents';

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.08 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

function formatCurrency(n: number) {
  return '$' + Math.abs(n).toLocaleString('en-US', { minimumFractionDigits: 2 });
}

function relativeDate(ts: string) {
  const diff = Date.now() - new Date(ts).getTime();
  const days = Math.floor(diff / 86_400_000);
  if (days === 0) return 'Today';
  if (days === 1) return 'Yesterday';
  return `${days} days ago`;
}

export default function Dashboard() {
  const navigate = useNavigate();
  const refreshKey = usePaymentEvents();
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [cards, setCards] = useState<Card[]>([]);
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const accs: Account[] = await fetchAccounts('arman.haeri@example.com');
        if (cancelled) return;
        setAccounts(accs);
        if (accs.length > 0) {
          const [txns, crds] = await Promise.all([
            fetchTransactions(accs[0].id, 5),
            fetchCards(accs[0].id),
          ]);
          if (cancelled) return;
          setTransactions(txns);
          setCards(crds);
        }
      } catch { /* silently degrade to empty */ }
      finally { if (!cancelled) setLoading(false); }
    })();
    return () => { cancelled = true; };
  }, [refreshKey]);

  const totalBalance = accounts.reduce((s, a) => s + a.balance, 0);
  const income = transactions.filter(t => t.flowType === 'income').reduce((s, t) => s + t.amount, 0);
  const expenses = transactions.filter(t => t.flowType === 'outcome').reduce((s, t) => s + t.amount, 0);
  const activeCards = cards.filter(c => c.status === 'active').length;

  const stats = [
    { label: 'Total Balance', value: formatCurrency(totalBalance), icon: Wallet, gradient: 'from-[var(--color-primary)] to-purple-500' },
    { label: 'Income (Recent)', value: '+' + formatCurrency(income), icon: ArrowDownLeft, gradient: 'from-emerald-500 to-green-500' },
    { label: 'Expenses (Recent)', value: '-' + formatCurrency(expenses), icon: ArrowUpRight, gradient: 'from-amber-500 to-orange-500' },
    { label: 'Active Cards', value: String(activeCards), icon: CreditCard, gradient: 'from-cyan-500 to-blue-500' },
  ];

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-[var(--color-primary-light)]" />
      </div>
    );
  }

  return (
    <motion.div variants={container} initial="hidden" animate="show" className="space-y-8">
      <motion.div variants={item}>
        <h1 className="text-2xl font-bold">Welcome back, {accounts[0]?.accountHolderFullName?.split(' ')[0] ?? 'John'}</h1>
        <p className="text-[var(--color-text-muted)] mt-1">
          Here&apos;s what&apos;s happening with your finances
        </p>
      </motion.div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {stats.map(stat => (
          <motion.div key={stat.label} variants={item} className="glass rounded-2xl p-5 glow">
            <div className="flex items-center justify-between mb-4">
              <div
                className={`w-10 h-10 rounded-xl bg-gradient-to-br ${stat.gradient} flex items-center justify-center`}
              >
                <stat.icon className="w-5 h-5 text-white" />
              </div>
            </div>
            <p className="text-2xl font-bold">{stat.value}</p>
            <p className="text-sm text-[var(--color-text-muted)] mt-1">{stat.label}</p>
          </motion.div>
        ))}
      </div>

      {/* Recent Transactions */}
      <div>
        <motion.div variants={item} className="glass rounded-2xl p-6">
          <div className="flex items-center justify-between mb-5">
            <h2 className="text-lg font-semibold">Recent Transactions</h2>
            <button
              onClick={() => navigate('/transactions')}
              className="text-sm text-[var(--color-primary-light)] hover:underline"
            >
              View All
            </button>
          </div>
          <div className="space-y-3">
            {transactions.map(tx => (
              <div
                key={tx.id}
                className="flex items-center justify-between py-3 border-b border-[var(--color-border)] last:border-0"
              >
                <div className="flex items-center gap-3">
                  <div
                    className={`w-9 h-9 rounded-lg flex items-center justify-center ${
                      tx.flowType === 'income'
                        ? 'bg-[var(--color-success)]/10'
                        : 'bg-[var(--color-surface-hover)]'
                    }`}
                  >
                    {tx.flowType === 'income' ? (
                      <ArrowDownLeft className="w-4 h-4 text-[var(--color-success)]" />
                    ) : (
                      <ArrowUpRight className="w-4 h-4 text-[var(--color-text-muted)]" />
                    )}
                  </div>
                  <div>
                    <p className="text-sm font-medium">{tx.description}</p>
                    <p className="text-xs text-[var(--color-text-muted)]">
                      {tx.category} &middot; {relativeDate(tx.timestamp)}
                    </p>
                  </div>
                </div>
                <span
                  className={`text-sm font-semibold ${
                    tx.flowType === 'income' ? 'text-[var(--color-success)]' : 'text-[var(--color-text)]'
                  }`}
                >
                  {tx.flowType === 'income' ? '+' : '-'}{formatCurrency(tx.amount)}
                </span>
              </div>
            ))}
            {transactions.length === 0 && (
              <p className="text-sm text-[var(--color-text-muted)] text-center py-4">No transactions yet</p>
            )}
          </div>
        </motion.div>
      </div>
    </motion.div>
  );
}
