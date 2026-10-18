## Data binder fixes: method-scoped names, contained storage claims, SPECIAL-NAMES redeclaration, one SR11 zero-length test, BY VALUE formal class

Waves 1111 group D and 1112 group A, one root: `src/Cobol.Net.Compiler/Binding/DataBinder*.cs` and its screens. Five notes
landed (PB3222, PB3218, PB3286, PB3216 in wave 1111; PB3192 in wave 1112, which cherry-picked wave 1111's unlanded commits
onto main and re-gated them first), each at its root, each with the sibling arm swept. None claimed an inventory row, so no
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

**PB3192 — a method's names are the method's from the moment they are declared.** Wave 1111 re-probed it and handed it on
with a design; wave 1112 re-probed all three repros on its own build (p16: false COBOLNET1529 "duplicate type-name" plus
COBOLNET1740; p17: `A 3` for `A 5`; p18: false COBOLNET1547) and the third symptom (q1: a sibling method resolved a TYPE
copy's member, then CS0103). The root: a method's entries registered their names in the CLASS's maps and
`OoScopeSubtree` moved them after the whole method had bound, while `ActiveScope` read only the statement-time method; type
declarations never moved, and TYPE copies composed later never moved. The fix: each record or type declaration a method's
entry opens is the method's at once (`OoRootOwner`, written in `BindEntries`); every registration (data-name, index-names,
condition-name, type-name) asks `NameScopeOf` (the owner of the item's root), at bind and at a post-build copy alike;
`OoScopeSubtree` is deleted. `ActiveScope` and the constant and literal-alias lookups read one `CurrentMethodScope` (the
statements' method, else the method whose data binds). `OoMethodDataScope` gains `TypeDecls` (a method's type-name is local,
8.4.6.2.2 and 13.18.27.3 SR4), `TryFindTypeDecl(name, scope)` answers the method first and none of the object's when the
method declares the word otherwise (11.7.4 GR5), `AllTypeDecls` feeds the whole-forest passes, and the 8.4.2.2.1 rule-4
lapsed-member arms are scoped per element. `Declares` also asks `Constants.OtherWords`, the words a method's entries declare
before they bind, so a later method entry hides the object's word while the data division binds (p17); a type
declaration's members no longer join that set (13.18.58.4 GR1: they name nothing outside a group of the type).
Sibling sweep: a method's `01 B SAME AS A` resolved its operand in program scope and copied the OBJECT's `A` (new probe s1,
`A AB` for `A ABCDE`); it now resolves in `ScopeOf` the entry's root. An intrinsic's type-name argument asks the statement's
scope. Goldens in `cases/11.7-METHOD-ID/`: `pb3192_method_typedef_scope` (accepted 2002+: method TYPEDEF over the object's,
an unreferenced template member, a TYPE copy's index and condition names, SAME AS, a sibling that sees only the object's),
`pb3192_method_constant_length_scope` (A 5 / B 6 / C 3), and two refusals, `pb3192-method-type-copy-member-in-sibling`
(COBOLNET1639) and `pb3192-method-type-name-in-sibling` (COBOLNET1530). Drift test `MethodNameRegistrationDriftTests`
pins the funnels; `GlobalNameClassDriftTests` now holds `Declares` to the method's `TypeDecls`. `docs/COBOLNET_OO_DESIGN.md`
describes the registration rule.

Self-review (pr-test-analyzer, silent-failure-hunter, comment-analyzer lenses on the diff): the two fail-open arms are
deliberate and named (an undecidable class admits a BY VALUE formal, as the argument side does; a contained head with
several global candidates claims nothing, as the BY REFERENCE arm always did); every added golden names the leg that can
fail; one tool-written file came out CRLF (the line-endings audit caught it before any leg ran) and one message
("a alphabet-name") was reworded before it shipped.

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run 20261017T222631Z-6eab63); the semgrep
baseline for `cobolnet-raw-diagnostic-code-literal` went 245 to 244 and was locked in.

Codes: COBOLNET4250 used; 4251 and 4252 returned.
