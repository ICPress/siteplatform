# Financial Metrics — Definitions and Calculation Methodology

*A reader's guide to how the reported financial metrics are selected and calculated.*

This guide defines each metric's calculation, reporting scope, units and required evidence.

## Calculation conventions

The metrics describe a company's reported financial position, activity, profitability, cash flow, or capital structure. Financial values generally come from the company's financial statements and accompanying disclosures. Some are taken directly from a reported total; others are calculated from compatible reported figures.

**Four conventions apply throughout this guide:**

1. **Consistent company and reporting scope.** Components represent the same reporting entity and compatible consolidated, parent-attributable, segment, or security-class scope. A subsidiary, product, or note component does not automatically represent the whole company.
2. **Consistent period.** Income and cash-flow measures cover a period (a quarter or a fiscal year). Balance-sheet figures and period-end shares refer to a particular date. A year-to-date cash-flow or earnings amount is converted to a standalone quarter only when compatible prior cumulative amounts are available.
3. **Consistent units.** Monetary amounts retain the reporting currency; there is no automatic conversion to US dollars. Source amounts presented in thousands or millions are converted to full currency or share units. Per-share measures use currency per share, profitability margins use percentages, and leverage ratios use multiples.
4. **Sufficient evidence.** Directly reported figures are used when they have the right scope and meaning. Otherwise, a value is calculated from consistent components. If the necessary figures cannot be established reliably, a metric may be unavailable rather than estimated from an unrelated amount.

A standard XBRL tag or a dimensionless context does not by itself prove company-wide scope. In a combined filing, the service excludes other numeric registrant identifiers when the requested registrant's native CIK is present. A note subtotal, a segment disclosure or a security-award population needs its own scope assessment. Untagged HTML can also supply financial evidence when its company, date, columns, units and caption are sufficiently bound.

Amounts retain their filing currency, including BRL; no exchange-rate conversion is applied. Inline XBRL scale is applied once. Table percentages and monetary amounts are distinguished using nearby caption and unit evidence. Reporting precision can allow rounded observations to reconcile; it does not permit incompatible populations or units to be combined. Missing and nil observations are distinct from a reported zero.

Rendered EPS observations use currency-per-share units such as `USD/share` or
`CNY/share`. Native source units such as `USD/shares` remain accepted on the same
per-share basis and can remain in source provenance. A weighted-average share
count is a separate quantity: adjacent “Basic” or “Diluted” rows do not become EPS
merely because an EPS heading appears earlier in the same table. Explicit local
unit and scale captions take precedence over a broader table default.

Currency and scale are resolved independently: a proven currency is retained even
when the source does not state a scale. An explicit financial-statement reporting
currency declaration takes precedence over incidental transaction-currency mentions.
Qualified names such as Canadian dollars and New Taiwan dollars take precedence
over generic dollar wording. Unresolved foreign currency remains unresolved rather
than defaulting to USD. Currency, scale and the selected monetary column each need
support; resolving one does not validate the others.

### Useful calculation terms

Metric metadata describes three independent questions. `DirectCalculatedEstimated`
records derivation. `EvidenceQuality` records support for the interpretation:
`Supported`, `Corroborated`, or `IndependentlyReconciled`, with separate estimated,
conflicted and unassessed states. `PopulationCoverage` records `Complete`, `Partial`,
or `Unknown`; unknown is not zero. `AvailabilityReason` distinguishes supported
availability from insufficient evidence and processing states. A numeric result's
presence does not by itself imply complete coverage or independent reconciliation.
Validation witnesses alone establish corroboration; an independently reconciled
quality label requires an explicit accounting proof.

Report JSON keeps numeric values in each table's `Rows` and
lists omitted metrics in `UnavailableMetrics`. Each availability entry includes
the metric key, reason, evidence quality, population coverage and notes. A
processing-budget limit therefore remains distinguishable from insufficient
financial evidence after serialization.

A direct reported amount with established period, units and scope can remain
available without reconstructing the entire statement. Component populations need
compatible scope and non-overlap. Statement reconciliation uses disclosed precision;
unexplained residuals are not assigned to expense, interest or another category.
Separate operating income and gains are kept outside operating expense. Separately
reported interest is kept outside the canonical Other nonoperating result.

Native context periods can survive unresolved visual headers when an independent
closed financing or complementary permanent/temporary-equity bridge proves the
observation's role. Amounts embedded in row captions do not become liability cells.
HTML and native observations feed the same financial resolvers. Ordinary D&A on
the operations statement takes precedence over a cash-flow aggregate that also
includes discontinued operations. Generic ordinary D&A remains distinct from
strict operating policies for software/cloud amortization.

A complete supported debt total can include carrying balances with unknown maturity.
Those balances are not silently placed in noncurrent debt. The current subset is
qualified as partial and a complete noncurrent balance is unavailable while maturity
remains unresolved. Equity-classified perpetual instruments and debt-like assets
are excluded from liabilities. A bounded primary-statement reconstruction that
exceeds its candidate budget leaves unavailable affected metrics with
`ProcessingBudgetExceeded`; directly supported unrelated metrics remain available.

The default `PrimaryDocuments` ingestion mode uses the accession's XBRL ZIP when
available: only documents inside that archive can supply filing evidence, including
its schemas and presentation, label, definition and calculation linkbases. It does
not supplement the ZIP with SEC-generated `FilingSummary.xml` or `R*.htm` reports.
If the ZIP is confirmed absent (HTTP 404/410), it instead reads all HTML/XHTML files
listed in that accession's directory. This fallback is not truncated by
`MaxFilingPackageDocuments`. XBRL ZIP downloads have no configured compressed-size
limit. Expanded document-set and per-document byte limits still apply
(`MaxFilingDocumentSetBytes` and the per-document limits).
An inaccessible or invalid ZIP is not treated as proof that no ZIP exists.
`FullFilingPackage` uses broader generated-report enrichment.

- **Average balance:** unless a more specific reported basis is selected, the arithmetic mean of the beginning and ending balances: `(beginning balance + ending balance) / 2`.
- **Weighted-average shares:** a time-weighted average of shares outstanding throughout an earnings period, not the share count on the last day.
- **Standalone quarter:** activity during the three-month reporting period. When only cumulative year-to-date information is provided, `current YTD amount − preceding YTD amount` can be used for additive flow amounts. Weighted-average share counts are handled on a time-weighted basis, not by simply subtracting two share averages.
- **Successor reporting period:** an annual filing can present separate predecessor and successor accounting populations. A successor period retains its explicitly reported start and end dates, even when shorter than a year. Predecessor and successor earnings are not combined into an invented full-year result. Establishing reporting dates does not establish company-wide eligibility for every observation in that period.
- **Annualization for returns:** calculated quarterly or other short-period return-on-assets and return-on-equity figures may multiply the period return by `365 / number of days in the earnings period`. Full-year returns are not multiplied. Profit margins, debt ratios and earnings per share are not annualized this way.
- **Reported versus calculated:** a *reported* measure is the company's explicitly disclosed amount. A *calculated* measure is constructed from its underlying statement figures. These may differ when the company's definition includes adjustments.

## 1. Revenue and gross profit

| Metric | Calculation or reported basis | How to interpret it |
|---|---|---|
| **Total revenue** (`total_revenue`) | The company's consolidated revenue or sales for the reporting period. When no single complete total is available, compatible revenue components may be combined. | Represents the full appropriate revenue scope, not merely a segment or one category of customer receipts. |
| **Revenue from customer contracts** (`revenue_from_customer_contracts`) | Revenue reported under the accounting rules for contracts with customers (ASC 606 or the corresponding applicable disclosure). | Can be smaller than total revenue when the company also earns rental, interest, lease or other revenue outside customer-contract accounting. |
| **Net revenue** (`net_revenue`) | The issuer's stated net sales or net revenue after the reductions reflected in that measure. | Retained separately from gross revenue. |
| **Gross revenue** (`gross_revenue`) | The issuer's reported gross revenue before the deductions used to arrive at net revenue, when such a measure is disclosed. | Not automatically the same as gross billings, transaction volume or total sales handled for customers. |
| **Cost of revenue** (`cost_of_revenue`) | Reported cost of revenue or cost of sales; alternatively, `matching revenue − gross profit` when those totals reconcile. | Includes the direct costs assigned to earning the corresponding revenue. It covers the same activity as the revenue figure. |
| **Cost of products sold** (`cost_of_products_sold`) | Reported direct cost attributable to products sold. | Can exclude the cost of services and other revenue streams; it is not always the full company-wide cost of revenue. |
| **Gross profit** (`gross_profit`) | `Revenue − cost of revenue`, or a directly reported, comparable gross-profit total. | Profit after direct costs but before expenses presented below gross profit. |
| **Gross margin** (`core_gross_margin`) | `100 × gross profit / matching revenue`. | Percentage of revenue remaining after direct costs. |

