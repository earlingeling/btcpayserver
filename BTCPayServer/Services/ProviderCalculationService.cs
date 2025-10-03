using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Models.ServerViewModels;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Services
{
    public class ProviderCalculationService
    {
        private readonly ApplicationDbContextFactory _dbContextFactory;

        public ProviderCalculationService(ApplicationDbContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<ProviderStepCalculation>> GetProviderCalculations(string providerName)
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            return await ctx.ProviderStepCalculations
                .Where(x => x.ProviderName == providerName)
                .OrderBy(x => x.StepNumber)
                .ToListAsync();
        }

        public async Task<ProviderStepCalculationSettings> GetProviderCalculationSettings(string providerName)
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            var calculations = await ctx.ProviderStepCalculations
                .Where(x => x.ProviderName == providerName)
                .OrderBy(x => x.StepNumber)
                .ToListAsync();

            return new ProviderStepCalculationSettings
            {
                ProviderName = providerName,
                Steps = calculations.Select(c => new StepCalculation
                {
                    StepNumber = c.StepNumber,
                    CalculationType = c.CalculationType,
                    CalculationFormula = c.CalculationFormula,
                    DisplayFormat = c.DisplayFormat ?? "",
                    Description = $"Step {c.StepNumber} calculation"
                }).ToList()
            };
        }

        public async Task SaveProviderCalculations(string providerName, List<StepCalculation> calculations)
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            // Remove existing calculations for this provider
            var existingCalculations = await ctx.ProviderStepCalculations
                .Where(x => x.ProviderName == providerName)
                .ToListAsync();
            
            ctx.ProviderStepCalculations.RemoveRange(existingCalculations);

            // Add new calculations
            foreach (var calc in calculations)
            {
                var calculation = new ProviderStepCalculation
                {
                    ProviderName = providerName,
                    StepNumber = calc.StepNumber,
                    CalculationType = calc.CalculationType,
                    CalculationFormula = calc.CalculationFormula,
                    DisplayFormat = calc.DisplayFormat,
                    Created = DateTimeOffset.UtcNow,
                    Updated = DateTimeOffset.UtcNow
                };
                ctx.ProviderStepCalculations.Add(calculation);
            }

            await ctx.SaveChangesAsync();
        }

        public async Task AddProviderStep(string providerName, int stepNumber, string calculationType = "amount_due")
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            var calculation = new ProviderStepCalculation
            {
                ProviderName = providerName,
                StepNumber = stepNumber,
                CalculationType = calculationType,
                CalculationFormula = GetDefaultFormula(calculationType),
                DisplayFormat = GetDefaultDisplayFormat(calculationType),
                Created = DateTimeOffset.UtcNow,
                Updated = DateTimeOffset.UtcNow
            };

            ctx.ProviderStepCalculations.Add(calculation);
            await ctx.SaveChangesAsync();
        }

        public async Task RemoveProviderStep(string providerName, int stepNumber)
        {
            using var ctx = _dbContextFactory.CreateContext();
            
            var calculation = await ctx.ProviderStepCalculations
                .FirstOrDefaultAsync(x => x.ProviderName == providerName && x.StepNumber == stepNumber);

            if (calculation != null)
            {
                ctx.ProviderStepCalculations.Remove(calculation);
                await ctx.SaveChangesAsync();
            }
        }

        public string EvaluateCalculation(string formula, object invoiceData)
        {
            try
            {
                // This is a simplified evaluation - in a real implementation,
                // you might want to use a more robust expression evaluator
                // For now, we'll return the formula as-is for display purposes
                return formula;
            }
            catch (Exception ex)
            {
                return $"Error evaluating formula: {ex.Message}";
            }
        }

        private string GetDefaultFormula(string calculationType)
        {
            return calculationType switch
            {
                "amount_due" => "srvModel.orderAmountFiat",
                "dynamic_amount" => "(parseFloat(srvModel.orderAmountFiat.replace(/[^0-9.]/g, '')) / 100 * 1.05) + (0.00025 * parseFloat(srvModel.rate.replace(/[^0-9.]/g, '')) / 100) + 20",
                "custom" => "srvModel.orderAmountFiat",
                _ => "srvModel.orderAmountFiat"
            };
        }

        private string GetDefaultDisplayFormat(string calculationType)
        {
            return calculationType switch
            {
                "amount_due" => "{{$t('pmnt_due_amount')}}: <b>{{result}} kr</b>",
                "dynamic_amount" => "{{$t('pmnt_dynamic_amount')}}: <b>{{result}} kr</b>",
                "custom" => "Amount: <b>{{result}}</b>",
                _ => "Result: <b>{{result}}</b>"
            };
        }
    }
}