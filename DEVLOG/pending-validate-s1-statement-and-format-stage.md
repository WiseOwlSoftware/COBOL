## PB5577 S1: the VALIDATE statement runs, its format stage checks numeric, alphabetic and boolean items, and EC-VALIDATE-FORMAT is raised

PB5577 says 2002 requires VALIDATE and the compiler declines it. The branch carries the design (`docs/rearchitecture/DESIGN-validate-facility.md`, five stages S1 to S5)
and builds S1. Before: `VALIDATE REC` compiled with the warning COBOL1580 and did nothing, and every EC-VALIDATE name was refused with COBOL1710.

**Mechanism.** `ValidateBinder` (it replaces `DeclinedFacilityBinder`) binds each identifier-1 to a `BoundValidate` (one per identifier, in a `BoundImplicitSeries`, so a
RESUME NEXT STATEMENT lands at the next implicit statement, §2023:14.9.50.4 GR3) over the elements of the operand: groups, REDEFINES views (§2002:13.16.42.3 GR3, each
definition checked independently; a non-canonical view of a Tier-B class has no structural cursor, so it is placed as a reference to it is, through
`PlaceBuilder.PlaceForItem`), and tables as `ValidateLoop`s (fixed counts and the OCCURS DEPENDING ON current count, GR7 b and c). Stage one's test is a
`BoundClassCondition` per numeric, alphabetic or boolean item, rendered by the one condition renderer (a native numeric field folds to true; an image-stored one is
tested on its characters). `ValidateEmitter` keeps one internal indicator per element (GR5: set only from valid) and raises EC-VALIDATE-FORMAT from stage five when the
checking is enabled at the statement (`NodeKindRaises` declares it, `EmitConditionRaise` raises it, nonfatal). A part of an operand no cursor reaches is COBOL7363.

**Deleted, with every caller.** COBOL1580 and COBOL1710 (retired, never reallocated), `StatementBinder.BindDeclinedStatement` and `BindUnsupportedFacility`,
`DeclinedFacilityBinder`, the EC-VALIDATE row of `EcNameResolution.DeclinedEcFamilies`, the three `declined-ec-validate-*` negative goldens and the PermissiveInert
exception-name witnesses that used them; `DeclinedFacilityOperandDriftTests` lost the guard for `BindUnsupportedFacility` and now censuses `BindIntroducedAt`, `ImplicitStatementSeriesDriftTests` lost its VALIDATE exemption
(the verb binds through `BoundImplicitSeries.Of` and cites §14.9.50.4), and the architecture-edges and binder-member-readers files carry `ValidateBinder` where they carried `DeclinedFacilityBinder`.

**Goldens.** `cases/14.9.50-VALIDATE/pb5577-validate-format-stage` (valid record, a bad numeric, alphabetic and table item, the DEPENDING count, a subscripted operand, two
identifiers, content unchanged, checking off) and `pb5577-validate-redefines-boolean`, at 2002, 2014 and 2023; expected lines derived from §2023:14.9.50.4 GR3, GR5, GR6 and
§2002:13.16.42.3 GR3. The refusal below 2002 is the existing `pb3148-validate-statement-below-2002`.

**Rows.** The 23 2023 rows of the VALIDATE statement and its dependants keep the verdict DOCUMENTED-NON-SUPPORT: the module stays "Not claimed" in `docs/CONFORMANCE.md` section 5 until
S5 (the conformance profile claims a module as a whole), and the derived verdict `validate-only` forces that verdict on every rule of the statement (a first batch that made eight of
them CONFORMS was caught by `DerivedVerdictDriftTests.NoValidateConditionedRow_Diverges` and reverted). Their notes now say what is implemented, which stage builds the rest and
that the verdict flips at S5; every dead witness is retired (`audit_witness_loss.py`: 0 unexcused, 50 retired). The 2002 rows of the note stay GAP: the 2002 inventory's change
records are unclassified (kb/Work PB4996), so none can take a verdict yet.

**Not built.** The format check of an edited picture (no picture-conformance predicate exists in the runtime), DEFAULT, DESTINATION, CLASS and the 88-level VALID/INVALID entries,
INVALID WHEN, PRESENT WHEN format 2, the held ODO count, RECORD VARYING padding and VALIDATE-STATUS: S2 to S5 of the design.
