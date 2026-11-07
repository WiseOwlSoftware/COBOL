## PB4822: a reference to a function whose AS literal-5 was refused draws no COBOL1505

`FUNCTION MYF AS ""` is refused by §2023:12.3.8.3 SR2 (COBOL1761) and registers no function specifier, so `FUNCTION MYF(1)` then drew COBOL1505 "declared but no prototype", a cascade of the first error. Re-probed on the wave build: reproduced.

Fix: `ConfigurationSlice.RefusedFunctionSpecifiers` records the declared-but-refused word (twin of `RefusedProgramSpecifiers`, PB4708), `ConfigurationStep` inherits it into contained programs, and `UdfBinder.UdfBindCall` / `UdfBindPointerCall` ask it before the prototype table. Asking first also stops a refused specifier from letting the bare word resolve to a same-named function of the group. The statement funnel (PB1029) demands an error per refusal node, so `EditionContext.NoteCauseReportedEarlier` answers for the report made at the specifier instead of a second diagnostic.

Tests: `ExternalRepositoryResolutionTests.AReferenceToAFunctionWhoseLiteral5IsRefused_AddsNoCascade` (explicit form, function-pointer form, contained program) and the golden `pb4822-function-literal5-refused-reference`. Closes no inventory row.
