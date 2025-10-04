/**
 * Calculation Engine for BTCPay Server Checkout
 * Handles dynamic calculation of provider step text with placeholders
 */

class CalculationEngine {
    constructor() {
        this.calculations = new Map();
        this.srvModel = null;
    }

    /**
     * Initialize the calculation engine with provider data
     * @param {Array} providers - Array of provider objects with calculations
     */
    initialize(providers) {
        this.calculations.clear();
        
        providers.forEach(provider => {
            if (provider.calculations && provider.calculations.length > 0) {
                provider.calculations.forEach(calc => {
                    const key = `${provider.name}_${calc.stepNumber}_${calc.calculationType}`;
                    this.calculations.set(key, {
                        providerName: provider.name,
                        stepNumber: calc.stepNumber,
                        calculationType: calc.calculationType,
                        formula: calc.calculationFormula,
                        displayFormat: calc.displayFormat || '{{result}}'
                    });
                });
            }
        });
    }

    /**
     * Set the srvModel data for calculations
     * @param {Object} srvModel - The srvModel object with invoice data
     */
    setSrvModel(srvModel) {
        this.srvModel = srvModel;
    }

    /**
     * Process step text and replace calculation placeholders with calculated values
     * @param {string} stepText - The step text containing placeholders
     * @param {string} providerName - The name of the provider
     * @returns {string} - Processed step text with calculations replaced
     */
    processStepText(stepText, providerName) {
        if (!stepText || !this.srvModel) {
            return stepText;
        }

        // Find all calculation placeholders: {{calc_stepNumber_type_providerName}}
        const placeholderRegex = /\{\{calc_(\d+)_([^}]+)\}\}/g;
        let processedText = stepText;

        let match;
        while ((match = placeholderRegex.exec(stepText)) !== null) {
            const [fullMatch, stepNumber, rest] = match;
            
            // Parse the rest to separate type and provider
            // The format is: type_providerName
            // We need to find the last underscore to separate them
            const lastUnderscoreIndex = rest.lastIndexOf('_');
            if (lastUnderscoreIndex === -1) {
                continue;
            }
            
            const calculationType = rest.substring(0, lastUnderscoreIndex);
            const placeholderProviderName = rest.substring(lastUnderscoreIndex + 1);
            
            // Only process placeholders for the current provider
            if (placeholderProviderName !== providerName) {
                continue;
            }

            // Find the calculation
            const calculation = this.findCalculation(providerName, parseInt(stepNumber), calculationType);
            if (calculation) {
                try {
                    // Evaluate the formula
                    const result = this.evaluateFormula(calculation.formula);
                    
                    // Apply display format
                    const formattedResult = calculation.displayFormat.replace('{{result}}', result);
                    
                    // Replace placeholder with formatted result
                    processedText = processedText.replace(fullMatch, formattedResult);
                } catch (error) {
                    // Replace with error message or leave as is
                    processedText = processedText.replace(fullMatch, `[Calculation Error: ${error.message}]`);
                }
            } else {
                // Replace with placeholder text or leave as is
                processedText = processedText.replace(fullMatch, `[Calculation not found: ${calculationType}]`);
            }
        }

        return processedText;
    }

    /**
     * Find a calculation by provider, step number, and type
     * @param {string} providerName - The provider name
     * @param {number} stepNumber - The step number
     * @param {string} calculationType - The calculation type
     * @returns {Object|null} - The calculation object or null if not found
     */
    findCalculation(providerName, stepNumber, calculationType) {
        const key = `${providerName}_${stepNumber}_${calculationType}`;
        return this.calculations.get(key) || null;
    }

    /**
     * Safely evaluate a JavaScript formula using srvModel data
     * @param {string} formula - The JavaScript formula to evaluate
     * @returns {number} - The calculated result
     */
    evaluateFormula(formula) {
        if (!formula || !this.srvModel) {
            throw new Error('Formula or srvModel is missing');
        }

        try {
            // Create a safe evaluation context with srvModel data
            const context = {
                srvModel: this.srvModel,
                parseFloat: parseFloat,
                parseInt: parseInt,
                Math: Math,
                // Add any other safe functions that might be needed
            };

            // Create a function that evaluates the formula in the context
            const evaluateFunction = new Function('srvModel', 'parseFloat', 'parseInt', 'Math', `return ${formula}`);
            
            // Execute the formula
            const result = evaluateFunction(
                context.srvModel,
                context.parseFloat,
                context.parseInt,
                context.Math
            );

            // Ensure we return a number
            const numResult = parseFloat(result);
            if (isNaN(numResult)) {
                throw new Error('Formula result is not a valid number');
            }

            return numResult;
        } catch (error) {
            throw new Error(`Formula evaluation failed: ${error.message}`);
        }
    }

    /**
     * Get all available calculations for a provider
     * @param {string} providerName - The provider name
     * @returns {Array} - Array of calculation objects
     */
    getProviderCalculations(providerName) {
        const providerCalculations = [];
        this.calculations.forEach((calc, key) => {
            if (calc.providerName === providerName) {
                providerCalculations.push(calc);
            }
        });
        return providerCalculations;
    }

    /**
     * Check if a provider has calculations
     * @param {string} providerName - The provider name
     * @returns {boolean} - True if provider has calculations
     */
    hasCalculations(providerName) {
        return this.getProviderCalculations(providerName).length > 0;
    }
}

// Create global instance
window.calculationEngine = new CalculationEngine();

// Export for module systems if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CalculationEngine;
}