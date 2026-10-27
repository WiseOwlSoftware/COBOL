## PB3347: a switch condition-name and a level-88 of one spelling in one source element are non-unique, and `IF S-ON` is refused

**The defect.** `SWITCH-1 ON STATUS IS S-ON` in SPECIAL-NAMES plus `88 S-ON VALUE 1` under a data item of the same
program compiled clean, and the unqualified `IF S-ON` bound the level-88 and printed ON for an OFF switch. The
mythos-lane adjudication (2026-10-25) settled the reading: both are the one type of user-defined word, condition-name
(§8.3.2.2.4), so declaring the pair is legal (§8.3.2.2 asks for one type per word), but the name is then non-unique and
§8.4.2.2.1 1) ("No other name has the identical spelling") requires qualification, §8.4.2.2.3 SR1 requires every
explicitly referenced non-unique name to be made unique, and a switch condition-name takes no qualifier (§8.8.4.6.2).
§8.4.6.1 does not rescue the 88: it governs identical names in DIFFERENT, nested source elements, so it decides only the
inherited case (a container's switch name yields to the contained program's own 88), which was already right.

**Re-probe on this build (deae52c90).** `p1` (IF S-ON over the pair): compiled clean, printed OFF with the switch unset
and with it ON, the 88 bound silently. `p2` (`IF S-ON OF V`): Q-OFF, right. `p3` (switch only): OFF/ON, right. `a1` (two
same-named 88s): COBOL1639, so the uniqueness rule existed but did not count the switch. `a2` (pair declared, never
referenced): clean, right. `s3` (switch in the outer SPECIAL-NAMES, 88 in the contained program): INNER-OFF, right.

**The fix.** Three parts, one root (the 88 won because nothing counted the switch as a candidate):
- `ConfigurationNames` now tells a name the element declares from one `InheritFrom` took from a container
  (`DeclaresOwn`), where before both lived in one dictionary.
- `DataBinder.OwnSwitchConditionCompetes` is the other half of the §8.4.2.2.1 count beside
  `QualifiedConditionCandidates`: a reference with no suffix at all (no qualifier, subscript or reference modifier) to a
  spelling this element's own SPECIAL-NAMES declares as a switch condition-name counts the switch as one more candidate.
  `ConditionBinder.ConditionOf`, the one resolution every condition position reads (IF, EVALUATE, PERFORM UNTIL, SEARCH
  WHEN, SET condition-name TO TRUE), reports the existing COBOL1639 (warning plus the first level-88 under `--permissive`),
  once per source reference. A qualified reference names the level-88 alone (§8.4.2.2.3 SR5); a switch name only a
  container declared still yields to the element's own item (§8.4.6.1, `ReferenceResolver.DescribesOwnItem`,
  unchanged). No new diagnostic code; COBOL5254-COBOL5256 are returned.
- The "level-88 wins / NC211A regression guard" ordering rationale in `ConditionBinder.AnalyzeBareOperand`,
  `SetAlterBinder.SwitchCondOf` and `DataBinder.SwitchConditions` is rewritten to the real rule, and the test that pinned
  the wrong answer (`Switch_Level88WinsOverSwitchStatusName`, with its golden) is deleted.

**NIST.** NC211A is the program the old order was written for: its `A` column-7 lines declare the switch names and its
`B` lines are the substitute for a processor WITHOUT switches (level-88s spelling the same names). `--nist` kept both,
so the program became the pair. The CCVS indicator table (`FixedFormConverter.KindOf`) now treats `B` as an excluded
alternate beside `P/J/H/E/U`: WiseOwl COBOL implements switches, so `A` is the active configuration. NC174A and NC254A
carry `B` lines too, but only unreachable `MOVE "SWITCHES NOT IMPLEMENTED" TO RE-MARK` bodies of their delete paths; the
three goldens are unchanged (`NistDifferentialTests` NC174A, NC211A, NC254A pass; `NistRunner` already runs with
`COBOL_SWITCH_1=ON`). The convention is recorded in `DESIGN-frontend-grammar.md` and pinned by two new
`IndicatorArea_OnlyFixedIndicators_OutsideNist` rows.

**Goldens.** `conformance-suite/cases/8.4.2.2-Qualification/`: `pb3347_switch_condition_name_beside_level88` (the pair
declared, `S-ON OF V` names the 88, `S-OFF` the switch: Q1-FALSE, SW-OFF, Q2-TRUE, all four editions),
`pb3347_inherited_switch_condition_name_yields` (INNER-ON), and the refusals `pb3347-switch-condition-name-ambiguous-if`
and `-set` (COBOL1639 at 85, 2002, 2014 and 2023, in `wiseowl/diagnostics.json`). `ConfigurationNamesTests` pins the
own-versus-inherited fact.

**Not done, by design.** PB3216's declaration-side refusal is not a sibling: the declaration pair is legal. Mnemonic-,
alphabet- and class-names against a data-name of one spelling are already COBOL2692 (a different type of user-defined
word), probed unchanged. A switch condition-name re-declared by a contained program cannot arise (a contained program
may not write a CONFIGURATION SECTION, COBOL1643).

**Every position.** One probe program refuses the pair at `IF NOT S-ON`, `IF (S-ON)`, `IF S-ON AND V = 2`, `PERFORM UNTIL
S-ON`, `EVALUATE TRUE WHEN S-ON` and `SET S-ON TO TRUE` (six COBOL1639, one each), and accepts `IF S-ON OF V`: all reach
`ConditionOf`.

**Self-review.** Architecture: the count lives in one predicate beside `QualifiedConditionCandidates`; `FlagConformancePass`
reads only the pure level-88 set and is unaffected. Full code: the first leg-1 run was RED on my own new golden (GOBACK in
the contained program is COBOL-2002+, so the 85 leg refused it); the program now ends with EXIT PROGRAM. Performance: the
predicate runs only after a level-88 matched, one dictionary and one set lookup. Duplication: none, the old "88 wins"
wording was deleted at its three sites rather than reworded in a fourth.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` at 2b5b3bc08, Linux half (WSL clone of HEAD)
GREEN; the first run (RED, the GOBACK golden) is described above.
