var BitDiamond;
(function (BitDiamond) {
    var Modules;
    (function (Modules) {
        Modules.homeModlue = angular.module('home', ['ui.router', 'ngSanitize', 'ngAnimate']);
        //directives
        Modules.homeModlue.directive('boxLoader', function () { return new BitDiamond.Directives.BoxLoader(); });
        //services
        Modules.homeModlue.service('__transport', BitDiamond.Utils.Services.DomainTransport);
        Modules.homeModlue.service('__dom', BitDiamond.Utils.Services.DomModelService);
        Modules.homeModlue.service('__notify', BitDiamond.Utils.Services.NotifyService);
        Modules.homeModlue.service('__userContext', BitDiamond.Utils.Services.UserContext);
        Modules.homeModlue.service('__blockChain', BitDiamond.Services.BlockChain);
        Modules.homeModlue.service('__account', BitDiamond.Services.Account);
        Modules.homeModlue.service('__notification', BitDiamond.Services.Notification);
        Modules.homeModlue.service('__xe', BitDiamond.Services.XE);
        //controllers
        Modules.homeModlue.controller('Landing', BitDiamond.Controllers.Home.Landing);
    })(Modules = BitDiamond.Modules || (BitDiamond.Modules = {}));
})(BitDiamond || (BitDiamond = {}));
//# sourceMappingURL=home.js.map