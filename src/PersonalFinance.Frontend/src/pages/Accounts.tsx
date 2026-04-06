import { useEffect, useState } from 'react';
import { usePaymentEvents } from '../hooks/usePaymentEvents';
import { motion } from 'framer-motion';
import { Wallet, CreditCard, Users, Eye, Loader2 } from 'lucide-react';
import { fetchAccounts, fetchCards } from '../lib/api';
import type { Account, Card } from '../lib/types';

const CARD_GRADIENTS = [
  'from-[var(--color-primary)] to-purple-600',
  'from-emerald-500 to-teal-600',
  'from-amber-500 to-orange-600',
  'from-cyan-500 to-blue-600',
];

const container = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.08 } },
};

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 },
};

function fmt(n: number) {
  return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2 });
}

export default function Accounts() {
  const refreshKey = usePaymentEvents();
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [cards, setCards] = useState<Card[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const accs: Account[] = await fetchAccounts('arman.haeri@example.com');
        if (cancelled) return;
        setAccounts(accs);
        if (accs.length > 0) {
          const crds: Card[] = await fetchCards(accs[0].id);
          if (!cancelled) setCards(crds);
        }
      } catch { /* degrade gracefully */ }
      finally { if (!cancelled) setLoading(false); }
    })();
    return () => { cancelled = true; };
  }, [refreshKey]);

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
        <h1 className="text-2xl font-bold">Accounts</h1>
        <p className="text-[var(--color-text-muted)] mt-1">Manage your accounts, cards, and beneficiaries</p>
      </motion.div>

      {/* Account Overview */}
      {accounts.map((account) => (
        <motion.div key={account.id} variants={item} className="glass rounded-2xl p-6 glow">
          <div className="flex items-center gap-4 mb-6">
            <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-[var(--color-primary)] to-purple-500 flex items-center justify-center">
              <Wallet className="w-6 h-6 text-white" />
            </div>
            <div>
              <h2 className="text-lg font-semibold">{account.accountHolderFullName}</h2>
              <p className="text-sm text-[var(--color-text-muted)]">{account.id} &middot; {account.currency}</p>
            </div>
            <div className="ml-auto text-right">
              <p className="text-2xl font-bold">{fmt(account.balance)}</p>
              <p className="text-sm text-[var(--color-text-muted)]">Available Balance</p>
            </div>
          </div>
          <div className="grid grid-cols-3 gap-3">
            {account.paymentMethods.map((pm) => (
              <div key={pm.id} className="bg-[var(--color-surface-hover)] rounded-xl p-3">
                <p className="text-xs text-[var(--color-text-muted)]">{pm.type}</p>
                <p className="text-sm font-semibold mt-1">{fmt(pm.availableBalance)}</p>
                {pm.cardNumber && <p className="text-xs text-[var(--color-text-muted)] mt-1">{pm.cardNumber}</p>}
              </div>
            ))}
          </div>
        </motion.div>
      ))}

      {/* Cards */}
      <motion.div variants={item}>
        <div className="flex items-center gap-2 mb-4">
          <CreditCard className="w-5 h-5 text-[var(--color-primary-light)]" />
          <h2 className="text-lg font-semibold">Cards</h2>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {cards.map((card, i) => (
            <div key={card.id} className={`rounded-2xl p-5 bg-gradient-to-br ${CARD_GRADIENTS[i % CARD_GRADIENTS.length]} text-white relative overflow-hidden`}>
              <div className="absolute inset-0 bg-white/5 backdrop-blur-sm" style={{ clipPath: 'circle(80% at 100% 0%)' }} />
              <div className="relative z-10">
                <div className="flex items-center justify-between mb-8">
                  <p className="text-xs font-medium opacity-80 uppercase">{card.type}</p>
                  <Eye className="w-4 h-4 opacity-60" />
                </div>
                <p className="text-sm font-semibold mb-1">{card.name}</p>
                <p className="text-lg font-mono tracking-wider mb-4">{card.number}</p>
                <div className="flex justify-between items-end">
                  <div>
                    <p className="text-xs opacity-70">Balance</p>
                    <p className="text-sm font-semibold">{fmt(card.balance)}</p>
                  </div>
                  {card.limit > 0 && (
                    <div className="text-right">
                      <p className="text-xs opacity-70">Limit</p>
                      <p className="text-sm font-semibold">{fmt(card.limit)}</p>
                    </div>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      </motion.div>

      {/* Beneficiaries — still static since there's no API to list them from the frontend */}
      <motion.div variants={item}>
        <div className="flex items-center gap-2 mb-4">
          <Users className="w-5 h-5 text-[var(--color-primary-light)]" />
          <h2 className="text-lg font-semibold">Beneficiaries</h2>
        </div>
        <p className="text-sm text-[var(--color-text-muted)]">Ask the AI assistant to list your beneficiaries.</p>
      </motion.div>
    </motion.div>
  );
}