**Example:** Revenue of 1,000 and cost of revenue of 600 produce gross profit of 400 and a gross margin of **40%**.

For a bank's net revenue presentation, compatible consolidated net interest
income before credit-loss provision plus total noninterest income establishes
the complete revenue population. Customer-contract fees remain a separate
subset. A gross-interest revenue note does not replace this net basis. The
statement occurrences, entity, currency and duration must agree. A
customer-revenue QName on ceded reinsurance commissions does not make those
acquisition-cost offsets customer-service revenue; physical accounting scope
and the revenue-recognition disclosure govern eligibility.

Gross profit may be zero or negative. Revenue of 1,000 and a complete matching cost of revenue of 1,100 produce gross profit of −100 and a gross margin of −10%. Reconstruction requires a complete cost population: a product-only cost or a cost caption explicitly excluding depreciation is insufficient by itself. A missing cost category is not treated as zero.

## 2. Operating results and other income

Expense scope is established from the actual occurrence: its caption, statement
section, surrounding subtotals and compatible reconciliation. A broad XBRL concept
such as `OperatingExpense` can describe direct operating costs rather than the
complete expense population. Repeated native occurrences of the same entity,
context, concept and amount retain that narrower scope even when a note's total
row has no caption. Components remain evidence; unidentified expense lines are
not summed merely because their arithmetic closes. A directly reported total
with established scope does not require disclosure of every underlying expense.

| Metric | Calculation or reported basis | How to interpret it |
|---|---|---|
| **Operating expenses** (`operating_expense`) | Reported expenses *below gross profit*, a complete sum of the corresponding operating expense categories, or a reconciled bridge between gross profit and operating income. | Generally includes categories such as selling, general and administrative expenses and research and development, depending on the statement. Costs already included in cost of revenue are not deducted a second time. |
| **Operating income** (`operating_income`) | The eligible company-wide operating profit reported on the income statement, or a fully supported operating-profit bridge. A simple statement without separately presented operating gains may use `gross profit − operating expenses`. | Profit generated by operations under the company's accounting presentation, before items reported outside operating income. |
| **Other income / expense** (`other_income_expense`) | The signed net amount of identifiable other income and expense outside the relevant operating result, using the statement's reported classification. | Positive income and negative expense are retained with their proper signs; financing items are not added twice. |
| **Other operating income** (`operating_other_income`) | A supported signed Other-income contribution included before reported operating income. | Kept separate from nonoperating Other. |
| **Pretax income** (`pretax_income`) | Reported profit before income taxes for the relevant reporting scope. | Includes applicable operating and nonoperating items before the income-tax charge or benefit. |
| **Operating margin** (`core_operating_margin`) | `100 × operating income / matching revenue`. | Measures operating profit as a percentage of revenue. |

An operating gain on disposal, restructuring classification, or another separately presented item can affect the bridge between gross profit, operating expenses and operating income. The relationship follows the statement's presentation; not every non-expense line is part of operating expenses.

Where a gain is included above reported operating income, the appropriate bridge can be `gross profit + operating disposition gain − operating income`. A gain shown below operating income is excluded from that bridge. Physical row order is evaluated within its source document and table; repeating the same native gain in a note does not change its demonstrated nonoperating placement. A cash-flow reconciliation adjustment is not proof of operating-income inclusion.

Some functional income statements report gross profit and pretax income without a separate operating subtotal. The library can derive operating expense and operating income from a bounded statement interval when every intervening numeric row is identified, operating charges precede the financing block, and the signed operating and nonoperating amounts independently reconcile to reported pretax income. That path requires the same native entity/context, period, currency, document, table and selected column. It accommodates different numbers of expense categories; it does not search arbitrary subsets for a convenient sum. A note's operating-cost subtotal is insufficient evidence for this company-wide bridge.

### Joint ventures, associates and equity-method earnings

The treatment follows the issuer's consolidated statement presentation. A caption such as "earnings from joint ventures" does not by itself make the amount an operating expense, financing income or revenue. Classification uses the physical statement interval, compatible entity/period/currency evidence and the reported subtotals. Repetition in a note or cash-flow reconciliation does not establish a different classification.

| Placement of the equity-method result | Treatment in the metrics |
|---|---|
| **Included before reported operating income** | Retained as a separate signed operating contribution. A profit increases operating income; a loss reduces it. It is not netted into the operating-expense population merely to make `gross profit − expenses` equal operating income. |
| **Below operating income and before pretax income** | Excluded from the operating-expense bridge. It remains in the reported pretax earnings scope when included in that subtotal. It is not an interest addback or deduction merely because it relates to an investment. |
| **Presented after tax or below a subtotal explicitly before equity-method income** | Not moved into pretax income. The narrower `pretax_income_before_equity_method_investments` measure is retained where identified; an after-tax investee result is not grossed up using an assumed tax rate. |

When equity-method earnings are included in operations, the supported bridge is:

```text
Operating income = Gross profit − Operating expenses + Signed operating JV/associate earnings
```

**Example:** Gross profit of 600, operating expenses of 300 and operating JV earnings of 100 produce operating income of **400**. Operating expenses remain **300**, rather than the 200 implied by subtracting operating income directly from gross profit. If the JV result is a loss of 100, operating income is **200**, with the same operating-expense amount of 300.

Separately identified equity-method income is kept outside a generic Other-income residual by default. A supported broader IFRS Other-finance presentation can include the separately presented associate result in its nonoperating Other population. The source presentation and selected scope determine that treatment; it is not a universal rule that all JV earnings belong in `other_income_expense`. A reported subtotal and its JV component are not both counted.

Generic EBIT uses the verified primary-statement pretax endpoint and the eligible financing adjustment. JV earnings already inside that endpoint remain there; an after-tax JV result is not added to it. Generic EBITDA then adds qualifying issuer D&A. The library does not use an equity-method earnings line as authority to add an investee's underlying revenue, expenses, D&A or borrowings to the issuer's consolidated totals. An issuer's separately disclosed proportionate or adjusted measure requires its own supported definition.

## 3. EBIT, EBITDA and their variants

These earnings measures use the following definitions.

### EBIT — earnings before interest and taxes

**Generic EBIT** (`ebit`) always uses the library's financing-adjusted earnings definition, calculated from the unadjusted financial-statement pretax amount and a validated nonoperating interest or financing bridge:

```text
Generic EBIT = Pretax income + net nonoperating financing expense
```

The financing adjustment is **positive** for an expense being added back and **negative** for net financing income being removed. If a company has pretax income of 200 and net nonoperating interest expense of 30, generic EBIT is **230**.

Separately identified debt-extinguishment losses and gains extend this financing adjustment only when supported by the primary statement or an independently closed note population matching its reported subtotal. A financing loss is added back and a financing gain removed. These components do not become pure interest expense. Ordinary D&A may be established by either separate depreciation/amortization rows or a combined D&A row in a closed, exact-period issuer EBITDA reconciliation; the resulting generic EBITDA still uses canonical generic EBIT.

Stock-based compensation, restructuring and acquisition costs remain within the unadjusted earnings used for generic EBIT. `ebit_reported` records an eligible issuer-disclosed, unadjusted EBIT amount with its source definition and accounting basis.

EBIT is a library-defined analytical measure derived from GAAP/IFRS statement evidence, rather than a universally standardized GAAP/IFRS subtotal. Fixed arithmetic improves comparability but still requires compatible entity, period, currency and financing scope. If the pretax endpoint or qualifying financing adjustment is unsupported, generic EBIT and its calculated dependents remain unavailable.

