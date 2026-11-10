# The per-edition conformance suites — design (kb/Work PB3160, PB4614)

**Status: Revision 3, APPROVED by the owner 2026-11-04** (decisions 1-17 and 17a-17j, §1.1). It replaces revision 2
(approved 2026-10-14), whose slices S0a to S2 built the tree it extends (§13). Revision 3 answers kb/Work PB4614: what a test covers (§3.4), how a diagnosis is judged (§3.2), how
a test is placed at each edition (§3.5), how completeness and NIST-85 subsumption are measured (§4.1, §6.3), and how
every test is derived from the text (§6.1, §6.2). Seventeen owner decisions and the seventeenth's ten
sub-decisions bind it (§1.1). Figures were measured on main at 33f2c1ecc, decision 17's at 022838e99, by the scripts in
`E:/COBOL-coord/scratch/pb4614/`, to which a path below with no root is relative.

## 1. The direction and the decisions

The owner, 2026-10-14: "Separately, I think we should evolve all test cases to separate suites for each COBOL
specification version, more or less positioning them as conformance tests equivalent to the NIST tests. However, we
need not use the NIST structure as that is a 40+ year old designs. We can modernize". The owner then decided that "Any
COBOL compiler" can run the suites (expected results are outputs and diagnosis sites, never WiseOwl codes, and the suite
can be licensed on its own); that they are "A: one tree, generated" (§4); that revision 1's case format, tree, harness
and migration "Looks right"; and "Pilot now, rest after GAP": the move and the pilot now, the clause-by-clause
completion after zero GAP and zero known defects (kb/Work PB2911).

**Success** is one suite per edition (85, 2002, 2014, 2023, and 2027 once published), each witnessing every rule of its
edition that a program can observe, inherited rules included, and every coverage point of those rules (§3.4), by tests
whose expected results come from the ISO text. The COBOL-85 suite measurably subsumes NIST CCVS85 (§6.3). Any conforming
compiler can run a suite and get a per-test verdict that FAILs only for a non-conformance. A rule WiseOwl declines
(documented non-support) counts as witnessed only once some compiler has passed a test written from the standard.

**"Every rule" means every row of the rule catalog.** Some normative prose has no row (8.3.2.1's "not more than 63
characters" sentence, 6.4.2's continuation rules); a lexical scan finds at most about 129 of the 2,189 "shall"
sentences of clauses 6-16 in no row (`review-fidelity/uncatalogued_shall.py`). §9's text-to-rows check closes that gap
(S19).

**What "85" is.** ISO 1989:1985 with its two amendments: FLAG-85 flags incompatibilities "between the specifications in
ISO 1989, including ISO 1989/Amd 1 and ISO 1989/Amd 2, and this International Standard" (`t2002.txt:3588-3590`), and the
2002 working document's F.2 item 77 says "Previously, the intrinsic function facility was a separate module and its
implementation was optional" (`t2002.txt:44112`). So NIST-85's IF module (45 programs, 732 units) is in scope. Every
85 fact, this definition included, is inferred from the 2002 working document, NIST-85 or a vendor document and marked
`inferred` (decision 10, §3.5).

### 1.1 The owner's decisions of 2026-11-04

The owner, 2026-11-04 (PB4614): "we should run a deep architectural design for the test suite at some point to decide
how they should be structured. E.g. For each ISO section reference? Each alternative statement? How do we test for
proper error reporting? It's a hard problem." (PB4613): "Our test suites must be comprehensive enough to subsume and
replace the NIST-85 tests for Cobol-85, plus provide equivalent best in brand test suites for each subsequent Cobol
version." And: "Fuzz generation is random and we want a comprehensive ISO spec test suite".

| # | Decision | Lands in |
|---|---|---|
| 1 | Every inventory rule row has a test, AND every alternative of every general format (each choice, optional phrase, repetition) appears in at least one accepted test. No pairwise combinations | §3.4 |
| 2 | The portable suite judges a violation at the ISO minimum: a compiler that flags it at the marked line passes whether it warns or refuses. WiseOwl's exact codes and refusals are pinned in a separate WiseOwl layer | §3.2 |
| 3 | NIST-85 subsumption is a measured feature map: every NIST-85 test mapped to the ISO rules it exercises, each with an 85 suite test, and a report showing 100% of NIST's tested behavior covered, plus what NIST missed. NIST programs are not copied | §6.3 |
| 4 | The COBOL-85 suite comes first, until it measurably subsumes NIST-85; then 2002, 2014, 2023, each reusing the tests that stay valid | §8 |
| 5 | A 2014 placement resting on inference carries a marker naming its evidence (FCD, Annex E, 2002 and 2023 agreeing); the report lists those tests separately | §3.5, §4.1 |
| 6 | A test depending on an Annex A.1 choice reads the expected value from each compiler's profile (WiseOwl's from `docs/CONFORMANCE.md`); an undocumented choice is reported, not failed | §3.3, §5 |
| 7 | A row is suite-witnessed at an edition only when EVERY applicable coverage point inside it is (format alternatives, table cells, lettered sub-items); otherwise it is partial, and points witnessed is a second axis | §3.4, §4.1 |
| 8 | The coverage measure may read WiseOwl's parse tree to credit format points; cases stay compiler-neutral and no verdict depends on it | §3.4 |
| 9 | A diagnostic away from the marked site is UNLOCATED; one that also fires on the control is NOT DISCRIMINATING; neither is a FAIL | §3.2, §5 |
| 10 | The 1985 text is held (`specs/ANSI_COBOL_1985.md`, PB4850): a COBOL-85 fact is proved from it through the rule lineage (owner 2026-11-08, kb/Work PB4991; DESIGN-spec-conformance-review.md §10.2) | §3.2, §3.5 |
| 11 | NIST-85's 17 FIPS subset-flagging programs (177 "NON-CONFORMING STANDARD" flags; FIPS PUB 21-1, a US federal layer with no ISO text) are outside the suite: the NIST map lists them as excluded with that reason, and "subsumes NIST-85" means all of NIST's ISO-language tests | §6.3 |
| 12 | WiseOwl implements the Communication facility at 85 and 2002 (NIST's CM module), so the suites can witness it; its current refusal gets a kb/Work note | §3.5, §6.3 |
| 13 | Intake programs (fuzz, vetting and report repros) are committed as source material holding provenance only; their target rows and status live in their kb/Work notes | §6.2 |
| 14 | The portable 2014 suite withholds the four unproven 2014 edges (picture-63, the float format phrase, OPTIONS FLOAT-BINARY, OPTIONS FLOAT-DECIMAL) and the rows resting on them, listed as unplaced at 2014 with their evidence | §3.5 |
| 15 | The pilot (S3, PB3178) runs after S5-S8 and before the coverage-point catalog, and sets its scope | §8 |
| 16 | The tool slices (generator, point extractor, NIST crosswalk) may start now as pilot work; the GAP rise they cause is reported as newly measured scope, not regression | §4.1, §8 |
| 17 | Fewer files: "We should reduce the number of files in the test suites by colocating closely related COBOL features in the same source file." The unit of the tree is a case file holding the tests of closely related features | §3-§6, §9, §13 |

Decision 17's sub-decisions, approved by the owner 2026-11-04 ("approve 17a-17j"):
- 17a. **Grouping** (§3): one clause folder's tests in rule order, about 12 a file, at most 25.
- 17b. **Identity** (§3.1): a file block for the source form, then one block per test.
- 17c. **One run per file** (§3, §4): a generated driver labels and CALLs each test; a test's output is its section.
- 17d. **Per-test verdicts** (§5): a closed, equal section PASSes; any other test reruns solo, and that verdict stands.
- 17e. **Editions** (§3): bands stay per test; each edition's and profile's group holds only the tests that apply.
- 17f. **Flagged tests** (§3.2): one `.flagged.jsonl` per file; the test holding a site is its control.
- 17g. **A file that does not compile** (§5): each of its tests compiles solo.
- 17h. **What runs alone** (§3): a test whose text or state reaches past its own units, or that needs its own compile.
- 17i. **Migration** (§6): one slice, S5b, neutral by WiseOwl's verdicts.
- 17j. **The WiseOwl layer** (§3.2, §4): a file per case file, keyed by test id.

## 2. What exists (main at 33f2c1ecc)

Revision 1's adapter passed 5 of 40 positive 2002 programs on GnuCOBOL 3.2, mostly not for GnuCOBOL non-conformances,
which shaped revision 2's portable model. GnuCOBOL's `-std` (no 2023) selects "warnings/features for a specific
dialect", not a conformance mode.

- **The tree.** 6,323 tests: 6,104 in `cases/` (1,186 in `_unassigned/`), the rest in `wiseowl/cases/`; the suites
  carry the 37 cleared portable, one flagged. 3,370 `wiseowl/diagnostics/<id>.json` files hold `{refuse-at,
  expect}` (2,349 a bare code, 430 code and text, 591 text only); every pinned code is an error, matched as a substring
  anywhere, never at a line.
- **The inventory.** 4,214 rows: GR 1,852, SR 1,375, FMT 322, RV 224, DOC 222, AR 219; one GAP. Bands start at 85
  (2,017), 2002 (1,573), 2014 (295) and 2023 (329), all running through 2023.
- **What the suites witness.** 13 of 1,808 observable rows at 85, 30 of 3,199 at 2002, 34 of 3,462 at 2014, 34 of
  3,758 at 2023 (FMT 0 of 322). Yet 7,447 of the 12,366 row-and-edition pairs outside documented non-support have an
  unverified `cases/` test, and 3,396 of those 3,800 rows are named by some header: from 2002 most work is converting
  tests, at 85 mostly writing them. Of 60 random accepted tests citing only rules in band at 85, 5 compile at `--std
  85`; the rest use post-85 constructs in passing (`review-feasibility/probe85/results.json`).
- **Editions.** Rows recording no evidence for their start: 169 of 295 at 2014, 183 of 329 at 2023, 1,183 of 1,573 at
  2002, 1,535 of 2,017 at 85 (`review-feasibility/edges_85_2002.py`). PB4608 checked 94 2014 edges against the FCD: 87
  confirmed, 3 moved, 4 undecidable. Nothing checks a band against the construct registry: the 7 ENTRY-CONVENTION rows
  are dated 2014 though the 2002 working document holds the clause.
- **NIST CCVS85.** 459 programs (364 with goldens); 6,929 paragraph-name test units.
  31 flagging programs annotate 217 expected flags, none asserted: 40 OBSOLETE in 14 programs and 177 NON-CONFORMING
  STANDARD in 17, which hold no paragraph unit (`rev3c/fips_units.py`). The modules 2002 and 2014 removed hold 605 units
  (Segmentation 340 in 13 programs, Debug 214 in 15, Communication 51 in 9), whose 37 programs set ANSI-REFERENCE
  nowhere (`rev3b/sgdb_refs.py`).
- **Cost history.** The golden lanes of 2026-09-02 to 2026-10-08 closed 606 rows at 0.75 to 1.1 rows per program and
  about $0.81 a row, each a one-edition WiseOwl golden; refuters overturned 67 of 158 first-round decisions. Of fuzz
  night 10's 1,028 AGREE programs, 99 passed the portability lint.

## 3. The case file and its tests

**A case file holds the tests of one clause** (decision 17). A test, also called a case, is one program or the few
source units it needs (callees, functions, classes), witnessing one rule or a few tightly related ones, with its own
placement and verdict. At 022838e99 each of 6,333 tests (6,114 in `cases/`) is a file; with 2,959 outputs, 3,376
WiseOwl diagnosis files, 7 options files and 83 library and data texts, the suite holds 12,758 tracked files
(`rev3e/group_files2.py`). CCVS85's 389 programs with paragraph-name units hold a median of 9 (`check17/ccvs_units.py`,
counting 6,958 units case-insensitively against §2's 6,929). A test keeps its own program, not a paragraph of a shared
one, so one test's SPECIAL-NAMES (453 of today's tests write one) cannot change another's meaning.

