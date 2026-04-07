using Interaction.Application.UseCases.Comments.CreateComment;
using Interaction.Application.UseCases.Comments.DeleteComment;
using Interaction.Application.UseCases.Comments.GetComments;
using Interaction.Application.UseCases.Comments.UpdateComment;
using Interaction.Application.UseCases.GetArticleCounters;
using Interaction.Application.UseCases.LikeArticle;
using Interaction.Application.UseCases.TrackArticleView;
using Interaction.Application.UseCases.UnlikeArticle;
using Microsoft.Extensions.DependencyInjection;

namespace Interaction.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInteractionApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ITrackArticleViewUseCase, TrackArticleViewUseCase>();
        services.AddScoped<ILikeArticleUseCase, LikeArticleUseCase>();
        services.AddScoped<IUnlikeArticleUseCase, UnlikeArticleUseCase>();
        services.AddScoped<IGetArticleCountersUseCase, GetArticleCountersUseCase>();

        services.AddScoped<ICreateCommentUseCase, CreateCommentUseCase>();
        services.AddScoped<IGetCommentsUseCase, GetCommentsUseCase>();
        services.AddScoped<IUpdateCommentUseCase, UpdateCommentUseCase>();
        services.AddScoped<IDeleteCommentUseCase, DeleteCommentUseCase>();

        return services;
    }
}