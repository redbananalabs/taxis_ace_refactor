using TaxiDispatch.Configuration;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace TaxiDispatch.Data
{
    public class TaxiDispatchContext : IdentityDbContext<AppUser, AppRole, int>
    {
        public TaxiDispatchContext(DbContextOptions<TaxiDispatchContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AppRefreshToken> AppRefreshTokens { get; set; }
        public virtual DbSet<TenantUser> TenantUsers { get; set; }
        public virtual DbSet<DriverUserProfile> DriverUserProfiles { get; set; }
        public virtual DbSet<AccountUserLink> AccountUserLinks { get; set; }
        public virtual DbSet<UserDeviceRegistration> UserDeviceRegistrations { get; set; }

        public virtual DbSet<Booking> Bookings { get; set; }
        public virtual DbSet<BookingVia> BookingVias { get; set; }
        public virtual DbSet<BookingChangeAudit> BookingChangeAudits { get; set; }
        public virtual DbSet<Tariff> Tariffs { get; set; }
        public virtual DbSet<LocalPOI> LocalPOIs { get; set; }
        public virtual DbSet<UserProfile> UserProfiles { get; set; }
        public virtual DbSet<DriverInvoiceStatement> DriverInvoiceStatements { get; set; }
        public virtual DbSet<DriverAvailability> DriverAvailabilities { get; set; }
        public virtual DbSet<DriverLocationHistory> DriverLocationHistories { get; set; }
        public virtual DbSet<DriverMessage> DriverMessages { get; set; }
        public virtual DbSet<DriverAllocation> DriverAllocations { get; set; }
        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<AccountInvoice> AccountInvoices { get; set; }
        public virtual DbSet<CompanyConfig> CompanyConfig { get; set; }
        public virtual DbSet<MessagingNotifyConfig> MessagingNotifyConfig { get; set; }
        public virtual DbSet<ReviewRequest> ReviewRequests { get; set; }
        public virtual DbSet<DriverAvailabilityAudit> DriverAvailabilityAudits { get; set; }
        public virtual DbSet<TurnDown> TurnDowns { get; set; }
        public virtual DbSet<WebBooking> WebBookings { get; set; }
        public virtual DbSet<AccountPassenger> AccountPassengers { get; set; }
        public virtual DbSet<DriverOnShift> DriversOnShift { get; set; }
        public virtual DbSet<DriverExpense> DriverExpenses { get; set; }
        public virtual DbSet<DriverShiftLog> DriversShiftLogs { get; set; }
        public virtual DbSet<UINotification> UINotifications { get; set; }
        public virtual DbSet<WebAmendmentRequest> WebAmendmentRequests { get; set; }
        public virtual DbSet<DocumentExpiry> DocumentExpirys { get; set; }
        public virtual DbSet<UserActionLog> UserActionsLog { get; set; }
        public virtual DbSet<JobOffer> JobOffers { get; set; }
        public virtual DbSet<UrlMapping> UrlMappings { get; set; }
        public virtual DbSet<CreditNote> CreditNotes { get; set; }
        public virtual DbSet<COARecord> COARecords { get; set; }
        public virtual DbSet<AccountTariff> AccountTariffs { get; set; }
        public virtual DbSet<GeoFence> GeoFences { get; set; }
        public virtual DbSet<QRCodeClick> QRCodeClicks { get; set; }
        public virtual DbSet<ZoneToZonePrice> ZoneToZonePrices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>(b =>
            {
                b.ToTable("AppUsers");
            });

            modelBuilder.Entity<IdentityUserClaim<int>>(b =>
            {
                b.ToTable("AppUserClaims");
            });

            modelBuilder.Entity<IdentityUserLogin<int>>(b =>
            {
                b.ToTable("AppUserLogins");
                b.Property(x => x.LoginProvider).HasMaxLength(128);
                b.Property(x => x.ProviderKey).HasMaxLength(128);
            });

            modelBuilder.Entity<IdentityUserToken<int>>(b =>
            {
                b.ToTable("AppUserTokens");
                b.Property(x => x.LoginProvider).HasMaxLength(128);
                b.Property(x => x.Name).HasMaxLength(128);
            });

            modelBuilder.Entity<AppRole>(b =>
            {
                b.ToTable("AppRoles");
            });

            modelBuilder.Entity<IdentityRoleClaim<int>>(b =>
            {
                b.ToTable("AppRoleClaims");
            });

            modelBuilder.Entity<IdentityUserRole<int>>(b =>
            {
                b.ToTable("AspNetUserRoles");
            });

            modelBuilder.Entity<JobOffer>()
                .Property(e => e.Data)
                .HasConversion(new DictionaryToJsonConverter())
                .Metadata.SetValueComparer(new DictionaryValueComparer());

            var stringListConverter = new ValueConverter<List<string>, string>(
                v => v == null || v.Count == 0 ? "[]" : System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>()
            );

            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
                c => c != null ? c.ToList() : new List<string>()
            );

            var latLongConverter = new ValueConverter<List<LatLong>, string>(
                v => System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => v == null ? new List<LatLong>() : System.Text.Json.JsonSerializer.Deserialize<List<LatLong>>(v, (JsonSerializerOptions)null)!
            );

            var latLongComparer = new ValueComparer<List<LatLong>>(
                (a, b) => System.Text.Json.JsonSerializer.Serialize(a, (JsonSerializerOptions)null) ==
                          System.Text.Json.JsonSerializer.Serialize(b, (JsonSerializerOptions)null),
                v => System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions)null).GetHashCode(),
                v => v == null ? new List<LatLong>() : v.ToList()
            );

            modelBuilder.Entity<GeoFence>()
                .Property(e => e.PolygonData)
                .HasConversion(latLongConverter)
                .Metadata.SetValueComparer(latLongComparer);

            modelBuilder.Entity<GeoFence>()
                .Property(e => e.PolygonData)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<CompanyConfig>()
                .Property(c => c.BrowserFCMs)
                .HasConversion(stringListConverter)
                .Metadata.SetValueComparer(stringListComparer);

            modelBuilder.Entity<UserProfile>(b =>
                b.ToTable("AppUserProfiles")
            );

            modelBuilder.Entity<TenantUser>(b =>
            {
                b.ToTable("TenantUsers");
                b.HasOne(x => x.User)
                    .WithOne()
                    .HasForeignKey<TenantUser>(x => x.UserId);
            });

            modelBuilder.Entity<DriverUserProfile>(b =>
            {
                b.ToTable("DriverUserProfiles");
                b.HasOne(x => x.User)
                    .WithOne()
                    .HasForeignKey<DriverUserProfile>(x => x.UserId);
            });

            modelBuilder.Entity<AccountUserLink>(b =>
            {
                b.ToTable("AccountUserLinks");
                b.HasOne(x => x.User)
                    .WithOne()
                    .HasForeignKey<AccountUserLink>(x => x.UserId);

                b.HasOne(x => x.Account)
                    .WithMany()
                    .HasForeignKey(x => x.AccNo);
            });

            modelBuilder.Entity<UserDeviceRegistration>(b =>
            {
                b.ToTable("UserDeviceRegistrations");
                b.HasIndex(x => new { x.UserId, x.DeviceType });
            });

            modelBuilder.Entity<Booking>(b =>
            {
                b.ToTable("Bookings");

                b.Property("PickupPostCode").HasDefaultValue("''");
                b.Property("PassengerName").HasDefaultValue("''");
                b.Property("PhoneNumber").HasDefaultValue("''");
                b.Property("Email").HasDefaultValue("''");
                b.Property("BookedByName").HasDefaultValue("''");
                b.Property("ConfirmationStatus").HasDefaultValue(ConfirmationStatus.Select);
                b.Property("PaymentStatus").HasDefaultValue(PaymentStatus.Select);
                b.Property("Scope").HasDefaultValue(BookingScope.Cash);
                b.Property("Cancelled").HasDefaultValue(0);
                b.Property("IsAllDay").HasDefaultValue(0);
                b.Property("Price").HasDefaultValueSql("0");
            });

            modelBuilder.Entity<Booking>()
                .HasMany(b => b.Vias)
                .WithOne(b => b.Booking)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DriverInvoiceStatement>()
                .Property(prop => prop.StatementId)
                .UseIdentityColumn(1000, 1);

            modelBuilder.Entity<AccountInvoice>()
                .Property(prop => prop.InvoiceNumber)
                .UseIdentityColumn(90000, 1);

            modelBuilder.Entity<CreditNote>()
                .Property(prop => prop.Id)
                .UseIdentityColumn(1000, 1);

            modelBuilder.Entity<UserProfile>()
                .HasOne(o => o.User);

            modelBuilder.Entity<Booking>()
                .Property(o => o.VatAmountAdded)
                .HasColumnType("decimal(18, 2)");

            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await AuditChanges();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private async Task AuditChanges()
        {
            DateTime now = DateTime.Now.ToUKTime();

            var entityEntries = ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Added ||
                            x.State == EntityState.Modified ||
                            x.State == EntityState.Deleted)
                .ToList();

            foreach (EntityEntry entityEntry in entityEntries)
            {
                if (entityEntry.Entity is Booking)
                    await CreateAuditAsync(entityEntry, now);
            }
        }

        private async Task CreateAuditAsync(EntityEntry entityEntry, DateTime timeStamp)
        {
            if (entityEntry.State == EntityState.Added || entityEntry.State == EntityState.Deleted)
            {
                var changeAudit = await GetChangeAuditAsync(entityEntry, timeStamp);
                await BookingChangeAudits.AddAsync(changeAudit);
            }
            else
            {
                foreach (var prop in entityEntry.OriginalValues.Properties)
                {
                    var originalValue = !string.IsNullOrWhiteSpace(entityEntry.OriginalValues[prop]?.ToString())
                        ? entityEntry.OriginalValues[prop]?.ToString()
                        : null;

                    var currentValue = !string.IsNullOrWhiteSpace(entityEntry.CurrentValues[prop]?.ToString())
                        ? entityEntry.CurrentValues[prop]?.ToString()
                        : null;

                    if (originalValue != currentValue)
                    {
                        var changeAudit = await GetChangeAuditAsync(entityEntry, timeStamp);
                        changeAudit.PropertyName = prop.Name;
                        changeAudit.OldValue = originalValue;
                        changeAudit.NewValue = currentValue;
                        await BookingChangeAudits.AddAsync(changeAudit);
                    }
                }
            }
        }

        private async Task<BookingChangeAudit> GetChangeAuditAsync(EntityEntry entityEntry, DateTime timeStamp)
        {
            var uid = (int)entityEntry.CurrentValues["ActionByUserId"];

            return new BookingChangeAudit
            {
                EntityName = entityEntry.Entity.GetType().Name,
                Action = entityEntry.State.ToString(),
                EntityIdentifier = GetEntityIdentifier(entityEntry),
                UserFullName = await GetUserFullNameAsync(uid),
                TimeStamp = timeStamp
            };
        }

        private static string GetEntityIdentifier(EntityEntry entityEntry)
        {
            if (entityEntry.Entity is Booking)
            {
                return $"{entityEntry.CurrentValues["Id"]}";
            }

            return "None";
        }

        private async Task<string> GetUserFullNameAsync(int uid)
        {
            var fullname = await Users.Where(o => o.Id == uid)
                .Select(o => o.FullName)
                .FirstOrDefaultAsync();

            return fullname ?? string.Empty;
        }
    }

    public class TaxiDispatchContextFactory : IDesignTimeDbContextFactory<TaxiDispatchContext>
    {
        public TaxiDispatchContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TaxiDispatchContext>();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);

            return new TaxiDispatchContext(optionsBuilder.Options);
        }
    }
}
