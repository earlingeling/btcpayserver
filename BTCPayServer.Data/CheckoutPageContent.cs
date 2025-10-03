#nullable enable
using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Data
{
    public class CheckoutPageContent
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string PageKey { get; set; } = string.Empty;
        
        [Required]
        public string Language { get; set; } = string.Empty;
        
        public string? Title { get; set; }
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
        
        public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
    }
}