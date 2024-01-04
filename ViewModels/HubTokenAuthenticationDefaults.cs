/*
    AmplitudeSoundboard
    Copyright (C) 2021-2023 dan0v
    https://git.dan0v.com/AmplitudeSoundboard

    This file is part of AmplitudeSoundboard.

    AmplitudeSoundboard is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    AmplitudeSoundboard is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with AmplitudeSoundboard.  If not, see <https://www.gnu.org/licenses/>.
*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Amplitude.ViewModels
{
    public static class HubTokenAuthenticationDefaults
    {
        public const string AuthenticationScheme = "HubTokenAuthentication";

        public static AuthenticationBuilder AddHubTokenAuthenticationScheme(this AuthenticationBuilder builder)
        {
            return AddHubTokenAuthenticationScheme(builder, (options) => { });
        }

        public static AuthenticationBuilder AddHubTokenAuthenticationScheme(this AuthenticationBuilder builder, Action<HubTokenAuthenticationOptions> configureOptions)
        {
            return builder.AddScheme<HubTokenAuthenticationOptions, HubTokenAuthenticationHandler>(AuthenticationScheme, configureOptions);
        }
    }
    public class HubTokenAuthenticationOptions : AuthenticationSchemeOptions { }

    public class HubTokenAuthenticationHandler : AuthenticationHandler<HubTokenAuthenticationOptions>
    {
        public IServiceProvider ServiceProvider { get; set; }

        public HubTokenAuthenticationHandler(
          IOptionsMonitor<HubTokenAuthenticationOptions> options,
          ILoggerFactory logger,
          UrlEncoder encoder,
          ISystemClock clock,
          IServiceProvider serviceProvider)
          : base(options, logger, encoder, clock)
        {
            ServiceProvider = serviceProvider;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // get access token from header (authorization: bearer <token>)
            var token = Request.Headers["Authorization"].ToString().Replace("bearer ", "", StringComparison.OrdinalIgnoreCase);
            
            // implement logic to authenticate
            if (!string.IsNullOrWhiteSpace(token) && token.Equals("wakka5353wakkaop"))
            {
                var claims = new[] { new Claim("token", token) };
                var identity = new ClaimsIdentity(claims, nameof(HubTokenAuthenticationHandler));
                var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            return Task.FromResult(AuthenticateResult.Fail("No access token provided."));
        }
    }

    public class HubRequirement : AuthorizationHandler<HubRequirement, HubInvocationContext>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HubRequirement requirement, HubInvocationContext resource)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        public override Task HandleAsync(AuthorizationHandlerContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                context.Succeed(context.PendingRequirements.First());
                return Task.CompletedTask;
            }
            context.Fail();
            return Task.CompletedTask;
        }
    }
}