using Microsoft.EntityFrameworkCore;
using PersonalFinance.Common.Data;
using PersonalFinance.Common.Data.Entities;
using PersonalFinance.Common.Models;

namespace PersonalFinance.PaymentApi.Services;

public interface IPaymentService
{
    Task<Payment> ProcessPaymentAsync(PaymentRequest request);
    Task<List<Payment>> GetPaymentsAsync();
}

public class PaymentService(PersonalFinanceDbContext db) : IPaymentService
{
    public async Task<Payment> ProcessPaymentAsync(PaymentRequest request)
    {
        // SqlServerRetryingExecutionStrategy requires user transactions to be
        // wrapped inside ExecuteAsync so the entire unit can be retried.
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();

            // --- 0. Idempotency check ---
            // If an idempotency key is provided, reject duplicates immediately.
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var existing = await db.Payments
                    .FirstOrDefaultAsync(p => p.IdempotencyKey == request.IdempotencyKey);
                if (existing is not null)
                {
                    await tx.RollbackAsync();
                    return new Payment(existing.Id, existing.AccountId, existing.Amount, existing.Description,
                        existing.Timestamp, existing.RecipientName, existing.RecipientBankCode,
                        existing.PaymentType, existing.CardId, existing.Category, existing.Status);
                }
            }

            // --- 0b. Duplicate detection window (60 seconds) ---
            // Even without an idempotency key, reject if an identical payment
            // (same account, amount, description, recipient) was made recently.
            var cutoff = DateTimeOffset.UtcNow.AddSeconds(-60);
            var duplicate = await db.Payments.FirstOrDefaultAsync(p =>
                p.AccountId == request.AccountId &&
                p.Amount == request.Amount &&
                p.Description == request.Description &&
                p.RecipientName == request.RecipientName &&
                p.Timestamp >= cutoff);
            if (duplicate is not null)
            {
                await tx.RollbackAsync();
                return new Payment(duplicate.Id, duplicate.AccountId, duplicate.Amount, duplicate.Description,
                    duplicate.Timestamp, duplicate.RecipientName, duplicate.RecipientBankCode,
                    duplicate.PaymentType, duplicate.CardId, duplicate.Category, duplicate.Status);
            }

            // --- 1. Generate next payment ID ---
            var lastPayment = await db.Payments
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
            var nextPayNum = 1;
            if (lastPayment?.Id is { } lastId && lastId.StartsWith("PAY-") && int.TryParse(lastId[4..], out var n))
                nextPayNum = n + 1;
            var paymentId = $"PAY-{nextPayNum:D4}";

            var status = request.PaymentType switch
            {
                "BankTransfer" => "pending",
                "CreditCard" => "paid",
                "DirectDebit" => "paid",
                _ => request.Status
            };

            var timestamp = DateTimeOffset.UtcNow;

            // --- 2. Insert payment record ---
            var entity = new PaymentEntity
            {
                Id = paymentId,
                AccountId = request.AccountId,
                Amount = request.Amount,
                Description = request.Description,
                Timestamp = timestamp,
                RecipientName = request.RecipientName,
                RecipientBankCode = request.RecipientBankCode,
                PaymentType = request.PaymentType,
                CardId = request.CardId,
                Category = request.Category,
                Status = status,
                IdempotencyKey = request.IdempotencyKey
            };
            db.Payments.Add(entity);

            // --- 3. Insert a matching transaction so it shows in history ---
            var lastTxn = await db.Transactions
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();
            var nextTxnNum = 1;
            if (lastTxn?.Id is { } lastTxnId && lastTxnId.StartsWith("TXN-") && int.TryParse(lastTxnId[4..], out var tn))
                nextTxnNum = tn + 1;

            db.Transactions.Add(new TransactionEntity
            {
                Id = $"TXN-{nextTxnNum:D3}",
                AccountId = request.AccountId,
                Description = request.Description,
                Type = "payment",
                FlowType = "outcome",
                RecipientName = request.RecipientName,
                Amount = request.Amount,
                Timestamp = timestamp,
                PaymentType = request.PaymentType,
                CardId = request.CardId,
                Category = request.Category ?? "General",
                Status = status
            });

            // --- 4. Deduct from account balance ---
            var account = await db.Accounts.FindAsync(request.AccountId);
            if (account is not null)
            {
                account.Balance -= request.Amount;
            }

            // --- 5. Update card balance (credit card) or matching payment method ---
            // The CardId may be a Card ID (e.g. "CARD-001") or a PaymentMethod ID (e.g. "PM-001").
            // Try both lookups to handle either case.
            if (request.CardId is not null)
            {
                var card = await db.Cards.FindAsync(request.CardId);
                if (card is null)
                {
                    // CardId might be a PaymentMethod ID — resolve the linked card via card number
                    var pm = await db.PaymentMethods.FindAsync(request.CardId);
                    if (pm?.CardNumber is not null)
                    {
                        var lastFour = pm.CardNumber.Replace("****", "");
                        card = await db.Cards.FirstOrDefaultAsync(c =>
                            c.AccountId == request.AccountId && c.Number.EndsWith(lastFour));
                    }
                }
                if (card is not null)
                {
                    card.Balance -= request.Amount;
                }
            }

            // Update the PaymentMethod available balance that matches the payment type
            PaymentMethodEntity? paymentMethod;
            if (request.CardId is not null)
            {
                // First try direct lookup (CardId may be a PaymentMethod ID)
                paymentMethod = await db.PaymentMethods.FindAsync(request.CardId);
                if (paymentMethod is null)
                {
                    // CardId is a Card ID — find the PaymentMethod by matching card number
                    var linkedCard = await db.Cards.FindAsync(request.CardId);
                    if (linkedCard?.Number is not null)
                    {
                        var lastFour = linkedCard.Number[^4..];
                        paymentMethod = await db.PaymentMethods.FirstOrDefaultAsync(pm =>
                            pm.AccountId == request.AccountId && pm.CardNumber != null && pm.CardNumber.EndsWith(lastFour));
                    }
                }
            }
            else
            {
                paymentMethod = await db.PaymentMethods.FirstOrDefaultAsync(pm =>
                    pm.AccountId == request.AccountId && pm.Type == request.PaymentType);
            }

            if (paymentMethod is not null)
            {
                paymentMethod.AvailableBalance -= request.Amount;
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return new Payment(entity.Id, entity.AccountId, entity.Amount, entity.Description, entity.Timestamp,
                entity.RecipientName, entity.RecipientBankCode, entity.PaymentType, entity.CardId, entity.Category, entity.Status);
        });
    }

    public async Task<List<Payment>> GetPaymentsAsync()
        => await db.Payments
            .Select(p => new Payment(p.Id, p.AccountId, p.Amount, p.Description, p.Timestamp,
                p.RecipientName, p.RecipientBankCode, p.PaymentType, p.CardId, p.Category, p.Status))
            .ToListAsync();
}
