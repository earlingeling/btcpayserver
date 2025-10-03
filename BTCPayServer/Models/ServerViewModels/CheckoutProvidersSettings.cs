using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Models.ServerViewModels
{
    public class CheckoutProvidersSettings
    {
        public List<Provider> Providers { get; set; } = new();
    }

    public class Provider
    {
        [Required]
        [Display(Name = "Provider Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Icon Class")]
        public string Icon { get; set; }

        [Required]
        [Display(Name = "Button Class")]
        public string ButtonClass { get; set; }

        [Required]
        [Display(Name = "Fee Text")]
        public string FeeText { get; set; }

        [Display(Name = "Badge Icon")]
        public string BadgeIcon { get; set; }

        [Display(Name = "Enabled Countries")]
        public List<string> EnabledCountries { get; set; } = new();

        [Display(Name = "Translations")]
        public ProviderTranslations Translations { get; set; } = new();
    }

    public class ProviderTranslations
    {
        [Display(Name = "Intro Text")]
        public MultiLanguageText IntroText { get; set; } = new();

        [Display(Name = "Steps")]
        public List<MultiLanguageStep> Steps { get; set; } = new();

        [Display(Name = "Outro Text")]
        public MultiLanguageText OutroText { get; set; } = new();
    }

    public class MultiLanguageText
    {
        [Display(Name = "English")]
        public string English { get; set; }

        [Display(Name = "Norwegian")]
        public string Norwegian { get; set; }

        [Display(Name = "Swedish")]
        public string Swedish { get; set; }

        [Display(Name = "Danish")]
        public string Danish { get; set; }
    }

    public class MultiLanguageStep
    {
        [Required]
        [Display(Name = "Step Number")]
        public int StepNumber { get; set; }

        [Display(Name = "Step Text")]
        public MultiLanguageText StepText { get; set; } = new();
    }
}