using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Data;
using BTCPayServer.Models.ServerViewModels;
using BTCPayServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using AuthenticationSchemes = BTCPayServer.Abstractions.Constants.AuthenticationSchemes;

namespace BTCPayServer.Controllers
{
    public partial class UIServerController
    {

        [HttpGet("server/checkout-content")]
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> CheckoutContent()
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
                        Steps = GetStepsForProvider(p.ProviderName)
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

            return View("CheckoutContent", model);
        }

        [HttpGet("server/checkout-providers/page/{pageKey}")]
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
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
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
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
            return RedirectToAction("CheckoutContent");
        }

        [HttpGet("server/checkout-providers/provider/{providerId}")]
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> EditProvider(string providerId)
        {
            if (providerId == "add")
            {
                // Adding new provider
                var addModel = new ProviderEditModel
                {
                    ProviderName = "add",
                    Provider = new Provider
                    {
                        Name = "",
                        Icon = "fas fa-coins",
                        ButtonClass = "btn btn-primary",
                        FeeText = "3-8% avgift",
                        EnabledCountries = new List<string>(),
                        Translations = new ProviderTranslations
                        {
                            IntroText = new MultiLanguageText(),
                            OutroText = new MultiLanguageText(),
                            Steps = new List<MultiLanguageStep>()
                        }
                    },
                    Calculations = new ProviderStepCalculationSettings()
                };
                
                return View("EditProvider", addModel);
            }

            using var ctx = _dbContextFactory.CreateContext();
            
            // Load provider details
            var providerTranslation = await ctx.CheckoutProviderTranslations
                .FirstOrDefaultAsync(x => x.ProviderName == providerId && x.Language == "en");

            if (providerTranslation == null)
            {
                TempData[WellKnownTempData.ErrorMessage] = "Provider not found";
                return RedirectToAction("CheckoutContent");
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
                    Steps = GetStepsForProvider(providerTranslation.ProviderName)
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


        [HttpPost("server/checkout-providers/provider/{providerId}")]
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> SaveProvider(string providerId, ProviderEditModel model)
        {
            try
            {
                // Determine if this is a new provider or editing existing
                bool isNewProvider = providerId == "add";
                string finalProviderName = isNewProvider ? model?.Provider?.Name : providerId;

                // Validate provider name
                if (string.IsNullOrWhiteSpace(finalProviderName))
                {
                    return RedirectToAction("EditProvider", new { providerId });
                }

                // Check for duplicate names (only for new providers)
                if (isNewProvider)
                {
                    using var ctx = _dbContextFactory.CreateContext();
                    var existingProvider = await ctx.CheckoutProviderTranslations
                        .FirstOrDefaultAsync(p => p.ProviderName == finalProviderName);
                    
                    if (existingProvider != null)
                    {
                        return RedirectToAction("EditProvider", new { providerId });
                    }
                }

                // Save provider translations
                await SaveProviderTranslations(
                    finalProviderName,
                    model?.Provider?.Translations?.IntroText ?? new MultiLanguageText(),
                    model?.Provider?.Translations?.OutroText ?? new MultiLanguageText(),
                    model?.Provider?.Translations?.Steps ?? new List<MultiLanguageStep>(),
                    model?.Provider?.EnabledCountries ?? new List<string>()
                );

                // Save provider calculations
                await _calculationService.SaveProviderCalculations(finalProviderName, model?.Calculations?.Steps ?? new List<StepCalculation>());
            }
            catch (Exception ex)
            {
                // Log error but don't show to user for now
                System.Diagnostics.Debug.WriteLine($"Error saving provider: {ex.Message}");
            }

            return RedirectToAction("CheckoutContent");
        }

        [HttpPost("server/checkout-providers/provider/{providerId}/delete")]
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> DeleteProvider(string providerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(providerId))
                {
                    TempData[WellKnownTempData.ErrorMessage] = "Invalid provider ID";
                    return RedirectToAction("CheckoutContent");
                }

                using var ctx = _dbContextFactory.CreateContext();
                
                // Delete all translations for this provider
                var translations = await ctx.CheckoutProviderTranslations
                    .Where(p => p.ProviderName == providerId)
                    .ToListAsync();
                
                if (translations.Any())
                {
                    ctx.CheckoutProviderTranslations.RemoveRange(translations);
                    await ctx.SaveChangesAsync();
                    TempData[WellKnownTempData.SuccessMessage] = $"Provider '{providerId}' deleted successfully!";
                }
                else
                {
                    TempData[WellKnownTempData.ErrorMessage] = $"Provider '{providerId}' not found";
                }
            }
            catch (Exception ex)
            {
                TempData[WellKnownTempData.ErrorMessage] = $"Error deleting provider: {ex.Message}";
            }

            return RedirectToAction("CheckoutContent");
        }


        [HttpPost("server/checkout-providers/provider/{providerId}/add-step")]
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> AddProviderStep(string providerId, int stepNumber, string calculationType = "amount_due")
        {
            await _calculationService.AddProviderStep(providerId, stepNumber, calculationType);
            TempData[WellKnownTempData.SuccessMessage] = $"Step {stepNumber} added successfully";
            return RedirectToAction("EditProvider", new { providerId });
        }

        [HttpPost("server/checkout-providers/provider/{providerId}/remove-step")]
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> RemoveProviderStep(string providerId, int stepNumber)
        {
            await _calculationService.RemoveProviderStep(providerId, stepNumber);
            TempData[WellKnownTempData.SuccessMessage] = $"Step {stepNumber} removed successfully";
            return RedirectToAction("EditProvider", new { providerId });
        }


        private async Task SaveProviderTranslations(string providerName, MultiLanguageText introText, MultiLanguageText outroText, List<MultiLanguageStep> steps, List<string> enabledCountries)
        {
            using var ctx = _dbContextFactory.CreateContext();
            var languages = new[] { "en", "no", "sv", "da" };

            // Debug: Log what we're trying to save
            System.Diagnostics.Debug.WriteLine($"Saving provider: {providerName}, Countries: {string.Join(",", enabledCountries ?? new List<string>())}");

            foreach (var lang in languages)
            {
                var existing = await ctx.CheckoutProviderTranslations
                    .FirstOrDefaultAsync(x => x.ProviderName == providerName && x.Language == lang);

                if (existing == null)
                {
                    existing = new CheckoutProviderTranslation
                    {
                        ProviderName = providerName,
                        Language = lang
                    };
                    ctx.CheckoutProviderTranslations.Add(existing);
                }

                // Update provider details (use English as base)
                if (lang == "en")
                {
                    existing.EnabledCountries = JsonConvert.SerializeObject(enabledCountries);
                }

                // Update intro text
                existing.IntroText = lang switch
                {
                    "en" => introText.English,
                    "no" => introText.Norwegian,
                    "sv" => introText.Swedish,
                    "da" => introText.Danish,
                    _ => ""
                };

                // Update outro text
                existing.OutroText = lang switch
                {
                    "en" => outroText.English,
                    "no" => outroText.Norwegian,
                    "sv" => outroText.Swedish,
                    "da" => outroText.Danish,
                    _ => ""
                };

                // Update steps
                var providerSteps = steps.Select(s => new ProviderStep
                {
                    StepNumber = s.StepNumber,
                    StepText = lang switch
                    {
                        "en" => s.StepText.English,
                        "no" => s.StepText.Norwegian,
                        "sv" => s.StepText.Swedish,
                        "da" => s.StepText.Danish,
                        _ => ""
                    }
                }).ToList();

                existing.Steps = JsonConvert.SerializeObject(providerSteps);
                existing.Updated = DateTimeOffset.UtcNow;
            }

            await ctx.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"Successfully saved provider: {providerName}");
        }


        private string GetTranslation(string providerName, string language, string type)
        {
            using var ctx = _dbContextFactory.CreateContext();
            var translation = ctx.CheckoutProviderTranslations
                .FirstOrDefault(x => x.ProviderName == providerName && x.Language == language);
            
            return type switch
            {
                "intro" => translation?.IntroText ?? "",
                "outro" => translation?.OutroText ?? "",
                _ => ""
            };
        }

        private List<MultiLanguageStep> GetStepsForProvider(string providerName)
        {
            using var ctx = _dbContextFactory.CreateContext();
            var translations = ctx.CheckoutProviderTranslations
                .Where(x => x.ProviderName == providerName)
                .ToList();
            
            if (!translations.Any())
                return new List<MultiLanguageStep>();
            
            var englishStepsJson = translations.FirstOrDefault(x => x.Language == "en")?.Steps;
            var englishSteps = string.IsNullOrEmpty(englishStepsJson) 
                ? new List<ProviderStep>() 
                : JsonConvert.DeserializeObject<List<ProviderStep>>(englishStepsJson) ?? new List<ProviderStep>();
            
            return englishSteps.Select(step => new MultiLanguageStep
            {
                StepNumber = step.StepNumber,
                StepText = new MultiLanguageText
                {
                    English = GetStepTranslation(providerName, step.StepNumber, "en"),
                    Norwegian = GetStepTranslation(providerName, step.StepNumber, "no"),
                    Swedish = GetStepTranslation(providerName, step.StepNumber, "sv"),
                    Danish = GetStepTranslation(providerName, step.StepNumber, "da")
                }
            }).ToList();
        }

        private string GetStepTranslation(string providerName, int stepNumber, string language)
        {
            using var ctx = _dbContextFactory.CreateContext();
            var translation = ctx.CheckoutProviderTranslations
                .FirstOrDefault(x => x.ProviderName == providerName && x.Language == language);
            
            var step = translation?.StepsList?.FirstOrDefault(s => s.StepNumber == stepNumber);
            return step?.StepText ?? "";
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
    }
}