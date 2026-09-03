using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using POSSystem.Models;

namespace POSSystem.Models
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<BusinessSetting> BusinessSettings { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<SaleReturn> SaleReturns { get; set; }
        public DbSet<SaleReturnItem> SaleReturnItems { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Product>().HasIndex(p => p.Sku).IsUnique();

            // Barcode unique hai jab diya gaya ho -- nullable column pe unique index SQL Server mein
            // khud multiple NULLs allow karta hai, sirf actual duplicate barcodes reject hote hain
            builder.Entity<Product>().HasIndex(p => p.Barcode).IsUnique();

            // SaleReturnItem -> SaleItem: NO cascade delete (SaleReturn -> SaleReturnItem cascade rehne do,
            // isse cascade path clash SQL Server mein resolve ho jata hai)
            builder.Entity<SaleReturnItem>()
                .HasOne(sri => sri.SaleItem)
                .WithMany()
                .HasForeignKey(sri => sri.SaleItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}