Where only a properly classified gross interest expense is available and no conflicting net-financing presentation exists, that amount may serve as the addback. Interest that forms part of core operations—such as funding costs at many banks and lenders—is not automatically added back. Unrelated finance costs are not treated as interest merely because they appear under a financial caption.

Pure interest expense excludes broader funding-cost totals containing items such as fair-value adjustments and securitization costs, even if the issuer tags the total as `InterestExpense`. A repeated occurrence captioned only “Total” or “Subtotal” retains the economic scope evidenced by its own table. An explicitly captioned interest-expense component in that same broader disclosure is assessed separately; the presence of other financing costs elsewhere in the table does not itself disqualify that component.

| Metric | Calculation or reported basis |
|---|---|
| **EBIT** (`ebit`) | Calculated from unadjusted pretax income and the library's validated nonoperating financing adjustment. |
| **Reported EBIT** (`ebit_reported`) | An eligible explicit issuer-reported, unadjusted EBIT figure, with its disclosed definition and source basis. |
| **Calculated EBIT** (`ebit_calculated`) | `Pretax income + qualifying net nonoperating financing expense`. |
| **EBIT margin** (`ebit_margin`) | `100 × canonical generic EBIT / corresponding total revenue`. |
| **Reported EBIT margin** (`ebit_margin_reported`) | An eligible explicitly disclosed issuer EBIT margin, with its stated numerator and revenue conventions. |
| **EBIT margin on total revenue** (`ebit_margin_on_total_revenue`) | Compatibility alias for `ebit_margin`, using the same resolved generic EBIT and canonical total revenue. |
| **Operating EBIT margin** (`operating_ebit_margin`) | `100 × operating income / corresponding revenue`. |

### EBITDA — earnings before interest, taxes, depreciation and amortization

**Calculated EBITDA** uses generic EBIT and a compatible measure of ordinary depreciation and amortization:

```text
Calculated EBITDA = Generic EBIT + qualifying depreciation and amortization
```

If generic EBIT is 230 and eligible depreciation and amortization total 40, calculated EBITDA is **270**.

Depreciation and amortization are not assumed to include impairments, natural-resource depletion, or every separately described software or cloud amortization item. Those amounts are included only in a measure whose stated definition permits them.

| Metric | Calculation or reported basis |
|---|---|
| **EBITDA** (`ebitda`) | Plain EBITDA on a supported calculated or eligible issuer-reported unadjusted basis, with derivation and source recorded in the result metadata. |
| **Reported EBITDA** (`ebitda_reported`) | The issuer's expressly reported unadjusted EBITDA. |
| **Calculated EBITDA** (`ebitda_calculated`) | `Generic EBIT + qualifying ordinary D&A`. |
| **Operating EBITDA** (`operating_ebitda`) | `Operating income + qualifying operating D&A`. |
| **Operating EBITA** (`operating_ebita`) | `Operating income + eligible intangible-asset or purchase-price-allocation amortization`; depreciation is not added back. |
| **Operating EBITDA proxy** (`operating_ebitda_proxy`) | An operating-income-plus-D&A approximation using a broader supported D&A basis. |
| **EBITDA including depletion** (`ebitda_including_depletion`) | Canonical generic EBIT plus qualifying DD&A, including depletion where that inclusion is explicit. |
| **EBITDAX** (`ebitdax`) | A resource-sector measure that includes specifically supported exploration-related adjustments in addition to the defined EBITDA basis. |
| **Adjusted EBITDA** (`adjusted_ebitda`) | An issuer-reported adjusted measure, using the company's disclosed reconciliation and adjustments. |
| **Core EBITDA** (`core_ebitda`) | The issuer's labeled core EBITDA, supported by its disclosure. |

## 4. Net income, earnings per share and share-based compensation

| Metric | Calculation or reported basis | How to interpret it |
|---|---|---|
| **Consolidated net income** (`net_income_consolidated`) | Profit or loss for the consolidated group after taxes, including amounts attributable to noncontrolling interests where applicable. | A group-wide profit measure. |
| **Net income from continuing and discontinued operations** (`net_income_continuing_and_discontinued`) | The reported total of continuing and discontinued operations for the period. | Does not treat profit from continuing operations alone as the total. |
| **Net income** (`kpi_netincome`) | The relevant issuer-reported net-income KPI, with its reporting scope retained. | Uses the reporting scope attached to the issuer's KPI. |
| **Net income attributable to common stockholders** (`net_income_common_stockholders`) | Net income identified as attributable or available to common stockholders, using the disclosed earnings-claim basis. | Earnings assigned to the disclosed common-shareholder claim. |
| **Net income available to common shareholders** (`net_income_available_to_common_shareholders`) | Profit available to holders of common shares after any applicable preferred or senior equity claims. | Common-shareholder earnings used for relevant per-share or equity-return comparisons. |
| **Basic earnings per share** (`basic_eps`) | Eligible reported basic EPS for the selected security. Its accounting identity is `earnings allocated to that security / its basic weighted-average shares`. | Matches earnings allocation and basic weighted-average shares for the selected security. |
| **Diluted earnings per share** (`diluted_eps`) | Eligible reported diluted EPS for the selected security, with the applicable dilutive numerator and denominator basis. An explicitly combined basic-and-diluted presentation can also establish this value. | Potential shares are included only when dilutive, with any necessary numerator adjustments. Equal rounded basic and diluted EPS alone does not prove equivalent components. |
| **Share-based compensation expense** (`core_share_based_compensation`) | The company's recognized period expense for employee or other share-based payment awards, drawn from the relevant company-wide expense information. | Not the total value of awards granted or the balance of outstanding options. |

**Example:** Earnings available to common shareholders of 120 million and basic weighted-average shares of 60 million give basic EPS of **2.00 currency units per share**. If diluted weighted-average shares are 62 million, with no numerator adjustment, diluted EPS is approximately **1.94**.

EPS reconciliations respect the stated precision of the earnings numerator, share denominator and displayed EPS. Equal basic and diluted EPS does not establish an identical denominator without supporting evidence. Company-wide share compensation can come from an explicit stock-compensation expense caption; a fund- or program-specific expense narrative does not establish total company expense, and an after-tax amount is not silently promoted to a pretax expense.

Preferred distributions or redemption-value accretion can reduce income available to common shareholders below parent net income. An explicitly reported issuer-wide EPS numerator adjusted for those claims may supply common income when it reconciles to the reported EPS and weighted-average shares. A sole listed ticker attached only to the denominator row does not itself establish different claimants: the native contexts must independently identify dimensionless, company-wide observations of the same entity and period, and the filing must have only one mapped security. Missing context/entity evidence, stock-class dimensions, multiple mapped securities and incompatible EPS cannot establish this equivalence. Class-specific allocations remain separate from issuer-wide common income.

### Security-scoped selection and EPS handling

The requested company's ticker is the security-selection input. A filing can map native stock-class observations to `SecurityClassTicker` and `SecurityClassTitle`; native dimensions remain separate evidence. A report can contain both issuer-wide financial metrics and security-specific EPS components. Attaching the report to one ticker does not make all its metrics class-specific.

`SelectSecurityScoped` selects the following metrics and internal components:

| Metric or internal component | Selection predicate | Scope and publication |
|---|---|---|
| `basic_average_shares` | `IsGenericPerShareFact` | Selects the requested security's basic weighted-average EPS denominator when mapped; does not automatically sum classes. |
| `diluted_average_shares` | `IsGenericPerShareFact` | Selects the requested security's diluted weighted-average EPS denominator when mapped; does not automatically sum classes or conversion populations. |
| Internal basic EPS numerator (`ConceptCatalog.BasicEpsNumerator`) | `IsEpsNumeratorFact` | An internal component used for EPS reconciliation and guarded income mappings on the established security basis. |
| `diluted_ni_available_to_common_stockholders` (directly reported numerator selection) | `IsEpsNumeratorFact` | Selects a directly reported diluted EPS numerator on the eligible security basis. If absent, the guarded alternatives described below may supply the published metric. |

