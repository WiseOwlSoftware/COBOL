# DESIGN - the VALIDATE facility (data validation, input distribution, error indication)

> **Status: DESIGN, APPROVED BY THE CODEBASE OWNER (Claude, CLAUDE.md "Ownership"); S1 BUILT (kb/Work PB5577), S2 to S5 NOT YET.**
> The validation clauses still draw COBOL1708 until their stages land (`docs/conformance/5/A.4.14.md`, `docs/conformance/4/VALIDATE.md`);
> the VALIDATE statement runs (`ValidateBinder`, `ValidateEmitter`), COBOL1580 and COBOL1710 are retired, and EC-VALIDATE-FORMAT is raised.
> Work register: kb/Work PB5577 (the 2002 rows) with the 2023 rows of the same clauses.

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
text rules at its own `--std` (S1 to S5 read each clause at both). The facility's change sets therefore delete: `DeclinedFacilityBinder` (S1, replaced by `ValidateBinder`), COBOL1580 and
COBOL1710 (S1: the three EC-VALIDATE negative goldens went with COBOL1710), and then `DeclinedFacilityPass`'s validation arms,
`CobolDeclined.g4`'s validation rules and COBOL1708 (their negative goldens become positive goldens; S5), flipping the A.4.14 cells of
`docs/conformance/` when the last stage lands: `docs/CONFORMANCE.md` section 5 keeps the module "Not claimed" until then (the suite
profile claims a module as a whole). No shim keeps the old codes alive (CLAUDE.md rule 4).

## 2. Shape: expand at BIND time, as INITIALIZE does

`BoundInitialize` (`Binding/Bound/BoundInitialize.cs`) is the model: the binder expands the statement into a flat series of
per-element actions that the emitter renders through the ONE MOVE path. VALIDATE gets the same shape:

- `BoundValidate(Place Written, IReadOnlyList<ValidateStep> Steps)` - one node per identifier-1, in source order in a
  `BoundImplicitSeries` (§2002:14.8.46.3 GR3: a separate implicit VALIDATE per identifier; a declarative that RESUMEs NEXT STATEMENT
  continues at the next one). A step is a `ValidateElement` or a `ValidateLoop` over one table dimension.
- The plan is built by `ValidateBinder` (`Binding/Procedure/Verbs/ValidateBinder.cs`) walking the operand's elements (GR2: the
  operand and every subordinate item except class index, object and pointer) in DEFINITION ORDER over a `PlaceCursor`. Each element
  becomes a `ValidateElement` carrying its `Place`, and the clauses
  that apply to each of the five stages (GR4), already bound (S1: the format test only): the PICTURE/USAGE/SIGN format check, the DEFAULT recipe
  (§2002:13.16.16), the DESTINATION receivers, the CLASS test and the 88-level VALID/INVALID entries, the INVALID WHEN
  conditions (several fold into one OR, GR6d), PRESENT WHEN (§2002:13.16.39), and the VALIDATE-STATUS items whose FOR
  names the element (§2002:13.16.60, in data-division order, GR6e).
- An OCCURS dimension is a `ValidateLoop` (as `InitializeLoop`; S1) over the table's CURRENT count (`OccurrenceCounts.Current`: the
  fixed count, the DEPENDING item's value, a dynamic table's capacity). The count is read at loop entry in S1; the stages that re-walk the
  table (S3, S4) must read the value GR7c fixes at the format stage ONCE and hold it (a statement-local), because a DESTINATION move
  (stage two) can change the DEPENDING item. A VARYING counter (§2002:13.16.62) is set at loop entry and incremented per occurrence (S3).
