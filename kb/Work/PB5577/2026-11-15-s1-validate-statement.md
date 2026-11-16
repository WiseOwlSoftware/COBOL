**2026-11-15, implementer wave 20262044 (group A): stage S1 of docs/rearchitecture/DESIGN-validate-facility.md landed.**

Re-probe at the base (81a39d006f plus the design commit): `VALIDATE REC` compiled with COBOL1580 and did nothing; `>>TURN EC-VALIDATE CHECKING ON`, a USE
declarative for EC-VALIDATE-FORMAT and `RAISE EXCEPTION EC-VALIDATE-CONTENT` were refused with COBOL1710.

What S1 builds: `VALIDATE` binds to a `BoundValidate` per identifier-1 (a `BoundImplicitSeries`, GR3) over the elements of the operand (GR2): groups, REDEFINES
views (§2002:13.16.42.3 GR3, reached through `PlaceBuilder.PlaceForItem` where `MemberCursor.Child` is null), fixed and OCCURS DEPENDING ON tables as
`ValidateLoop`s (GR7 b, c). Stage one is a `BoundClassCondition` per numeric, alphabetic or boolean item rendered by the one condition renderer; stage
five raises EC-VALIDATE-FORMAT (nonfatal) through `EmitConditionRaise` when the checking is enabled at the statement. COBOL1580 and COBOL1710 are retired, the
EC-VALIDATE names resolve, COBOL7363 refuses a part of an operand no cursor reaches. Goldens: `cases/14.9.50-VALIDATE/pb5577-validate-format-stage` and
`pb5577-validate-redefines-boolean` (2002, 2014, 2023); the refusal below 2002 is the existing `pb3148-validate-statement-below-2002`.

Not built (each registered in the design, section 6): the format check of an EDITED picture (needs a picture-conformance predicate the runtime does not have),
the DEFAULT value, DESTINATION, CLASS and the 88-level VALID/INVALID entries, INVALID WHEN, PRESENT WHEN format 2, the held ODO count, RECORD VARYING padding,
VALIDATE-STATUS. The 2023 rows of the statement keep DOCUMENTED-NON-SUPPORT (the derived verdict `validate-only` holds every rule of the statement there while A.4.14 is Not claimed; they flip at S5), with notes saying what is built; the 2002 rows of this note stay GAP (the 2002 inventory's change records are
unclassified, kb/Work PB4996), so `closes_rows` stays empty.
