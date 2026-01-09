using Microsoft.EntityFrameworkCore;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Tickets
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketDraft> TicketDrafts { get; }
    DbSet<TicketComment> TicketComments { get; }
    DbSet<TicketAttachment> TicketAttachments { get; }
    DbSet<TicketHistory> TicketHistory { get; }
    DbSet<TicketCustomField> TicketCustomFields { get; }
    DbSet<TicketCategory> TicketCategories { get; }
    DbSet<TicketStatusEntity> TicketStatuses { get; }
    DbSet<TicketLink> TicketLinks { get; }
    DbSet<TicketSurvey> TicketSurveys { get; }
    DbSet<Priority> Priorities { get; }

    // Organization
    DbSet<Company> Companies { get; }
    DbSet<Department> Departments { get; }
    DbSet<DepartmentMember> DepartmentMembers { get; }
    DbSet<Team> Teams { get; }
    DbSet<TeamMember> TeamMembers { get; }

    // Products
    DbSet<Product> Products { get; }
    DbSet<ProductPlan> ProductPlans { get; }
    DbSet<ProductVersion> ProductVersions { get; }
    DbSet<CompanyProduct> CompanyProducts { get; }
    DbSet<CompanyProductPlan> CompanyProductPlans { get; }
    DbSet<CompanyPriority> CompanyPriorities { get; }

    // SLA
    DbSet<SLARule> SLARules { get; }
    DbSet<BusinessHours> BusinessHours { get; }
    DbSet<Holiday> Holidays { get; }
    DbSet<EscalationRule> EscalationRules { get; }

    // Knowledge Base
    DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles { get; }
    DbSet<KnowledgeBaseArticleTag> KnowledgeBaseArticleTags { get; }
    DbSet<KnowledgeBaseArticleFeedback> KnowledgeBaseArticleFeedbacks { get; }

    // Surveys
    DbSet<SurveyTemplate> SurveyTemplates { get; }

    // Chat/AI
    DbSet<ChatSession> ChatSessions { get; }
    DbSet<ChatMessage> ChatMessages { get; }

    // Permissions & Auth
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserPermission> UserPermissions { get; }
    DbSet<RevokedToken> RevokedTokens { get; }

    // Notifications & Audit
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }

    // Team Chat
    DbSet<TeamMessage> TeamMessages { get; }
    DbSet<TeamMessageAttachment> TeamMessageAttachments { get; }
    DbSet<TeamMessageReaction> TeamMessageReactions { get; }
    DbSet<TeamMessageEditHistory> TeamMessageEditHistories { get; }
    DbSet<GroupChat> GroupChats { get; }
    DbSet<GroupChatMember> GroupChatMembers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
