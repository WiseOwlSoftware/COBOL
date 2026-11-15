# DESIGN - the VALIDATE facility (data validation, input distribution, error indication)

> **Status: DESIGN, APPROVED BY THE CODEBASE OWNER (Claude, CLAUDE.md "Ownership"), NOT YET BUILT.** Today the facility is
> declined (`docs/conformance/5/A.4.14.md`, `docs/conformance/4/VALIDATE.md`): the validation clauses draw COBOL1708, the
> EC-VALIDATE family COBOL1710, and the VALIDATE statement is accepted inert with COBOL1580. This document is the shape that
> replaces the decline. Work register: kb/Work PB5577 (the 2002 rows) with the 2023 rows of the same clauses.

## 1. Why the decline ends

`python scripts/spec/cite.py --check "§2002:3.1" "An implementation may optionally provide the elements specified in Annex A, Communications facility."`
(verdict OK) shows the one optional element of 2002 is Annex A, the communication facility. `"§2002:3.1.1" "An implementation
shall accept the syntax and provide the functionality for all standard language elements required by this International
Standard"` (OK) makes VALIDATE, its statement (§2002:14.8.46) and its data-description clauses required at `--std 2002`. The
decline rested on §2023:A.4.1's optional-module licence, which 2002 does not have (PB5574 decision 1: a 2002 rule is judged
on the 2002 text).

**Decision (this design, owner-delegated 2026-11-10): the facility is IMPLEMENTED at every edition that has it (2002, 2014,
2023) and claimed in `docs/CONFORMANCE.md` section 5 as such.** A per-edition split (working at 2002, declined at 2023) would
keep two answers to one construct in the compiler for the sake of a licence the user never asked us to use; 2023
marks the facility obsolete (Table 13 NOTE), which is an obsolescence warning (COBOL0903) the version matrix carries,
not a reason to decline; where the 2023 clause text differs from 2002's (NONE in DEFAULT, message-tag) the edition's own
text rules at its own `--std` (S1 to S5 read each clause at both). The same change set therefore deletes:
`DeclinedFacilityPass`'s validation arms, `CobolDeclined.g4`'s validation rules, `DeclinedFacilityBinder`, COBOL1580's
VALIDATE use, COBOL1708 and COBOL1710 (their negative goldens become positive goldens), and flips the A.4.14 cells of
`docs/conformance/`. No shim keeps the old codes alive (CLAUDE.md rule 4).

## 2. Shape: expand at BIND time, as INITIALIZE does

`BoundInitialize` (`Binding/Bound/BoundInitialize.cs`) is the model: the binder expands the statement into a flat series of
per-element actions that the emitter renders through the ONE MOVE path. VALIDATE gets the same shape:

- `BoundValidate(IReadOnlyList<ValidatePlan> Operands)` - one plan per identifier-1, in source order (§2002:14.8.46.3 GR3:
  a separate implicit VALIDATE per identifier; a declarative that RESUMEs NEXT STATEMENT continues at the next one).
- `ValidatePlan` is built by a new `ValidateBinder` (`Binding/Procedure/Verbs/ValidateBinder.cs`, replacing
  `DeclinedFacilityBinder`) walking the operand's elements (GR2: the operand and every subordinate item except class index,
  object and pointer) in DEFINITION ORDER. Each element becomes a `ValidateElement` carrying its `Place`, and the clauses
  that apply to each of the five stages (GR4), already bound: the PICTURE/USAGE/SIGN format check, the DEFAULT recipe
  (§2002:13.16.16), the DESTINATION receivers, the CLASS test and the 88-level VALID/INVALID entries, the INVALID WHEN
  conditions (several fold into one OR, GR6d), PRESENT WHEN (§2002:13.16.39), and the VALIDATE-STATUS items whose FOR
  names the element (§2002:13.16.60, in data-division order, GR6e).
- An OCCURS dimension is an `ValidateLoop` (as `InitializeLoop`), with ODO evaluated at the format stage and held for
  the later stages (GR7c). A VARYING counter (§2002:13.16.62) is set at loop entry and incremented per occurrence.
- A REDEFINES item is validated through the redefined area only where the clause set says so (§2002:13.16.42); the
  binder owns that decision, never the emitter.
- No C# text in the bound node (BackendContractDriftTests). The emitter (`CodeGen/Verbs/ValidateEmitter.cs`) renders the
  plan; the five stage loops run over ALL elements before the next stage (GR4), so the emitter lays the stages out as five
  passes over the same element list, never one pass per element.

## 3. The internal indicator (GR5)

