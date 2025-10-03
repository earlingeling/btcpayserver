using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Models.ServerViewModels
{
    public class ProviderStepCalculationSettings
    {
        [Required]
        public string ProviderName { get; set; } = string.Empty;
        
        public List<StepCalculation> Steps { get; set; } = new();
    }

    public class StepCalculation
    {
        [Required]
        public int StepNumber { get; set; }
        
        [Required]
        [Display(Name = "Calculation Type")]
        public string CalculationType { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Formula")]
        public string CalculationFormula { get; set; } = string.Empty;
        
        [Display(Name = "Display Format")]
        public string DisplayFormat { get; set; } = string.Empty;
        
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    public class CheckoutContentOverviewModel
    {
        public CheckoutPageContentSettings StaticPages { get; set; } = new();
        public CheckoutProvidersSettings Providers { get; set; } = new();
    }

    public class PageContentEditModel
    {
        [Required]
        public string PageKey { get; set; } = string.Empty;
        
        public string PageTitle { get; set; } = string.Empty;
        
        public PageTranslations Translations { get; set; } = new();
    }

    public class ProviderEditModel
    {
        [Required]
        public string ProviderName { get; set; } = string.Empty;
        
        public Provider Provider { get; set; } = new();
        
        public ProviderStepCalculationSettings Calculations { get; set; } = new();
    }
}