`SelectSecurityScoped` first searches across all candidate concepts for an eligible fact mapped to the requested ticker, with the exact earnings period, accession and form. It resolves that security population before ordinary concept/catalog precedence. All four selections enable `requireRequestedSecurityWhenMapped` when the filing contains more than one distinct mapped security ticker. If the requested ticker is mapped but no eligible matching component exists, this direct selection returns no value. Numerator flow selection also applies the requested-security predicate in that multiple-mapped-security case.

When the strict multiple-security guard does not apply, `SelectSecurityScoped` can select eligible candidates through `SelectDirect` under the supplied predicate. `IsEpsNumeratorFact` rejects unmapped explicit stock-class dimensions and stock-class classifications; it can admit eligible issuer-wide numerators. `IsGenericPerShareFact` rejects observations mapped to another ticker and, once the requested ticker is mapped, rejects unmapped explicit class observations and suppresses dimensionless duplicates when a matching mapped observation of the same concept and period exists. Without a requested-security mapping, eligible class-axis observations can still remain candidates. The multiple-security guard counts **mapped tickers, not all legal classes**; an unlisted Class B or incomplete mapping therefore does not establish a complete class inventory.

EPS itself follows a different selection path:

| Metric | Current selection |
|---|---|
| `basic_eps` | `SelectDirect(ConceptCatalog.BasicEps, ..., IsGenericPerShareFact)`. |
| `diluted_eps` | `SelectDirect(ConceptCatalog.DilutedEps, ..., IsGenericPerShareFact)`, with a narrowly supported mapping from an explicitly combined basic-and-diluted EPS row if a separate diluted value is missing. |
| `adjusted_eps` | `SelectDirect(ConceptCatalog.AdjustedEps, ..., IsGenericPerShareFact)`; remains a separate issuer-adjusted measure. |

These EPS selections are security-filtered even though they do not call `SelectSecurityScoped`. The core calculator selects reported EPS rather than generally reconstructing missing EPS by dividing published net income by published shares. The arithmetic example above explains the accounting identity; it is not permission to combine independently selected income and denominator rows. A qualifying EPS footnote can supply the class evidence: the parser does not universally bypass notes or force selection from the face statement.

For diluted numerator selection, the calculator prefers the directly reported numerator. If missing, it considers, in order: a broad income occurrence validated in the same EPS disclosure; a validated basic EPS numerator; a validated issuer-wide common-income pool when there are no competing mapped securities; and a validated anti-dilution/no-dilution equivalence. Each alternative has its own period, filing, entity, security-scope and EPS-identity checks. A broad income occurrence must belong to the same EPS table and uniquely reconcile on the eligible security basis; arithmetic coincidence alone is insufficient. The code does not manufacture an exact numerator by multiplying rounded EPS by shares.

`ValidateReportedEpsDenominator` checks eligible reported shares against the corresponding reported EPS and matching numerator within the filing's reporting precision. When the required compatible evidence exists and the identity fails, the denominator becomes unavailable. **When the matching numerator or required compatibility evidence is absent, this check leaves the selected denominator unchanged; publication alone does not prove a completed EPS reconciliation.** Metric publication preserves the statement-aware basic/diluted share result, including an unavailable result. An explicitly combined basic-and-diluted share row, or supported no-dilution disclosure, may establish a missing diluted denominator; equal rounded EPS alone cannot.

The JSON source model retains context IDs, dimensions and security mappings. EPS components require a validated common security and earnings-allocation basis.

## 5. Share counts and book value per share

### Period-end shares and average shares are different

| Metric | Calculation or reported basis | Used for |
|---|---|---|
| **Common shares outstanding at period end** (`common_shares_outstanding_period_end`) | Eligible shares legally outstanding on the balance-sheet date. When supported, `issued common shares − treasury shares`, or the sum of distinct outstanding common-share classes. | Book value per share and point-in-time ownership measures. |
| **Basic average shares** (`basic_average_shares`) | Basic weighted-average shares for the selected EPS security/population, when established. Conceptually, `sum(shares outstanding × time outstanding) / total period time` within that population. | Basic EPS on the matching class and earnings-allocation basis; not automatically an issuer-wide sum. |
| **Diluted average shares** (`diluted_average_shares`) | Diluted weighted-average shares for the selected EPS security/population, reflecting eligible dilutive options, awards, convertibles or other potential shares. | Diluted EPS on the matching numerator and conversion basis; not automatically an issuer-wide sum. |
| **Latest practicable-date shares** (`common_shares_outstanding_latest_practicable_date`) | A dated share count, commonly presented on a filing cover page after the balance-sheet date. | An ownership snapshot at the stated practicable date. |

**Example:** If 100 million shares were outstanding for half the period and 120 million for the other half, basic weighted-average shares are **110 million**, while period-end shares are **120 million**. A stock split, change in share class or issuer EPS restatement can require the reported weighted-average figure to be adjusted on its stated basis.

**Share-class example:** If the selected security has 100 million basic weighted-average shares and another class has 20 million, `basic_average_shares` records the selected security's 100 million. For period-end shares, legally outstanding counts of 110 million and 25 million across two nonoverlapping common classes give 135 million. Period-end and latest-practicable shares use their own selectors. Aggregating diluted class counts requires evidence about conversion and dilution overlap.

Authorized shares, unexercised options, restricted stock awards and dollar-denominated repurchase authorizations are not equivalent to shares actually outstanding. Where a company has multiple common classes, only nonoverlapping, legally outstanding classes are added together. An American Depositary Share (ADS) may represent a different number of underlying ordinary shares; its disclosed conversion ratio is needed before the bases can be compared.

Dates describing the inception of a repurchase program are temporal evidence, not share quantities. A valid small count such as `2,018 common shares outstanding` remains eligible when it is bound to the correct security and reporting date.

When exact outstanding or treasury-share counts are unavailable, a **retirement-based estimate** may use the reported issued count if the same filing establishes the retirement treatment, compatible same-period repurchased and retired share counts agree, and a dated balance-sheet or equity-statement closing balance reports zero treasury-stock carrying value. Conflicting share counts, incompatible classes, different reporting periods or unretired purchases prevent this estimate. The estimate assumes no undisclosed remaining treasury shares or incompatible legal-share claims. A rounded monetary zero does not prove exactly zero treasury shares; currency is not converted to shares using an assumed price. The outstanding-share metric and dependent book value per share retain **Estimated** status, evidence quality and the assumptions. Supported direct outstanding counts and compatible issued-minus-treasury share-count reconciliations take priority.

Comparative captions containing both issued and outstanding counts are bound to their dated columns before selecting the current outstanding count. The supported complex-caption path also requires a separately dated closing share count in the equity statement. Issued-minus-treasury arithmetic needs compatible share-count evidence; a monetary capital balance does not establish a share multiplier. When exact and rounded observations describe one legal class, their reporting intervals must intersect before the more precise count is retained. Conflicting counts, comparative years and restricted award subsets cannot be combined into an issuer total.

A dated, locally bound assertion that shares are "issued and outstanding,
excluding treasury shares" establishes an outstanding count even when the native
concept identifies issued shares. Treasury shares are not subtracted again.
Issued-minus-treasury arithmetic requires evidence that the issued operand is
gross issued capital, with compatible registrant, security-class and date scope.
The issued-share concept name alone does not authorize subtraction, and unrelated
narrative cannot override the selected count.

### Equity and book value

| Metric | Calculation or reported basis | How to interpret it |
|---|---|---|
| **Stockholders' equity attributable to parent** (`stockholders_equity_attributable_to_parent`) | Total consolidated equity less the portion attributable to noncontrolling interests, or the directly reported parent-attributable total. | Owners' reported book equity, not group equity including minority owners. |
| **Book value per share** (`core_book_value_per_share`) | `Eligible common book equity / period-end common shares outstanding`. Common equity is based on attributable equity after deducting eligible preferred-stock **carrying value**, where applicable. | Uses period-end legal shares, not basic or diluted average shares. |

**Example:** Parent equity of 1,000 million, preferred-stock carrying value of 100 million, and 90 million common shares outstanding produces common book value of **10.00 per share**.

A company's separately reported *economic* or *tangible* book value per share can use different deductions (for example, preferred liquidation preference or intangible assets). Those are different measures from the book-value calculation above.

