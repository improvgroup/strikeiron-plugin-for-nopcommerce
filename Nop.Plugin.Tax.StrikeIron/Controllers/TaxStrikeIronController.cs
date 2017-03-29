using System;
using System.Web.Mvc;
using Nop.Plugin.Tax.StrikeIron.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Tax;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Tax.StrikeIron.Controllers
{
    [AdminAuthorize]
    public class TaxStrikeIronController : BasePluginController
    {
        #region Fields

        private readonly ITaxService _taxService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly StrikeIronTaxSettings _strikeIronTaxSettings;

        #endregion

        #region Ctor

        public TaxStrikeIronController(ITaxService taxService,
            ISettingService settingService,
            ILocalizationService localizationService,
            StrikeIronTaxSettings strikeIronTaxSettings)
        {
            this._taxService = taxService;
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._strikeIronTaxSettings = strikeIronTaxSettings;
        }

        #endregion

        #region Methods

        [ChildActionOnly]
        public ActionResult Configure()
        {
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

        [ChildActionOnly]
        [FormValueRequired("save")]
        public ActionResult Configure(TaxStrikeIronModel model)
        {
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

        [ChildActionOnly]
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("testUsa")]
        public ActionResult TestUsa(TaxStrikeIronModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            model.TestingUsaResult = Test(model, model.TestingUsaZip);

            return View("~/Plugins/Tax.StrikeIron/Views/Configure.cshtml", model);
        }

        [ChildActionOnly]
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("testCanada")]
        public ActionResult TestCanada(TaxStrikeIronModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            model.TestingCanadaResult = Test(model, province: model.TestingCanadaProvinceCode);
            
            return View("~/Plugins/Tax.StrikeIron/Views/Configure.cshtml", model);
        }

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

                return string.IsNullOrEmpty(error) ? string.Format("Rate for zip {0}: {1}", zip, taxRate.ToString("p")) : error;
            }
            catch (Exception exc)
            {
                return exc.ToString();
            }
        }

        #endregion
    }
}