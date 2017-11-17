using Nop.Core.Configuration;

namespace Nop.Plugin.Tax.StrikeIron
{
    public class StrikeIronTaxSettings : ISettings
    {
        /// <summary>
        /// StrikeIron License Key
        /// </summary>
        public string LicenseKey { get; set; }
    }
}