**The form.** A comment block opens the file and each test, which runs to the next test's block. A block holds one
field per line, `*> <field>: <value>` (a scalar, `[a, b]` or `{k: v}`); in fixed form `*>` sits in columns 7-8, which
every edition reads as commentary, in free form in column 1; a long list continues on lines indented four spaces.

```cobol
      *>--- conformance-file
      *> source-format: fixed
      *>---
      *>--- test
      *> id: compute-multi-receiver-size-error
      *> rules: [GR-14.9.8.4-2]
      *> accept: {from: 85}
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CMPMR.
      *> ... the test, ending with EXIT PROGRAM at 85 ...
       END PROGRAM CMPMR.
      *>--- test
      *> id: compute-rounded-receiver
```

**Grouping** (17a). A file's tests lie in one clause folder (§4), ordered by first rule and cut at rule boundaries into
files of about 12, at most 25 (a refusal). Today's 4,928 assigned tests make 617 files, median 10: 86 of one, 346 of
eight or more holding 4,134 tests (`rev3d/group_files.py`). A file is named for what it holds
(`compute-size-error.cob`), uniquely across both trees. Sibling clauses are not merged: a folder is a clause.

**One run per file** (17c, 17e). For each edition and profile the generator writes the file's grouped program (§4): a
generated driver, then every test that applies there, meaning accepted at that edition, cleared for a portable suite,
not withheld on an `unproven` edge (§3.5), its `requires` claimed and none of its `words` reserved by the profile
(§5), and not run alone. For each test the driver DISPLAYs `*TEST <id>` and CALLs its first program definition that is
not a prototype (132 accepted assigned tests open with a prototype, function, class or interface,
`check17/first_unit.py`); it ends by DISPLAYing `*DONE`. "The compilation units in a compilation group may be all or
part of a run unit or may be unrelated compilation units" (§2023:10.2); 50 NIST-85 programs, 41 green in WiseOwl's NIST
lane, hold a caller and its callees (`check17/nist_multi.py`; `inferred`). Each adapter states how the driver becomes
the entry (§5): "The implementor shall document how a main program is identified" (§2023:15.65.4 GR 5, MODULE-NAME; A.1
item 136; the standard says nowhere else). A test's solo program, the driver and that test alone, defines what the test
means; a grouped PASS stands for the solo verdict (§5). Bands stay per test: 216 of the 378 folders holding accepted
tests mix `accept` sets, and splitting by band would make 1,215 files.

**A test returns to the driver** and writes no STOP statement unless marked `main`. From 2002 it ends with GOBACK or at
the end of its procedure division: "In these cases, an implicit GOBACK statement without any optional phrases is
executed" (§2023:14.6.3; `t2002.txt:20800-20803` likewise). At 85 it ends with EXIT PROGRAM as the last statement of its
sentence, which meets the 85 rule that the 2002 F.2 item 50 relaxes: "EXIT PROGRAM is allowed to appear as other than
the last statement in a consecutive sequence of imperative statements" (`t2002.txt:44016-44017`; `inferred`). EXIT
PROGRAM is archaic in 2023 (F.1 item 1), and an archaic test's flagged program may hold no other archaic element
(§3.2), so its control is accepted from 2002 and ends with GOBACK.

**What runs alone** (17h). A test marked `main` runs as the main program, with no driver: its subject is the run unit's
end or the main program, where EXIT PROGRAM "is treated as if it were a CONTINUE statement" (§2023:14.9.14.4 GR 2). The
generator derives the rest. A test whose text reaches past its units runs solo: a REPLACE statement "remains active
until it is placed in the inactive state, it is canceled, or the end of the compilation group is reached" (§2023:7.2.4.4 GR
5), and TURN enables checking "for the procedure division statements and procedure division headers that follow in the
compilation group" (§2023:7.3.25.4 GR 6). So does a test touching run-unit state, one that sets the runtime locale, which "remains
in effect for the run unit until another runtime locale is established" (§2023:8.2.1); references RANDOM, whose
"subsequent references without specifying argument-1 return the next number in the current sequence" (§2023:15.75.3 GR 5);
or reads the last exception status, which "exists for the entire run unit" (§2023:14.6.13.1.1). Of 2,329 accepted assigned
tests, 382 hold a directive or REPLACE statement (283 a TURN), 21 sit in the EXIT, GOBACK and STOP clauses, and 26
more touch that state: 426 (`check17/solo_union.py`, `check17/run_unit_state.py`). A refused test, a flagged program,
a control's leg (§3.2), a `build` test, a test that starts `servers` and a test with its own WiseOwl compile options (§4) run solo too.

**Labelled output** (17c). A test's output is its section, the lines after its header up to the next header or `*DONE`.
`<file>.out` holds the expected sections, headers included, so where every test applies it equals the grouped program's
stdout. A narrower file, `<file>.<edition>.out` or `<file>.<item>=<value>.out` (§3.3) or both, holds only the sections
that differ; a test's expected section comes from the narrowest file holding it. Every accepted test DISPLAYs a line,
so a test that never ran cannot pass by printing nothing; no test writes a literal beginning `*TEST `.

**Independence.** In one file the generator refuses two accepted tests defining one outermost source-unit name or the
driver's (a contained program's name need be unique only within its outermost program, §2023:11.10.3 SR 3), naming one
external file, or declaring one EXTERNAL name: the run unit would share them. Today's accepted assigned tests reuse 5
outermost names in 10 tests and 2 external file names in 4 (`rev3e/outer_names.py`, `rev3e/assign_names_accepted.py`;
19 file names in 41 counting refused tests, which run solo, `check17/assign_names.py`); S5b renames them.

### 3.1 Fields

The file block holds one field, `source-format` (`fixed` or `free`, required; fixed in any file with a test accepted at
85). A test's block holds the rest (17b):

| Field | Meaning |
|---|---|
| `id` | Unique across both trees; letters, digits, `-`, `_`; stable from the first release. Naming neither clause nor file, so a test can move |
| `rules` | The inventory rows witnessed. A rule 2023 lacks has a qualified id (`GR-A.2.5.3-4@2002`); a facility no held text describes is `construct:<id>` (§3.5). `[]` only in `_unassigned/`. Format points are read from the parse, never written here (§3.4) |
| `accept` | Editions at which the test compiles and runs with its section equal to its expected section. `{from: <edition>}` means that edition and every later one, so a new edition inherits the test |
| `constructs` | The `constructs/` rows the test uses incidentally whose window is not every edition; each accepted edition lies inside each window. The constructs of its rules come from their facilities (§3.5) |
| `requires` | Optional or processor-dependent elements needed, by stable `constructs/` module id (`module-report-writer`), never by A.4 number, which differs between editions. The module row records the element's status per edition; a profile not claiming it where it is optional or processor-dependent gets NOT APPLICABLE |
| `implementor-defined` | A.1 items whose enumerated value the output depends on (§3.3) |
| `files` | Copybooks, data files and other units the test needs, beside the case file |
| `main` | `yes`: the test runs as the run unit's main program, alone; the only test that may write a STOP statement |
| `build` | WiseOwl tests only (PB2101): ordered compile steps of a multi-unit test, `"<file>"` or `"<file> > <output>"`, `"{self}"` for the test; it runs solo. Refused in `cases/`: how separate units find each other is implementor-defined |
| `servers` | WiseOwl tests only (PB4745, DESIGN-message-control.md §0 Q9): a map from a message server name to a declared file, `{BALANCE: balsrv.cbl}`. The in-process runner compiles each file as a `build` unit is (its options `wiseowl/options/<id>.<file>.json`) and starts its main program, before the test, as a concurrent run unit serving the name in the test's own MCS directory; it ends them with the test. A name is compared as a server name is (trailing spaces removed, without regard to case); a file's assembly is neither the test's nor a build step's nor another server's. It runs solo. Refused in `cases/`: how a second run unit is started and found is implementor-defined |
| `portable` | `unverified` on every migrated test until its clause pass clears it; the suites carry only cleared tests |

A flagged test has no block of its own: it is one line of its file's `<file>.flagged.jsonl` (§3.2).

### 3.2 Tests a compiler must diagnose

The standard never requires a compiler to refuse a program. ISO §2023:4.2.2 requires "a warning mechanism that optionally
may be invoked by the user at compile time to indicate violations of the general formats and the explicit syntax
rules", and of general rules that "could have been classified as syntax rules" it says "An implementation may, but is
not required to, flag violations of such rules". Archaic and obsolete elements (§2023:4.2.12, §2023:4.2.13), nonstandard
extensions (§2023:4.2.10) and unsupported processor-dependent elements (§2023:4.2.6) follow the same pattern.

