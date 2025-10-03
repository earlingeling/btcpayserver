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

        [HttpGet("server/checkout-providers/{providerId}")]
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> EditProvider(string providerId)
        {
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

        [HttpPost("server/checkout-providers/{providerId}")]
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
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
            return RedirectToAction("CheckoutContent");
        }

        [HttpPost("server/checkout-providers/{providerId}/add-step")]
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
        public async Task<IActionResult> AddProviderStep(string providerId, int stepNumber, string calculationType = "amount_due")
        {
            await _calculationService.AddProviderStep(providerId, stepNumber, calculationType);
            TempData[WellKnownTempData.SuccessMessage] = $"Step {stepNumber} added successfully";
            return RedirectToAction("EditProvider", new { providerId });
        }

        [HttpPost("server/checkout-providers/{providerId}/remove-step")]
        [Authorize(Policy = Client.Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
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
    }
}