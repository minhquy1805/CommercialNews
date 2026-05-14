using Authorization.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace CommercialNews.Api.Authorization;

public static class AuthorizationPolicyRegistrationExtensions
{
    public static IServiceCollection AddApiAuthorizationPolicies(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.AuthzRolesRead,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.Roles.Read)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzRolesCreate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.Roles.Create)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzRolesUpdate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.Roles.Update)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzRolesActivate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.Roles.Activate)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzRolesDeactivate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.Roles.Deactivate)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzPermissionsRead,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.Permissions.Read)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzPermissionsCreate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.Permissions.Create)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzPermissionsUpdate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.Permissions.Update)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzPermissionsActivate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.Permissions.Activate)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzPermissionsDeactivate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.Permissions.Deactivate)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzRolePermissionsRead,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.RolePermissions.Read)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzRolePermissionsGrant,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.RolePermissions.Grant)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzRolePermissionsRevoke,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.RolePermissions.Revoke)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzUserRolesRead,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.UserRoles.Read)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzUserRolesAssign,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.UserRoles.Assign)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzUserRolesRevoke,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.UserRoles.Revoke)));

            options.AddPolicy(
                AuthorizationPolicies.AuthzUserPermissionsReadEffective,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Authz.UserPermissions.ReadEffective)));

            options.AddPolicy(
                AuthorizationPolicies.ContentArticlesCreate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Articles.Create)));

            options.AddPolicy(
                AuthorizationPolicies.ContentArticlesRead,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Articles.Read)));

            options.AddPolicy(
                AuthorizationPolicies.ContentArticlesUpdate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Articles.Update)));

            options.AddPolicy(
                AuthorizationPolicies.ContentArticlesPublish,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Articles.Publish)));

            options.AddPolicy(
                AuthorizationPolicies.ContentArticlesUnpublish,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Articles.Unpublish)));

            options.AddPolicy(
                AuthorizationPolicies.ContentArticlesArchive,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Articles.Archive)));

            options.AddPolicy(
                AuthorizationPolicies.ContentArticlesDelete,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Articles.Delete)));

            options.AddPolicy(
                AuthorizationPolicies.ContentArticlesReadRevisions,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Articles.ReadRevisions)));

            options.AddPolicy(
                AuthorizationPolicies.ContentArticlesReadLifecycleEvents,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Articles.ReadLifecycleEvents)));

            options.AddPolicy(
                AuthorizationPolicies.ContentArticlesReadTags,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Articles.ReadTags)));

            options.AddPolicy(
                AuthorizationPolicies.ContentCategoriesCreate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Categories.Create)));

            options.AddPolicy(
                AuthorizationPolicies.ContentCategoriesRead,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Categories.Read)));

            options.AddPolicy(
                AuthorizationPolicies.ContentCategoriesUpdate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Categories.Update)));

            options.AddPolicy(
                AuthorizationPolicies.ContentCategoriesDelete,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Categories.Delete)));

            options.AddPolicy(
                AuthorizationPolicies.ContentCategoriesRestore,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Categories.Restore)));

            options.AddPolicy(
                AuthorizationPolicies.ContentTagsCreate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Tags.Create)));

            options.AddPolicy(
                AuthorizationPolicies.ContentTagsRead,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Tags.Read)));

            options.AddPolicy(
                AuthorizationPolicies.ContentTagsUpdate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Tags.Update)));

            options.AddPolicy(
                AuthorizationPolicies.ContentTagsDelete,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Tags.Delete)));

            options.AddPolicy(
                AuthorizationPolicies.ContentTagsRestore,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Content.Tags.Restore)));

            options.AddPolicy(
                AuthorizationPolicies.SeoMetadataCreate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Seo.Metadata.Create)));

            options.AddPolicy(
                AuthorizationPolicies.SeoMetadataRead,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Seo.Metadata.Read)));

            options.AddPolicy(
                AuthorizationPolicies.SeoMetadataUpdate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Seo.Metadata.Update)));

            options.AddPolicy(
                AuthorizationPolicies.SeoSlugRoutesCreate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Seo.SlugRoutes.Create)));

            options.AddPolicy(
                AuthorizationPolicies.SeoSlugRoutesRead,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Seo.SlugRoutes.Read)));

            options.AddPolicy(
                AuthorizationPolicies.SeoSlugRoutesUpdate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Seo.SlugRoutes.Update)));

            options.AddPolicy(
                AuthorizationPolicies.SeoSlugRoutesActivate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Seo.SlugRoutes.Activate)));

            options.AddPolicy(
                AuthorizationPolicies.SeoSlugRoutesDeactivate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Seo.SlugRoutes.Deactivate)));

            options.AddPolicy(
                AuthorizationPolicies.SeoSlugRoutesGenerate,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Seo.SlugRoutes.Generate)));

            options.AddPolicy(
                AuthorizationPolicies.AuditLogsRead,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Audit.Logs.Read)));

            options.AddPolicy(
                AuthorizationPolicies.AuditLogsReadDetail,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Audit.Logs.ReadDetail)));

            options.AddPolicy(
                AuthorizationPolicies.AuditLogsReadByCorrelation,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Audit.Logs.ReadByCorrelation)));

            options.AddPolicy(
                AuthorizationPolicies.AuditLogsReadByEvent,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Audit.Logs.ReadByEvent)));

            options.AddPolicy(
                AuthorizationPolicies.IdentityUsersRead,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Identity.Users.Read)));

            options.AddPolicy(
                AuthorizationPolicies.IdentityUsersReadSecurity,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Identity.Users.ReadSecurity)));

            options.AddPolicy(
                AuthorizationPolicies.IdentityUsersManageStatus,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Identity.Users.ManageStatus)));

            options.AddPolicy(
                AuthorizationPolicies.IdentityUsersManageSecurity,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Identity.Users.ManageSecurity)));

            options.AddPolicy(
                AuthorizationPolicies.IdentityUsersVerifyEmail,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Identity.Users.VerifyEmail)));

            options.AddPolicy(
                AuthorizationPolicies.IdentityUsersRevokeSessions,
                policy => policy.Requirements.Add(
                    new PermissionRequirement(PermissionKeys.Identity.Users.RevokeSessions)));

        });

        return services;
    }
}