**The portable judgment is a site, never the message** (decision 2). The standard does not specify "The form or content
of error, flagging, or warning messages" (§1), so no portable test tells a warning from an error: a flagged test
PASSes when the compiler reports a diagnostic at its site, the suite's discriminator, not an ISO requirement. A
diagnostic elsewhere is UNLOCATED, one that also fires on the control NOT DISCRIMINATING, and neither is a FAIL
(decision 9). A test's `kind` names the mechanism the standard requires, and so its compiles and runs (legs):

| `kind` | Mechanism | Rows it may witness | Legs where flagged |
|---|---|---|---|
| `syntax` | §2023:4.2.2 | `FMT-` rows; `SR-` rows `identified-as` syntax rules | flag-mode compile, a diagnostic at the site; not accepted there |
| `nonstandard` | §2023:4.2.10: "This warning mechanism shall flag only extensions that are syntactically distinguishable" | a construct outside the edition's window, named in `constructs` | as `syntax` |
| `obsolete` | §2023:4.2.13, F.2 | the element's rows and construct | accepted (compile, run, section) AND flag-mode with a diagnostic at the site |
| `archaic` | §2023:4.2.12, F.1 | as `obsolete` | as `obsolete` |
| `processor-dependent` | §2023:4.2.6 | an element named in `requires` | flagged where the profile does not claim it; accepted where it does |
| `conformance` | §2023:4.2.2's suboption for 14.8.2, 14.8.3, 9.3.8.2.3 | those subclauses' rows, general rules included | the adapter's `conformance` command |
| `directive` | FLAG-02 (7.3.14.4), FLAG-14 (7.3.15.4); at 2002 and 2014 FLAG-85, FLAG-NATIVE-ARITHMETIC; POP (7.3.20.4 GR 2) | the directive's rows | the FLAG directives: the ordinary compile, accepted, with a diagnostic at the site, since "the warning mechanism is enabled for that option" (FLAG-NATIVE-ARITHMETIC: "flagging is enabled", `t2002.txt:3667`). POP: either compile; its warning that "the POP directive was unsuccessful" enables nothing |

**The mechanisms differ by edition**, held as data (S6). The FCD's 3.2 and the 2002 working document's 3.1 subclauses
match 2023's (`t2014.txt:1125-1259`, `t2002.txt:1013-1157`; the 2002 `conformance` suboption names 14.7 and 9.3.7.1.2;
archaic elements in F.1 and G.1). The directives are FLAG-02 and FLAG-14 at 2023, FLAG-02 and the obsolete FLAG-85 and
FLAG-NATIVE-ARITHMETIC at 2014, FLAG-85 and FLAG-NATIVE-ARITHMETIC at 2002. At 85 the generator refuses
`conformance`, `archaic` and `directive`; every other 85 mechanism is `inferred` (decision 10), the obsolete one from
"the obsolete flagging feature of the previous COBOL standard" (`t2002.txt:43078`).

**Accepted and flagged.** A `syntax` or `nonstandard` test is never accepted and flagged at one edition. An `obsolete`,
`archaic` or `directive` test is accepted wherever it is flagged: "An implementation shall support obsolete language
elements of the facilities for which support is claimed" (§2023:4.2.13; §2023:4.2.12 likewise for archaic ones), and "A
conforming implementation shall support obsolete language elements except for elements that are also optional or
processor-dependent" (F.2; such a test names the element in `requires`). Because "The interaction between obsolete
language elements and other language elements is undefined unless otherwise specified" (F.2), an accepted obsolete or
archaic test's output may depend only on the element's own behaviour (§6.1's refuter). A flag's editions come from the
construct's `obsoleteIn` and `archaicIn`. The 85 obsolete elements are inferred to be the 18 features the 2002 F.1
item 1 calls "classified as obsolete in the previous COBOL standard" (`t2002.txt:43047-43070`), corroborated by NIST's
40 OBSOLETE annotations.

**The site.** A mark is a label on a line: in free form the floating comment `*> <<a` ends the line; in fixed form the
sequence number area holds `<<a`, because 85 has no floating comment and margin R "is a fixed position defined by the
implementor" (§2023:6.3.1), while the sequence number area "may consist of any character in the computer's coded character
set" (§2023:6.3.2; held for 2002 and 2014, `t2002.txt:2262`, `t2014.txt:2377`; inferred for 85). A label has at most four
characters, so `<<` and it fit the six columns, and is unique in its case file. The lines carrying one label form one
site: the whole statement or entry, both places for a rule that relates two, the entry a required clause is missing
from. Every flagged test has one site holding one violation. An obsolete or archaic flagged program holds its element
once and no other of that kind, because §2023:4.2.13 asks for a mechanism "to indicate use of an obsolete element in a
compilation group" (§2023:4.2.12: "to indicate use of an archaic language element in a compilation group"), and a compiler
reporting such use once per group would give control and test the same diagnostic. So the generator refuses such a
line at an edition where its control holds another element of that kind; a diagnostic per use is a WiseOwl pin. A site
in a declared library text is found at that file's line or at the COPY statement's.

**Which rows may be `syntax` flags.** FMT rows, and SR rows `identified-as` syntax rules by the heading they sit under:
1,310 SR rows sit under "Syntax rules" and 65 under other headings (`review-fidelity/sr_headings.py`). For those 65, as
for general rules, "it is left to the implementor's discretion to determine what is syntactically distinguishable"
(§2023:4.2.2), so WiseOwl's diagnosis of them is pinned only in the WiseOwl layer.

**A flagged test is derived from its control** (17f). The control is an accepted test of the case file written without
the violation (for an obsolete or archaic element, with its replacement: GOBACK for EXIT PROGRAM), its site's lines
marked. The test whose text holds a site is its control, so a flagged line names none, and the generator refuses a
label in no test or in two. `<file>.flagged.jsonl` holds one line per flagged test:
`{"id": "pic-x-before-s", "rules": ["SR-13.18.40.3-2"], "kind": "syntax", "at": {"from": 85}, "site": "a", "with": ["       01 V PIC XS9."]}`.
The flagged program is the control's solo program with the site's lines replaced by `with`, so no other test of the
file can stop it compiling or add a diagnostic to it. The twin is exact by construction, one control serves any number
of violations, and PICTURE's 413 forbidden Table 10 cells are 413 lines of one file. An obsolete or archaic line is
also accepted, with its control's section unless it names its own (`"out"`). The control is accepted at every edition
its lines flag.

**The verdict of a flagged test**, judged in order:
1. the control's solo program did not compile: NOT APPLICABLE (a compiler lacking the feature gets no vacuous PASS);
2. the control's solo program, under the same mechanism (flag-mode, the `conformance` command, or the directive's option
   off or the POP successful), reports a diagnostic at its site: NOT DISCRIMINATING. Not a FAIL, because §2023:4.2.2, unlike §2023:4.2.10, has no "shall flag only",
   and the harness cannot tell an informational message from a flag;
3. the test's legs report a diagnostic at a line of its site: PASS;
4. its flag compile reports a diagnostic the control's does not, elsewhere or unlocated: UNLOCATED;
5. otherwise: FAIL.

**Run-time violations** are judged only from 2002: common exception processing (the 2002 working document's F.2 item
28) "is enabled by the TURN compiler directive and changes to the USE statement" (`t2002.txt:43922-43924`). The test
turns checking on, traps the condition and prints its `EC-` name, an ordinary `accept` test that runs solo (§3). The
generator refuses a run-time expectation at 85; exit status and abnormal termination are not portable expectations.

**The WiseOwl layer** (17j). `wiseowl/diagnostics/<file>.jsonl` holds one line per test of its case file, sorted by id:
`{"test": "<id>", "lines": [<n>, ...], "at": [{"editions": [2002] | {"from": 2002}, "code": "COBOLnnnn", "severity": "error" | "warning"}]}`.
`lines` counts from the test's block, so an edit to one test never moves another's pins, and is needed only on a test
with no mark (a refusal not yet made a flagged test) or one pinning each use of an obsolete element; otherwise the
site's first line is the line. The in-process runner requires the code, with that severity, at each line and edition,
and where the test is also accepted, compares output. Message text lives once, in `DiagnosticCatalog`.

### 3.3 Expected output that is portable

The device form of DISPLAYed data is the implementor's: §2023:14.9.11.4 GR 1 "Any conversion of data required between
literal-1 or the data item referenced by identifier-1 and the device is defined by the implementor" (A.1 item 56). Two
conforming compilers print one signed numeric item as `01E` and `+01.5`.

- **The portability lint.** A portable test DISPLAYs only alphanumeric literals, alphanumeric items and numeric-edited
  items; never an un-edited numeric item, a function result, an arithmetic expression or a national operand (WiseOwl
  prints `N"ABC"` as `ABC`, GnuCOBOL 3.2 as UTF-16BE, both conforming). A number is MOVEd to a numeric-edited item
  first; a national test compares inside the program and DISPLAYs a literal naming the outcome. DISPLAY shows "the
  alphanumeric representation of that figurative constant" (GR 3), but only SPACE, ZERO, QUOTE and ALL literals of
  allowed characters are portable, since "The character that has the highest ordinal position in the program collating
  sequence is associated with the figurative constant HIGH-VALUE" (§2023:12.3.7.4 GR 8; GR 9 likewise for LOW-VALUE). A
  reference-modified display numeric item is alphanumeric (§2023:8.4.3.3.4 GR 6 c) and allowed unless signed without SIGN
  SEPARATE: "Neither the representation nor the position of that operational sign is specified" (§2023:13.18.52.4 GR 4).
- **The lint judges the storage an operand shows.** A group is allowed only when every item it holds is alphanumeric,
  numeric-edited, or unsigned or separately signed display numeric, and none is SYNCHRONIZED ("The effect of this clause
  is defined by the implementor", §2023:13.18.55.4 GR 9). An item whose storage another entry describes (REDEFINES, the
  records of one FD, SAME RECORD AREA) is judged by that entry too. The lint reads declarations, not data flow, and
  refuses `UPON`, fixed-form text past column 72 and any name it cannot classify. As first measured, 1,084 of 2,869
  accepted tests pass it.
- **Alphanumeric output is portable only by a profile statement.** GR 1 covers alphanumeric operands too, so a profile
  states its alphanumeric DISPLAY conversion as the encoding the adapter decodes stdout with (`identity` for WiseOwl
  and GnuCOBOL); a profile stating none gets UNDOCUMENTED on every accepted test.
