#nullable enable
using System;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Data
{
    public class ProviderStepCalculation
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string ProviderName { get; set; } = string.Empty;
        
        [Required]
        public int StepNumber { get; set; }
        
        [Required]
        public string CalculationType { get; set; } = string.Empty;
        
        [Required]
        public string CalculationFormula { get; set; } = string.Empty;
        
        public string? DisplayFormat { get; set; }
        
        public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
        
        public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
    }
}