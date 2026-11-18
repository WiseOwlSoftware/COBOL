## Leads: three low leads of the step 3 review (diagnostics), open on origin/main

Source: review-LS3 round 1, CLEAN with three low leads, `E:/COBOL-coord/scratch/reports/w20262104c-PB5925-report.md` lines 146-148. Each was
re-read on origin/main 0d4042678a for this file; none was probed by running code.

**R1-1. The diagnostic's Column is a UTF-16 index, the schema and the model call it a character position.**
Sites: `src/WiseOwl.Cobol.Compiler/Listing/DiagnosticSectionBuilder.cs#Build` (line 28-31), `src/WiseOwl.Cobol.Listing/ListingDocument.cs#ListingDiagnostic`
(the `Column` param, line 289), `docs/rearchitecture/DESIGN-listing.md` (line 681), `tests/.../TextListingRendererTests.cs`.
`Build` sets `column = map.ExpandedIndexAt(line, column) + 1` where `ReferenceFormatMap.ExpandedIndexAt` returns an index into the expanded .NET
string, so it counts UTF-16 code units; the record says "The 1-based character position in that line's text (tabs expanded)" and the inline row
prints it as `(col n)`. For a line with a supplementary-plane character (U+1D11E is two code units) the column of a name AFTER it is one more than
its character position, and the test pins that reading (column 3 for the X after U+1D11E). Decide one meaning (character, UTF-16 unit or display
column, which DESIGN-listing section 6.7's cross-reference columns must agree with), write it in the record, the schema description and the design,
and change the code or the words. The same count is used for the cross-reference and the object-code map (`ListingBuilder.cs:109` calls the same
function); sweep them in the same change.

**R1-2. SeverityWord restates the compiler's severity words with no test tying the two.**
Sites: `src/WiseOwl.Cobol.Listing/Text/TextListingRenderer.cs#SeverityWord` (line 234), its callers `StatisticsText.cs:43` and the renderer's lines 198
and 228, and the compiler's `Diagnostic.Render` (the other place the words error / warning / info are spelled).
Its doc comment says it is "the word every diagnostic line of the compiler uses"; nothing holds that. The Listing assembly cannot reference the
Compiler (the model references nothing), so the tie must live in a test: one drift test that renders each `ListingSeverity` and each compiler
severity through `Diagnostic.Render` and asserts the words equal. A third spelling would otherwise drift silently.

**R1-3. Result.Diagnostics defaults to [] and its agreement with the printed report is witnessed on three of the driver's returns.**
Sites: `src/WiseOwl.Cobol.Compiler/CompilerDriver.cs` (the returns at lines 318, 337, 341, 350, 373, 382, 388, 396 set `Diagnostics = Reported(...)`),
`tests/WiseOwl.Cobol.Tests.Conformance/Listing/ListingDiagnosticsTests.cs#EveryPrintedDiagnostic_IsARecord_AndTheListingHoldsEachOne` (line 163; the
theory's shapes are frontend, bind, warnings).
The record/print agreement is witnessed on the front-end-error, bind-error and success-with-warnings returns; the CheckOnly, the size-limit
BindError (COBOL7641), the expansion-failure, the BackendError and the OutputNotWritable (catch) returns set the same field with no test. A
return that forgets `Diagnostics = ` leaves `[]` while the report still prints the error, so the record and the print disagree (not demonstrated; no such return exists today). Make the
default a failure (a `Result` built without the diagnostics cannot be constructed) or add the missing shapes to the theory; the first is the
structure that makes the next return automatic (rule 5).