- **Enumerated A.1 values.** Item 148: §2023:13.18.43.4 GR 17 "It is implementor defined whether format 3 of the RECORD
  clause produces fixed-length records or variable-length records". Such a test names the item in `implementor-defined`
  and has a section per value, in `<file>.148=fixed.out`. Every DOC row carries a decided `enumerates` field (the values
  and the words that name them, or `[]`); the generator reads the values from it and refuses an item that enumerates
  nothing. Such an item's tests, the sign convention's among them (§2023:13.18.52.4 GR 4, GR 5 b), are WiseOwl-only.
- **A missing value is reported, not failed** (decision 6). §2023:4.2.5 requires "the implementor shall specify, at a
  minimum, the implementor-defined language elements that are identified as required", so a profile lacking a value has
  a documentation gap: UNDOCUMENTED, counted apart. WiseOwl's values are `docs/CONFORMANCE.md` §7's (§5).

### 3.4 What a test covers: rule rows and coverage points

**The rule row stays the unit of a verdict**, and the inventory gains no rows for alternatives; a numbered list inside a
rule is addressed by citation path (owner, 2026-08-08, PB55).

**A row's coverage points** are its format points (accept and must-diagnose), its table cells and its lettered
sub-items. A row is suite-witnessed at an edition only when every point applicable there is; otherwise it is PARTIAL,
and points witnessed of points applicable is the second axis (decision 7).

**Format points come from the notation of §2023:5.2:**
- a bracket, whose portion "may be explicitly specified or that portion of the general format may be omitted"
  (§2023:5.2.6.2): each alternative written, and the bracket omitted;
- a brace (§2023:5.2.6.3): each alternative written; and "If one of the alternatives contains only optional words, that
  alternative is the default and is selected unless another alternative is explicitly specified", so a default is both
  written and omitted;
- choice indicators (§2023:5.2.6.4): each alternative alone, and two or more out of printed order ("The alternatives may be
  specified in any order");
- an ellipsis, where "the ellipsis represents the position at which the user elects repetition of a portion of a format"
  (§2023:5.2.7): the portion written at least twice;
- an optional word, which "may be written to add clarity when the clause or phrase in which they are defined is written
  in the source unit" (§2023:5.2.3): written, and omitted;
- every format: "When more than one arrangement exists for a specific language construct, the general format is
  separated into multiple formats that are numbered and named" (§2023:5.2.1), a single format included, since 95 of the 322
  FMT sections hold no delimiter (`derivation/format-choice-points.json`).

The 322 FMT rows' 499 printed diagrams hold 765 brackets, 607 braces and 66 pairs of choice indicators (25 delimiters
unpaired). Untracked, alternatives go unwitnessed: a lexical scan of 6,324 programs finds MOVE CORR in 2 and PERFORM
... TEST BEFORE in 1.

**Must-diagnose format points.** Elements "shall be written in the compilation group in the sequence given in the
general format" (§2023:5.2.1), keywords are required (§2023:5.2.2), one alternative of a brace "shall be explicitly specified or is
implicitly selected" (§2023:5.2.6.3), and of choice indicators §2023:5.2.6.4 says "any single alternative shall be specified
only once". So each FMT row owes, once each: per brace with no default, no alternative written; per choice-indicator
set, one alternative written twice; per required keyword, the keyword left out. Each is one line of a control's
`.flagged.jsonl`; S14 counts them.

**Each outcome once, every edition, no combinations** (decision 1). A combination the standard constrains is a rule row
of its own (READ's AT END with NOT AT END, INSPECT's BEFORE with AFTER). Modeled from the rendered pages, MOVE, PERFORM,
READ, INSPECT, OCCURS and PICTURE need 147 statements to show each of their 457 outcomes once; scaled by their ratio of
statements to delimiters (147 to 116), all 322 formats need about 1,950, and since a test covers many points of its
rows, they fill a few hundred tests.

**Points inside a rule.** A Yes or 'x' table cell is an accept point; a No cell under a syntax rule is a must-diagnose
point, a `syntax` flagged line. "The rules of the PICTURE clause specified in 13.18.40.6, Precedence rules, are syntax
rules" (§2023:5.3.2), and in Table 10 "An 'x' at an intersection indicates that the symbol(s) at the top of the column may
precede (but not necessarily immediately) in character-string-1 the symbol(s) at the left of the row" (§2023:13.18.40.6):
576 cells, 163 'x', so SR-13.18.40.3-2 owes 163 accept and 413 must-diagnose points. MOVE's Table 16 (§2023:14.9.25.3 SR
10) holds 31 Yes and 29 No cells in one row. Neither row is witnessed until all its cells are.

**The point catalog is generated from the printed page.** `scripts/spec/data/format-points.json` holds every FMT row's
points (id, kind, words, edition window), written from the licensed PDF by a script built on
`scripts/spec/figure_options.py` and committed, so CI checks its shape and digest without the PDF. Never from the
markdown: the
transcription of PERFORM Format 3 put imperative-statement-1 inside the repeated WHEN brace, where the printed page
(folio 682) prints it outside (PB4676). The 25 unpaired delimiters are adjudicated on rendered pages first.
- **Ids are words**: the row, the format number and the point's words where unique (`FMT-14.9.28/f<n>/TEST/BEFORE`);
  only an operand-kind alternative, which prints no word, is keyed by position.
- **Windows are derived**: a point printing a word first reserved after V (`reserved-words.json`) does not exist at V
  unless a governing `constructs/` row says otherwise. 35 of the 176 FMT rows banded from 85 print words first
  reserved later (`review-feasibility/point_windows.py`).
- **Each edition's figures**: CALL ... ON OVERFLOW is archaic in the 2002 text (G.1 item 4) and 2023 §2023:14.9.4 prints no
  OVERFLOW, so the catalog also holds the points of the 2002 and FCD figures, matched to 2023 points by their words and
  carrying their text's evidence class; a 2002-figure point also belongs to 85, `inferred`. An 85 alternative 2002
  removed is reached only through NIST's units (§6.3).

**Which points a test exercises is read, never declared** (decision 8). A WiseOwl-side step reads WiseOwl's parse of
each test at each accepted edition and writes `wiseowl/points/<file>.json`, `{"<test id>": {"<edition>": [<point id>, ...]}}`. A keyword
point matches the token stream of the statement's parse node, hidden channel included, so an optional word is seen
either way; the one lexer rule that drops a keyword, `PIC_IS : 'IS' -> skip` (`CobolLexer.g4:1329`), moves to the
hidden channel (S15). Only an operand-kind point needs a hand-kept map to its grammar element, held by a drift test.
Table cells and lettered sub-items are credited there too; how, since no parse node shows one, S14 settles. Only the
coverage measure uses WiseOwl's parser; a grammar defect that credits a wrong point is bounded by the map's drift test,
the grammar-versus-printed-figure audit (`spec-grammar-conformance`), and a second compiler's run of the test.

### 3.5 Placing a test at each edition

A test's editions are checked against the inventory's bands (does the rule exist at V?) and the windows of constructs,
functions and reserved words (does the facility exist at V?). An edge is the edition where a band or window starts or
ends.

**Every edge records evidence that can prove it.** **Bands are a view of the rule lineage** (owner 2026-11-08, kb/Work PB4991, PB4996; DESIGN-spec-conformance-review.md §10.2): one rule list per edition and a change record between each adjacent pair, so a band is a KEPT chain's span and an 85 edge's evidence is the 1985 text (`iso-1985`, `cite.py --check`ed), as decision 10 states. A newest-edition rule is at 85 exactly when a KEPT record of 1985 carries into it (a bridged one through 2002 and 2014 as well; a CHANGED record proves no band); The band rule has one definition, `lineage.chain_bands`, and S5's band reader (PB4683) takes the positions below 2023 from it. Until the stored `85` token agrees with it the token stays stored, since it is the only 85 position the rules no KEPT chain reaches yet have, and the view measures the distance (`lineage.band_drift`, `inventory_view.py --band-drift`, DESIGN-spec-conformance-review.md §10.2); the lineage's fleet adjudication (PB4995) and the refutation of the upheld records (PB5008) close it, and the token then leaves the bands. The facilities' windows are held to the same lineage by the EDITION GATES audit (`edition_gates.py`): a window's edge rests on a rule its citation names that changes between the editions (NEW, CHANGED or REMOVED), and a citation proves it only when every rule it names changes. The classes below remain for the editions whose text is not yet held (2002 and 2014, until PB4813). Held, untracked, in
`E:/COBOL-coord/specs-drafts/`: the 2002 working document of 2001-12-04 (`t2002.txt`) and the 2014 FCD (`t2014.txt`),
with their PDFs. Each edge carries `evidence: {class, source, locator}`, and the validator refuses a locator offered for
an edge its source does not speak about:

| Class (strongest first) | What it is | Edges it can prove |
|---|---|---|
| `iso-2023` | the 2023 text, `cite.py --check`ed | E.1-E.3 and FLAG-14's options: 2014\|2023. FLAG-02's options: 2002\|2014 ("incompatible between ISO 1989:2002 and ISO/IEC 1989:2014", 7.3.14.1). No 85\|2002 edge |
| `draft-stated` | a held draft's change list or flag directive names the change | the FCD's E.2 (16 items): 2002\|2014. The 2002 Annex F and FLAG-85's options: the 2002 side of 85\|2002 |
| `two-texts` | the rule's sentence in two held texts on one side of the edge | the 2002 text and the FCD: the 2014 side of 2014\|2023; the FCD and 2023 where 2002 lacks it: the 2014 side of 2002\|2014 |
| `one-text` | one held draft alone | the edge on that draft's side |
| `inferred` | a change list's silence; a committee draft; the compiler's record (`constructs/`, `IntrinsicCatalog`, `reserved-words.json`, a decided sibling row); NIST-85; a vendor document | any edge, marked. Every 85 fact is `inferred`, its source naming what it rests on |
| `unproven` | a check against the held text of that edition found the edge undecidable | none: the placement is withheld from that edition's portable suite (decision 14) |