The common book-equity denominator excludes a legal common class only when the
filing explicitly establishes that its holders have neither dividend nor
liquidation rights in that equity. That rights evidence remains in provenance;
the general legal shares-outstanding count is retained separately. Nonvoting
shares are not excluded merely because they lack votes. Different native class
members can also represent overlapping issued and outstanding populations;
their class and legal-share basis must be resolved before addition.

Total consolidated equity, parent-attributable equity and the noncontrolling-interest component remain separate. A standard total-equity tag whose name mentions noncontrolling interests is not itself the minority-interest component. Leverage ratios use consolidated permanent equity, while common book value uses the appropriate parent/common claim.

Noncontrolling interests embedded in common stock or additional paid-in capital
remain claims of other owners. Deduct them once when they are proven to be inside
the selected starting equity balance. A directly supported parent-attributable
balance is not reduced again. An aggregate NCI disclosure that includes redeemable
NCI is one population: its redeemable subset is not another deduction. If part of
that aggregate is separately presented outside permanent equity, remove that part
from the aggregate before calculating the NCI embedded in permanent equity.

A subsidiary, VIE or other partial NCI disclosure does not establish the complete
NCI population merely because its native context has no dimensions. Unresolved
ownership coverage cannot become zero through an equity or book-value fallback.
Independently supported parent equity and consolidated leverage equity remain
usable on their respective claim bases. Each ROE endpoint uses its own dated
claims; preferred carrying value is not deducted from an endpoint already
reconciled as common equity.

Temporary or redeemable equity is kept outside permanent equity. The accounting equation is `assets = liabilities + temporary equity + permanent equity`. A custom native temporary-equity observation can be retained despite an unresolved visible date header only when its native entity/context and unit match, its caption and position support temporary equity between liabilities and total equity, and its reported amount independently closes that equation. This uses an observed amount; it does not manufacture temporary equity from a residual or assume a missing amount is zero.

Temporary claims retain their reported signs, including negative mezzanine
balances and net-of-dividends amounts. A redeemable-noncontrolling-interest
subtotal covers that category; it does not necessarily include separate temporary
preferred-stock claims. Aggregate and class-specific claims are counted once.
Stock-class dimensions are admitted for this population only through a closed
physical/class reconciliation, rather than a general relaxation of scope rules.

The actual occurrence and ownership evidence determine whether equity is parent
or consolidated equity; a broad IFRS equity concept does not establish that it
includes noncontrolling interests. Incomplete or misclassified equity evidence
must not invalidate independently supported reported liabilities and then supply
a circular reconstruction of those liabilities. Beginning and ending equity
populations are assessed independently. These rules qualify the inputs to book
value, leverage and ROE; they do not change their formulas or ROE annualization.

## 6. Debt, leases and leverage

The leverage measures use **interest-bearing financing debt on a carrying-value basis**, with separate lease treatment for US GAAP and IFRS. A debt aggregate is not added to the same borrowing components again.

### Financing debt included in leverage

| Reporting basis | Borrowings and lease obligations included | Excluded or kept separate |
|---|---|---|
| **US GAAP** | Interest-bearing short-term borrowings, current maturities of long-term debt, noncurrent borrowing balances and qualifying **finance/capital lease liabilities**, provided those leases are not already included in the borrowing figure. | Operating lease liabilities and nonfinancing operating payables. |
| **IFRS** | Interest-bearing current and noncurrent borrowings and separately presented lease liabilities where they are not already contained in the borrowing totals. | Duplicate debt or lease components, unrelated derivative liabilities and nonfinancing obligations. |

Reported borrowing balances and standardized leverage debt may therefore differ. For example, a company's long-term debt line might exclude separately shown finance leases that are included in the standardized leverage measure.

Eligible financing liabilities include separately presented subordinated debt,
trust pass-through borrowings and sold-repurchase-agreement liabilities. Their
membership, carrying basis and overlap with reported totals require the same
evidence as other borrowings. Under IFRS, debt securities issued as a proven
liability-side primary-statement borrowing are distinguished from debt securities
held as investments. A debt-like name alone does not establish liability scope,
and mixed deposit/funding populations are not admitted wholesale.

### Lease inclusion and population coverage

Current and noncurrent lease carrying liabilities are resolved independently.
A valid current component does not require a separately reported lease aggregate.
Compatible current and noncurrent components may establish the lease population;
a matching aggregate corroborates that population without being added again.
A generic lease-liability tag nested in a supported noncurrent liability bucket
is a noncurrent component, rather than an issuer-wide lease total.

The amounts used are **lease-liability carrying values**, classified under the applicable accounting framework. Under US GAAP, the resolver distinguishes finance/capital leases from operating leases; a combined operating-and-finance-lease caption does not establish a pure finance-lease amount. Under IFRS, the separately reported eligible lease-liability population is included under the policy above.

Lease amounts are added only where their population is separate from the selected borrowing balance. A supported debt-and-finance-lease aggregate already owns those leases. Its constituent liabilities are not added again, and the combined aggregate does not become a debt-only reported-borrowing metric. This assessment applies separately to current, noncurrent and total coverage: a noncurrent borrowing row that includes finance leases does not establish that the current row includes them too.

Containment belongs to a particular borrowing representation and lease component. A broad debt-and-lease tag does not establish that every lease maturity is included. When a reported total is independently reconciled to the selected borrowing rows, each supported lease component is assessed against that representation: embedded components remain validation evidence and only proven excluded components are added. For example, a total of 100 that contains noncurrent finance leases of 5 but excludes separately located current finance leases of 3 produces standardized debt of **103**, rather than 100 or 108. Unresolved overlap does not authorize an addition.

Embedded leases can be established by a compatible positive unit-weight native
calculation relationship to represented combined debt, or by a closed physical
note bridge from instrument components to a gross subtotal and, after explicit
deductions, to the selected carrying balance. A classified lease pair can inherit
an independently proved aggregate's containment only when the pair reconciles to
that aggregate. Entity, date, currency, source and column evidence must agree;
equal amounts or a combined concept name alone do not prove containment.

When a compatible lease aggregate is more precise than its rounded current and
noncurrent components, the aggregate is preferred for the total and the rounded
components remain reconciliation evidence. Precision does not resolve conflicting
populations or establish an unsupported maturity allocation.

A dated, reconciled other-liabilities note can establish a lease carrying amount's location outside borrowings. Its closing subtotal and any subsequent current/noncurrent breakdown describe the same liability population; the breakdown is not additional debt. Reconciliation allows disclosed rounding and retains the distinction between carrying balances, fair values and undiscounted payments.

A lease note can establish placement outside borrowings through a referenced
row footnote naming an other-liabilities balance-sheet bucket. The marker and
destination must agree within the same native disclosure block, including its
valid linked inline-XBRL continuations; unrelated,
unreferenced or conflicting paragraphs supply no placement evidence. A rendered
note can establish the same relationship when its compatible component rows
sum to its reported total and that total matches a separate balance-sheet bucket
in the same document and period, after each amount's reporting scale is
established. An explicit document-wide declaration for financial-statement notes
can establish an otherwise unstated note scale; conflicting explicit units are
preserved. A table merely mentioning leases or other
liabilities is insufficient. Placement in a borrowing bucket indicates containment,
rather than permission to add those leases again.

A carrying lease can also identify its location directly through a liability-bucket
row in a lease table, or through a specifically named other-liabilities note.
A closed note-component reconciliation supplies equivalent placement evidence.
Positive calculation relationships to a separately presented nonborrowing
liability bucket also establish placement. A carrying lease total may be
corroborated by compatible current and noncurrent lease/financing rows on the
balance sheet that reconcile to that total. The discounted liability total is
used; future contractual payments are retained only as supporting disclosure.

Native US-GAAP debt-only current-maturity and noncurrent tags on the consolidated
balance sheet can establish a separate borrowing population from independently
tagged finance-lease carrying liabilities when the visible debt caption agrees
with that scope. Explicit evidence that the borrowing row includes those leases
remains binding. A generic borrowing note or a broad combined debt-and-lease
caption does not establish this separation. Bond and debenture carrying amounts
are debt-security instruments; compatible current and noncurrent bond balances
can form their aggregate, with disclosed zero balances retained as evidence.

