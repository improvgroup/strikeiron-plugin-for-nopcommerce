// Contributor(s): Bill Eisenman, ALOM Technologies, USA. Upgraded to TaxBasic v5

using System;
using System.Web.Routing;
using Nop.Core.Caching;
using Nop.Core.Plugins;
using Nop.Plugin.Tax.StrikeIron.TaxDataBasic;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Tax;

namespace Nop.Plugin.Tax.StrikeIron
{
    /// <summary>
    /// StrikeIron tax provider
    /// </summary>
    public class StrikeIronTaxProvider : BasePlugin, ITaxProvider
    {
        #region Constants

        private const string TAXRATEUSA_KEY = "Nop.taxrateusa.zipCode-{0}";
        private const string TAXRATECANADA_KEY = "Nop.taxratecanada.province-{0}";

        #endregion

        #region Fields

        private readonly ISettingService _settingService;
        private readonly ICacheManager _cacheManager;
        private readonly StrikeIronTaxSettings _strikeIronTaxSettings;

        #endregion

        #region Ctor

        public StrikeIronTaxProvider(ISettingService settingService,
            ICacheManager cacheManager,
            StrikeIronTaxSettings strikeIronTaxSettings)
        {
            this._settingService = settingService;
            this._cacheManager = cacheManager;
            this._strikeIronTaxSettings = strikeIronTaxSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets tax rate
        /// </summary>
        /// <param name="calculateTaxRequest">Tax calculation request</param>
        /// <returns>Tax</returns>
        public CalculateTaxResult GetTaxRate(CalculateTaxRequest calculateTaxRequest)
        {
            var result = new CalculateTaxResult();
            var address = calculateTaxRequest.Address;

            if (address == null)
            {
                result.AddError("Address is not set");
                return result;
            }
            if (address.Country == null)
            {
                result.AddError("Country is not set");
                return result;
            }

            string licenseKey = _strikeIronTaxSettings.LicenseKey;
            decimal taxRate;

            if (address.Country.TwoLetterIsoCode.ToLower() == "us")
            {
                if (String.IsNullOrEmpty(address.ZipPostalCode))
                {
                    result.AddError("Zip is not provided");

                    return result;
                }

                string error = "";

                taxRate = GetTaxRateUsa(address.ZipPostalCode, licenseKey, ref error);

                if (!String.IsNullOrEmpty(error))
                {
                    result.AddError(error);

                    return result;
                }
            }
            else if (address.Country.TwoLetterIsoCode.ToLower() == "ca")
            {
                if (address.StateProvince == null)
                {
                    result.AddError("Province is not set");

                    return result;
                }

                string error = "";

                taxRate = GetTaxRateCanada(address.StateProvince.Abbreviation, licenseKey, ref error);

                if (!String.IsNullOrEmpty(error))
                {
                    result.AddError(error);

                    return result;
                }
            }
            else
            {
                result.AddError("Tax can be calculated only for USA zip or Canada province");

                return result;
            }

            result.TaxRate = taxRate * 100;

            return result;
        }

        /// <summary>
        /// Gets a tax rate
        /// </summary>
        /// <param name="zipCode">zip</param>
        /// <param name="licenseKey">License key</param>
        /// <param name="error">Error</param>
        /// <returns>Tax rate</returns>
        public decimal GetTaxRateUsa(string zipCode, 
            string licenseKey,
            ref string error)
        {
            var key = string.Format(TAXRATEUSA_KEY, zipCode);
            decimal result = decimal.Zero;

            try
            {
                result = _cacheManager.Get(key, () =>
                {
                    var tax = Decimal.Zero;

                    var taxService = new TaxDataBasic.TaxDataBasic
                    {
                        LicenseInfoValue = new LicenseInfo
                        {
                            RegisteredUser = new RegisteredUser
                            {
                                UserID = licenseKey
                            }
                        }
                    };

                    // The GetTaxRateUS operation can now be called.  The output type for this operation is SIWSOutputOfTaxRateUSAData.
                    // Note that for simplicity, there is no error handling in this sample project.  In a production environment, any
                    // web service call should be encapsulated in a try-catch block.
                    var wsOutput = taxService.GetTaxRateUS(zipCode);

                    // The output objects of this StrikeIron web service contains two sections: ServiceStatus, which stores data
                    // indicating the success/failure status of the the web service request; and ServiceResult, which contains the
                    // actual data returne as a result of the request.

                    // ServiceStatus contains two elements - StatusNbr: a numeric status code, and StatusDescription: a string
                    // describing the status of the output object.  As a standard, you can apply the following assumptions for the value of
                    // StatusNbr:
                    //   200-299: Successful web service call (data found, etc...)
                    //   300-399: Nonfatal error (No data found, etc...)
                    //   400-499: Error due to invalid input
                    //   500+: Unexpected internal error; contact support@strikeiron.com
                    if ((wsOutput.ServiceStatus.StatusNbr >= 200) && (wsOutput.ServiceStatus.StatusNbr < 300))
                    {
                        tax = Convert.ToDecimal(wsOutput.ServiceResult.TotalUseTax);
                    }
                    else
                    {
                        // StrikeIron does not return SoapFault for invalid data when it cannot find a zipcode. 
                        throw new Exception(string.Format("[{0}] - {1}", wsOutput.ServiceStatus.StatusNbr,
                            wsOutput.ServiceStatus.StatusDescription));
                    }

                    return tax;
                });
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Gets a tax rate
        /// </summary>
        /// <param name="province">province</param>
        /// <param name="licenseKey">License key</param>
        /// <param name="error">Error</param>
        /// <returns>Tax rate</returns>
        public decimal GetTaxRateCanada(string province, 
            string licenseKey,
            ref string error)
        {
            string key = string.Format(TAXRATECANADA_KEY, province);
            decimal result = decimal.Zero;

            try
            {
                result = _cacheManager.Get(key, () =>
                {
                    var tax = decimal.Zero;

                    var taxService = new TaxDataBasic.TaxDataBasic
                    {
                        LicenseInfoValue = new LicenseInfo
                        {
                            RegisteredUser = new RegisteredUser
                            {
                                UserID = licenseKey
                            }
                        }
                    };

                    // The GetTaxRateCanada operation can now be called.  The output type for this operation is SIWSOutputOfTaxRateCanadaData.
                    // Note that for simplicity, there is no error handling in this sample project.  In a production environment, any
                    // web service call should be encapsulated in a try-catch block.
                    var wsOutput = taxService.GetTaxRateCanada(province);

                    // The output objects of this StrikeIron web service contains two sections: ServiceStatus, which stores data
                    // indicating the success/failure status of the the web service request; and ServiceResult, which contains the
                    // actual data returne as a result of the request.
                    // 
                    // ServiceStatus contains two elements - StatusNbr: a numeric status code, and StatusDescription: a string
                    // describing the status of the output object.  As a standard, you can apply the following assumptions for the value of
                    // StatusNbr:
                    //   200-299: Successful web service call (data found, etc...)
                    //   300-399: Nonfatal error (No data found, etc...)
                    //   400-499: Error due to invalid input
                    //   500+: Unexpected internal error; contact support@strikeiron.com
                    if ((wsOutput.ServiceStatus.StatusNbr >= 200) && (wsOutput.ServiceStatus.StatusNbr < 300))
                    {
                        tax = Convert.ToDecimal(wsOutput.ServiceResult.Total);
                    }
                    else
                    {
                        throw new Exception(string.Format("[{0}] - {1}", wsOutput.ServiceStatus.StatusNbr,
                            wsOutput.ServiceStatus.StatusDescription));
                    }

                    return tax;
                });
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "TaxStrikeIron";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Tax.StrikeIron.Controllers" }, { "area", null } };
        }
        
        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new StrikeIronTaxSettings()
            {
                LicenseKey = ""
            };

            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.StrikeIron.LicenseKey", "StrikeIron license key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.StrikeIron.LicenseKey.Hint", "Specify StrikeIron license key.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.StrikeIron.TestingUsa.Title", "Test Online Tax Service (USA)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.StrikeIron.TestingUsa.Zip", "Zip Code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.StrikeIron.TestingUsa.Zip.Hint", "Specify zip code for testing. For example, type '10001'.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.StrikeIron.TestingCanada.Title", "Test Online Tax Service (Canada)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.StrikeIron.TestingCanada.ProvinceCode", "Two Letter Province Code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.StrikeIron.TestingCanada.ProvinceCode.Hint", "Specify two letter province code for testing. For example, type 'ON'.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Tax.StrikeIron.TestService.Button", "Test service");

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<StrikeIronTaxSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Tax.StrikeIron.LicenseKey");
            this.DeletePluginLocaleResource("Plugins.Tax.StrikeIron.LicenseKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.StrikeIron.TestingUsa.Title");
            this.DeletePluginLocaleResource("Plugins.Tax.StrikeIron.TestingUsa.Zip");
            this.DeletePluginLocaleResource("Plugins.Tax.StrikeIron.TestingUsa.Zip.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.StrikeIron.TestingCanada.Title");
            this.DeletePluginLocaleResource("Plugins.Tax.StrikeIron.TestingCanada.ProvinceCode");
            this.DeletePluginLocaleResource("Plugins.Tax.StrikeIron.TestingCanada.ProvinceCode.Hint");
            this.DeletePluginLocaleResource("Plugins.Tax.StrikeIron.TestService.Button");

            base.Uninstall();
        }

        #endregion
    }
}
