#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BTCPayServer.Client.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Services
{
    public class Language
    {
        public Language(string code, string displayName)
        {
            DisplayName = displayName;
            Code = code;
        }

        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("currentLanguage")] public string DisplayName { get; set; }
    }

    public class LanguageService
    {
        private readonly ILogger<LanguageService> _logger;
        private readonly Language[] _languages;
        private readonly Dictionary<string, JObject> _translations;

        public LanguageService(IWebHostEnvironment environment, ILogger<LanguageService> logger)
        {
            _logger = logger;
            _translations = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);

            // Load standard translations
            var standardPath = Path.Combine(environment.WebRootPath, "locales", "checkout");
            LoadTranslationsFromPath(standardPath);

            // Load custom translations
            var customPath = Path.Combine(environment.WebRootPath, "locales", "checkout", "custom");
            LoadTranslationsFromPath(customPath, isCustom: true);

            // Collect the languages from the translations
            _languages = _translations.Values
                .Select(t => t.ToObject<Language>())
                .Where(l => l != null)
                .ToArray()!;
        }

        private void LoadTranslationsFromPath(string path, bool isCustom = false)
        {
            if (!Directory.Exists(path))
            {
                _logger.LogWarning($"Directory not found: {path}");
                return;
            }

            var files = Directory.GetFiles(path, "*.json");
            foreach (var file in files)
            {
                try
                {
                    using var stream = new StreamReader(file);
                    var json = stream.ReadToEnd();
                    var langObject = JObject.Parse(json);

                    var langCode = langObject["code"]?.ToString();
                    if (string.IsNullOrEmpty(langCode))
                    {
                        _logger.LogWarning($"Language code not found in file {file}");
                        continue;
                    }

                    if (!_translations.TryGetValue(langCode, out var existingTranslation))
                    {
                        _translations[langCode] = langObject;
                    }
                    else
                    {
                        // Merge translations, custom translations override standard ones
                        existingTranslation.Merge(langObject, new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Union,
                            MergeNullValueHandling = MergeNullValueHandling.Merge
                        });
                        _translations[langCode] = existingTranslation;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not parse language file {file}");
                }
            }
        }

        public Language[] GetLanguages()
        {
            return _languages;
        }

        public IEnumerable<SelectListItem> GetLanguageSelectListItems()
        {
            return GetLanguages().Select(l => new SelectListItem
            {
                Value = l.Code,
                Text = l.DisplayName,
                Disabled = false
            }).OrderBy(lang => lang.Text);
        }

        public Language? FindLanguageInAcceptLanguageHeader(string? acceptLanguageHeader)
        {
            if (acceptLanguageHeader is null)
                return null;

            IDictionary<string, float> acceptedLocales = new Dictionary<string, float>();
            var locales = acceptLanguageHeader.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < locales.Length; i++)
            {
                try
                {
                    var oneLocale = locales[i];
                    var parts = oneLocale.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    var locale = parts[0];
                    var qualityScore = 1.0f;
                    if (parts.Length == 2)
                    {
                        var qualityScorePart = parts[1];
                        if (qualityScorePart.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                        {
                            qualityScorePart = qualityScorePart.Substring(2);
                            qualityScore = float.Parse(qualityScorePart, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            // Invalid format, continue with next
                            continue;
                        }
                    }

                    if (!locale.Equals("*", StringComparison.OrdinalIgnoreCase))
                    {
                        acceptedLocales[locale] = qualityScore;
                    }
                }
                catch (System.FormatException)
                {
                    // Can't use this piece, moving on...
                }
            }

            var sortedAcceptedLocales = from entry in acceptedLocales orderby entry.Value descending select entry;
            foreach (var pair in sortedAcceptedLocales)
            {
                var lang = FindLanguage(pair.Key);
                if (lang != null)
                {
                    return lang;
                }
            }

            return null;
        }

        public Language? FindLanguage(string locale)
        {
            var supportedLangs = GetLanguages();
            var split = locale.Split('-', StringSplitOptions.RemoveEmptyEntries);
            var lang = split[0];
            var country = split.Length == 2 ? split[1] : split[0].ToUpperInvariant();

            var langStart = lang + "-";
            var langMatches = supportedLangs
                .Where(l => l.Code.Equals(lang, StringComparison.OrdinalIgnoreCase) ||
                            l.Code.StartsWith(langStart, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var countryMatches = langMatches;
            var countryEnd = "-" + country;
            countryMatches = countryMatches.Where(l =>
                l.Code.EndsWith(countryEnd, StringComparison.OrdinalIgnoreCase)).ToList();
            return countryMatches.FirstOrDefault() ?? langMatches.FirstOrDefault();
        }

        public Language? AutoDetectLanguageUsingHeader(IHeaderDictionary headerDictionary, string? defaultLang)
        {
            if (headerDictionary?.TryGetValue("Accept-Language",
                out var acceptLanguage) is true && !string.IsNullOrEmpty(acceptLanguage))
            {
                return FindLanguageInAcceptLanguageHeader(acceptLanguage.ToString()) ?? FindLanguageInAcceptLanguageHeader(defaultLang);
            }
            return FindLanguageInAcceptLanguageHeader(defaultLang);
        }

        public JObject GetTranslations(string langCode)
        {
            if (_translations.TryGetValue(langCode, out var translation))
            {
                return translation;
            }
            return new JObject();
        }
    }
}
