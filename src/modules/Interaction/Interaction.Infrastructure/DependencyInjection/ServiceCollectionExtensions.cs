using Interaction.Application.Ports.Persistence.Read;
using Interaction.Application.Ports.Persistence.Transactions;
using Interaction.Application.Ports.Persistence.Write;
using Interaction.Infrastructure.Persistence.Exceptions;
using Interaction.Infrastructure.Persistence.Repositories.Read;
using Interaction.Infrastructure.Persistence.Repositories.Write;
using Interaction.Infrastructure.Persistence.Sql;
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

        services.AddScoped<IArticleViewEventRepository, ArticleViewEventRepository>();
        services.AddScoped<IArticleLikeRepository, ArticleLikeRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IArticleInteractionStatsRepository, ArticleInteractionStatsRepository>();

        services.AddScoped<ICommentQueryRepository, CommentQueryRepository>();
        services.AddScoped<IArticleInteractionStatsQueryRepository, ArticleInteractionStatsQueryRepository>();

        return services;
    }
}