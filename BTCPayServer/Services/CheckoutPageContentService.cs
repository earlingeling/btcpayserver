using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Models.ServerViewModels;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Services
{
    public class CheckoutPageContentService
    {
        private readonly ApplicationDbContextFactory _dbContextFactory;

        public CheckoutPageContentService(ApplicationDbContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<CheckoutPageContentSettings> GetPageContentSettings()
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            var pageContents = await ctx.CheckoutPageContent
                .OrderBy(x => x.PageKey)
                .ThenBy(x => x.Language)
                .ToListAsync();

            var pages = pageContents
                .GroupBy(x => x.PageKey)
                .Select(g => new PageContent
                {
                    PageKey = g.Key,
                    Title = g.FirstOrDefault(x => x.Language == "en")?.Title ?? "",
                    Translations = new PageTranslations
                    {
                        Title = new MultiLanguageText
                        {
                            English = g.FirstOrDefault(x => x.Language == "en")?.Title ?? "",
                            Norwegian = g.FirstOrDefault(x => x.Language == "no")?.Title ?? "",
                            Swedish = g.FirstOrDefault(x => x.Language == "sv")?.Title ?? "",
                            Danish = g.FirstOrDefault(x => x.Language == "da")?.Title ?? ""
                        },
                        Content = new MultiLanguageText
                        {
                            English = g.FirstOrDefault(x => x.Language == "en")?.Content ?? "",
                            Norwegian = g.FirstOrDefault(x => x.Language == "no")?.Content ?? "",
                            Swedish = g.FirstOrDefault(x => x.Language == "sv")?.Content ?? "",
                            Danish = g.FirstOrDefault(x => x.Language == "da")?.Content ?? ""
                        }
                    }
                })
                .ToList();

            return new CheckoutPageContentSettings { Pages = pages };
        }

        public async Task<PageTranslations> GetPageTranslations(string pageKey)
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            var contents = await ctx.CheckoutPageContent
                .Where(x => x.PageKey == pageKey)
                .ToListAsync();

            return new PageTranslations
            {
                Title = new MultiLanguageText
                {
                    English = contents.FirstOrDefault(x => x.Language == "en")?.Title ?? "",
                    Norwegian = contents.FirstOrDefault(x => x.Language == "no")?.Title ?? "",
                    Swedish = contents.FirstOrDefault(x => x.Language == "sv")?.Title ?? "",
                    Danish = contents.FirstOrDefault(x => x.Language == "da")?.Title ?? ""
                },
                Content = new MultiLanguageText
                {
                    English = contents.FirstOrDefault(x => x.Language == "en")?.Content ?? "",
                    Norwegian = contents.FirstOrDefault(x => x.Language == "no")?.Content ?? "",
                    Swedish = contents.FirstOrDefault(x => x.Language == "sv")?.Content ?? "",
                    Danish = contents.FirstOrDefault(x => x.Language == "da")?.Content ?? ""
                }
            };
        }

        public async Task SavePageContent(string pageKey, PageTranslations translations)
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            var languages = new[] { "en", "no", "sv", "da" };
            
            foreach (var lang in languages)
            {
                var content = await ctx.CheckoutPageContent
                    .FirstOrDefaultAsync(x => x.PageKey == pageKey && x.Language == lang);

                var title = lang switch
                {
                    "en" => translations.Title.English,
                    "no" => translations.Title.Norwegian,
                    "sv" => translations.Title.Swedish,
                    "da" => translations.Title.Danish,
                    _ => ""
                };

                var textContent = lang switch
                {
                    "en" => translations.Content.English,
                    "no" => translations.Content.Norwegian,
                    "sv" => translations.Content.Swedish,
                    "da" => translations.Content.Danish,
                    _ => ""
                };

                if (content == null)
                {
                    content = new CheckoutPageContent
                    {
                        PageKey = pageKey,
                        Language = lang,
                        Title = title,
                        Content = textContent,
                        Created = DateTimeOffset.UtcNow,
                        Updated = DateTimeOffset.UtcNow
                    };
                    ctx.CheckoutPageContent.Add(content);
                }
                else
                {
                    content.Title = title;
                    content.Content = textContent;
                    content.Updated = DateTimeOffset.UtcNow;
                }
            }

            await ctx.SaveChangesAsync();
        }

        public async Task InitializeDefaultContent()
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            // Check if content already exists
            if (await ctx.CheckoutPageContent.AnyAsync())
                return;

            var defaultPages = new[]
            {
                new { PageKey = "crypto_intro", Title = "Introduction to Cryptocurrency" },
                new { PageKey = "kyc_info", Title = "KYC - Know Your Customer" },
                new { PageKey = "payment_guide", Title = "Payment Guide" }
            };

            foreach (var page in defaultPages)
            {
                foreach (var lang in new[] { "en", "no", "sv", "da" })
                {
                    var content = new CheckoutPageContent
                    {
                        PageKey = page.PageKey,
                        Language = lang,
                        Title = page.Title,
                        Content = $"Default content for {page.PageKey} in {lang}",
                        Created = DateTimeOffset.UtcNow,
                        Updated = DateTimeOffset.UtcNow
                    };
                    ctx.CheckoutPageContent.Add(content);
                }
            }

            await ctx.SaveChangesAsync();
        }
    }
}