A current/noncurrent financing population can span separate note tables when
each loan, bond and lease family independently reconciles to its compatible
reported carrying aggregate and the aggregates are proven nonoverlapping.
The resulting maturity views retain both the original classified observations
and the aggregate evidence. A missing family component, incompatible scope or
unresolved reconciliation prevents this complete-partition reconstruction;
an independently supported total remains eligible on its own evidence.
A mixed operating/finance lease note can establish a finance component's
placement by reconciling distinct operating, finance and total columns to the
same carrying-value lease bucket on the balance sheet. This does not add the
operating component to US-GAAP financing debt. Explicit conflicting placement,
incompatible dates/entities or an unreconciled column population prevent the
addition.
Entity, date, currency and liability classification must remain compatible; asset
rows and undiscounted payment schedules do not establish this relationship.
The caption and section determine whether a generic lease-liability concept is
an aggregate or a noncurrent component. Current leases are added to a noncurrent
component once, rather than treating that component as the all-maturity total.
Combined borrowing-and-lease rows retain their classified maturity. A complete
current combined row can be paired with nonoverlapping noncurrent borrowing and
finance-lease rows; an all-maturity borrowing subtotal does not suppress those
separately established leases.

For IFRS, equal borrowing and lease amounts alone do not prove overlap. The supported borrowing/lease reconciliation requires both current and noncurrent class disclosures to establish that the leases are already included, with compatible entity, period, currency and physical note evidence. A separately reported total lease obligation can also be used when it reconciles to its current and noncurrent portions, including in a 40-F financial-statement package. Genuine separate lease liabilities retain their inclusion under the table above.

Separately supported held-for-sale finance-lease liabilities remain financing obligations until derecognition. A complete held-for-sale partition can be included once where it is outside the ordinary lease population. Classification as held for sale does not itself make a liability disappear.

If only one lease maturity side is supported and no complete aggregate resolves the gap, the disclosed amount remains usable evidence. Where the calculator combines that partial lease population with borrowings, the result is marked **Estimated** with **Partial** population coverage: a supported minimum, not a complete debt total. The missing lease side is not assumed to be zero. A complete lease aggregate can support total debt even when its maturity allocation is unavailable.

**Examples:** Under this policy, borrowings of 100 plus separate finance leases of 10 and operating leases of 20 produce US-GAAP leverage debt of **110**. IFRS borrowings of 100 plus a separate eligible lease-liability total of 30 produce **130**. If the reported borrowing/debt aggregate already includes the relevant leases, those same amounts are counted once.

Disjointness can be supported by distinct compatible balance-sheet rows, a
reconciled partition or a verified containment relationship; an explicit narrative
statement or calculation linkbase is not mandatory. Unknown overlap is different
from missing coverage: borrowings of 100 and leases of 20 with unresolved overlap
do not establish either a complete 120 or a lower bound of 120. Independently
supported borrowing amounts retain their own reported definition.

### Financing liabilities held for sale

Identified disposal-group borrowings remain financing liabilities until
derecognition. Their inclusion requires compatible registrant, instant date,
currency and carrying-value evidence, together with support that they are outside
the selected ordinary borrowing population. A reconciled disposal-liability note
and a separate balance-sheet liability bucket can establish that relationship.
Repeated disclosures and group totals with their constituent leaves are counted
once. A rollforward transfer is validation evidence, not another closing balance.
Held-for-sale presentation and a custom tag containing `CurrentPortion` do not
by themselves establish contractual maturity. Unresolved overlap prevents addition;
unresolved maturity remains unallocated after an otherwise supported addition.

### Contractual maturity versus reported classification

Reported borrowing classification follows the issuer's balance sheet. The separate leverage-debt maturity view follows contractual maturity: supported commercial paper and debt due within a year remain current for leverage even when a refinancing arrangement permits long-term balance-sheet presentation. This changes the allocation of carrying debt, not its measurement into gross contractual principal. Short-term debt and current maturities can be combined only when the filing demonstrates distinct, nonoverlapping categories; an aggregate is not added to its constituents. A maturity disclosure alone does not establish financing eligibility, a complete instrument population or an issuer-wide amount.

Current/noncurrent section headings can classify an otherwise generic liability caption when the row is bound to that section. The same note caption under current and noncurrent sections can therefore have different maturity roles. An occurrence with no supported maturity remains unallocated. Asset placement, equity treatment and mixed trade/financing captions still have to pass financing eligibility checks; a "current" heading does not turn an operating payable or debt-like asset into funded debt.

Contractual repayment schedules often disclose **undiscounted principal, interest or lease payments**. Carrying-value debt requires the corresponding supported liability balances and measurement evidence. A supported gross/net bridge accounts for disclosed discounts, premiums, fair-value adjustments and issuance costs as applicable.

Gross principal, net carrying balances and current/noncurrent subsets of the same obligation are alternative or contained representations. They are not independent additive debts. A supported gross-to-net bridge remains authoritative even when a broad carrying-value tag labels the gross row. Source location, measurement and economic coverage determine eligibility; formula wording and the relative size of competing amounts do not establish it. A reconciled note can support a valid carrying total without being classified as a face statement. Presentation-network role names identify candidate concepts, while physical occurrence evidence establishes statement authority.

**Refinancing example:** An issuer presents $100m of commercial paper and $50m of term debt due within a year as long-term borrowings under refinancing arrangements. With $5m of separately supported current finance leases, current leverage debt is **$155m**. If the complete carrying-value debt population is $1,100m and the current partition is complete, noncurrent leverage debt is **$945m**. Reported borrowing metrics retain the issuer's balance-sheet classification.

### Unknown maturity and reconciliation

Completeness is assessed for the declared debt definition. It can be established
by a directly reported scoped total, structurally exhaustive compatible categories
or an independently reconciled partition. Merely adding compatible components
does not establish completeness. A noncurrent residual requires a complete,
compatible total and current population with no unresolved maturity. A directly
supported complete noncurrent partition remains eligible on its own evidence.
Aliases preserve the canonical result's coverage, quality and availability state.

Compatibility of debt and maturity-subset observations authorizes their supported
arithmetic independently of population completeness. A successful compatibility
check does not label the result `Complete`; that state requires its own scoped
completeness basis.

Financing eligibility, carrying amount, lease coverage and maturity allocation are independent decisions. A complete supported financing population can satisfy:

```text
Total debt = Classified current debt + Classified noncurrent debt + Debt with unallocated maturity
```

Unknown maturity does not make the debt zero and does not automatically make it noncurrent. When the resolved population contains unallocated maturity, total debt and supported total-debt ratios can remain available. The current subset is qualified as partial, and the complete `noncurrent_leverage_debt` metric is unavailable. A known noncurrent component can remain evidence without being presented as the complete noncurrent population. `Total debt − current debt` is used as a noncurrent partition only when compatible complete coverage supports that subtraction; a partial current subset cannot close the allocation.

A debt note may independently establish carrying debt through `total principal − issuance costs = reported closing carrying balance`. The supported native reconciliation requires compatible note rows and rejects an unexplained separate current borrowing. A complete total can therefore be available without inventing a missing current classification as zero. Repeated native and rendered observations remain evidence occurrences; they are not additional liabilities or additional issuance-cost deductions.

