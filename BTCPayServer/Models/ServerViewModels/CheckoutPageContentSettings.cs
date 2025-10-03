using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Models.ServerViewModels
{
    public class CheckoutPageContentSettings
    {
        public List<PageContent> Pages { get; set; } = new();
    }

    public class PageContent
    {
        [Required]
        public string PageKey { get; set; } = string.Empty;
        
        public string Title { get; set; } = string.Empty;
        
        public PageTranslations Translations { get; set; } = new();
    }

    public class PageTranslations
    {
        [Display(Name = "Title")]
        public MultiLanguageText Title { get; set; } = new();
        
        [Display(Name = "Content")]
        public MultiLanguageText Content { get; set; } = new();
    }

}