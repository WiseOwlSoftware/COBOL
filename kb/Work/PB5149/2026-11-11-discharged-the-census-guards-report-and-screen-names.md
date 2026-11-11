## Landed 2026-11-11 (wave 20261998 A) - DISCHARGED: report-names and screen-names are refused by the user-word census; a constant-name is not a data item

**Re-probe (Debug build of the group A branch, base cba742bfb5).** The note supposed the PB5073 GLOBAL-name screen
(`DataBinder.ScreenGlobalNameUniqueness`) was the only guard of a name shared by a data item and a report-name,
screen-name or constant-name. Probes (scratch `E:/COBOL-coord/scratch/w-mythos/w20261998a/p/`):

- **report-name** (`c3.cob`, `RD RR GLOBAL` beside `01 RR PIC 9`): `error COBOL2692: 'RR' names two types of user-defined
  word in this source element (report-name and data-name)`. The census (`UserWordCensus`, fed by `DeclareUserWord`,
  `ReportSectionBinder.cs:79`) refuses it at every edition, with or without GLOBAL. Authority: §2023:8.3.2.2 ("a given
  user-defined word may be used as only one type of user-defined word"; `cite.py --check` OK). The exception group is
  constant-name, data-name, property-name, record-key-name and record-name; a report-name is not in it.
- **screen-name** (`c4.cob`): the SCREEN SECTION is refused by name, COBOL1560 (an optional module,
  §2023:4.2.7, for which no support is claimed), so no screen-name reaches the binder; where one is ever accepted it
  goes through the same funnel (`DataBinder.cs:565`, `DeclareUserWord(sn, UserWordKind.ScreenName)`).
- **constant-name** (`c1.cob`, `c2.cob`: `01 KK CONSTANT GLOBAL AS 5.` beside `01 KK PIC 9 GLOBAL`, both orders): accepted.
  §2023:8.3.2.2 exception 3 lets a constant-name and a data-name share a spelling, and §2023:8.4.2.1 names "any other
  data item"; a data item is a "unit of data defined by a data description entry or resulting from the evaluation of an
  identifier" (§2023:3.53), and a constant entry (§2023:13.10.1: "A constant entry defines a constant. A constant may be
  used in place of a literal.") is not a data description entry, so the sentence does not reach a constant. The
  standard states no declaration rule for the pair; the rule that bites is the reference rule (see the lead below).

**Witness added.** `conformance-suite/cases/8.3.2.2-User-defined-words/pb5149-report-name-and-global-data-name.cob`
(refused at 85, 2002, 2014 and 2023, `expect` `(report-name and data-name)`), so the report-name arm is pinned by a
golden of its own; `generate.py --check` GREEN.

**Lead (not this mechanism, not fixed here).** `DISPLAY KK` with `01 KK CONSTANT GLOBAL AS 5.` and `01 KK PIC 9 VALUE 1.`
in one program compiles and displays 5: the resolver picks the constant. §2023:8.4.2.2.1 requires qualification of a
user-defined name unless "No other name has the identical spelling", and a constant cannot be qualified, so the reference
is ambiguous and should be refused (COBOL1639's family). Reference resolution, not a declaration screen.