| Metric | Calculation or reported basis | Unit |
|---|---|---|
| **Current leverage debt** (`core_current_debt`, `current_leverage_debt`) | Carrying amounts of supported borrowings contractually due within a year plus qualifying current lease liabilities not already included. | Currency |
| **Noncurrent leverage debt / long-term debt** (`core_long_term_debt_noncurrent`, `noncurrent_leverage_debt`) | Supported noncurrent carrying debt and qualifying leases; a residual requires a compatible complete total and current partition. Unresolved maturity is not assumed to be noncurrent or zero. | Currency |
| **Total leverage debt** (`total_debt`) | Supported carrying-value financing population, including qualifying leases once. Equals current plus noncurrent leverage debt when maturity allocation is complete; a supported total can retain debt with unallocated maturity. Partial lease populations are qualified as described above. | Currency |
| **Total borrowings** (`total_borrowings`) | Supported borrowing carrying amounts, including separately established held-for-sale borrowings outside the ordinary borrowing rows. Separately reported lease liabilities retain their own population. | Currency |
| **Reported current borrowings** (`reported_current_borrowings`) | The issuer's current borrowing carrying amount, before adding separately reported leases. | Currency |
| **Reported noncurrent borrowings** (`reported_noncurrent_borrowings`) | The issuer's noncurrent borrowing carrying amount, before adding separately reported leases. | Currency |
| **Debt-to-equity ratio** (`core_debt_to_equity_ratio`) | `Total leverage debt / consolidated total equity`. | Multiple (`x`) |
| **Debt-to-capital ratio** (`core_debt_to_capital_ratio`) | `Total leverage debt / (total leverage debt + consolidated total equity)`. | Multiple (`x`) |

For these leverage ratios, **consolidated total equity includes noncontrolling interests**, even though parent-attributable book equity is used for the common-shareholder book-value measure. Gross contractual debt may also differ from net carrying-value borrowings because of discounts, premiums or unamortized issuance costs.

**Example:** Total leverage debt of 600 and consolidated total equity of 400 yield debt-to-equity of **1.50x** and debt-to-capital of **0.60x**. The latter is expressed as a multiple, not as 60 in the metric field.

## 7. Assets, liabilities and liquidity

| Metric | Calculation or reported basis | How to interpret it |
|---|---|---|
| **Total assets** (`core_total_assets`) | Complete consolidated asset total at the reporting date. | Not a segment or one asset class. |
| **Total liabilities** (`core_total_liabilities`) | Complete consolidated liabilities total at the reporting date. | Includes operating and financing liabilities, unlike leverage debt. |
| **Current assets** (`core_current_assets`) | Assets classified as current on the balance sheet. | Includes eligible cash, receivables, inventory and other current categories under the company's reporting basis. |
| **Current liabilities** (`core_current_liabilities`) | Liabilities classified as current on the balance sheet. | Includes both current operating obligations and current financing obligations. |
| **Current ratio** (`core_current_ratio`) | `Current assets / current liabilities`. | A liquidity multiple; values above 1 indicate current assets exceed current liabilities. |
| **Net property, plant and equipment** (`core_property_plant_and_equipment_net`) | Reported total PP&E after accumulated depreciation; alternatively, `gross PP&E − accumulated depreciation` when the inputs completely reconcile. | Includes the whole relevant PP&E balance, not just regulated, nonregulated or segment assets. |

**Example:** Current assets of 750 and current liabilities of 500 give a current ratio of **1.50x**.

When a native custom total net PP&E competes with a narrower standard-tagged category, the total must reconcile to gross PP&E less accumulated depreciation in the same entity/context, filing, currency unit and source document. This specific arithmetic proof can retain native custom PP&E/depreciation facts despite an unresolved visible header. It does not waive validation for unrelated custom or inferred HTML facts.

## 8. Profitability and return ratios

| Metric | Calculation or reported basis | Interpretation |
|---|---|---|
| **Return on assets (ROA)** (`return_on_assets`) | Always calculated: `100 × annualization factor × unadjusted consolidated net income / average total assets`. | Income includes noncontrolling interests, matching consolidated assets. |
| **Return on equity (ROE)** (`return_on_equity`) | Always calculated: `100 × annualization factor × unadjusted income available to parent common shareholders / average parent common equity`. | Excludes noncontrolling interests and preferred/senior claims from the corresponding income/equity basis. Parent-owner amounts are equivalent when no separate preferred/senior claim exists. |
| **Issuer-reported ROA / ROE** (`issuer_reported_return_on_assets`, `issuer_reported_return_on_equity`) | The eligible ratio explicitly disclosed by the issuer, with its reported methodology. | Reflects the issuer's disclosed income, period and averaging conventions. |
| **Consolidated net profit margin** (`core_net_profit_margin`) | `100 × consolidated net income / consolidated revenue`. | After-tax profit earned per unit of revenue. |
| **Gross margin** (`core_gross_margin`) | `100 × gross profit / matching revenue`. | Profit after direct costs. |
| **Operating margin** (`core_operating_margin`) | `100 × operating income / matching revenue`. | Profit from operating activities. |
| **EBIT margin** (`ebit_margin`) | Always `100 × canonical generic EBIT / matching total revenue`. | Uses the same library-defined EBIT numerator across issuers. |
| **Issuer-reported EBIT margin** (`ebit_margin_reported`) | The eligible EBIT margin disclosed by the issuer. | Reflects the issuer's stated numerator and revenue conventions. |

The claimant assessment accommodates common or ordinary shareholders and limited
partners when their income and equity represent the same ownership population.
Preferred, exchangeable and noncontrolling claims remain separate. A closed equity
statement can establish the common claim without requiring an unrelated liability
extraction to succeed. Assets less liabilities is not automatically parent common
equity. Explicit column dates and segment/claimant scope take precedence over
inferred filing-period labels; a comparative segment balance cannot supply the
opening consolidated or common-equity endpoint.

Both canonical returns use the same fixed policy across issuers:

- **Income:** unadjusted financial-statement net income under the issuer's accounting framework, including discontinued operations when included in the verified net-income total. ROE requires matching issuer-wide common-shareholder income and equity.
- **Average balance:** `(beginning balance + ending balance) / 2`. The beginning balance is immediately before the income period starts; the ending balance is at its end. Entity, currency, accounting basis and claimant scope must match. A standalone Q2 uses Q1-end and Q2-end balances.
- **Annualization:** the factor is **1 for an annual reporting period**, including a 52/53-week fiscal year. For an interim period it is **365 / exact inclusive days in the income period**. This is an annualized period return, not a trailing-twelve-month return or a forecast. Numerator and denominator dates and averaging methodology remain in calculation metadata.
- **Availability:** both balance endpoints and the matching income must be supported. Missing inputs leave the ratio unavailable. ROA requires positive asset endpoints. ROE requires positive equity at both endpoints and a positive average; zero, negative or sign-crossing equity makes it unavailable. A net loss with valid positive balances produces a negative return.

Share buybacks can reduce positive common equity and raise standardized ROE without improving earnings. The fixed formula preserves that economic effect; it does not reconstruct a hypothetical equity balance or cap a supported high return. Compare ROE with ROA, leverage and the underlying equity balances. A two-point average also cannot describe all balance movements within the period. Consistent definitions improve comparability but do not remove differences between US GAAP and IFRS or between business models.

**Quarterly example:** Net income of 25 for a 91-day quarter and average assets of 1,000 give an annualized ROA of `100 × (25 / 1,000) × (365 / 91)` = **about 10.03%**.

## 9. Cash flow measures

| Metric | Calculation or reported basis | How to interpret it |
|---|---|---|
| **Net cash from operating activities** (`core_net_cash_from_operating_activities`) | Reported net operating cash flow for the period, with standalone-quarter amounts reconstructed from cumulative cash-flow statements when appropriate. | Cash generated by or used in ordinary operating activities. |
| **Net cash from investing activities** (`core_net_cash_from_investing_activities`) | Reported net investing cash flow for the period. | Includes eligible asset purchases, disposals, investments and related transactions. |
| **Net cash from financing activities** (`core_net_cash_from_financing_activities`) | Reported net financing cash flow for the period. | Reflects borrowing, repayment, share issuance, distributions and similar financing cash flows. |

Cash-flow amounts preserve their reported signs: net outflows are generally negative, and inflows positive. A net financing or investing cash outflow is not converted into a positive expense figure.

Standalone quarters can be reconstructed across native XBRL and rendered HTML
when both operands establish the same registrant, economic activity, currency,
accounting scope and fiscal year-to-date start, with the preceding cumulative
period ending at the quarter boundary. Each operand retains independently
supported units and scale normalization. Known unresolved restatement or recast
basis differences prevent subtraction. Source format alone does not prevent it.
Signed net flows are subtracted as reported even when their signs differ;
purchase-outflow components use their supported expenditure magnitude. Neither
different concept names nor opposite signs authorize an arbitrary sign reversal.
Cash-flow captions must be bound to statement rows and dated columns, including
common wording and punctuation variants; narrative numbers and noncash adjustments
do not become cash-flow subtotals.

