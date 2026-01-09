using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Common.Interfaces;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketDraft> TicketDrafts => Set<TicketDraft>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketHistory> TicketHistory => Set<TicketHistory>();
    public DbSet<TicketCustomField> TicketCustomFields => Set<TicketCustomField>();
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    public DbSet<TicketStatusEntity> TicketStatuses => Set<TicketStatusEntity>();
    public DbSet<TicketLink> TicketLinks => Set<TicketLink>();
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DepartmentMember> DepartmentMembers => Set<DepartmentMember>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductPlan> ProductPlans => Set<ProductPlan>();
    public DbSet<ProductVersion> ProductVersions => Set<ProductVersion>();
    public DbSet<CompanyProduct> CompanyProducts => Set<CompanyProduct>();
    public DbSet<CompanyProductPlan> CompanyProductPlans => Set<CompanyProductPlan>();
    public DbSet<CompanyPriority> CompanyPriorities => Set<CompanyPriority>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    // SLA
    public DbSet<SLARule> SLARules => Set<SLARule>();
    public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<EscalationRule> EscalationRules => Set<EscalationRule>();

    // Knowledge Base
    public DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles => Set<KnowledgeBaseArticle>();
    public DbSet<KnowledgeBaseArticleTag> KnowledgeBaseArticleTags => Set<KnowledgeBaseArticleTag>();
    public DbSet<KnowledgeBaseArticleFeedback> KnowledgeBaseArticleFeedbacks => Set<KnowledgeBaseArticleFeedback>();

    // Surveys
    public DbSet<SurveyTemplate> SurveyTemplates => Set<SurveyTemplate>();
    public DbSet<TicketSurvey> TicketSurveys => Set<TicketSurvey>();

    // Chat/AI
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Team Chat
    public DbSet<TeamMessage> TeamMessages => Set<TeamMessage>();
    public DbSet<TeamMessageAttachment> TeamMessageAttachments => Set<TeamMessageAttachment>();
    public DbSet<TeamMessageReaction> TeamMessageReactions => Set<TeamMessageReaction>();
    public DbSet<TeamMessageEditHistory> TeamMessageEditHistories => Set<TeamMessageEditHistory>();
    public DbSet<GroupChat> GroupChats => Set<GroupChat>();
    public DbSet<GroupChatMember> GroupChatMembers => Set<GroupChatMember>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure self-referencing relationships
        builder.Entity<Ticket>()
            .HasOne(t => t.MergedIntoTicket)
            .WithMany(t => t.MergedTickets)
            .HasForeignKey(t => t.MergedIntoTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TicketCategory>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TicketComment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TicketLink>()
            .HasOne(l => l.SourceTicket)
            .WithMany(t => t.SourceLinks)
            .HasForeignKey(l => l.SourceTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TicketLink>()
            .HasOne(l => l.TargetTicket)
            .WithMany(t => t.TargetLinks)
            .HasForeignKey(l => l.TargetTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        // TicketDraft configuration
        builder.Entity<TicketDraft>()
            .HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TicketDraft>()
            .HasIndex(d => d.UserId);

        // Configure indexes
        builder.Entity<Ticket>()
            .HasIndex(t => t.TicketNumber)
            .IsUnique();

        builder.Entity<Ticket>()
            .HasIndex(t => t.CreatedAt);

        builder.Entity<RevokedToken>()
            .HasIndex(t => t.Token)
            .IsUnique();

        // Team Chat configurations - prevent cascade delete cycles
        builder.Entity<GroupChatMember>()
            .HasOne(m => m.GroupChat)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.GroupChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<GroupChatMember>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<GroupChat>()
            .HasOne(g => g.CreatedBy)
            .WithMany()
            .HasForeignKey(g => g.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TeamMessage>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TeamMessage>()
            .HasOne(m => m.Team)
            .WithMany()
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TeamMessage>()
            .HasOne(m => m.GroupChat)
            .WithMany(g => g.Messages)
            .HasForeignKey(m => m.GroupChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeamMessage>()
            .HasOne(m => m.ReplyToMessage)
            .WithMany()
            .HasForeignKey(m => m.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TeamMessageAttachment>()
            .HasOne(a => a.Message)
            .WithMany(m => m.Attachments)
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeamMessageReaction>()
            .HasOne(r => r.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TeamMessageReaction>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TeamMessageEditHistory>()
            .HasOne(h => h.Message)
            .WithMany(m => m.EditHistory)
            .HasForeignKey(h => h.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
