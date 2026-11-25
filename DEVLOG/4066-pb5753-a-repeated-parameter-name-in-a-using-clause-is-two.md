## Entry 4066 — 2026-11-25 03:37 PST — PB5753: a repeated parameter-name in a USING clause is two formals at 2002, refused only from 2014

**The defect.** `INTERFACE-ID. G2DUPI USING P P.` (and its CLASS-ID twin) was refused COBOL2239 at `--std 2002`, citing
§2023:11.6.3 SR7 / §2023:11.3.3 SR9. Re-probed on this branch's base: both repros (`dupparm.cob`, `dupclass.cob`) refused at
2002 and at 2014 with the 2023 citation. `OoExpansion.AddSkeleton` asked the rule at every edition, and every header rule it
cites was a 2023 string.

**The text.** "A given parameter-name shall not appear more than once in a USING clause" is §2014:11.3.2 SR9 and §2014:11.6.2
SR7 (2023 the same); §2002:11.2.2 and §2002:11.5.2 have no such rule. §2002:11.2.3 GR5 / §2002:11.5.3 GR3 make each USING entry
"the name given to the formal parameter", so 2002's `USING P P` names two formals P; a reference to P identifies neither
(§2002:8.4.1), and an expansion would have two "corresponding actual parameters" for it (§2002:12.2.7.3 GR5 / GR8).

**The fix.** `DialectBehavior.ParameterNameAppearsOnceInUsingClause` (VCR row 7.129, changed in 2014) gates the USING-clause
refusal. Below it `AddSkeleton` refuses every reference to a repeated formal, COBOL2239 citing §2002:8.4.1; the references are
chosen by the substitution's own occurrence predicate (`OoExpansion.IsUserWordOccurrence`, now shared with
`SubstitutedTokens`), less the formal's own REPOSITORY specifier, so the refused set and the replaced set cannot drift. The
sibling sweep of `AddSkeleton`: its four other header rules (SR8 / SR4, GR6 / GR4 with 2002's "class-name" wording, the
EXPANDS-in-skeleton SR3) now cite the edition compiled under through one table, `OoExpansion.HeaderRules`. The lineage
screen places the behaviour under §2002:11.2 and §2002:11.5; `docs/OO_DESIGN.md` D12's Diagnostics paragraph states the
edition split.

**Goldens.** `cases/11.2@2002-CLASS-ID-PARAGRAPH/pb5753_repeated_parameter_name_2002` (accepted at 2002: an expansion of a
`USING P P` class runs, "PB5753G SHOW", "COUNT 1", "COUNT 2"; refused at 2014 and 2023) and
`cases/11.5@2002-INTERFACE-ID-PARAGRAPH/pb5753-repeated-parameter-name-referenced` (refused at every edition, COBOL2239).
`ParameterizedClassTests` pins each edition's citation.

**Rows.** None re-verdicted: no inventory row waited on this note (`closes_rows: []`).

**Found on the way, registered.** The EXPANDS-phrase and identity messages of `OoExpansion.Validate` still cite 2023 at 2002 and
2014, and §2023:12.3.8.4 GR1 (a parameterized class may be named only in a REPOSITORY paragraph) has no 2002 text; and
`UniqueReferenceRule.Citation` cites §2023:8.4.2.1 at 2002 and 2014.

