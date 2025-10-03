using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Data;
using BTCPayServer.Models.ServerViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthenticationSchemes = BTCPayServer.Abstractions.Constants.AuthenticationSchemes;

namespace BTCPayServer.Controllers
{
    public partial class UIServerController
    {
        [HttpGet("server/checkout-providers")]
        public async Task<IActionResult> CheckoutProviders()
        {
            using var ctx = _dbContextFactory.CreateContext();
            var providers = await ctx.CheckoutProviderTranslations
                .Where(x => x.Language == "en") // Use English as the base
                .ToListAsync();

            var model = new CheckoutProvidersSettings
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

            return View(model);
        }

        [HttpPost("server/checkout-providers")]
        public async Task<IActionResult> CheckoutProviders(CheckoutProvidersSettings model, string command)
        {
            if (command == "add-provider")
            {
                model.Providers.Add(new Provider
                {
                    Name = "New Provider",
                    Icon = "fas fa-coins",
                    ButtonClass = "btn btn-outline-primary btn-sm",
                    FeeText = "0% avgift",
                    EnabledCountries = new List<string> { "Other" },
                    Translations = new ProviderTranslations
                    {
                        IntroText = new MultiLanguageText(),
                        Steps = new List<MultiLanguageStep>
                        {
                            new MultiLanguageStep { StepNumber = 1, StepText = new MultiLanguageText() },
                            new MultiLanguageStep { StepNumber = 2, StepText = new MultiLanguageText() }
                        }
                    }
                });
                return View(model);
            }

            if (command?.StartsWith("add-step:") == true)
            {
                var providerIndexStr = command.Substring("add-step:".Length);
                if (int.TryParse(providerIndexStr, out int providerIndex) && 
                    providerIndex >= 0 && providerIndex < model.Providers.Count)
                {
                    var provider = model.Providers[providerIndex];
                    var nextStepNumber = provider.Translations.Steps.Count + 1;
                    provider.Translations.Steps.Add(new MultiLanguageStep 
                    { 
                        StepNumber = nextStepNumber, 
                        StepText = new MultiLanguageText() 
                    });
                }
                return View(model);
            }

            if (command?.StartsWith("remove-step:") == true)
            {
                var parts = command.Substring("remove-step:".Length).Split(':');
                if (parts.Length == 2 && 
                    int.TryParse(parts[0], out int providerIndex) && 
                    int.TryParse(parts[1], out int stepIndex) &&
                    providerIndex >= 0 && providerIndex < model.Providers.Count &&
                    stepIndex >= 0 && stepIndex < model.Providers[providerIndex].Translations.Steps.Count)
                {
                    model.Providers[providerIndex].Translations.Steps.RemoveAt(stepIndex);
                    // Renumber remaining steps
                    for (int i = 0; i < model.Providers[providerIndex].Translations.Steps.Count; i++)
                    {
                        model.Providers[providerIndex].Translations.Steps[i].StepNumber = i + 1;
                    }
                }
                return View(model);
            }

            if (command?.StartsWith("remove-provider:") == true)
            {
                var providerIndexStr = command.Substring("remove-provider:".Length);
                if (int.TryParse(providerIndexStr, out int providerIndex) && 
                    providerIndex >= 0 && providerIndex < model.Providers.Count)
                {
                    var provider = model.Providers[providerIndex];
                    // Remove from database
                    using var ctx = _dbContextFactory.CreateContext();
                    var existingTranslations = await ctx.CheckoutProviderTranslations
                        .Where(x => x.ProviderName == provider.Name)
                        .ToListAsync();
                    ctx.CheckoutProviderTranslations.RemoveRange(existingTranslations);
                    await ctx.SaveChangesAsync();
                    
                    model.Providers.RemoveAt(providerIndex);
                }
                return View(model);
            }

            if (command == "Save")
            {
                // Save all providers to database
                foreach (var provider in model.Providers)
                {
                    await SaveProviderTranslations(provider);
                }
                
                TempData[WellKnownTempData.SuccessMessage] = StringLocalizer["Checkout provider settings saved successfully"].Value;
                return RedirectToAction(nameof(CheckoutProviders));
            }

            return View(model);
        }

        private async Task SaveProviderTranslations(Provider provider)
        {
            using var ctx = _dbContextFactory.CreateContext();
            var languages = new[] { "en", "no", "sv", "da" };
            var languageNames = new[] { "English", "Norwegian", "Swedish", "Danish" };

            foreach (var (lang, langName) in languages.Zip(languageNames))
            {
                var existing = await ctx.CheckoutProviderTranslations
                    .FirstOrDefaultAsync(x => x.ProviderName == provider.Name && x.Language == lang);

                if (existing == null)
                {
                    existing = new CheckoutProviderTranslation
                    {
                        ProviderName = provider.Name,
                        Language = lang
                    };
                    ctx.CheckoutProviderTranslations.Add(existing);
                }

                // Update provider details (use English as base)
                if (lang == "en")
                {
                    existing.IconClass = provider.Icon;
                    existing.ButtonClass = provider.ButtonClass;
                    existing.FeeText = provider.FeeText;
                    existing.BadgeIcon = provider.BadgeIcon;
                    existing.EnabledCountriesList = provider.EnabledCountries;
                }

                // Update intro text
                existing.IntroText = lang switch
                {
                    "en" => provider.Translations.IntroText.English,
                    "no" => provider.Translations.IntroText.Norwegian,
                    "sv" => provider.Translations.IntroText.Swedish,
                    "da" => provider.Translations.IntroText.Danish,
                    _ => ""
                };

                // Update outro text
                existing.OutroText = lang switch
                {
                    "en" => provider.Translations.OutroText.English,
                    "no" => provider.Translations.OutroText.Norwegian,
                    "sv" => provider.Translations.OutroText.Swedish,
                    "da" => provider.Translations.OutroText.Danish,
                    _ => ""
                };

                // Update steps
                var steps = provider.Translations.Steps.Select(s => new ProviderStep
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

                existing.StepsList = steps;
                existing.Updated = DateTimeOffset.UtcNow;
            }

            await ctx.SaveChangesAsync();
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
            var englishSteps = ctx.CheckoutProviderTranslations
                .FirstOrDefault(x => x.ProviderName == providerName && x.Language == "en");
            
            if (englishSteps?.StepsList == null) return new List<MultiLanguageStep>();

            return englishSteps.StepsList.Select(s => new MultiLanguageStep
            {
                StepNumber = s.StepNumber,
                StepText = new MultiLanguageText
                {
                    English = s.StepText,
                    Norwegian = GetStepTranslation(providerName, "no", s.StepNumber),
                    Swedish = GetStepTranslation(providerName, "sv", s.StepNumber),
                    Danish = GetStepTranslation(providerName, "da", s.StepNumber)
                }
            }).ToList();
        }

        private string GetStepTranslation(string providerName, string language, int stepNumber)
        {
            using var ctx = _dbContextFactory.CreateContext();
            var translation = ctx.CheckoutProviderTranslations
                .FirstOrDefault(x => x.ProviderName == providerName && x.Language == language);
            
            return translation?.StepsList?.FirstOrDefault(s => s.StepNumber == stepNumber)?.StepText ?? "";
        }
    }
}