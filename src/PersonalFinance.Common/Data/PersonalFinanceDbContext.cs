using Microsoft.EntityFrameworkCore;
using PersonalFinance.Common.Data.Entities;

namespace PersonalFinance.Common.Data;

public class PersonalFinanceDbContext(DbContextOptions<PersonalFinanceDbContext> options) : DbContext(options)
{
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<PaymentMethodEntity> PaymentMethods => Set<PaymentMethodEntity>();
    public DbSet<BeneficiaryEntity> Beneficiaries => Set<BeneficiaryEntity>();
    public DbSet<CardEntity> Cards => Set<CardEntity>();
    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PaymentMethodEntity>(e =>
        {
            e.HasKey(pm => pm.Id);
            e.Property(pm => pm.AvailableBalance).HasPrecision(18, 2);
            e.HasOne(pm => pm.Account).WithMany(a => a.PaymentMethods).HasForeignKey(pm => pm.AccountId);
        });

        modelBuilder.Entity<BeneficiaryEntity>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasOne(b => b.Account).WithMany(a => a.Beneficiaries).HasForeignKey(b => b.AccountId);
        });

        modelBuilder.Entity<CardEntity>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Balance).HasPrecision(18, 2);
            e.Property(c => c.Limit).HasPrecision(18, 2);
            e.HasOne(c => c.Account).WithMany(a => a.Cards).HasForeignKey(c => c.AccountId);
        });

        modelBuilder.Entity<TransactionEntity>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.HasOne(t => t.Account).WithMany(a => a.Transactions).HasForeignKey(t => t.AccountId);
        });

        modelBuilder.Entity<PaymentEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Amount).HasPrecision(18, 2);
            e.HasIndex(p => p.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");
            e.HasOne(p => p.Account).WithMany(a => a.Payments).HasForeignKey(p => p.AccountId);
        });
    }
}