- A REDEFINES entry is an element like any other (§2002:13.16.42.3 GR3: "each definition is used by the VALIDATE statement
  independently of the others"); PRESENT WHEN (S4) selects among them. `MemberCursor.Child` is null for a non-canonical view of a Tier-B class
  (INITIALIZE skips a redefinition), so `ValidateBinder.ChildCursor` builds that view's place as a reference to it is built,
  `PlaceBuilder.PlaceForItem` at the occurrence numbers of the tables above it.
- An element or operand whose storage no cursor reaches is refused by name, COBOL7363, never validated in part.
- No C# text in the bound node (BackendContractDriftTests). The emitter (`CodeGen/Verbs/ValidateEmitter.cs`) renders the
  plan; the five stage loops run over ALL elements before the next stage (GR4), so the emitter lays the stages out as five
  passes over the same element list, never one pass per element.

## 3. The internal indicator (GR5)

One small state per element: `NotProcessed | Valid | InvalidFormat | InvalidContent | InvalidRelation`,
assigned `Valid` at statement start and `NotProcessed` for an element skipped (PRESENT WHEN false, an ODO occurrence past
the count). It is set only from `Valid` (the NOTE of GR5: it never changes from one invalid kind to another). A group is
invalid on format if a subordinate is; invalid on content or relation if a subordinate is or its own indicator says so
(GR6e). S1 builds it as one `int` statement-local per element (`ValidateEmitter`; the elements of a table's loop share one, which
is all stage five's "if any of these internal indicators" needs). Stages three and four need a PER-OCCURRENCE indicator ("an elementary item
that is invalid on format does not undergo a content check"), which they introduce for the elements inside loops (a statement-local
array, stack-allocated for a fixed table, pooled for ODO). The indicator is not part of the record (there is no byte substrate, CLAUDE.md
"The project").

## 4. The five stages (§2002:14.8.46.3 GR6)

1. **Format (S1 built for numeric, alphabetic and boolean items).** Per elementary item: PICTURE/USAGE/SIGN compatibility. This is the
   numeric, alphabetic and boolean class tests the compiler already owns, bound as a `BoundClassCondition` and rendered by the one
   condition renderer (§2023:8.8.4.4.4 GR3 n, b, e), reused, not re-derived: a native numeric field folds to true, an image-stored (group
   moved, READ, REDEFINES) one is tested on its characters. A failing item is InvalidFormat and its content is not changed (GR5). NOT YET:
   the format check of an EDITED picture (numeric-edited and alphanumeric-edited: insertion characters present, the locale's monetary
   specification, §2023:13.18.40.4 GR19) needs a picture-conformance predicate the runtime does not have (the de-editing of §2023:14.6.13.2
   rule 4 is a MOVE-time decode, not a validator); it belongs with S4 and is registered in kb/Work PB5577; until then an operand holding an edited item is refused (COBOL7363), never reported valid. The DEFAULT
   value is established for every item §2002:13.16.16 covers, even with no DEFAULT clause. EC-DATA-INCOMPATIBLE raised by
   the format test is non-fatal inside VALIDATE (GR6).
2. **Input distribution.** DESTINATION receivers get the item by an implicit MOVE: the existing MOVE path.
3. **Content.** CLASS clause and the level-88 VALID/INVALID entries (§2002:13.16.61 format 5). Subordinate before group.
4. **Relation.** INVALID WHEN conditions, subordinate before group, through the ONE condition renderer.
5. **Error indication.** Set EC-VALIDATE and, per kind, EC-VALIDATE-FORMAT, -CONTENT, -RELATION; then update every
   VALIDATE-STATUS item whose FOR names an element, in data-division order (GR6e, §2002:13.16.60).

EC raising goes through the existing `EcEmitter`/`EcBinder` machinery (`EcBinder.RaiseDeclarations.cs`): the VALIDATE node
is the statement `NodeKindRaises` declares the EC-VALIDATE names for (S1: `EC-VALIDATE-FORMAT`; each later stage adds its name), the emitter
raises with `EmitConditionRaise` when `EnabledHere`, and `ExceptionCatalog`'s existing rows
(`EC-VALIDATE-FORMAT/CONTENT/RELATION` nonfatal, `-VARYING` fatal, `-IMP`) stay. The level-2 name EC-VALIDATE is the category of the
level-3 names, so a directive or declarative naming it covers whichever are raised.

## 5. Syntax and the statement's rules

The parse side exists (`Grammar/Core/CobolDeclined.g4` `validationClause`, `validateValidPhrase`); it is MOVED into the
data-division grammar proper (renamed off "declined", predicates kept per edition) and each clause gets its binder in
`DataBinder` (`ValuePlacement` for the format-5 entry). The statement's syntax rules SR1 to SR5 (§2002:14.8.46.2) are
already screened by `DeclinedFacilityBinder.ScreenValidateOperand`; that member moves to `ValidateBinder` unchanged. SR3
(a VALIDATE-STATUS clause referencing the operand) becomes live once the clause binds.

## 6. Stages of the build (each a landing: parser + binder + emitter + golden + manifest entry)

- **S1 (BUILT, kb/Work PB5577)** the statement, the element walk (groups, REDEFINES views, fixed and ODO tables), the indicator, the FORMAT
  stage for numeric, alphabetic and boolean items, EC-VALIDATE-FORMAT; `BoundValidate`, `ValidateBinder`, `ValidateEmitter`; COBOL1580 and
  COBOL1710 retired, the EC-VALIDATE names resolve. NOT in S1 (DEFAULT is a clause, so it comes with its clause): the DEFAULT value
  of §2023:13.18.17 and the edited-picture format check, both in S2/S4.
- **S2** DESTINATION (stage 2) and the DEFAULT/DESTINATION overlap rules (§2002:14.8.46.3 GR9, GR10).
- **S3** CLASS and the level-88 VALID/INVALID entries (stage 3); `VARYING` validation leg.
- **S4** INVALID WHEN, PRESENT WHEN format 2 (and with it the selection among REDEFINES views, §2002:13.16.42.3 GR3), the held ODO count and per-occurrence indicators (GR7 c), RECORD VARYING padding (GR8), the edited-picture format check.
- **S5** VALIDATE-STATUS (stage 5 updates), the EC-VALIDATE family goldens, the decline code deletions
  (COBOL1708), the `docs/CONFORMANCE.md` flip of module A.4.14, rows closed by `record_verdicts`.

Each stage keeps the decline for the clauses it has not yet built, so a clause is refused by name until it works and never
accepted inert (the VALIDATE statement was accepted inert until S1, which ended it: an inert statement beside clauses that act at
stage 1 would have been a wrong answer).

## 7. Tests

One positive golden per clause at its introducing edition (2002) and one refusal below it (COBOL0900, existing matrix row
`validation-clauses-2002`); runtime goldens compute expected values from §2002:E.19.7.1 (the spec's own worked examples:
USAGE DISPLAY and non-display items), not from observed output. Sort and file I/O tests (I11) are not reached: the
statement reads storage only; a FILE SECTION operand is covered by the GR8 padding golden in S4.
