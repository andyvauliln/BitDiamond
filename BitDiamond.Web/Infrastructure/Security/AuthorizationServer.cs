﻿using Axis.Jupiter;
using Axis.Luna;
using Axis.Luna.Extensions;
using Axis.Pollux.Authentication;
using Axis.Pollux.Authentication.Service;
using Axis.Pollux.Identity.Principal;
using Axis.Pollux.RBAC.Auth;
using BitDiamond.Core.Models;
using BitDiamond.Web.Infrastructure.Utils;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using UAParser;
using static Axis.Luna.Extensions.ExceptionExtensions;

namespace BitDiamond.Web.Infrastructure.Security
{
    public class AuthorizationServer : OAuthAuthorizationServerProvider, IDisposable//, IAuthenticationTokenProvider
    {
        private WeakCache _cache = null;
        private Parser Parser = Parser.GetDefault();

        public AuthorizationServer(WeakCache cache)
        {
            ThrowNullArguments(() => cache);
            
            _cache = cache;
        }

        #region OAuthAuthrizationServerProvider
        public override Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        => Task.Run(() =>
        {
            var _credentialAuthority = context.OwinContext.GetPerRequestValue<ICredentialAuthentication>(nameof(ICredentialAuthentication));
            var _dataContext = context.OwinContext.GetPerRequestValue<IDataContext>(nameof(IDataContext));

            #region delete old logons if they exist
            Operation.Try(() =>
            {
                var oldToken = context.Request.Headers.GetValues(WebConstants.OAuthCustomHeaders_OldToken)?.FirstOrDefault() ?? null;
                if (oldToken != null)
                {
                    var logon = _cache.GetOrRefresh<UserLogon>(oldToken);
                    if (logon != null)
                    {
                        logon.Invalidated = true;
                        _dataContext.Store<UserLogon>().Modify(logon, true);
                        _cache.Invalidate(oldToken);
                    }
                }
            })
            #endregion

            #region Verify the user exists
            .Then(opr =>
            {
                return _dataContext.Store<User>().Query
                    .Where(_u => _u.EntityId == context.UserName)
                    .FirstOrDefault()
                    .ThrowIfNull("invalid user credential")
                    .ThrowIf(_u => _u.Status != (int)AccountStatus.Active, "inactive user account");
            })
            #endregion

            #region verify credentials with the credential authority
            .Then(opr =>
            {
                _credentialAuthority.VerifyCredential(new Credential
                {
                    OwnerId = context.UserName,
                    Metadata = CredentialMetadata.Password,
                    Value = Encoding.UTF8.GetBytes(context.Password)
                })
                .Resolve();
                return opr.Result;
            })
            #endregion

            #region aggregate the claims that makeup the token
            .Then(opr =>
            {
                var identity = new ClaimsIdentity(context.Options.AuthenticationType);

                //identity.AddClaim(new Claim("user-name", context.UserName));
                identity.AddClaim(new Claim(ClaimTypes.Name, context.UserName));
                identity.AddClaim(new Claim(ClaimTypes.Sid, opr.Result.UId.ToString()));
                identity.AddClaim(new Claim("user-status", opr.Result.Status.ToString()));

                //roles
                _dataContext.Store<UserRole>().Query
                            .Where(_ur => _ur.UserId == opr.Result.EntityId)
                            .ForAll((_cnt, _next) => identity.AddClaim(new Claim(ClaimTypes.Role, _next.RoleName)));

                context.Validated(new Microsoft.Owin.Security.AuthenticationTicket(identity, null));
            })
            #endregion

            #region if any of the above failed...
            .Error(opr =>
            {
                context.SetError("invalid_grant", opr.Message);
                context.Rejected();
            });
            #endregion
        });

        public override Task TokenEndpointResponse(OAuthTokenEndpointResponseContext context)
        => Task.Run(() =>
        {
            //potential bug: capturing this datacontext is dangerous because if the cache gets invalidated and this logon is requested,
            //the datacontext will MOST LIKELY be disposed when it is queried for the logon.
            //The solution is to find a way to get at the current owin context, to get a fresh db context from there.
            var _dataContext = context.OwinContext.GetPerRequestValue<IDataContext>(nameof(IDataContext));

            //cache the logon associated to the given token
            _cache.GetOrAdd(context.AccessToken, _token =>
            {
                var agent = Parser.Parse(context.Request.Headers.Get("User-Agent"));

                var _l = _dataContext.Store<UserLogon>()
                    .QueryWith(_ul => _ul.User)
                    .Where(_ul => _ul.User.EntityId == context.Identity.Name)
                    .Where(_ul => _ul.OwinToken == _token) //get the bearer token from the header
                    .FirstOrDefault();

                if (_l != null) return _l;
                else
                {
                    _l = new UserLogon
                    {
                        UserId = context.Identity.Name,
                        Client = new Core.Models.UserAgent
                        {
                            OS = agent.OS.Family,
                            OSVersion = $"{agent.OS.Major}.{agent.OS.Minor}",

                            Browser = agent.UserAgent.Family,
                            BrowserVersion = $"{agent.UserAgent.Major}.{agent.UserAgent.Minor}",

                            Device = $"{agent.Device.Family}"
                        },
                        OwinToken = _token,
                        Location = null,

                        ModifiedOn = DateTime.Now
                    };

                    _dataContext.Store<UserLogon>().Add(_l).Context.CommitChanges();

                    return _l;
                }
            });
        });

        /// <summary>
        /// For custom authentication/authorizations
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task GrantCustomExtension(OAuthGrantCustomExtensionContext context) => base.GrantCustomExtension(context);
        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context) => Task.Run(() => context.Validated());

        public void Dispose()
        {
        }
        #endregion
    }
}