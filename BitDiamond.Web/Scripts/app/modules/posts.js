var BitDiamond;
(function (BitDiamond) {
    var Modules;
    (function (Modules) {
        Modules.postsModule = angular.module('posts', ['ui.router', 'ngSanitize', 'ngAnimate']);
        //directives
        Modules.postsModule.directive('ringLoader', function () { return new BitDiamond.Directives.RingLoader(); });
        Modules.postsModule.directive('boxLoader', function () { return new BitDiamond.Directives.BoxLoader(); });
        Modules.postsModule.directive('summernote', function ($compile) { return new BitDiamond.Directives.Summernote($compile); });
        //services
        Modules.postsModule.service('__transport', BitDiamond.Utils.Services.DomainTransport);
        Modules.postsModule.service('__notify', BitDiamond.Utils.Services.NotifyService);
        Modules.postsModule.service('__userContext', BitDiamond.Utils.Services.UserContext);
        Modules.postsModule.service('__posts', BitDiamond.Services.Posts);
        Modules.postsModule.service('__account', BitDiamond.Services.Account);
        Modules.postsModule.service('__systemNotification', BitDiamond.Services.Notification);
        //controllers
        Modules.postsModule.controller('NavBar', BitDiamond.Controllers.Shared.NavBar);
        Modules.postsModule.controller('SideBar', BitDiamond.Controllers.Shared.SideBar);
        Modules.postsModule.controller('List', BitDiamond.Controllers.Posts.List);
        Modules.postsModule.controller('Edit', BitDiamond.Controllers.Posts.Edit);
        Modules.postsModule.controller('Details', BitDiamond.Controllers.Posts.Details);
        //configure states
        Modules.postsModule.config(function ($stateProvider, $urlRouterProvider) {
            $urlRouterProvider.otherwise('/list');
            $stateProvider
                .state('list', {
                url: '/list',
                templateUrl: '/posts/list',
                controller: 'List',
                controllerAs: 'vm'
            })
                .state('edit', {
                url: '/edit/',
                params: {
                    post: null
                },
                templateUrl: '/posts/edit',
                controller: 'Edit',
                controllerAs: 'vm'
            })
                .state('details', {
                url: '/details/',
                params: {
                    post: null
                },
                templateUrl: '/posts/details',
                controller: 'Details',
                controllerAs: 'vm'
            });
        });
    })(Modules = BitDiamond.Modules || (BitDiamond.Modules = {}));
})(BitDiamond || (BitDiamond = {}));