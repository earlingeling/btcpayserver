# Checkout Content Management - Implementation Plan

## Overview
This branch implements database-driven content management for static checkout pages and enhanced provider management with dynamic calculations.

## Current State
- ✅ Provider system with database storage
- ✅ Multi-language provider translations
- ✅ Dynamic provider step display
- ❌ Static page content still in translation files
- ❌ Provider calculations not database-driven
- ❌ Individual provider management pages

## Implementation Phases

### Phase 1: Database Schema & Models (1-2 hours)
**Goal**: Create database structure for page content and provider calculations

#### Tasks:
1. **Create Migration**
   - `20250115000002_AddCheckoutPageContentAndCalculations.cs`
   - Add `CheckoutPageContent` table
   - Add `ProviderStepCalculations` table
   - Add indexes for performance

2. **Create Entities**
   - `CheckoutPageContent.cs` - Static page content storage
   - `ProviderStepCalculation.cs` - Provider step calculations
   - Update `ApplicationDbContext.cs`

3. **Database Schema**
   ```sql
   -- Static page content
   CREATE TABLE "CheckoutPageContent" (
       "Id" text PRIMARY KEY,
       "PageKey" text NOT NULL, -- "crypto_intro", "kyc_info", "payment_guide"
       "Language" text NOT NULL, -- "en", "no", "sv", "da"
       "Title" text,
       "Content" text NOT NULL, -- Plain text with \n line breaks
       "Created" timestamp with time zone NOT NULL,
       "Updated" timestamp with time zone NOT NULL
   );

   -- Provider step calculations
   CREATE TABLE "ProviderStepCalculations" (
       "Id" text PRIMARY KEY,
       "ProviderName" text NOT NULL,
       "StepNumber" int NOT NULL,
       "CalculationType" text NOT NULL, -- "amount_due", "dynamic_amount", "custom"
       "CalculationFormula" text NOT NULL, -- JavaScript-like formula
       "DisplayFormat" text, -- How to show the result
       "Created" timestamp with time zone NOT NULL,
       "Updated" timestamp with time zone NOT NULL
   );
   ```

### Phase 2: Services & ViewModels (1-2 hours)
**Goal**: Create business logic for content management

#### Tasks:
1. **Create ViewModels**
   - `CheckoutPageContentSettings.cs` - Page content management
   - `ProviderStepCalculationSettings.cs` - Provider calculations
   - Enhanced provider models

2. **Create Services**
   - `CheckoutPageContentService.cs` - CRUD operations for page content
   - `ProviderCalculationService.cs` - Provider calculation management
   - Formula evaluation logic

3. **Service Features**
   - Multi-language content loading
   - Calculation formula evaluation
   - Content validation and sanitization

### Phase 3: Admin Controllers (2-3 hours)
**Goal**: Create admin interface controllers

#### Tasks:
1. **Main Admin Controller**
   - `UIServerController.CheckoutContent.cs`
   - Overview page (`/server/checkout-providers`)
   - Page content editor (`/server/checkout-providers/page/{pageKey}`)

2. **Provider Detail Controller**
   - `UIServerController.CheckoutProviderDetail.cs`
   - Individual provider editor (`/server/checkout-providers/{providerId}`)
   - Step calculation management

3. **Controller Features**
   - CRUD operations for all content types
   - Multi-language form handling
   - Calculation formula validation

### Phase 4: Admin Views (2-3 hours)
**Goal**: Create admin interface views

#### Tasks:
1. **Main Overview View**
   - `CheckoutProviders.cshtml` - Updated overview with navigation
   - Static pages list
   - Providers list with step counts

2. **Page Content Editor**
   - `EditPageContent.cshtml` - Multi-language text editor
   - Tabbed interface for languages
   - Rich text area with line break support

3. **Provider Detail Editor**
   - `EditProvider.cshtml` - Complete provider management
   - Step calculation editor
   - Formula builder interface

## Static Content to Migrate

### Pages Identified:
1. **Crypto Introduction** (`crypto_intro`)
   - `pmnt_crypto_title`, `pmnt_crypto_content`, `pmnt_crypto_content_two`, `pmnt_crypto_content_three`, `pmnt_crypto_content_four`

2. **KYC Information** (`kyc_info`)
   - `pmnt_kyc_title`, `pmnt_kyc_content`, `pmnt_kyc_content_two`, `pmnt_kyc_content_three`, `pmnt_kyc_content_four`

3. **Payment Guide** (`payment_guide`)
   - `pmnt_guide_content`, `pmnt_guide_content_two`, `pmnt_guide_content_three`, `pmnt_guide_content_four`, `pmnt_guide_content_five`

### Calculation Formulas to Support:
1. **Amount Due**: `srvModel.orderAmountFiat`
2. **Dynamic Amount**: `(parseFloat(srvModel.orderAmountFiat.replace(/[^0-9.]/g, '')) / 100 * 1.05) + (0.00025 * parseFloat(srvModel.rate.replace(/[^0-9.]/g, '')) / 100) + 20`

## Admin Interface Design

### Navigation Structure:
```
/server/checkout-providers
├── 📄 Static Pages
│   ├── Crypto Introduction
│   ├── KYC Information
│   └── Payment Guide
└── 🏪 Payment Providers
    ├── Coinbase (3 languages, 5 steps)
    ├── Safello (2 languages, 3 steps)
    └── + Add New Provider
```

### Individual Provider Page:
```
/server/checkout-providers/{providerId}
├── Basic Info (Name, Icon, Button Style, Fee Text)
├── Country Availability (Checkboxes)
├── Translations (4 languages)
│   ├── Intro Text
│   ├── Steps (with calculations)
│   └── Outro Text
└── Step Calculations
    ├── Step 1: Amount Due
    ├── Step 2: Dynamic Amount
    └── Step 3: Custom Formula
```

## Technical Considerations

### Rich Text Storage:
- Store as plain text with `\n` line breaks
- Frontend will convert to HTML: `content.replace(/\n/g, '<br>')`
- No HTML formatting (bold/italic) - just paragraph structure

### Calculation System:
- JavaScript-like formula evaluation
- Access to `srvModel` properties
- Result formatting with templates
- Validation and error handling

### Performance:
- Cache page content in memory
- Load all languages for a page in single query
- Lazy load provider-specific content

## Success Criteria

✅ **Database Schema**: Tables created with proper indexes  
✅ **Admin Interface**: Complete CRUD for all content types  
✅ **Provider Management**: Individual provider pages working  
✅ **Calculation System**: Dynamic formulas working in admin  
✅ **Multi-language**: All content editable in 4 languages  
✅ **Navigation**: Clean admin interface with proper routing  

## Future Phases (Not in This Branch)

- Frontend checkout integration
- Translation file migration
- Content caching optimization
- Advanced formula builder UI

---

**Total Estimated Time**: 6-10 hours  
**Branch**: `feature/checkout-content-management`  
**Parent Branch**: `feature/checkout-provider-admin`