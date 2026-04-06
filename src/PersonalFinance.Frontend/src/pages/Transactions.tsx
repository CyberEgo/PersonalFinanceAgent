import { motion } from 'framer-motion';
import { ArrowUpRight, ArrowDownLeft, Search, Filter, Loader2 } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { usePaymentEvents } from '../hooks/usePaymentEvents';
import { cn } from '../lib/utils';
import { fetchAccounts, fetchTransactions } from '../lib/api';
import type { Transaction } from '../lib/types';

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.04 } },
};

const item = {
  hidden: { opacity: 0, y: 10 },
  show: { opacity: 1, y: 0 },
};

function relativeDate(ts: string) {
  const diff = Date.now() - new Date(ts).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return 'Just now';
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  const days = Math.floor(hrs / 24);
  return `${days}d ago`;
}

export default function Transactions() {
  const refreshKey = usePaymentEvents();
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('All');

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const accs = await fetchAccounts('arman.haeri@example.com');
        if (cancelled || accs.length === 0) return;
        const txns = await fetchTransactions(accs[0].id, 100);
        if (!cancelled) setTransactions(txns);
      } catch { /* degrade gracefully */ }
      finally { if (!cancelled) setLoading(false); }
    })();
    return () => { cancelled = true; };
  }, [refreshKey]);

  const categories = useMemo(() => {
    const cats = new Set(transactions.map(tx => tx.category));
    return ['All', ...Array.from(cats).sort()];
  }, [transactions]);

  const filtered = transactions.filter(tx => {
    const matchesSearch = tx.description.toLowerCase().includes(search.toLowerCase());
    const matchesCategory = category === 'All' || tx.category === category;
    return matchesSearch && matchesCategory;
  });

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-[var(--color-primary-light)]" />
      </div>
    );
  }

  return (
    <motion.div variants={container} initial="hidden" animate="show" className="space-y-6">
      <motion.div variants={item}>
        <h1 className="text-2xl font-bold">Transactions</h1>
        <p className="text-[var(--color-text-muted)] mt-1">View and filter your transaction history</p>
      </motion.div>

      {/* Filters */}
      <motion.div variants={item} className="flex flex-col sm:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Search transactions..."
            className="w-full pl-10 pr-4 py-2.5 rounded-xl glass text-sm outline-none focus:ring-2 focus:ring-[var(--color-primary)]/40 bg-transparent"
          />
        </div>
        <div className="flex items-center gap-2 overflow-x-auto pb-1">
          <Filter className="w-4 h-4 text-[var(--color-text-muted)] shrink-0" />
          {categories.map(cat => (
            <button
              key={cat}
              onClick={() => setCategory(cat)}
              className={cn(
                'px-3 py-1.5 text-xs rounded-full whitespace-nowrap transition-all',
                category === cat
                  ? 'bg-[var(--color-primary)] text-white'
                  : 'glass text-[var(--color-text-muted)] hover:text-[var(--color-text)]'
              )}
            >
              {cat}
            </button>
          ))}
        </div>
      </motion.div>

      {/* Transaction list */}
      <motion.div variants={item} className="glass rounded-2xl overflow-hidden">
        {filtered.length === 0 ? (
          <div className="p-12 text-center text-[var(--color-text-muted)]">
            No transactions found
          </div>
        ) : (
          <div className="divide-y divide-[var(--color-border)]">
            {filtered.map(tx => (
              <motion.div
                key={tx.id}
                variants={item}
                className="flex items-center justify-between p-4 hover:bg-[var(--color-surface-hover)] transition-colors"
              >
                <div className="flex items-center gap-4">
                  <div className={cn(
                    'w-10 h-10 rounded-xl flex items-center justify-center',
                    tx.flowType === 'income' ? 'bg-[var(--color-success)]/10' : 'bg-[var(--color-surface-hover)]'
                  )}>
                    {tx.flowType === 'income' ? (
                      <ArrowDownLeft className="w-5 h-5 text-[var(--color-success)]" />
                    ) : (
                      <ArrowUpRight className="w-5 h-5 text-[var(--color-text-muted)]" />
                    )}
                  </div>
                  <div>
                    <p className="text-sm font-medium">{tx.description}</p>
                    <p className="text-xs text-[var(--color-text-muted)]">
                      {tx.category} &middot; {tx.paymentType} &middot; {relativeDate(tx.timestamp)}
                    </p>
                  </div>
                </div>
                <div className="text-right">
                  <p className={cn(
                    'text-sm font-semibold',
                    tx.flowType === 'income' ? 'text-[var(--color-success)]' : 'text-[var(--color-text)]'
                  )}>
                    {tx.flowType === 'income' ? '+' : '-'}${Math.abs(tx.amount).toFixed(2)}
                  </p>
                  <span className={cn(
                    'text-[10px] font-medium px-2 py-0.5 rounded-full',
                    tx.status === 'paid' ? 'bg-[var(--color-success)]/10 text-[var(--color-success)]' : 'bg-[var(--color-warning)]/10 text-[var(--color-warning)]'
                  )}>
                    {tx.status}
                  </span>
                </div>
              </motion.div>
            ))}
          </div>
        )}
      </motion.div>
    </motion.div>
  );
}
