using Interaction.Application.Consumers.Content;
using Interaction.Application.UseCases.ArticleInteractionStats.GetArticleInteractionStats;
using Interaction.Application.UseCases.ArticleInteractionStats.MaterializeArticleInteractionStats;
using Interaction.Application.UseCases.ArticleInteractionTargets.ApplyArticleInteractionTargetProjection;
using Interaction.Application.UseCases.CommentModerationCases.DismissReportedCommentCase;
using Interaction.Application.UseCases.CommentModerationCases.GetModerationCaseByPublicId;
using Interaction.Application.UseCases.CommentModerationCases.GetModerationCases;
using Interaction.Application.UseCases.CommentModerationCases.HideReportedComment;
using Interaction.Application.UseCases.CommentReports.CreateCommentReport;
using Interaction.Application.UseCases.Comments.CreateComment;
using Interaction.Application.UseCases.Comments.DeleteOwnComment;
using Interaction.Application.UseCases.Comments.GetAdminCommentByPublicId;
using Interaction.Application.UseCases.Comments.GetAdminComments;
using Interaction.Application.UseCases.Comments.GetCommentModerationHistory;
using Interaction.Application.UseCases.Comments.GetPublicComments;
using Interaction.Application.UseCases.Comments.HideComment;
using Interaction.Application.UseCases.Comments.RestoreComment;
using Interaction.Application.UseCases.Likes.GetMyArticleLike;
using Interaction.Application.UseCases.Likes.LikeArticle;
using Interaction.Application.UseCases.Likes.UnlikeArticle;
using Interaction.Application.UseCases.Views.TrackArticleView;
using Microsoft.Extensions.DependencyInjection;

namespace Interaction.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInteractionApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IApplyArticleInteractionTargetProjectionUseCase, ApplyArticleInteractionTargetProjectionUseCase>();
        services.AddScoped<IContentInteractionEventIngestionService, ContentInteractionEventIngestionService>();

        services.AddScoped<IGetArticleInteractionStatsUseCase, GetArticleInteractionStatsUseCase>();
        services.AddScoped<IMaterializeArticleInteractionStatsUseCase, MaterializeArticleInteractionStatsUseCase>();

        services.AddScoped<ITrackArticleViewUseCase, TrackArticleViewUseCase>();

        services.AddScoped<IGetMyArticleLikeUseCase, GetMyArticleLikeUseCase>();
        services.AddScoped<ILikeArticleUseCase, LikeArticleUseCase>();
        services.AddScoped<IUnlikeArticleUseCase, UnlikeArticleUseCase>();

        services.AddScoped<ICreateCommentUseCase, CreateCommentUseCase>();
        services.AddScoped<IDeleteOwnCommentUseCase, DeleteOwnCommentUseCase>();
        services.AddScoped<IGetAdminCommentByPublicIdUseCase, GetAdminCommentByPublicIdUseCase>();
        services.AddScoped<IGetAdminCommentsUseCase, GetAdminCommentsUseCase>();
        services.AddScoped<IGetCommentModerationHistoryUseCase, GetCommentModerationHistoryUseCase>();
        services.AddScoped<IGetPublicCommentsUseCase, GetPublicCommentsUseCase>();
        services.AddScoped<IHideCommentUseCase, HideCommentUseCase>();
        services.AddScoped<IRestoreCommentUseCase, RestoreCommentUseCase>();

        services.AddScoped<ICreateCommentReportUseCase, CreateCommentReportUseCase>();

        services.AddScoped<IGetModerationCasesUseCase, GetModerationCasesUseCase>();
        services.AddScoped<IGetModerationCaseByPublicIdUseCase, GetModerationCaseByPublicIdUseCase>();
        services.AddScoped<IDismissReportedCommentCaseUseCase, DismissReportedCommentCaseUseCase>();
        services.AddScoped<IHideReportedCommentUseCase, HideReportedCommentUseCase>();

        return services;
    }
}