Verified registrant interim statements in an F-1/F-1A or S-1/S-1A registration
filing may supply a preceding cumulative baseline through the separate derivation
channel. The baseline must meet the same period, scope, currency and scale
requirements as the current operand. Its use does not make it a separately
published periodic quarter. For example, a compatible Q1 cumulative baseline
supports `standalone Q2 operating cash flow = H1 YTD operating cash flow − Q1
operating cash flow`; the calculation and dependent FCF formulas are unchanged.

| Metric | Calculation or reported basis |
|---|---|
| **Tangible capital expenditure** (`capex_tangible`) | Qualifying cash purchases of property, plant and equipment for the selected period. Cumulative amounts can be differenced on the compatible basis above. |
| **Free operating cash flow** (`free_operating_cash_flow`) | `Operating cash flow - qualifying operating-asset cash investment`. Qualifying investment comprises supported tangible purchases, intangible purchases and nonoverlapping issuer-owned recurring productive-asset cash expenditures. |
| **Tangible-capex free cash flow** (`fcf_tangible_capex_basis`) | `Operating cash flow - tangible capital expenditure`. |
| **Total-investment free cash flow** (`fcf_total_investment_basis`) | `Operating cash flow - tangible CapEx - intangible CapEx`, with compatible supported investment operands. |
| **Combined-investment free cash flow** (`fcf_combined_investment_basis`) | Operating cash flow less the supported reported combined capital-investment cash-flow line, retaining that line's scope. |
| **Recurring-productive-investment free cash flow** (`fcf_recurring_productive_investment_basis`) | Operating cash flow less compatible tangible/intangible investment and separately disclosed cash expenditure on issuer-owned recurring productive assets. Overlapping investment components are counted once. |

An issuer-defined free-cash-flow figure retains its disclosed investment scope and adjustments.

Qualifying investment includes supported capitalized software and other intangible
asset purchases, as well as recurring cash expenditures on owned operating assets
such as subscriber systems or customer-premises equipment. A carrying-value asset
balance, noncash addition or business-acquisition payment is not a cash CapEx
component. Components must be compatible and nonoverlapping; a combined total
and its constituent purchases are counted once. A directly reported combined
capital-investment cash-flow line can establish the investment basis when its
scope is supported and it excludes broader investment categories. When only
tangible CapEx is supported, the calculation records that evidence basis.
The formula and qualifications describe the selected investment population;
numeric availability alone does not establish its completeness.

A supported consolidated-entity CapEx partition retains its original component
contexts as evidence while supplying a company-wide investment operand for FCF.
Different component dimensions do not invalidate a total whose consolidation
scope has been established; an unresolved segment or entity population cannot
be substituted for it. The same operating-asset definition applies through the
main calculation and taxonomy enrichment paths.

The operating-asset formula and component metadata retain their own definition
for every investment presentation. Source notes identify the supported investment
population and its coverage. An unselected additional component does not establish
a zero expenditure or a complete investment population. Business acquisitions,
financial-investment purchases and noncash additions are excluded from qualifying
operating-asset cash investment.

For operating cash flow of 100, tangible purchases of 20, intangible purchases of
10 and separate qualifying recurring productive-asset expenditures of 5, operating
FCF is **65**, tangible-capex FCF is **80**, and total-investment FCF is **70**.
Each metric retains its own formula, components and semantic identity.

## 10. Industry-specific metrics

Some measures are meaningful only for particular businesses, and their published names or denominators may be industry-specific.

| Metric | Calculation or reported basis | Typical use |
|---|---|---|
| **Inventory turnover** (`sector_inventory_turnover`) | Eligible issuer-reported turnover, or, when the inventory and cost bases are established, `cost of goods sold / average inventory`. | Measures how often inventory is sold or consumed over a period. A report using sales instead of cost of goods sold is a different turnover convention. |
| **Expense ratio** (`kpi_expense_ratio`) | The issuer's reported sector expense ratio under its stated numerator and denominator; for a conventional property-and-casualty underwriting expense ratio, underwriting expenses are compared with the appropriate premium basis. | Not a universal company-wide operating-expense/revenue ratio. The specific insurance/sector presentation determines the appropriate basis. |

Other industry measures may include net interest income and net interest margin for banks, combined and loss ratios for insurers, funds from operations for real estate companies, depletion-inclusive measures for extractive businesses, and recurring revenue or retention metrics for subscription businesses. Each uses its industry-specific definition and disclosed scope.

## 11. Metric name reference

This reference groups the primary metrics described above by financial purpose.

| Purpose | Metric names |
|---|---|
| Revenue and gross result | Total revenue; revenue from customer contracts; net revenue; gross revenue; cost of revenue; cost of products sold; gross profit; gross margin |
| Operating and financing result | Operating expenses; operating income; operating margin; other income/expense; pretax income; generic EBIT; reported EBIT; generic EBIT margin; reported EBIT margin; EBITDA; reported EBITDA |
| Net earnings | Consolidated net income; net income KPI; net income from continuing and discontinued operations; net income attributable to common stockholders; net income available to common shareholders; basic EPS; diluted EPS; net profit margin |
| Shares and equity | Period-end common shares; basic average shares; diluted average shares; share-based compensation; equity attributable to parent; book value per share |
| Debt and balance sheet | Debt-to-capital; debt-to-equity; noncurrent long-term debt; total assets; total liabilities; current assets; current liabilities; current ratio; net PP&E |
| Returns and cash flow | Return on assets; return on equity; operating cash flow; investing cash flow; financing cash flow |
| Industry-specific | Inventory turnover; expense ratio |

## 12. Filing evidence and reconciliation boundaries

The parser preserves separate observations when a native fact or rendered cell appears in several statements or notes. A physical occurrence is identified by its document, table, row and selected column. Equal table numbers in different documents do not establish one table; equal values or a shared context do not establish that different debt instruments are duplicates. Conversely, repeated renderings of a selected source must not be counted again in debt diagnostics or gross/net reconciliations.

For example, the same parent-equity closing balance can appear in both quarterly and year-to-date statements of changes in equity. Both source occurrences are retained, and each retains its parent-owner dimension and attribution. They represent one closing balance for that date, rather than two amounts to add together.

XML context IDs, unit IDs and calculation-link locator labels are case-sensitive. Calculation relationships require a recognized summation-item arcrole and an explicit nonzero weight. Overrides and prohibitions are resolved within their network and equivalent relationship attributes; a negative weight remains a subtraction, not economic containment. Conflicting effective weights cannot authorize containment. The debt resolver requires positive unit-weight and compatible measurement evidence before using a parent-child calculation relationship as containment. These bounded checks validate the relationships used for metric resolution.

Metric eligibility requires evidence of the economic scope described in its definition. Unproved component coverage, conflicting observations and unsupported statement structures can leave a metric unavailable.

Debt publication preserves the shared semantic and population decision through
canonical calculation, taxonomy matching, rendered fallbacks, aliases and dependent
ratios. A rejected gross, ineligible or contradicted representation cannot return
as supported carrying debt merely because another path selects it. Its source
evidence and rejection reason remain available for reconciliation and diagnostics.

A proven material financing liability left unresolved in the requested population
prevents publication of a purported complete financing-debt total and ratios that
depend on that claim. This is a liability-scoped acceptance rule: investment assets,
loans held for sale and unrelated diagnostic warnings do not block valid debt.
A rendered copy of a native component observation does not establish an omitted
registrant liability when its physical source and component scope prove the match;
the native scope is retained. Equal values or similar captions alone are insufficient.

Complete borrowings do not automatically establish complete financing debt: qualifying
lease coverage requires its own evidence. Supported partial minima retain their
estimated status and partial coverage where permitted above; independently supported
reported borrowings retain their own definition. Aliases and ratios preserve the
selected dependency's availability, quality and coverage rather than recreating a
complete debt claim from its numeric amount.

---

**Metric comparability:** Comparisons across companies or periods depend on compatible metric definitions, accounting scopes, reporting durations, currencies, share bases and issuer-defined adjustments.
