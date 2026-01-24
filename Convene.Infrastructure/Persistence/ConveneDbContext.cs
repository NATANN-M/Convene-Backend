using Microsoft.EntityFrameworkCore;
using Convene.Domain.Entities;

namespace Convene.Infrastructure.Persistence
{
    public class ConveneDbContext : DbContext
    {
        internal readonly object OrganizerCreditBalances;

        public ConveneDbContext(DbContextOptions<ConveneDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<OrganizerProfile> OrganizerProfiles { get; set; } = null!;
        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<EventCategory> EventCategories { get; set; } = null!;
        public DbSet<TicketType> TicketTypes { get; set; } = null!;
        public DbSet<DynamicPricingRule> DynamicPricingRules { get; set; } = null!;


        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketScanLog> TicketScanLogs { get; set; }

        public DbSet<Payment> Payments { get; set; } = null!;

        public DbSet<EventFeedback> EventFeedbacks { get; set; } = null!;

        public DbSet<Notification> Notifications { get; set; } = null!;

        public DbSet<UserEventInteraction> UserEventInteractions { get; set; }
        public DbSet<UserRecommendation> UserRecommendations { get; set; }
        public DbSet<MlModelStorage> MlModelStorages { get; set; }
        public DbSet<RecommendationMetrics> RecommendationMetrics { get; set; }
        public DbSet<GatePerson> GatePersons { get; set; }

        public DbSet<OrganizerCreditBalance> OrganizerCreditBalance { get; set; }
        public DbSet<CreditTransaction> CreditTransactions { get; set; }
        public DbSet<PlatformSettings> PlatformSettings { get; set; }
        public DbSet<BoostLevel> BoostLevels { get; set; }
        public DbSet<EventBoost> EventBoosts { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PhoneNumber).HasMaxLength(20);

                entity.HasOne(u => u.OrganizerProfile)
                      .WithOne(p => p.User)
                      .HasForeignKey<OrganizerProfile>(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            //event


            modelBuilder.Entity<EventCategory>(entity =>
            {
                entity.HasIndex(c => c.Name);
                entity.Property(c => c.Name).HasMaxLength(100);
            });

            // Event basic config
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Events)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // TicketType
            modelBuilder.Entity<TicketType>(entity =>
            {
                entity.Property(t => t.Name).HasMaxLength(100);
                entity.HasOne(t => t.Event)
                      .WithMany(e => e.TicketTypes)
                      .HasForeignKey(t => t.EventId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // PricingRule
            modelBuilder.Entity<DynamicPricingRule>(entity =>
            {
                entity.HasOne(r => r.TicketType)
                      .WithMany(t => t.PricingRules)
                      .HasForeignKey(r => r.TicketTypeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            // Relationships for Booking
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
     .HasOne(b => b.User)
     .WithMany(u => u.Bookings)
     .HasForeignKey(b => b.UserId)
     .OnDelete(DeleteBehavior.Restrict);


            // Relationships for Ticket
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Booking)
                .WithMany(b => b.Tickets)
                .HasForeignKey(t => t.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Event)
                .WithMany()
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.TicketType)
                .WithMany()
                .HasForeignKey(t => t.TicketTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationships for TicketScanLog



            // Relationships for Payment

            modelBuilder.Entity<Payment>()
        .HasOne(p => p.Booking)
        .WithMany(b => b.Payments)
        .HasForeignKey(p => p.BookingId)
        .OnDelete(DeleteBehavior.Cascade);





            // Feedback relationships
            modelBuilder.Entity<EventFeedback>()
                .HasOne(f => f.Event)
                .WithMany(e => e.Feedbacks) // add List<EventFeedback> Feedbacks in Event entity if not exists
                .HasForeignKey(f => f.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventFeedback>()
                .HasOne(f => f.User)
                .WithMany() // optional: can track feedbacks per user if needed
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional: Add Rating constraints
            modelBuilder.Entity<EventFeedback>()
                .Property(f => f.Rating)
                .HasDefaultValue(5)
                .IsRequired();



            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserRecommendation>()
         .HasKey(x => new { x.UserId, x.EventId });

            modelBuilder.Entity<UserEventInteraction>()
                .HasKey(x => x.Id);





            // --------------------- GatePerson Config ---------------------
            modelBuilder.Entity<GatePerson>(entity =>
            {
                entity.ToTable("GatePersons");

                // Remove FullName, Email, PhoneNumber configurations since they're not in GatePerson entity

                // JSON assignment, no FK
                entity.Property(g => g.AssignmentsJson)
                    .HasColumnType("text");

                entity.Property(g => g.IsActive)
                    .HasDefaultValue(true);

                // Configure relationship with User
                entity.HasOne(g => g.User)
                    .WithMany() // User doesn't have navigation to GatePerson
                    .HasForeignKey(g => g.UserId)
                    .OnDelete(DeleteBehavior.SetNull); // Set null when User is deleted

                // Indexes for performance
                entity.HasIndex(g => g.UserId);
                entity.HasIndex(g => g.CreatedByOrganizerId);
                entity.HasIndex(g => g.IsActive);
            });


            // --------------------- TicketScanLog Config ---------------------
            modelBuilder.Entity<TicketScanLog>(entity =>
            {
                entity.ToTable("TicketScanLogs");

                entity.HasKey(x => x.Id);

                // Indexing for performance
                entity.HasIndex(x => x.TicketId);
                entity.HasIndex(x => x.EventId);
                entity.HasIndex(x => x.ScannerUserId);
                entity.HasIndex(x => x.ScannedAt);
                entity.HasIndex(x => x.IsValid);

                // Scanner Snapshot
                entity.Property(s => s.ScannerName)
                    .HasMaxLength(200);

                entity.Property(s => s.ScannerEmail)
                    .HasMaxLength(200);

                // Ticket Snapshot
                entity.Property(s => s.TicketTypeName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(s => s.TicketHolderName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(s => s.TicketHolderEmail)
                    .IsRequired()
                    .HasMaxLength(200);

                // Event Snapshot
                entity.Property(s => s.EventName)
                    .IsRequired()
                    .HasMaxLength(300);

                // Metadata
                entity.Property(s => s.Location)
                    .HasMaxLength(200);

                entity.Property(s => s.DeviceId)
                    .HasMaxLength(200);

                entity.Property(s => s.Reason)
                    .HasMaxLength(500);

                // Configure required properties
                entity.Property(s => s.TicketId)
                    .IsRequired();

                entity.Property(s => s.EventId)
                    .IsRequired();
            });


            // -------------------------------
            // OrganizerCreditBalance
            // -------------------------------
            modelBuilder.Entity<OrganizerCreditBalance>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.OrganizerProfileId).IsUnique();

                entity.Property(x => x.Balance).IsRequired();
                entity.Property(x => x.LastUpdated).IsRequired();

                entity.HasOne(x => x.OrganizerProfile)
                      .WithOne()
                      .HasForeignKey<OrganizerCreditBalance>(x => x.OrganizerProfileId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // -------------------------------
            // CreditTransaction
            // -------------------------------
            modelBuilder.Entity<CreditTransaction>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(x => x.CreditsChanged).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();

                entity.HasOne(x => x.OrganizerProfile)
                    .WithMany()
                    .HasForeignKey(x => x.OrganizerProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // -------------------------------
            // BoostLevel
            // -------------------------------
            modelBuilder.Entity<BoostLevel>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Description).IsRequired();
                entity.Property(x => x.CreditCost).IsRequired();
                entity.Property(x => x.DurationHours).IsRequired();
                entity.Property(x => x.Weight).IsRequired();
                entity.Property(x => x.IsActive).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();
            });

            // -------------------------------
            // EventBoost
            // -------------------------------
            modelBuilder.Entity<EventBoost>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.CreditsUsed).IsRequired();
                entity.Property(x => x.StartTime).IsRequired();
                entity.Property(x => x.EndTime).IsRequired();

                entity.HasOne(x => x.Event)
                    .WithMany()
                    .HasForeignKey(x => x.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.OrganizerProfile)
                    .WithMany()
                    .HasForeignKey(x => x.OrganizerProfileId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.BoostLevel)
                    .WithMany()
                    .HasForeignKey(x => x.BoostLevelId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // -------------------------------
            // PlatformSettings
            // -------------------------------
            modelBuilder.Entity<PlatformSettings>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.InitialOrganizerCredits).IsRequired();
                entity.Property(x => x.EventPublishCost).IsRequired();
                entity.Property(x => x.CreditPriceETB)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                entity.Property(x => x.UpdatedAt).IsRequired();
            });

            //EventBoost
            modelBuilder.Entity<EventBoost>()
     .HasOne(eb => eb.Event)
     .WithMany(e => e.EventBoosts)
     .HasForeignKey(eb => eb.EventId)
     .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventBoost>()
                .HasOne(eb => eb.OrganizerProfile)
                .WithMany()
                .HasForeignKey(eb => eb.OrganizerProfileId);

            modelBuilder.Entity<EventBoost>()
                .HasOne(eb => eb.BoostLevel)
                .WithMany()
                .HasForeignKey(eb => eb.BoostLevelId);



        }
    }
}
