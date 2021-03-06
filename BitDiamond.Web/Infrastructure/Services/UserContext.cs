﻿using BitDiamond.Core.Services;
using Axis.Pollux.Identity.Principal;
using static Axis.Luna.Extensions.ExceptionExtensions;
using BitDiamond.Core.Services.Query;
using BitDiamond.Core.Utils;
using System.Security.Claims;
using System.Linq;
using System;
using System.Collections.Generic;
using Axis.Luna.Extensions;
using BitDiamond.Core.Models;
using BitDiamond.Web.Infrastructure.Utils;

using static Axis.Luna.Extensions.ObjectExtensions;

namespace BitDiamond.Web.Infrastructure.Services
{
    public class UserContext : IUserContext
    {
        private OwinContextProvider _owinProvider = null;
        private IUserContextQuery _query = null;

        public UserContext(OwinContextProvider owinProvider, IUserContextQuery query)
        {
            ThrowNullArguments(() => owinProvider,
                               () => query);

            this._owinProvider = owinProvider;
            this._query = query;
        }

        private User _user = null;
        public User CurrentUser()
        {
            if (_user != null) return _user;

            else if (_owinProvider.Owin.Request.User?.Identity?.Name == null) return _user = new User
            {
                UserId = Constants.SystemUsers_Guest,
                Status = (int)AccountStatus.Active
            };

            else
            {
                var claimsIdentity = _owinProvider.Owin.Request.User.Identity as ClaimsIdentity;
                return _user = new User
                {
                    UserId = claimsIdentity.Claims.FirstOrDefault(_c => _c.Type == ClaimTypes.Name).Value,
                    Status = int.Parse(claimsIdentity.Claims.FirstOrDefault(_c => _c.Type == "user-status").Value),
                    UId = Guid.Parse(claimsIdentity.Claims.FirstOrDefault(_c => _c.Type == ClaimTypes.Sid).Value)
                };
            }
        }

        public UserLogon CurrentUserLogon() => Eval(() => _owinProvider.Owin.Environment[WebConstants.Misc_UserLogonOwinContextKey] as UserLogon);

        private List<string> _userRoles = null;
        public IEnumerable<string> CurrentUserRoles()
        {
            if (_userRoles != null) return _userRoles.ToArray();

            else if (_owinProvider.Owin.Request.User != null)
            {
                _userRoles = _owinProvider.Owin.Request.User.Identity
                    .As<ClaimsIdentity>()
                    .Claims
                    .Where(_c => _c.Type == ClaimTypes.Role)
                    .Select(_c => _c.Value)
                    .ToList();

                return _userRoles.ToArray();
            }

            else return new string[0];
        }

        public IUserContext Impersonate(string userId) => new ImporsonatedUserContext(_query.GetUserById(userId), _query);
    }

    public class ImporsonatedUserContext : IUserContext
    {
        private IUserContextQuery _query = null;
        private User _user = null;
        private List<string> _userRoles = null;

        public ImporsonatedUserContext(User user, IUserContextQuery query)
        {
            ThrowNullArguments(() => user, () => query);

            _user = user;
            _query = query;
        }

        public User CurrentUser() => _user;

        public UserLogon CurrentUserLogon() => null;

        public IEnumerable<string> CurrentUserRoles()
        => _userRoles ?? (_userRoles = _query.GetUserRoles(_user.UserId).ToList());

        public IUserContext Impersonate(string userId) => new ImporsonatedUserContext(_query.GetUserById(userId), _query);
    }
}