One small state per element occurrence: `NotProcessed | Valid | InvalidFormat | InvalidContent | InvalidRelation`,
assigned `Valid` at statement start and `NotProcessed` for an element skipped (PRESENT WHEN false, an ODO occurrence past
the count). It is set only from `Valid` (the NOTE of GR5: it never changes from one invalid kind to another). A group is
invalid on format if a subordinate is; invalid on content or relation if a subordinate is or its own indicator says so
(GR6e). The indicator lives in a statement-local array the emitter allocates (stack for a fixed plan, pooled for ODO); it is
not part of the record (there is no byte substrate, CLAUDE.md "The project").

## 4. The five stages (§2002:14.8.46.3 GR6)

1. **Format.** Per elementary item: PICTURE/USAGE/SIGN compatibility. This is the numeric class test and the
   alphabetic/boolean/national class tests the compiler already owns (`CobolNum.IsNumericClass`, the class-condition
   renderer), reused, not re-derived; a failing item is InvalidFormat and its content is not changed (GR5). The DEFAULT
   value is established for every item §2002:13.16.16 covers, even with no DEFAULT clause. EC-DATA-INCOMPATIBLE raised by
   the format test is non-fatal inside VALIDATE (GR6).
2. **Input distribution.** DESTINATION receivers get the item by an implicit MOVE: the existing MOVE path.
3. **Content.** CLASS clause and the level-88 VALID/INVALID entries (§2002:13.16.61 format 5). Subordinate before group.
4. **Relation.** INVALID WHEN conditions, subordinate before group, through the ONE condition renderer.
5. **Error indication.** Set EC-VALIDATE and, per kind, EC-VALIDATE-FORMAT, -CONTENT, -RELATION; then update every
   VALIDATE-STATUS item whose FOR names an element, in data-division order (GR6e, §2002:13.16.60).

EC raising goes through the existing `EcEmitter`/`EcBinder` machinery (`EcBinder.RaiseDeclarations.cs`): the VALIDATE node
is registered there as the raising statement for the four EC-VALIDATE names, and `ExceptionCatalog`'s existing rows
(`EC-VALIDATE-FORMAT/CONTENT/RELATION` nonfatal, `-VARYING` fatal, `-IMP`) stay.

## 5. Syntax and the statement's rules

The parse side exists (`Grammar/Core/CobolDeclined.g4` `validationClause`, `validateValidPhrase`); it is MOVED into the
data-division grammar proper (renamed off "declined", predicates kept per edition) and each clause gets its binder in
`DataBinder` (`ValuePlacement` for the format-5 entry). The statement's syntax rules SR1 to SR5 (§2002:14.8.46.2) are
already screened by `DeclinedFacilityBinder.ScreenValidateOperand`; that member moves to `ValidateBinder` unchanged. SR3
(a VALIDATE-STATUS clause referencing the operand) becomes live once the clause binds.

## 6. Stages of the build (each a landing: parser + binder + emitter + golden + manifest entry)

- **S1** the statement, the element walk, indicator, FORMAT stage (PICTURE/USAGE/SIGN), DEFAULT, stage 5 exceptions;
  `BoundValidate` and `ValidateEmitter` skeleton; the decline of COBOL1580 on the statement ends here.
- **S2** DESTINATION (stage 2) and the DEFAULT/DESTINATION overlap rules (§2002:14.8.46.3 GR9, GR10).
- **S3** CLASS and the level-88 VALID/INVALID entries (stage 3); `VARYING` validation leg.
- **S4** INVALID WHEN, PRESENT WHEN format 2, OCCURS/ODO/REDEFINES effects (GR7), RECORD VARYING padding (GR8).
- **S5** VALIDATE-STATUS (stage 5 updates), the EC-VALIDATE family goldens, the decline code deletions
  (COBOL1708, COBOL1710), `docs/CONFORMANCE.md` flip, rows closed by `record_verdicts`.

Each stage keeps the decline for the clauses it has not yet built, so a clause is refused by name until it works and never
accepted inert (the VALIDATE statement itself is the one exception today and ends in S1: accepting it inert while clauses
that act at stage 1 are refused would be a wrong answer).

## 7. Tests

One positive golden per clause at its introducing edition (2002) and one refusal below it (COBOL0900, existing matrix row
`validation-clauses-2002`); runtime goldens compute expected values from §2002:E.19.7.1 (the spec's own worked examples:
USAGE DISPLAY and non-display items), not from observed output. Sort and file I/O tests (I11) are not reached: the
statement reads storage only; a FILE SECTION operand is covered by the GR8 padding golden in S4.
