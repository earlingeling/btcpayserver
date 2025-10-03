using System;
using System.Threading.Tasks;
using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Data;
using BTCPayServer.Models.ServerViewModels;
using BTCPayServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthenticationSchemes = BTCPayServer.Abstractions.Constants.AuthenticationSchemes;

namespace BTCPayServer.Controllers
{
    public partial class UIServerController
    {
        private readonly CheckoutPageContentService _pageContentService;
        private readonly ProviderCalculationService _calculationService;

        [HttpGet("server/checkout-providers")]
        [Authorize(Policy = Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> CheckoutProviders()
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            // Load providers (English as base)
            var providers = await ctx.CheckoutProviderTranslations
                .Where(x => x.Language == "en")
                .ToListAsync();

            var providerSettings = new CheckoutProvidersSettings
            {
                Providers = providers.Select(p => new Provider
                {
                    Name = p.ProviderName,
                    Icon = p.IconClass ?? "fas fa-coins",
                    ButtonClass = p.ButtonClass ?? "btn btn-outline-primary btn-sm",
                    FeeText = p.FeeText ?? "0% avgift",
                    BadgeIcon = p.BadgeIcon,
                    EnabledCountries = p.EnabledCountriesList,
                    Translations = new ProviderTranslations
                    {
                        IntroText = new MultiLanguageText
                        {
                            English = p.IntroText ?? "",
                            Norwegian = GetTranslation(p.ProviderName, "no", "intro"),
                            Swedish = GetTranslation(p.ProviderName, "sv", "intro"),
                            Danish = GetTranslation(p.ProviderName, "da", "intro")
                        },
                        OutroText = new MultiLanguageText
                        {
                            English = p.OutroText ?? "",
                            Norwegian = GetTranslation(p.ProviderName, "no", "outro"),
                            Swedish = GetTranslation(p.ProviderName, "sv", "outro"),
                            Danish = GetTranslation(p.ProviderName, "da", "outro")
                        },
                        Steps = GetStepsForProvider(p.ProviderName, "en")
                    }
                }).ToList()
            };

            // Load page content
            var pageContentSettings = await _pageContentService.GetPageContentSettings();

            var model = new CheckoutContentOverviewModel
            {
                StaticPages = pageContentSettings,
                Providers = providerSettings
            };

            return View(model);
        }

        [HttpGet("server/checkout-providers/page/{pageKey}")]
        [Authorize(Policy = Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> EditPageContent(string pageKey)
        {
            var translations = await _pageContentService.GetPageTranslations(pageKey);
            
            var model = new PageContentEditModel
            {
                PageKey = pageKey,
                PageTitle = GetPageTitle(pageKey),
                Translations = translations
            };

            return View(model);
        }

        [HttpPost("server/checkout-providers/page/{pageKey}")]
        [Authorize(Policy = Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> SavePageContent(string pageKey, PageTranslations translations)
        {
            if (!ModelState.IsValid)
            {
                var model = new PageContentEditModel
                {
                    PageKey = pageKey,
                    PageTitle = GetPageTitle(pageKey),
                    Translations = translations
                };
                return View("EditPageContent", model);
            }

            await _pageContentService.SavePageContent(pageKey, translations);
            TempData[WellKnownTempData.SuccessMessage] = "Page content saved successfully";
            return RedirectToAction("CheckoutProviders");
        }

        [HttpGet("server/checkout-providers/{providerId}")]
        [Authorize(Policy = Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> EditProvider(string providerId)
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            // Load provider details
            var providerTranslation = await ctx.CheckoutProviderTranslations
                .FirstOrDefaultAsync(x => x.ProviderName == providerId && x.Language == "en");

            if (providerTranslation == null)
            {
                TempData[WellKnownTempData.ErrorMessage] = "Provider not found";
                return RedirectToAction("CheckoutProviders");
            }

            var provider = new Provider
            {
                Name = providerTranslation.ProviderName,
                Icon = providerTranslation.IconClass ?? "fas fa-coins",
                ButtonClass = providerTranslation.ButtonClass ?? "btn btn-outline-primary btn-sm",
                FeeText = providerTranslation.FeeText ?? "0% avgift",
                BadgeIcon = providerTranslation.BadgeIcon,
                EnabledCountries = providerTranslation.EnabledCountriesList,
                Translations = new ProviderTranslations
                {
                    IntroText = new MultiLanguageText
                    {
                        English = providerTranslation.IntroText ?? "",
                        Norwegian = GetTranslation(providerTranslation.ProviderName, "no", "intro"),
                        Swedish = GetTranslation(providerTranslation.ProviderName, "sv", "intro"),
                        Danish = GetTranslation(providerTranslation.ProviderName, "da", "intro")
                    },
                    OutroText = new MultiLanguageText
                    {
                        English = providerTranslation.OutroText ?? "",
                        Norwegian = GetTranslation(providerTranslation.ProviderName, "no", "outro"),
                        Swedish = GetTranslation(providerTranslation.ProviderName, "sv", "outro"),
                        Danish = GetTranslation(providerTranslation.ProviderName, "da", "outro")
                    },
                    Steps = GetStepsForProvider(providerTranslation.ProviderName, "en")
                }
            };

            // Load calculations
            var calculations = await _calculationService.GetProviderCalculationSettings(providerId);

            var model = new ProviderEditModel
            {
                ProviderName = providerId,
                Provider = provider,
                Calculations = calculations
            };

            return View(model);
        }

        [HttpPost("server/checkout-providers/{providerId}")]
        [Authorize(Policy = Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> SaveProvider(string providerId, ProviderEditModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("EditProvider", model);
            }

            // Save provider details (using existing logic from CheckoutProviders.cs)
            await SaveProviderTranslations(model.Provider);
            
            // Save calculations
            await _calculationService.SaveProviderCalculations(providerId, model.Calculations.Steps);

            TempData[WellKnownTempData.SuccessMessage] = "Provider saved successfully";
            return RedirectToAction("CheckoutProviders");
        }

        [HttpPost("server/checkout-providers/{providerId}/add-step")]
        [Authorize(Policy = Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> AddProviderStep(string providerId, int stepNumber, string calculationType = "amount_due")
        {
            await _calculationService.AddProviderStep(providerId, stepNumber, calculationType);
            TempData[WellKnownTempData.SuccessMessage] = $"Step {stepNumber} added successfully";
            return RedirectToAction("EditProvider", new { providerId });
        }

        [HttpPost("server/checkout-providers/{providerId}/remove-step")]
        [Authorize(Policy = Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> RemoveProviderStep(string providerId, int stepNumber)
        {
            await _calculationService.RemoveProviderStep(providerId, stepNumber);
            TempData[WellKnownTempData.SuccessMessage] = $"Step {stepNumber} removed successfully";
            return RedirectToAction("EditProvider", new { providerId });
        }

        private string GetPageTitle(string pageKey)
        {
            return pageKey switch
            {
                "crypto_intro" => "Crypto Introduction",
                "kyc_info" => "KYC Information", 
                "payment_guide" => "Payment Guide",
                _ => pageKey
            };
        }

        // Helper methods from existing CheckoutProviders.cs
        private string GetTranslation(string providerName, string language, string type)
        {
            using var ctx = _dbContextFactory.CreateContext();
            var translation = ctx.CheckoutProviderTranslations
                .FirstOrDefault(x => x.ProviderName == providerName && x.Language == language);

            string text = type switch
            {
                "intro" => translation?.IntroText ?? "",
                "outro" => translation?.OutroText ?? "",
                _ => ""
            };

            // If the text is empty for this language, fall back to English
            if (string.IsNullOrEmpty(text) && language != "en")
            {
                var englishTranslation = ctx.CheckoutProviderTranslations
                    .FirstOrDefault(x => x.ProviderName == providerName && x.Language == "en");
                text = type switch
                {
                    "intro" => englishTranslation?.IntroText ?? "",
                    "outro" => englishTranslation?.OutroText ?? "",
                    _ => ""
                };
            }
            return text;
        }

        private List<MultiLanguageStep> GetStepsForProvider(string providerName, string currentLanguage)
        {
            using var ctx = _dbContextFactory.CreateContext();
            var currentLanguageSteps = ctx.CheckoutProviderTranslations
                .FirstOrDefault(x => x.ProviderName == providerName && x.Language == currentLanguage);

            if (currentLanguageSteps?.StepsList == null || !currentLanguageSteps.StepsList.Any())
                return new List<MultiLanguageStep>();

            return currentLanguageSteps.StepsList.Select(step => new MultiLanguageStep
            {
                StepNumber = step.StepNumber,
                English = step.StepText,
                Norwegian = GetStepTranslation(providerName, step.StepNumber, "no"),
                Swedish = GetStepTranslation(providerName, step.StepNumber, "sv"),
                Danish = GetStepTranslation(providerName, step.StepNumber, "da")
            }).ToList();
        }

        private string GetStepTranslation(string providerName, int stepNumber, string language)
        {
            using var ctx = _dbContextFactory.CreateContext();
            var translation = ctx.CheckoutProviderTranslations
                .FirstOrDefault(x => x.ProviderName == providerName && x.Language == language);

            var stepText = translation?.StepsList?.FirstOrDefault(s => s.StepNumber == stepNumber)?.StepText ?? "";

            // If the text is empty for this language, fall back to English
            if (string.IsNullOrEmpty(stepText) && language != "en")
            {
                var englishTranslation = ctx.CheckoutProviderTranslations
                    .FirstOrDefault(x => x.ProviderName == providerName && x.Language == "en");
                stepText = englishTranslation?.StepsList?.FirstOrDefault(s => s.StepNumber == stepNumber)?.StepText ?? "";
            }
            return stepText;
        }

        private async Task SaveProviderTranslations(Provider provider)
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            var languages = new[] { "en", "no", "sv", "da" };
            
            foreach (var lang in languages)
            {
                var translation = await ctx.CheckoutProviderTranslations
                    .FirstOrDefaultAsync(x => x.ProviderName == provider.Name && x.Language == lang);

                var introText = lang switch
                {
                    "en" => provider.Translations.IntroText.English,
                    "no" => provider.Translations.IntroText.Norwegian,
                    "sv" => provider.Translations.IntroText.Swedish,
                    "da" => provider.Translations.IntroText.Danish,
                    _ => ""
                };

                var outroText = lang switch
                {
                    "en" => provider.Translations.OutroText.English,
                    "no" => provider.Translations.OutroText.Norwegian,
                    "sv" => provider.Translations.OutroText.Swedish,
                    "da" => provider.Translations.OutroText.Danish,
                    _ => ""
                };

                var steps = provider.Translations.Steps.Select(s => new ProviderStep
                {
                    StepNumber = s.StepNumber,
                    StepText = lang switch
                    {
                        "en" => s.English,
                        "no" => s.Norwegian,
                        "sv" => s.Swedish,
                        "da" => s.Danish,
                        _ => ""
                    }
                }).ToList();

                if (translation == null)
                {
                    translation = new CheckoutProviderTranslation
                    {
                        ProviderName = provider.Name,
                        Language = lang,
                        IntroText = introText,
                        OutroText = outroText,
                        Steps = System.Text.Json.JsonSerializer.Serialize(steps),
                        EnabledCountries = System.Text.Json.JsonSerializer.Serialize(provider.EnabledCountries),
                        IconClass = provider.Icon,
                        ButtonClass = provider.ButtonClass,
                        FeeText = provider.FeeText,
                        BadgeIcon = provider.BadgeIcon,
                        Created = DateTimeOffset.UtcNow,
                        Updated = DateTimeOffset.UtcNow
                    };
                    ctx.CheckoutProviderTranslations.Add(translation);
                }
                else
                {
                    translation.IntroText = introText;
                    translation.OutroText = outroText;
                    translation.Steps = System.Text.Json.JsonSerializer.Serialize(steps);
                    translation.EnabledCountries = System.Text.Json.JsonSerializer.Serialize(provider.EnabledCountries);
                    translation.IconClass = provider.Icon;
                    translation.ButtonClass = provider.ButtonClass;
                    translation.FeeText = provider.FeeText;
                    translation.BadgeIcon = provider.BadgeIcon;
                    translation.Updated = DateTimeOffset.UtcNow;
                }
            }

            await ctx.SaveChangesAsync();
        }
    }
}