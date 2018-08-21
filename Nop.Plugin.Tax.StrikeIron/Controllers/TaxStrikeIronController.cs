using System;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Tax.StrikeIron.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Tax;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Tax.StrikeIron.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class TaxStrikeIronController : BasePluginController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly StrikeIronTaxSettings _strikeIronTaxSettings;
        

        #endregion

        #region Ctor

        public TaxStrikeIronController(ILocalizationService localizationService,
            ILogger logger,
            IPermissionService permissionService,
            ISettingService settingService,
            ITaxService taxService,
            StrikeIronTaxSettings strikeIronTaxSettings)
        {
            this._localizationService = localizationService;
            this._logger = logger;
            this._permissionService = permissionService;
            this._settingService = settingService;
            this._taxService = taxService;
            this._strikeIronTaxSettings = strikeIronTaxSettings;
        }

        #endregion

        private string Test(TaxStrikeIronModel model, string zip = null, string province = null)
        {
            //clear testing results
            model.TestingUsaResult = "";
            model.TestingCanadaResult = "";

            var strikeIronTaxProvider = _taxService.LoadTaxProviderBySystemName("Tax.StrikeIron.Basic") as StrikeIronTaxProvider;

            if (strikeIronTaxProvider == null)
            {
                return "StrikeIron module cannot be loaded";
            }

            try
            {
                var error = string.Empty;
                var taxRate = zip != null ? strikeIronTaxProvider.GetTaxRateUsa(zip, ref error) : strikeIronTaxProvider.GetTaxRateCanada(province, ref error);

                return string.IsNullOrEmpty(error) ? $"Rate for zip {zip}: {taxRate:p}" : error;
            }
            catch (Exception exc)
            {
                _logger.Error($"StrikeIron error: {exc.Message}", exc);
                return exc.Message;
            }
        }

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            var model = new TaxStrikeIronModel
            {
                LicenseKey = _strikeIronTaxSettings.LicenseKey,
                TestingCanadaProvinceCode = "",
                TestingUsaZip = "",
                TestingUsaResult = "",
                TestingCanadaResult = ""
            };

            return View("~/Plugins/Tax.StrikeIron/Views/Configure.cshtml", model);
        }

        
        [FormValueRequired("save")]
        public IActionResult Configure(TaxStrikeIronModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (!ModelState.IsValid)
            {
                return Configure();
            }

            //clear testing results
            model.TestingUsaResult = "";
            model.TestingCanadaResult = "";

            //save settings
            _strikeIronTaxSettings.LicenseKey = model.LicenseKey;
            _settingService.SaveSetting(_strikeIronTaxSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return View("~/Plugins/Tax.StrikeIron/Views/Configure.cshtml", model);
        }
        
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("testUsa")]
        public IActionResult TestUsa(TaxStrikeIronModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (!ModelState.IsValid)
            {
                return Configure();
            }

            model.TestingUsaResult = Test(model, model.TestingUsaZip);

            return View("~/Plugins/Tax.StrikeIron/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("testCanada")]
        public IActionResult TestCanada(TaxStrikeIronModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (!ModelState.IsValid)
            {
                return Configure();
            }

            model.TestingCanadaResult = Test(model, province: model.TestingCanadaProvinceCode);
            
            return View("~/Plugins/Tax.StrikeIron/Views/Configure.cshtml", model);
        }
        
        #endregion
    }
}