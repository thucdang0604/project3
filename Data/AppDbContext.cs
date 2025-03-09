using Microsoft.EntityFrameworkCore;
using JamesThewProject.Models;

namespace JamesThewProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<FavoriteRecipe> FavoriteRecipes { get; set; }
        public DbSet<RecipeRating> RecipeRatings { get; set; }
        public DbSet<ContestSubmission> ContestSubmissions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<BannedWord> BannedWords { get; set; }
        public DbSet<RecipeComment> RecipeComments { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
      
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Contest> Contests { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Đảm bảo Email của User là duy nhất
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Thiết lập kiểu dữ liệu cho Price của SubscriptionPlan
            modelBuilder.Entity<SubscriptionPlan>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // Quan hệ giữa Subscription và User
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ giữa Subscription và SubscriptionPlan
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Plan)
                .WithMany()
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ giữa ContestSubmission và Contest
            modelBuilder.Entity<ContestSubmission>()
                .HasOne(cs => cs.Contest)
                .WithMany(c => c.Submissions)
                .HasForeignKey(cs => cs.ContestId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RecipeRating>()
        .HasOne(rr => rr.User)
        .WithMany(u => u.RecipeRatings)
        .HasForeignKey(rr => rr.UserId)
        .OnDelete(DeleteBehavior.Restrict); // hoặc .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<RecipeRating>()
        .HasIndex(rr => new { rr.RecipeId, rr.UserId })
        .IsUnique();
            modelBuilder.Entity<RecipeComment>()
    .HasOne(rc => rc.User)
    .WithMany(u => u.RecipeComments)
    .HasForeignKey(rc => rc.UserId)
    .OnDelete(DeleteBehavior.Restrict); // hoặc .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<RecipeComment>()
    .HasOne(rc => rc.Recipe)
    .WithMany(r => r.RecipeComments)
    .HasForeignKey(rc => rc.RecipeId)
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecipeComment>()
                .HasOne(rc => rc.User)
                .WithMany(u => u.RecipeComments)
                .HasForeignKey(rc => rc.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            // Không cấu hình mối quan hệ cho RecipeId vì bạn không muốn tạo khóa ngoại.
            modelBuilder.Entity<FavoriteRecipe>()
            .HasOne(fr => fr.User)
            .WithMany(u => u.FavoriteRecipes)
            .HasForeignKey(fr => fr.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Không cascade delete khi xóa User

            modelBuilder.Entity<FavoriteRecipe>()
                .HasOne(fr => fr.Recipe)
                .WithMany(r => r.FavoriteRecipes)
                .HasForeignKey(fr => fr.RecipeId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete cho Recipe nếu cần
        }
    }
}