A locator into an untracked text is committed as digest and line; `cite.py --text 2002|2014`, a mode S11 adds, checks
the quoted words where the texts are held. For example, 2023 E.2 item 22 ("The prior standard had a note that
conflicted with a rule") proves only READ PREVIOUS's 2014|2023 change; its 2014 side is `two-texts`, from the rule
printed in both drafts (`t2002.txt:26766-26767`, `t2014.txt:32274-32275`).

**A placement carries the weakest evidence it rests on** (decisions 5, 10, 14). The generator derives, per test and
edition, the weakest class among the edges the placement relies on (its rules' bands and its facilities' windows) and
writes it into the manifest, the results and the report, which lists every placement weaker than `iso-2023` apart, at
85 grouped by source. A placement on an `unproven` edge is withheld from that edition's portable suite and listed as
unplaced there with its evidence; WiseOwl's own manifest still runs it. The `unproven` edges today are PB4608's four
2014 ones, on which 8 S0b rows rest. A later check that proves or moves one re-places its tests with no hand edit.

**The evidence lives on the facility's window.** A row's `facility` is `construct:<id>` (`constructs/`),
`function:<NAME>` (`intrinsic-introductions.json`) or `word:<WORD>` (`reserved-words.json`), resolved by one script,
`scripts/spec/facility.py`. No window is copied, and each window's edges carry their evidence once, so a move like
PB4609's re-dates every row it bounds (hand lists missed the 7 ENTRY-CONVENTION rows).
The drift test is `band ⊆ window`, not equality: 80 clause-15 rows start later than their function
(`review-feasibility/fn_band_vs_window.py`). A row that starts later than its facility, or has none, records its own
edge, the only edge that lives on a row. 1,873 rows need a facility, 381 of them clause-15 rows
(`review-feasibility/facility_size.py`).

**Default evidence is derived from the change items.** Each of the 2002 working document's 189 Annex F items and the
FCD's E.2 items is mapped once, with a refuter, to the facilities and rows it touches. A script then classes the rows:
an 85 start no 2002 item touches is `inferred` from the text's silence; an 85 start an F.2 item names as new is refused
for adjudication; one an F.1 item names is an 85|2002 variant; a 2002 start whose facility an F.2 item names is
`draft-stated`, one no item names is refused for adjudication; the 2014 starts likewise against E.2.

**A rule whose content changed says so.** A band models existence only. A row whose content changed (SEARCH's
identifier-1 subscripted by an enclosing table from 2023, PB4609; READ PREVIOUS after OPEN) carries `variants`: the
editions on each side, each with evidence. A test of it accepted on both sides needs a per-edition expected section
or a flag on one side. The seeds are every change list the held texts print:
- 85|2002: the 2002 F.1, 40 "Substantive changes potentially affecting existing programs" (`t2002.txt:43045-43820`);
  the F.2 items that change a rule (item 18, COBOL words from 30 to 31 characters); FLAG-85's 10 options
  (`t2002.txt:3549`);
- 2002|2014: the FCD's E.2 (16 items, `t2014.txt:48699`) and FLAG-02's 5 options;
- 2014|2023: 2023 E.2's 37 VCR rows and FLAG-14's 12 options;
- `docs/VERSION_CHANGE_REFERENCE.md` Table 7's pre-2023 rows, re-cited.

An 85 test placed by a 2023-worded row whose 85 content differs would encode 2023 semantics, so S12 blocks the 85
clause passes.

**The rules of facilities 2023 removed become rows** (§10), catalogued from a held text that prints them, with digest
and line locators:
- **Ids that cannot collide**: `<kind>-<clause>-<n>@<edition>`, numbered in the latest held text that prints the rule,
  filed in `cases/<clause>@<edition>-<Name>/`, so CD 1.3's JSON PARSE (`GR-14.9.25.4-1@2027`) is never 2023's MOVE. An
  `@2023` id or an unresolved locator is refused.
- **The Communication facility**, from the 2002 working document's Annex A (`t2002.txt:33530-34708`; 7 general formats,
  32 syntax rules, 78 general rules, `rev3b/annexA_rules.py`), band 85 to 2002, removed by the FCD's E.2 item 1; at
  2002 "it is an obsolete element" and optional (`t2002.txt:33463`). WiseOwl implements it at 85 and 2002 (decision
  12).
