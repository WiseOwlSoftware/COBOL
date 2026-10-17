## Data binder fixes: contained storage claims, SPECIAL-NAMES redeclaration, one SR11 zero-length test, BY VALUE formal class

Wave 1111 group D, one root: `src/Cobol.Net.Compiler/Binding/DataBinder*.cs` and its screens. Four R2 review findings landed
(PB3222, PB3218, PB3286, PB3216), each at its root, each with the sibling arm swept. None claimed an inventory row, so no
verdict batch was recorded; each note carries `closes_rows: []` with its reason.

**PB3222 — the BY VALUE formal's class.** `ProcedureHeaderScreen.ByValueClass` listed PicCategory values, so a USAGE INDEX
formal (category numeric for storage, class index in 8.5.2.1 Table 2) passed BY VALUE. It now asks
`IntrinsicArgumentRules.ClassOfItem`, the classifier the argument side already uses (14.9.4.3 SR22), and fails open on an
undecidable class. Re-probing found the screen ran before a TYPE clause composed its subject: a `01 NX TYPE NT` formal
(NT numeric) was refused falsely, and an alphanumeric-typed one only by that accident. Both header arms now call
`CompleteDescriptionAhead(item)` first. Goldens: index formal and alphanumeric TYPE formal refused (COBOLNET1553), numeric
TYPE formal accepted.

**PB3218 — SR11's zero-length half.** The ALPHABET / CLASS operand screen tested the raw token, so `ALPHABET AZ IS "" & ""`
compiled clean while the LOCALE / ORDER TABLE screen refused the same operand (8.8.3.3 GR2). One predicate,
`IsZeroLengthOperand` (plus `IsZeroLengthConstant`), is asked of the decoded literal by both screens, and
`TryClauseTextLiteral`'s inline copy of the class-and-decode now reads `OperandLiteralClass` + `LiteralCharsOf`. A drift
test pins that `CobolLiteral.IsZeroLength` is called only inside the two predicates. Three goldens.

**PB3286 — a contained program's storage claims.** The ADDRESS OF and BY REFERENCE claims resolved a contained head in the
whole container scope; a container-local record sharing a name with a GLOBAL one hid the global record, which was never put
on a cell, and the contained `SET P TO ADDRESS OF X` was refused with a false "OCCURS-resident anchor". The two private
resolvers became `ResolveStorageClaimHead`, which filters a contained head to the GLOBAL roots (13.18.27.4 GR2). The
PtrBinder fallback reason now names the record's storage tier. Three goldens (both declaration orders, BY REFERENCE).

**PB3216 — a second SPECIAL-NAMES declaration.** ALPHABET, CLASS and switch condition-names dropped a repeat silently
(`TryAdd`) while LOCALE and DYNAMIC LENGTH STRUCTURE kept private refusals. `DeclareUserWord` now refuses a second
declaration of any kind in `UserWordKinds.DeclaredUnqualifiable` (and a switch's condition-names, explicitly) under one
descriptor, COBOLNET4250 (8.4.2.1). COBOLNET1665 is retired; its golden and docs name 4250. Mnemonic names join the set
(the sweep). A drift test fails when a SPECIAL-NAMES kind is declared without joining the set. Four goldens.

**PB3192 — not fixed here.** The fifth note of the group (a method's TYPEDEF is class-wide, a method constant's LENGTH OF
resolves against the object) re-probed true and is the OO method-scope restructuring: method names register class-wide
during the method's bind and move into its scope only afterwards, so it is a mechanism of its own. The note carries the
measured root, a third symptom found on the way (a method's TYPE clone leaks its subordinate names to a sibling method)
and the design; it stays open for a finisher.

Self-review (pr-test-analyzer, silent-failure-hunter, comment-analyzer lenses on the diff): the two fail-open arms are
deliberate and named (an undecidable class admits a BY VALUE formal, as the argument side does; a contained head with
several global candidates claims nothing, as the BY REFERENCE arm always did); every added golden names the leg that can
fail; one tool-written file came out CRLF (the line-endings audit caught it before any leg ran) and one message
("a alphabet-name") was reworded before it shipped.

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261017T222631Z-6eab63); the semgrep
baseline for `cobolnet-raw-diagnostic-code-literal` went 245 to 244 and was locked in.

Codes: COBOLNET4250 used; 4251 and 4252 returned.
