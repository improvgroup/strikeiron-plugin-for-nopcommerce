using Nop.Web.Framework;

namespace Nop.Plugin.Tax.StrikeIron.Models
{
    public class TaxStrikeIronModel
    {
        [NopResourceDisplayName("Plugins.Tax.StrikeIron.LicenseKey")]
        public string LicenseKey { get; set; }

        [NopResourceDisplayName("Plugins.Tax.StrikeIron.TestingUsa.Zip")]
        public string TestingUsaZip { get; set; }

        public string TestingUsaResult { get; set; }

        [NopResourceDisplayName("Plugins.Tax.StrikeIron.TestingCanada.ProvinceCode")]
        public string TestingCanadaProvinceCode { get; set; }

        public string TestingCanadaResult { get; set; }
    }
}