- **Debugging lines, WITH DEBUGGING MODE and PADDING CHARACTER**, from the 2002 text, band 85 to 2002 (obsolete in 2002:
  t2002.txt:2374, :11177, :12874; removed by the FCD's E.2 item 1).
- **FLAG-85 and FLAG-NATIVE-ARITHMETIC**, from the FCD (7.3.12, 7.3.13; `@2014` ids), band 2002 to 2014, removed by 2023
  E.2 item 21 as "classified as obsolete in the previous COBOL standard"; the other whole rules 2023 removed likewise.
  An alternative 2023 removed from a format it keeps is a point of the older figure (§3.4).
- **A facility no held text describes** (Segmentation, USE FOR DEBUGGING, and the other elements of the 2002 F.1 item 1,
  such as ALTER and ENTER) has no rule to catalogue: its obligations are NIST-85's units (§6.3), and its test names
  `construct:<id>`, allowed only for a construct marked `ruleText: none`.

**The edition schema changes once, first** (S5): ids, rule ids and folders keep their 2023 numbers, a rule 2023 lacks
taking a qualified id; a row adds a per-edition clause locator where its number differs; a band is a start and an
optional end, replacing S0b's `run-through-last`; `accept`, a flagged line's `at` and the WiseOwl layer take `{from:
<edition>}`. S5 respells the 1,231 `accept` lists that name 2023; S7 respells the 3,370 diagnoses once, after S5b has
moved them unchanged.

**2027 enters from its text, not from the gate's reds**, since a 2027 change WiseOwl has not implemented leaves the old
section passing. CD 1.3 renumbers 167 of 588 clauses (`review-feasibility/cd13_new.py`). Once the text is transcribed,
each 2023 row is matched to its 2027 rule: a changed `rule-digest` is a `variants` candidate, a new rule an `@2027`
row, an unmatched one a band ending at 2023 (S20, PB4397).

## 4. The tree, the generator and where it lives

```
conformance-suite/
  LICENSE, README.md            the suite's license (PB3159); how to run it, ISO's acknowledgment (§7)
  cases/<clause>/<file>.cob     case files, by ISO clause (cases/14.9.8-COMPUTE/), beside their tests' declared files (§3)
  cases/<clause>/<file>[.<ed>][.<item>=<v>].out  expected sections; a narrower file holds only those that differ
  cases/<clause>/<file>.flagged.jsonl         flagged tests derived from the file's controls (§3.2)
  cases/<clause>@<edition>-<Name>/            case files of rules 2023 does not hold (§3.5)
  cases/_unassigned/            one-test case files not yet given a rule; in no suite
  intake/<source>/              supplementary programs with provenance only (§6.2); no harness reader
  suites/                       build output, git-ignored: per edition the solo programs and manifest, and per profile
                                <edition>/<compiler>/ its grouped programs; suites/wiseowl/; completeness.json
  profiles/<compiler>.json      per edition: claims with evidence, A.1 values, DISPLAY conversion, extra reserved words
  adapters/<compiler>.json      compile, run, flag and conformance commands, the driver as entry, edition map (§5)
  baselines/<compiler>.json     the compiler's recorded verdicts, per test (§5)
  harness/                      the generator, its sibling modules, the runner
  wiseowl/diagnostics/<file>.jsonl  WiseOwl's diagnoses, one line per test (§3.2)
  wiseowl/points/<file>.json    points WiseOwl's parse credits, per test and accepted edition (§3.4); generated
  wiseowl/options/<file>.json   WiseOwl compile options keyed by test id (and build step); such a test runs solo
  wiseowl/cases/<clause>/       WiseOwl-only case files: documented non-support, no normative rule, not portable
scripts/spec/data/format-points.json   every FMT row's points (§3.4)
scripts/spec/facility.py               the one facility resolver (§3.5)
tests/nist/crosswalk/<program>.json    each NIST-85 program's units and dispositions (§6.3)
```

- **In this repository**, so a compiler fix and its test land together; the folder's LICENSE lets it be licensed apart.
- **Filed by clause**, because edition is metadata: an inherited rule's test is written once.
- **WiseOwl's facts follow the case file** (17j). PB4147's single registry of 3,055 refusals let the merge queue admit
  one refusal-adding pull request at a time (61 of 79 driver-file holds); a WiseOwl file per case file is shared only
  by branches editing that case file. No merge driver is needed: git's line merge conflicts only when two branches
  edit neighbouring lines of one case file, its `.out` or its `.jsonl`, which every branch adding a test to the clause
  now shares. Of 778 landings since 2026-10-21 (main at 04b40a165), 200
  touched a clause folder, and 9 times two consecutive ones touched one folder within an hour
  (`check17/merge_contention.py`). `RegistryShapeDriftTests` keeps every registry one file per entry and `.gitattributes` free of `merge=` (kb/Work PB5110).
- **The generator and its siblings.** `harness/generate.py` (with `harness/cobol_source.py` for facts read from program
  text) is the ONE reader of blocks and `.flagged.jsonl` lines, the one writer of manifests and the one place
  refusals are raised. At 1,936 lines it is already large, so revision 3's derivations go to sibling modules that take
  the parsed tests and return facts or refusals, each with a `--self-test` and one owning slice: `casefile.py` (S5b),
  `derive.py` (S6), `obligations.py` and `completeness.py` (S9), `placement.py` (S10-S12), `coverage.py` (S15),
  `crosswalk.py` (S17), and two tools, `reformat.py` (S8) and `group.py` (S5b's one-time migration).
- **The refusals.** Every "refuses" in §3 and §6.3 is a generator refusal. Besides them it refuses: a syntax flag on a
  row neither FMT- nor identified-as a syntax rule; a conformance flag outside 14.8.2, 14.8.3 and 9.3.8.2.3; a test
  accepted and flagged at one edition whose kind does not allow it; an `id` shared or equal to a file's name; a file of
  more than 25 tests, or of none; a folder not named `<clause>-<Name>` or `<clause>@<edition>-<Name>`; a test filed
  under a clause none of its rules belongs to, or with no rules outside `_unassigned/`; editions outside a band or
  window; a rule whose band begins after every construct its test is written in (PB870), unless the row's `notes` name
  it; a flagged line whose control is unaccepted at its editions, an obsolete or archaic flagged line at an edition where
  its control holds another element of that kind, and a mark no line names; free form at 85 in `cases/`; a STOP
  statement in a test that is not marked `main` and enters a grouped program; a test not marked `main` holding no program
  definition that is not a prototype (none today, `rev3e/noprog.py`); a test with no `accept`, no flagged line and no
  WiseOwl diagnosis; an accepted test that displays nothing; a literal beginning `*TEST `; an undeclared COPY or file,
  an output section naming no test of the file or equal to the section it narrows, an unknown field, a block in the
  other form; a `requires` name that is no module row; a lint violation in a suite test or a
  driver; a `wiseowl/` line naming no test of its file, a code `DiagnosticCatalog` lacks, an accepted edition pinned
  without a warning severity, or a points entry naming an unknown point or edition; a file under the removed
  `tests/conformance/`.
- **The manifests.** `suites/<edition>/manifest.json` lists every cleared test with its solo program, its expectation
  (sections per A.1 value, or the flag kind, legs, site lines and control), its placement class, `words` and `clause`
  (§5), and per case file its tests in run order. `generate.py --profile <file>` writes `suites/<edition>/<compiler>/`,
  that profile's grouped programs (§3). A release archive carries those of the shipped profiles, WiseOwl's and
  GnuCOBOL's, with the case files and the generator, so another compiler's profile gets its own; with none, a runner
  runs every test solo. `suites/wiseowl/manifest.json` holds every test of both trees at each edition it names, grouped
  by WiseOwl's profile, with the WiseOwl diagnoses, options and `build` steps; only the in-process runner reads it.

### 4.1 The completeness report

For each edition, each row in band is counted once, under the first witness kind that holds: documented non-support,
suite test, determination (a DOC row `docs/CONFORMANCE.md` §7 carries), derivation, WiseOwl-only, none. The old count
read "none 0" at 85, so the report adds these axes:

- **WiseOwl-only, split** into a `cases/` test at this edition not yet cleared, one at another edition only, and an xUnit
  test only. At 85 the 2,017 rows divide as 427 positive case only, 305 positive and negative, 350 negative only, 457
  other editions only, 265 xUnit only, 2 NIST only, 20 no reference, 191 documented non-support.
- **Polarity.** An FMT row, and an SR row with `restricts: yes` `identified-as` a syntax rule, needs an accepted AND a
  flagged case; GR, RV and AR rows an accepted case. Of the 691 SR rows in band at 85, 240 have only negative witnesses,
  all WiseOwl refusals.
- **Coverage points** (decision 7): PARTIAL rows, and points witnessed of points applicable per row, format and edition.
- **Removed facilities**: by their `@<edition>` rows, and by NIST-85 units where no text describes them, the second as
  unit-level coverage, never rule coverage.
- **Placement evidence** (decisions 5, 10, 14): per edition below 2023, placements by class, those weaker than
  `iso-2023` listed, and the tests withheld on an `unproven` edge, with its evidence (§3.5).
- **NIST-85** (§6.3), at 85: the dispositions, the percentage of ISO-language units subsumed (rule-level and unit-level
  apart), the excluded units with reasons, and the 85 rows no unit exercises: what NIST missed.
- **Newly measured scope** (decision 16): the GAP that rows and points added by a measuring slice (S13, S14, S19, S20)
  cause, apart from regression.
- **The catalog's own completeness** (§9): normative sentences no row holds; until zero, every figure is relative to
  the catalog.

The generator writes the report to `suites/completeness.json`; `gen_conformance_notes.py` carries its figures into the
kb/Conformance burn-down, whose ledger page is the owner's (PB2912), so a new section there is raised as a lead.

## 5. The harness, the adapters and the results

- **`harness/run.py`, Python 3 standard library only**, runs on Windows, Linux and z/OS, and executes manifests.
- **An adapter is configuration**: command templates for compile, run, flag-mode and `conformance` (absent, the
  `conformance` tests are NOT APPLICABLE); how a diagnostic's file and line are read; ASSIGN mapping; file-name limits;
  crash exit codes; the edition map; how its compile command makes a group's driver the entry (§3; S3 verifies it on
  both compilers). GnuCOBOL: `cobc -std={std} -fformat={source-format} -x -I . -o {program} {source}`, `{85: cobol85,
  2002: cobol2002, 2014: cobol2014}`, base names of at most 31 characters. Its flag command enables every mechanism
  the kinds need: at `-std=cobol85 -fsyntax-only` cobc is silent on ALTER until its archaic and obsolete warnings are
  on. A compiler needing more (IBM's batch jobs) supplies a Python hook with the same operations. Shipped: WiseOwl and
  GnuCOBOL; IBM Enterprise COBOL and Rocket Visual COBOL documented. `harness/config.py` is the one loader and validator
  of adapters, profiles and baselines, and `generate.py --check` checks each against what only the repository knows.
- **A profile is evidence, not assertion.** Each claimed A.4 module and A.3 element carries its evidence. WiseOwl's are
  the rows `docs/CONFORMANCE.md` §5 and §2 dispose Claimed or Partial, held there and its A.1 values to §7 by DOC AUDITS
  (`scripts/docchecks/register.py`); GnuCOBOL's were measured on cobc 3.2.0 and S3 verifies them. A profile states
  each enumerating A.1 item's value and its extra reserved words per edition (GnuCOBOL's: what `cobc --list-reserved`
  lists beyond `reserved-words.json`).
- **Each program runs in its own fresh directory** with its tests' declared files and stdin closed; case files run in
  parallel.
- **A case file runs in three steps** (17d, 17g), so one failing test never hides another or fails the file. (1) Compile
  the profile's grouped program (§4), which holds only tests the profile can apply; if it does not translate, compile
  each of them solo, since "Translation may be unsuccessful due to factors other than lack of conformance of a
  compilation group" (§2023:4.5) and limits "may prevent the successful translation by a conforming implementation of some
  compilation groups that meet the requirements of standard COBOL" (§2023:4.2.15). A construct the compiler lacks without
  its profile saying so then costs only its own test, so no file is split by construct. (2) Run it and split stdout at
  the driver's headers wherever they fall (a test ending WITH NO ADVANCING leaves the next header mid-line); a test whose
  section closes and equals its expected section PASSes. (3) Run every other test solo (a section that differs, or one
  never closed: an abnormal end, a crash, the hang guard); the solo run gives its verdict. A test that FAILs grouped and
  PASSes solo is recorded as INTERFERENCE beside its PASS, a suite defect counted apart. A grouped PASS stands for the
  solo verdict; the reverse, a grouped PASS whose solo run would FAIL, is checked only by §9's nightly run on WiseOwl.
  A file of n tests costs one compile when all pass, at most n + 1 when the group does not translate.
- **Verdicts**, per test and edition: PASS; FAIL (an output difference; an accepted test whose solo program did not
  translate or run; a flagged test whose control was clean and whose compiles reported nothing the control did not);
  NOT APPLICABLE (the edition, or a needed optional or processor-dependent element, unclaimed; the control did not
  compile; an extra reserved word used; the adapter lacks the mechanism); UNDOCUMENTED (§3.3); NOT DISCRIMINATING and
  UNLOCATED (§3.2); NO VERDICT (a crash or the hang guard on the solo run). The last four are counted apart. Exit
  status is not judged.
- **An extra reserved word counts only as a user-defined word.** "An implementation that introduces additional reserved
  words as nonstandard extensions conforms" (§2023:4.2.10), and a word "reserved in the specified language construct or
  context" (§2023:8.10) used in that context is a keyword use. So each test's `words` are its words that are none of the
  standard's, plus the names it defines that its edition does not reserve; the generator intersects them with a
  profile to build its group, and a runner does so for a solo test, reading no program.
- **Results** are JUnit XML and JSON, one result per test naming its file, totalled per edition, clause and placement
  class, INTERFERENCE apart, with CHANGES against the compiler's baseline, so a second compiler is a regression net, not
  a permanently red leg. A run is RED when a verdict differs from its record and is FAIL or NO VERDICT or was a recorded
  PASS; with no baseline, on any FAIL or NO VERDICT; on WiseOwl, also on any INTERFERENCE. `--record-baseline`
  replaces the baseline (with `--editions`, those editions' records).
- **In WiseOwl's pipeline** the xUnit runner in `WiseOwl.Cobol.Tests.Conformance` executes the same manifests
  in-process by the same three steps, one xUnit result per test; it checks `wiseowl/diagnostics/` and recomputes
  `wiseowl/points/`. An accepted test compiles alone only when it must (at least 426 of 2,329, §3). A drift test runs
  `run.py` with the WiseOwl adapter on the same suites and requires equal verdicts, in every gate and on both CI
  operating systems, with both output normalizations pinned to `harness/normalization-fixture.json`. The GnuCOBOL
  adapter does not replace `scripts/gnucobol_differential.py`, which runs the other direction.

## 6. Migration

1. **Done (S0a-S2):** the `editions` field in one spelling, every empty row decided, and every program moved,
   behaviour-neutrally, into `cases/<clause>/`, `_unassigned/` or `wiseowl/cases/`, each negative's expectation in
   `wiseowl/diagnostics/`.
2. **Case files, once (S5b, 17i).** `harness/group.py` concatenates each clause folder's tests in rule order into case
   files (§3), and the 219 `wiseowl/cases/` tests into 76 likewise. The 1,186 `_unassigned/` tests take the same
   blocks as one-test case files, since one block parser serves every tree (§10). Test ids do not change, so the
   inventory's 8,579 `conformance:` refs stand; the 259 references to 211 moved `.cob` paths on 226 lines of `kb/`,
   `docs/`, `tests/`, `scripts/` and `src/` (`DEVLOG/` excluded, `rev3e/path_refs.py`) are rewritten in the same
   change. The tool:
   - converts free form to fixed with `reformat.py` (S8), since 217 folders hold both forms and keeping them apart
     would make 781 files; a test it refuses, or whose rules concern the reference format, stays free in its folder's
     free-form file;
   - makes every test that enters a group return to the driver (§3), in every source unit of both trees. Of the 2,329
     accepted assigned tests, 2,292 outside the EXIT, GOBACK and STOP clauses write a STOP statement, 127 in a later
     unit (a callee, or a program after a prototype or class), as do 216 of the 219 `wiseowl/cases/` tests
     (`rev3e/d17_fix_measures.py`, `check17/stop_other_units.py`). STOP RUN becomes EXIT PROGRAM in a test accepted at
     85, otherwise GOBACK. In a callee that changes where control goes, so a test whose section then differs is marked
     `main`, as are the three clauses' tests. Refused and `_unassigned/` tests (2,487 and 1,154 write a STOP) enter no
     group and keep it;
   - renames the reused outermost names and external file names (§3) with their CALLs and ASSIGNs, and writes each
     `.out` as its test's section;
   - adds a DISPLAY to the 13 accepted tests with an empty `.out` (8 assigned), their one recorded output change, since
     a test that displays nothing is refused (§4);
   - moves each diagnosis file, unchanged, into a line of its case file's `.jsonl` (S7 respells each once), and each of
     the 7 options files into its case file's, keyed by test id.
   Apart from those DISPLAYs, every test's WiseOwl verdict and output at every edition, solo and grouped, must equal
   its verdict before (S2's neutrality rows); a test that differs runs solo or `main`, with a kb/Work note. 12,758 files
   become 4,306, of which 2,372 are `_unassigned/`'s (`rev3e/group_files2.py`).
3. **Clause by clause, COBOL-85 first** (decision 4). For each test: run it on WiseOwl at every later edition; write a
   control for each negative and derive its flagged line; apply the lint; read its points. Each difference is decided
   by the ISO text as a real edition difference (a narrower expected section, a flag, or a `variants` entry), a
   compiler defect (a kb/Work note), or a non-portable test (rewritten, or kept WiseOwl-only). A test that clears is
   marked portable. The 85 pass starts from the 1,074 tests that
   cite only rules in band at 85 and name no later construct (703 accepted, 371 refused), which reach at most 539 more
   85 rows; S3 replaces that bound. The 166 `_unassigned/` tests that run only at 85 get their construct key. Uncited
   programs get rule ids in their clause's pass and join one of its case files, or stay WiseOwl-only.
4. **Unmet obligations** get tests written from the standard into their clause's case files (§6.1).
5. **NIST CCVS85 stays in `tests/nist/` until the 85 suite subsumes it** (§6.3), then leaves the gate and the nightly
   guard; its source stays as the crosswalk's input.
6. Each clause pass runs in the loop as a PB3160 campaign wave, once released (§8).

### 6.1 Obligations: every case derived from the text

`harness/obligations.py` derives each row's obligations at each edition in its band from the inventory's fields and the
point catalog:
- GR or RV: an accepted test whose output shows the rule's effect;
- AR: an accepted test, and from 2002 one observing the exception condition where the rule sets one;
- SR: an accepted test of the legal form, plus a flagged test where `restricts` is `yes` and it is `identified-as` a
  syntax rule (otherwise its flagged test is WiseOwl's);
- FMT: an accepted test and every coverage point; any row: its table cells and lettered sub-items;
- DOC: a test per value where `enumerates` names values, otherwise `docs/CONFORMANCE.md`'s determination;
- `@<edition>` rows the same; a `construct:` facility its NIST units (§6.3); a standing derivation, or documented
  non-support until implemented: none.

**Three row fields are decided once, with a refuter, and the validator requires each**, since a row holds a
`rule-digest`, not its text:
- `restricts` on every SR row, `yes` or `no` with the restricting words cited, seeded by a lexical pass (`shall`,
  `may ... only`, `cannot` and the like). At least 99 SR rows restrict without "shall", such as
  SR-14.9.14.3-7 "An EXIT PROGRAM statement may be specified only in a program procedure division"
  (`review-fidelity/restrict_count.py`). FMT rows restrict by the notation.
- `identified-as` on every SR and FMT row, derived from the headings; only the 65 SR rows outside "Syntax rules" need a
  decision.
- `enumerates` on every DOC row (§3.3).

They sit on the inventory row, because the catalog is regenerated from the text and adjudications live in the
inventory.

**Size.** Until the fields are decided: at least 1,010 of 1,375 SR rows restrict (911 with "shall", plus the 99); all
322 FMT rows owe flagged tests at every edition in band, plus the must-diagnose points; about 2,300 format alternatives
are owed before optional words and ellipses. S9 computes the figures. New authoring is the 404 rows no test names, the
flagged tests and the points no converted test covers.

**The authoring lane** writes a test for an unmet obligation into its clause's case file from a dossier (the rule's
text, the row's fields, points, band, evidence, facility window and `variants`, the change rows that touch it, the
existing tests) and derives the expected result from the ISO text before running anything. The refuter keeps the
seven overturn kinds and adds `not-portable`, `point-not-exercised` and `obsolete-interaction`. The validator runs
`generate.py --check`, WiseOwl at every accepted and flagged edition with the control, and GnuCOBOL as a second
signal, never the authority. The new shape adds editions, flagged tests and lint rewrites to the old lanes' cost (§2),
so S3 measures it.

### 6.2 Supplementary programs: fuzz, vetting and report repros

A fuzz program, vetting probe or report repro (PB4613) is a draft for an unmet obligation, never a test. It is committed
under `conformance-suite/intake/<source>/` holding its provenance only (the fuzz night and seed, the probe, the note
it reproduced); its target rows and status live in its kb/Work note (decision 13). The generator neither reads nor
reports the folder, so it is no second work list. A program enters the suite only through §6.1: mapped to rows, its
expected result re-derived from the ISO text, rewritten to pass the lint, and run at every edition its rows allow. One
whose obligation a test already meets is dropped, unless it reproduced a defect, when it stays a WiseOwl-only
regression test.

### 6.3 Subsuming and replacing NIST-85

**A feature map, not a copy** (decision 3). Subsumption is a property of the suite: a rule is covered when the suite
holds a validated 85 test of it that some compiler has passed.

**The unit is NIST's own test**: each paragraph-name test unit (6,929) and each of the 40 OBSOLETE flags of 14 programs,
6,969 units. The 17 FIPS programs' 177 NON-CONFORMING STANDARD flags are excluded (decision 11); those programs hold no
paragraph unit. NIST's references seed the map but do not decide it: 2,245 of the 6,967 test lines of its golden
reports name a paragraph keyed by format (`MPY-TEST-F1-n`), and its 1,928 ANSI-REFERENCE settings point into the
unheld 1985 text.

**The crosswalk**, `tests/nist/crosswalk/<program>.json`, gives each unit one disposition:
- `rows`: the rows it exercises and their points. A flag unit's rows must be of the right polarity. The unit is subsumed
  when an 85 suite case of each row is credited with each of those points (accepted, or flagged for a flag unit).
  Communication's units map to its `@2002` rows.
- `unit`: a unit of a facility no held text describes (Segmentation, the Debug units 2002 removed, ALTER and the other
  F.1 item 1 elements). The unit is the obligation; its entry names the 85 test reproducing it, which names
  `construct:<id>` and derives its output from the unit's own expectation. Reported as unit-level coverage.
- `excluded`, with a reason: `fips-pub-21-1` (decision 11) or `ccvs-scaffolding` (CCVS's own machinery, such as the
  X-card parameters). A CCVS defect is not excluded: RL118A tests ISO behavior with a wrong expectation, so
  it maps as `rows` with a `differs` entry.
- `open`: not yet mapped.

No unit is `not-claimed`: WiseOwl implements Communication (decision 12), the one candidate the study found.

**NIST corroborates; a disagreement is recorded.** An 85 case mapped from a unit agrees with NIST's golden on the
behavior they share, or its entry carries `differs: {cite, cause}`, `cause` a `variants` entry or `nist-defect`. A unit
whose golden is a divergent row of `tests/nist/corpus.tsv` (its note cites the justifying ISO §) needs a `differs`, and
its test cites that §'s row and derives its expected section from it.

**The seed.** A lexical map of the NIST programs reaches 116 subclauses, 56 with no test accepted at 85
(`nist85_clause_map.json`), and confuses facilities sharing a keyword; compiling them at `--std 2023 --permissive`
shows what 2023 removed, such as ALTER at 92 sites (`nist85_probe_2023.json`).

**One NIST map**: S17 moves the 16 `nist:` test-refs into the crosswalk, and the ref grammar stops accepting them.
**The audit** (`harness/crosswalk.py`) refuses an unknown row, point, construct or case; a row whose band
excludes 85; a case not accepted (or not flagged) at 85; a `unit` case not naming its construct; a divergent golden
without `differs`; an exclusion without a known reason; a unit with no disposition.

**"Subsumes and replaces NIST-85"** holds when no unit is `open`, every `rows` and `unit` unit is subsumed, every
divergent unit has its `differs`, and the report reads 100% of the units not excluded, rule-level and unit-level apart,
with the excluded units listed. Only then does §6 step 5 retire `tests/nist/` from the gate. The crosswalk's cost
(6,969 units in 442 programs, adjudicated per program with a refuter) is unmeasured, so S3 maps a sample first.

## 7. Releases, versioning and the license

- CI builds per-edition release archives (case files, generated programs, harness, adapters, profiles, README,
  LICENSE) from a tag. Nothing is published until the owner chooses the license (PB3159); until then the folder is
  under the repository's Business Source License.
- The README carries the ISO standard's acknowledgment paragraphs verbatim: its Introduction permits reproducing the
  specifications wholly or partly on that condition (`specs/ISO_COBOL_2023.md:1146`, an unnumbered clause that `cite.py`
  cannot check).
- The suite has its own version (`2026.1`); each archive names its edition, suite version and the WiseOwl commit it was
  verified on.

## 8. Order of work

1. **Now** ("Pilot now, rest after GAP"): the tool slices of §13 are released as pilot work (decision 16) in §13's
   `blocked_by` order, so S14 and S15 follow S3 and S20 waits for the 2027 text. They change no test's expected
   behaviour, and the GAP their new rows and points add is reported as newly measured scope (§4.1).
2. **The pilot, S3, runs after S5-S8 and S5b and before S14 and S15, and sets their scope** (decision 15). It runs §6
   steps 3 and 4 at all four editions on WiseOwl and GnuCOBOL, crediting points by hand, for clause 11 (114 rows),
   §2023:14.9.8 COMPUTE and one file statement (`pb355_start_length_past_operand`), plus one sample per long pole of the 85
   path: crosswalk programs needing `rows`, `unit` and `differs`, new 85 tests, MOVE's Table 16 as one control, and
   `restricts` for its SR rows. It compiles the 1,074 85 candidates at 85, checks the driver as entry and each edition's
   return on both compilers, and measures the compiles grouping saves, the cost per obligation and the share of step 3's
   outcomes.
3. **After zero GAP and zero known defects, COBOL-85 first** (decision 4): the 85 clause passes, the new 85 tests and
   the crosswalk, until §6.3's report reads that the suite subsumes NIST-85, after S5, S12, S13 and the Communication
   facility (decision 12). Then 2002, 2014 and 2023 in turn, each running forward over the tests earlier editions
   cleared.
4. **2027**, once published: the schema is ready (S5), and its differences are found from the text first (S20).

## 9. Drift tests

All but one run in every gate. The generator's refusals (§3-§4, §6.3: case files, blocks, editions against bands and
windows, labels, sections, drivers, flags and `wiseowl/` lines) run as one audit, `generate.py --check`, among the
gate's and CI's audits before the build, so a refused test is red in seconds. Beyond them:

- Every suite `test-ref` resolves; no merge driver governs a suite path.
- Both runners report the same verdict for every test; both normalizations agree on the fixture.
- No test shows INTERFERENCE on WiseOwl: any a run sees is RED (§5). Nightly, and in the authoring validator for the
  files a lane writes, every test also runs solo, which catches a grouped PASS whose solo run FAILs; this compiles every
  test twice, so it stays out of the gate.
- `format-points.json` matches the printed figures where the PDF is held, and its shape and digest everywhere; every
  point's window agrees with its words unless a construct governs it, and no construct window contradicts them.
- Every operand-kind point of an implemented format maps to a grammar element; every `wiseowl/points/` file equals what
  the parse credits now.
- Every SR row has a decided `restricts`, every SR and FMT row an `identified-as` equal to its heading, every DOC row a
  decided `enumerates`; every row and edition outside documented non-support and derivations yields an obligation.
- Every normative sentence of clauses 6-16 and the A.1 references is held by a row or recorded as covered by one,
  read from `specs/ISO_COBOL_2023.md`; it starts red (about 129 sentences) and S19 brings it to zero.
- Every row starting after 85 names a resolvable facility, its band lies within the window, and a later start records
  its own edge.
- Every edge below 2023 has a class that can prove it; `iso-2023` locators pass `cite.py --check`, draft locators the
  per-edition check where held; every 85 start, and the 85 side of every 85|2002 edge, is `inferred`.
- Every obsolete and archaic flag's editions equal its construct's.
- Every construct window and grammar version predicate whose edge the held editions span rests on a lineage edge its
  citation names (the EDITION GATES audit, `scripts/spec/edition_gates.py`, kb/Work PB4996).
- Every crosswalk file passes its audit; no inventory `test-ref` names a NIST program.
- Every row added by a measuring slice records the slice.
- `reformat.py`, `group.py` and every sibling module pass their self-tests.

## 10. Rejected alternatives

- **Four physical copies** of inherited tests: copies drift (CLAUDE.md rules 4, 5). **Today's layout run forward**:
  WiseOwl-specific. **A separate repository**: every compiler fix spans two. **A .NET harness**: no z/OS.
  **NIST-style self-checking programs**: more code for no gain.
- **Must-refuse negatives, vendor codes in tests, a portable judgment of severity**: the standard leaves message form
  to the implementor.
- **The clause in the test id**: a mis-filed test could never move. **Two block parsers**: two semantics.
- **One test per file**: 12,758 files (decision 17). **Tests as paragraphs of one shared program**, CCVS's shape:
  shared data-names, configuration and directives. **Files split by edition band** (1,215 files) **or by construct**:
  per-profile groups and the solo fallback make both unnecessary. **A label on every output line**: every DISPLAY
  rewritten.
- **One row per format alternative**: about 2,000 rows with no rule text. **Pairwise coverage**: INSPECT alone needs
  700 statements.
- **Points declared by hand or read from the markdown figures**: drift and inherited transcription defects.
- **A control judged by "no diagnostic at all"**: informational messages make it unportable.
- **Hand-written flagged tests kept in step with controls**: 413 hand-kept copies of the control for Table 10 alone.
- **Polarity from "shall"**: misses "may ... only" (`review-fidelity/fmt_shall_source.py`).
- **Windows copied into one register**: 533 rows are bounded by windows held elsewhere.
- **A removed facility witnessed by one accepted and one flagged test**: 605 NIST units covered by about six tests.
- **Re-keying ids to each edition's numbers**: 1,787 rows and 173 folders for 2027 alone
  (`review-feasibility/cd13_new.py`). **One generator module**: slices contending for one 1,936-line file.
- **Hand lists** of intake target rows or enumerating A.1 items: a second work list, and nothing catches an omission.

## 11. Open

- The suite's license and product name (PB3159).
- The IBM and Rocket adapters are unverified: no access to either compiler.
- GnuCOBOL has no 2023 dialect; its 2023 suite is NOT APPLICABLE until it has one.

## 12. What revision 3 changed and why

Revision 2's review is its own §12 (`E:/COBOL-coord/scratch/PB3160-design-review-mythos.md`). Two reviews of revision
3's first draft (feasibility and architecture, F1-F12; fidelity to the standard, T1-T17) raised 29 findings, each
re-measured or re-cited and folded in, F3 in part; their records are `review-feasibility/` and `review-fidelity/`.
Decision 17's case file (§3-§6, §9, §13) was then revised against a checker's findings (`check17/`, `rev3e/`); the
text before it is `DESIGN-conformance-suite.rev3c.md.pre17`. §1.1's "Lands in" column maps each decision to its
sections.

## 13. Implementation slices

Each slice is one kb/Work note in the PB3160 campaign, ordered by `blocked_by`; the notes hold status, this section
scope. Every slice ships its tests and drift tests and corrects this document where it proves it wrong (CLAUDE.md
rules 2, 5, 6).

**Landed:** S0a-S2 (PB3172-PB3177). **Held:** S4 (PB3179), the release archives, until the owner chooses the license
(PB3159).

**Filed from revision 3** (2026-11-04), in this order, each blocked by the one before; each owns one module or register.
- **S5 — the edition schema (§3.5).** (PB4683) Band start and optional end in schema, validator and C# twin; per-edition clause
  locators; `@<edition>` ids and folders; `{from: <edition>}`.
- **S8 — the free-to-fixed converter (§6).** (PB4684) `reformat.py`, ahead of S5b, which uses it.
- **S5b — the case file (§3-§5, §6 step 2; decision 17).** (PB4685) `casefile.py`: blocks, solo programs, per-profile grouped
  programs and their driver (`generate.py --profile`), sectioned outputs, `main` and what runs alone, the case-file
  refusals; the three-step run and INTERFERENCE in both runners, one xUnit result per test; the adapters' entry
  statement; the per-file WiseOwl layer and options keyed by test; `group.py` and the one migration of §6 step 2.
- **S6 — diagnosis legs and derived flagged tests (§3.2, §5).** (PB4686) `derive.py` and `<file>.flagged.jsonl`; labelled sites
  and derived controls; the mechanism table as data and the kinds' legs in the generator and both runners; the verdict
  order; the `flag` and `control` fields deleted; module rows and per-edition profile claims; `archaicIn`; GnuCOBOL's
  archaic and obsolete warnings.
- **S7 — the WiseOwl layer (§3.2).** (PB4687) The 3,370 diagnoses respelled once to `{test, lines, at}`, the 1,021 carrying
  message text turned into codes; judged at lines; warnings pinnable.
- **S3 — the pilot (PB3178, §8).** Its measurements set S14's and S15's scope and size S11, S17 and S18.
- **S9 — row fields and obligations (§6.1, §4.1).** (PB4688) `restricts`, `enumerates` and `identified-as`; `obligations.py`,
  `completeness.py` and the report's WiseOwl-only, polarity and newly-measured-scope axes.
- **S10 — facility links (§3.5).** (PB4689) `facility.py`, `facility` on the 1,873 rows, `band ⊆ window`, `constructs` derived
  through rules, in `placement.py`.
- **S11 — edge evidence (§3.5), sized from S3 and S10.** (PB4690) The classes on the windows, PB4608's four edges `unproven`;
  the change items mapped with a refuter; the per-edition citation check; derived markers; `unproven` withholding.
- **S12 — content variants (§3.5).** (PB4691) `variants`, seeded from the change lists and FLAG directives' options. Blocks the
  85 passes.
- **S13 — removed facilities' rows (§3.5).** (PB4692) The extractor on the held drafts for the facilities and whole rules 2023
  removed; the `ruleText: none` constructs.
- **S14 — the coverage-point catalog (§3.4), scoped by S3.** (PB4693) The script over the 2023, FCD and 2002 figures; word ids;
  derived windows; must-diagnose points; the 25 unpaired delimiters; table cells and sub-items; the CI digest check.
- **S15 — crediting points from the parse (§3.4), sized by S14.** (PB4694) `PIC_IS` to the hidden channel; keyword points from
  node tokens; the operand-kind map; `wiseowl/points/`; `coverage.py`.
- **S16 — the A.1 verdict (§3.3, §5).** (PB4695) UNDOCUMENTED in both runners; A.1 values held to `docs/CONFORMANCE.md` §7;
  `implementor-defined.json` deleted.
- **S17 — the NIST-85 crosswalk (§6.3), sized by S3.** (PB4696) 459 files adjudicated per program with a refuter; the 17 FIPS
  programs `excluded`; `crosswalk.py`; the 16 `nist:` refs moved; the NIST axis.
- **S18 — the authoring lane (§6.1, §6.2).** (PB4697) The dossier, the new overturn kinds, and the validator, which runs the
  INTERFERENCE check on the case files it writes.
- **S19 — the text-to-rows check (§9)** (PB4698), and the missing rows added through the extractor, 8.3.2.1 and 6.4.2 first.
- **S20 — 2027 from its text (§3.5, PB4397).** (PB4699)

**The defects the study found** live in kb/Work, not here (CLAUDE.md rule 8): PB4676 and PB4677, and a note for each
other finding in `diag/checkpoint.jsonl` and the reviews' checkpoints, filed before any slice names it.

The remaining clauses are filed as campaign notes from S3's measurement, 85 first, held until zero GAP and zero known
defects.
