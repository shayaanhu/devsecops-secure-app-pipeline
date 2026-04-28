using CarpoolApp.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CarpoolApp.Server.Data
{

    public class CarpoolDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Ride> Rides { get; set; }
        public DbSet<RideRequest> RideRequests { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationMember> ConversationMembers { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public CarpoolDbContext(DbContextOptions<CarpoolDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints

            // Define composite primary key for ConversationMember
            modelBuilder.Entity<ConversationMember>()
                .HasKey(cm => new { cm.ConversationId, cm.UserId });


            // User -> Driver (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Driver)
                .WithOne(d => d.User)
                .HasForeignKey<Driver>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Passenger (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Passenger)
                .WithOne(p => p.User)
                .HasForeignKey<Passenger>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Driver -> Vehicle (One-to-Many)
            modelBuilder.Entity<Driver>()
                .HasMany(d => d.Vehicles)
                .WithOne(v => v.Driver)
                .HasForeignKey(v => v.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Driver -> Ride (One-to-Many)
            modelBuilder.Entity<Driver>()
                .HasMany(d => d.Rides)
                .WithOne(r => r.Driver)
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Vehicle -> Ride (One-to-Many) (Restrict delete to prevent multiple cascade paths)
            modelBuilder.Entity<Vehicle>()
                .HasMany(v => v.Rides)
                .WithOne(r => r.Vehicle)
                .HasForeignKey(r => r.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ride -> RideRequest (One-to-Many)
            modelBuilder.Entity<Ride>()
                .HasMany(r => r.RideRequests)
                .WithOne(rr => rr.Ride)
                .HasForeignKey(rr => rr.RideId)
                .OnDelete(DeleteBehavior.Cascade);

            // Passenger -> RideRequest (One-to-Many)
            modelBuilder.Entity<Passenger>()
                .HasMany(p => p.RideRequests)
                .WithOne(rr => rr.Passenger)
                .HasForeignKey(rr => rr.PassengerId)
                .OnDelete(DeleteBehavior.Restrict); // Change to Restrict

            // Conversation -> ConversationMember (One-to-Many)
            modelBuilder.Entity<Conversation>()
                .HasMany(c => c.Members)
                .WithOne(cm => cm.Conversation)
                .HasForeignKey(cm => cm.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Conversation -> Message (One-to-Many)
            modelBuilder.Entity<Conversation>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> ConversationMember (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.ConversationMembers)
                .WithOne(cm => cm.User)
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Message (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Messages)
                .WithOne(m => m.Sender)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Notification (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.UniversityEmail)
                .IsUnique();

            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.NumberPlate)
                .IsUnique();

            // Configure enums as strings in the database
            modelBuilder.Entity<Ride>()
                .Property(r => r.Status)
                .HasConversion<string>();

            modelBuilder.Entity<RideRequest>()
                .Property(rr => rr.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion<string>();
        }
    }
}