---
heading: "## 4. Documented non-support facilities (§2023:4.2.6 / §2023:4.2.7 / §2023:4.2.13)"
---
The following facilities are **not implemented** (commit and rollback only for the sequential organizations, item 2),
and every element of each is **recognized and refused or
warned BY NAME** — never a generic parse error, and never silently accepted. Which of the two it is follows from
WHICH annex licenses the decline, and the distinction is normative, not stylistic:

* A **processor-dependent** element (Annex A.3, §2023:4.2.6) may be accepted, and §2023:4.2.6 ¶3 makes the compile-time
  **warning mechanism** mandatory — so the MCS *statements* and the RECORD DELIMITER
  *clause* is accepted-inert with a named **Warning** (COBOL1778) and the program still runs.
  ⛔ **The VALIDATE statement (COBOL1580) is in this band but NOT under this licence** (kb/Work PB709):
  VALIDATE is Annex A.4.14 and is not listed in Annex A.3 at all, so §2023:4.2.6 mandates nothing about it. §2023:4.2.7
  requires only that its non-support be identified here (item 3 below, and §5 row A.4.14); accepting it inert
  and naming it, rather than refusing it, is a documented CHOICE, taken because the facility is additive — a
  program with a VALIDATE statement has a well-defined meaning with the facility inert — and because §2023:4.2.13
  additionally makes it obsolete at COBOL-2023. Citing §2023:4.2.6 for it would claim a mandate the standard does
  not give and would misdescribe the posture: a decline under the processor-dependent clause reads "we could
  not", a decline under the optional clause reads "we chose not to, and documented it".
* An **optional** element (Annex A.4, §2023:4.2.7) is different: A.4.1 says an implementation "shall accept the syntax
  and provide the functionality for an optional element **only when support for that language element is claimed
  by the implementor**". For a module, or the part of one, that §5 does not claim, accepting the syntax is itself the
  non-conformance — so those elements are refused by name with an **Error**: **COBOL1560 / 1707** (screen
  handling, A.4.2), **1705** (FORMAT / SELECT WHEN, A.4.8), **1706** (WRITE / REWRITE FILE, A.4.13) and
  **1708 / 1710** (VALIDATE's data-division clauses and every declined module's
  exception-names — the `declined-optional-element` suppress family). All of them emit through the ONE
  declined-element seam, `EditionContext.Declined`, whose refuse rule (`EditionContext.Refuses`) chooses the channel.
  ⚠ **`--permissive` moves SOME of these and not others, and the axis is whether an accepted element could
  change the ANSWER the source states.** 1708/1710 downgrade to warnings. The VALIDATE clauses (1708) bind to a no-op, because their sibling
  STATEMENT is already accept-inert (1580).
  A declined module's exception-name (1710) is accepted as an ordinary exception-name and RESOLVES: no
  statement of the declined facility ever sets its condition (§2023:14.6.13.1.1 licenses raising nothing for it),
  so `>>TURN … CHECKING ON` enables a check only a RAISE can trigger, and a `RAISE EXCEPTION` of the name
  raises it exactly as written (§2023:14.9.29.4 GR1): a USE declarative for it runs, a WHEN phrase matches it,
  and execution continues per Table 13 — an observable result, but the one the source states
  (`DocumentedNonSupportWitnessTests.DeclinedModuleExceptionName_Permissive_*`). 1560/1705/1706/1707 do **not** move in either direction: an
  inert screen `ACCEPT` is re-read as the device ACCEPT and moves the wrong data, an inert `FORMAT` clause
  changes which bytes reach the medium, an inert `WRITE FILE` writes the wrong record. The fact lives on
  the descriptor (`PermissiveInert`) and is read in the seam, never tested at an emit site.
