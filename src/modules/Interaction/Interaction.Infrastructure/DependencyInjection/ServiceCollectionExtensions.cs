using Interaction.Application.Ports.Persistence;
using Interaction.Application.Ports.Services;
using Interaction.Infrastructure.Persistence.Exceptions;
using Interaction.Infrastructure.Persistence.Repositories;
using Interaction.Infrastructure.Persistence.Sql;
using Interaction.Infrastructure.Services.ArticleViews;
using Interaction.Infrastructure.Services.CommentContent;
using Interaction.Infrastructure.Services.CommentReports;
using Microsoft.Extensions.DependencyInjection;

namespace Interaction.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInteractionInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<InteractionUnitOfWork>();
        services.AddScoped<IInteractionUnitOfWork>(sp => sp.GetRequiredService<InteractionUnitOfWork>());

        services.AddSingleton<InteractionSqlExceptionTranslator>();

        services.AddScoped<IArticleInteractionTargetProjectionRepository, ArticleInteractionTargetProjectionRepository>();
        services.AddScoped<IArticleViewCountRepository, ArticleViewCountRepository>();
        services.AddScoped<IArticleLikeRepository, ArticleLikeRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ICommentReportRepository, CommentReportRepository>();
        services.AddScoped<ICommentModerationCaseRepository, CommentModerationCaseRepository>();
        services.AddScoped<ICommentModerationActionHistoryRepository, CommentModerationActionHistoryRepository>();
        services.AddScoped<IArticleInteractionStatsRepository, ArticleInteractionStatsRepository>();

        services.AddSingleton<IArticleViewAcceptancePolicy, AllowAllArticleViewAcceptancePolicy>();
        services.AddSingleton<ICommentContentPolicy, BlockedTermsCommentContentPolicy>();
        services.AddSingleton<ICommentReportPolicy, DefaultCommentReportPolicy>();

        return services;
    }
}
