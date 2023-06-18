using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Entities;
using OrchardCore.Settings;
using OrchardCore.Users.Models;
using OrchardCore.Users.Services;

namespace OrchardCore.Users.Filters;

public class TwoFactorAuthenticationAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly UserOptions _userOptions;

    public TwoFactorAuthenticationAuthorizationFilter(IOptions<UserOptions> userOptions)
    {
        _userOptions = userOptions.Value;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (!(context.HttpContext?.User?.Identity?.IsAuthenticated ?? false)
            || context.HttpContext.Request.Path.Equals("/" + _userOptions.LogoffPath, StringComparison.OrdinalIgnoreCase)
            || context.HttpContext.Request.Path.Equals("/" + _userOptions.EnableAuthenticatorPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var siteService = context.HttpContext.RequestServices.GetService<ISiteService>();

        if (siteService == null)
        {
            return;
        }

        var loginSettings = (await siteService.GetSiteSettingsAsync()).As<LoginSettings>();

        if (loginSettings.RequireTwoFactorAuthentication
            && loginSettings.IsTwoFactorAuthenticationEnabled()
            && context.HttpContext.User.HasClaim(claim => claim.Type == TwoFactorAuthenticationClaimsProvider.TwoFactorAuthenticationClaimType))
        {
            context.Result = new RedirectResult("~/" + _userOptions.EnableAuthenticatorPath);
        }
    }
}
