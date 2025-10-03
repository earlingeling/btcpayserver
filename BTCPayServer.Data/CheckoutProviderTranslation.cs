#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace BTCPayServer.Data
{
    public class CheckoutProviderTranslation
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ProviderName { get; set; } = string.Empty;

        [Required]
        public string Language { get; set; } = string.Empty;

        public string? IntroText { get; set; }

        public string? OutroText { get; set; }

        [Column(TypeName = "jsonb")]
        public string? Steps { get; set; }

        [Column(TypeName = "jsonb")]
        public string? EnabledCountries { get; set; }

        public string? IconClass { get; set; }

        public string? ButtonClass { get; set; }

        public string? FeeText { get; set; }

        public string? BadgeIcon { get; set; }

        public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;

        [NotMapped]
        public List<string> EnabledCountriesList
        {
            get => string.IsNullOrEmpty(EnabledCountries) ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(EnabledCountries) ?? new List<string>();
            set => EnabledCountries = JsonConvert.SerializeObject(value);
        }

        [NotMapped]
        public List<ProviderStep> StepsList
        {
            get => string.IsNullOrEmpty(Steps) ? new List<ProviderStep>() : JsonConvert.DeserializeObject<List<ProviderStep>>(Steps) ?? new List<ProviderStep>();
            set => Steps = JsonConvert.SerializeObject(value);
        }
    }

    public class ProviderStep
    {
        public int StepNumber { get; set; }
        public string StepText { get; set; } = string.Empty;
    }
}