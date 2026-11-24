## Entry 4036 — 2026-11-24 06:36 PST — PB5707: at --std 2002 a CURRENCY SIGN PICTURE SYMBOL literal-8 may not be national or repeated

Re-probe on this build: `CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL N"U"` and two identical clauses `... PICTURE SYMBOL "U"` both compiled at `--std 2002` and printed `[  USD12.50]`. §2002:12.2.6.2 SR22 says "Literal-8 shall be an alphanumeric literal consisting of a single character" and "No two occurrences of literal-8 may be the same value"; §2014:12.3.6.2 SR26 admits a national literal-8 and SR21 lets two clauses repeat a symbol with identical strings, so the two rules are 2002-only (all citations `cite.py --check` OK).

Fix: `DialectBehavior.CurrencyPictureSymbolLiteralAlphanumericAndUnique` (changed in 2014, VCR row 7.127, doc-facts entry). `SpecialNamesBinder.CurrencyOperandOf` refuses a national literal-8 below 2014 (COBOL0892) but still names the symbol, so a later `PIC U...` is not a cascade; `SwitchBindCurrency` refuses a literal-8 already written by an earlier clause of the unit (COBOL0891), kept in the new `ConfigurationSlice.PictureSymbolLiterals`, which a contained unit does not inherit. No new diagnostic code was used.

Goldens: `pb5707-currency-picture-symbol-national-literal-8` and `pb5707-currency-picture-symbol-literal-8-repeated` (accepted at 2014 and 2023, expected `[ USD12.50]` derived from the floating insertion picture; each refused at 2002 by its diagnostics file). Row `2002-SR-12.2.6.2-22` closed (GAP 1135 to 1134).

