## COBOL-2002 reads its own TEST-NUMVAL-C, floating-literal and default-ordering-table rules

Four notes (PB5721, PB5720, PB5613, PB6039) were three mechanisms where the 2002 text says less or something else than the
2014 and 2023 text, and the compiler applied the later wording at every edition. Each is now an edition gate in the one table
(`DialectBehavior`), with VCR rows 7.118 and 7.119.

**PB5721, TEST-NUMVAL-C.** The re-probe on this build reproduced it: `TEST-NUMVAL-C(NE)` for a numeric-edited item and
`TEST-NUMVAL-C(AE)` for an alphanumeric-edited item were each COBOL1627, and `TEST-NUMVAL-C("U1 12.5" "U1")` was COBOL1627 on
argument-2. §2002:15.76.2 AR1 asks argument-1 of class alphanumeric or national (the edited categories are class
alphanumeric) and AR2 only the class of argument-1; §2014:15.87.2 AR1 imports NUMVAL-C's argument rules whole, as
§2023:15.94.3 AR1 does. `DialectBehavior.TestNumvalCImportsNumvalCArgumentRules` (2014) now drives the CharacterCategory predicate of
the TEST-NUMVAL-C schema row (`since: 2014`) and the content screen in `BindNumvalCFamily`; `CobolIntrinsics.TestNumvalC` takes
`contentUnrestricted`, so a data item holding "U1" is scanned as written at 2002 (the renderer passes it, as it does for NUMVAL-C at 85).
The COBOL1627 content message cites its own edition's rule. Goldens: `pb5721-test-numval-c-class-rules-2002` (0, 4, 0, 0) and the
refusal `pb5721-test-numval-c-category-rules-2014` (2014 and 2023).

**PB5613 and PB6039, floating-point literal limits.** At 2002 a 33-digit significand and an exponent of 1000 compiled. §2002:8.3.1.2.2.2
rules 2 and 3 say 31 digits and three; §2014:8.3.1.2.2.2 and §2023:8.3.3.3.3 SR2/SR3 say 36 and four. `NumericLiteral.CheckFloatingPointForm`
takes `wideLimits` (from `DialectBehavior.FloatingLiteralWideLimits`, 2014) and the messages in `SpecialNamesBinder.NormalizeNumericLiteral`
cite the edition's own rule. Goldens: `pb5613-floating-literal-wide-limits-2014` (1234567) and the refusal `pb5613-floating-literal-wide-limits-2002`.
Sibling sweep: the fixed-point literal limit is a floor ("shall allow for ... 1 through 31 digits", both editions), so no refusal follows;
`pb4797_working_numerators_past_the_value_range` used four-digit exponents under `accept: [2002, ...]` and is now accepted from 2014.

**PB5720, STANDARD-COMPARE's default table.** The re-probe showed the claimed "=" was the checking-off result of the EC-ORDER-NOT-SUPPORTED
path for the 2002 name. §2002:15.69.2 AR4 names the default table 'ISO14651_2002_TABLE1', §2014:15.79.2 AR5 'ISO14651_2010_TABLE1',
§2023:15.85.3 AR5 'ISO 14651_2020_TABLE1'. The processor has one derived table (owner decision Q4), so `CollationEngine.DefaultOrderingTableNames`
carries each edition's own name for it, matched with spaces and underscores ignored; the 2002 name resolves (no COBOL1662, no
EC-ORDER-NOT-SUPPORTED) and STANDARD-COMPARE("a" "B") is "<". Golden `pb5720-standard-compare-default-table-2002`. The level-error message no
longer names the 2023 table at another edition.

Rows: closes `2002-AR-15.76.2-1`, `2002-AR-15.76.2-2`, `2002-AR-15.69.2-4`, `2002-SR-14.8.25.2-3` (record_verdicts, GAP 1167 to 1163).
`2002-DOC-B.1-62` (the documented exponent range) stays on PB5613: no 2002 DOC row has a CONFORMS home yet.
