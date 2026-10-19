## PB2500 - a word that is both a declared constant-name and a device-name-1 names the CONSTANT's file in ASSIGN TO

**What was wrong.** `SELECT F ASSIGN TO KN` with `01 KN CONSTANT AS 'p12out.txt'.` compiled clean and wrote `kn.txt`, the
spelling of KN as device-name-1, never `p12out.txt`: the constant was never consulted. The ASSIGN clause admits
`{device-name-1 | literal-1}` at one position (section 12.4.5.1), section 13.10.3 SR2 admits a constant-name wherever a
format specifies a literal, section 8.3.2.1 4) lets one word be both a system-name and a user-defined word, and section
12.4.5.2 SR5 leaves the meaning of both to the implementor. The standard does not say which reading wins, so the note
(an analysis, filed from the PB780 adjudication) needed a decision.

**Decision.** Latitude, so CLAUDE.md rule 1's precedence applies: ISO leaves it open, GnuCOBOL 3.2 substitutes the declared
constant, so the program's own declaration of the word wins over the implementor's system-name of the same spelling. That is
already the answer two other positions give (the LOCALE clause's external-locale-name-1, kb/Work PB1942; the ALPHABET
clause's code-name), so ASSIGN joins them rather than starting a third rule. Documented in `docs/CONFORMANCE.md` section 7
DOC-A.1-71, with the two-operand corner: the leading word of `ASSIGN TO DISK name` is the device class even when the
program declares a constant `DISK`, because the only allowable two-operand list is class + name.

**Re-probe.** Lead A (ASSIGN) reproduced on this tree (`kn.txt` written). Lead B (LOCALE) did NOT: `LOCALE LC IS FR.` with
`FR CONSTANT AS 'de-DE'` already prints `07.01.2026`, because `DataBinder.LocaleBind` asks `NamesConstant` first (PB1942).
The note was written before that landing's determination reached its text.

**Fix.** `AssignTargetRule.WordText` reads a word through the new `LiteralEnvironment.ReadConstant` (the `DataBinder.ReadConstant`
lookup the AS phrases use): a constant is literal-1 and meets section 12.4.5.2 SR4 as a written literal does (alphanumeric,
not zero-length; a numeric, national, boolean or zero-length constant is COBOLNET2645 naming the constant), a constant whose
own entry was refused reports at the entry only (verified: one COBOLNET1547, no COBOLNET2645), and any other word is
device-name-1. No new diagnostic code (the code range was returned unused). The old `Text` method's device-name arm
(`return t.GetText()`) is the `NotAConstant` arm now.

**Sweep.** The grammar positions that write `cobolWord | literal` where the word has a system-name reading are exactly
ASSIGN, LOCALE and the ALPHABET literal phrase; the first is fixed here, the other two already did this. The rest are
constant-only (`specialNamesLiteral`, the AS phrases) or semantically inert (`VALUE OF`).

**Goldens.** `12.4.5-File-control/pb2500_assign_constant_name` (accept 2002/2014/2023): WR and RR are one file only if the
constant was substituted, the file the old reading would have written is absent (OPTIONAL OPEN INPUT, status 05), a device
class followed by a constant, AT END (status 10), OPEN INPUT of an absent file answering 35 through FILE STATUS and through a
USE AFTER STANDARD ERROR declarative with no FILE STATUS, and a word naming no constant still device-name-1 (35 on its absent
file). `pb2500-assign-constant-numeric` (refused at 2002/2014/2023) and `pb2500-assign-constant-below-2002` (the constant entry
is a 2002 introduction, COBOLNET0900 at 85). `AssignConstantNameTests` (Conformance, 9 cases) pins each refused shape's exact
wording, the silent recovery, a word that names no constant, a hexadecimal-alphanumeric constant read by a contained
program's ASSIGN through a GLOBAL constant, and the DISK-constant corner.

**The generated check.** `ConstantNameLiteralPositionDriftTests.Unwitnessed` argued the `assignTarget` and `localeClause`
slots as this note's open latitude. The ASSIGN golden witnesses the first, so the test turned red at the landing's Linux
unit leg ("Unwitnessed argues slot(s) a golden now witnesses: assignTarget"); the implementer's leg-1 gate never planned
it. Both entries and their rationale are gone, and `12.3.7-SPECIAL-NAMES/pb2500_locale_constant_name` witnesses the LOCALE
slot: a constant `FR_FR` standing for "ja-JP" formats LOCALE-DATE in the ja-JP form, where the system-name reading would
give the fr-FR form the literal leg shows. DOC-A.1-181 lists it.

**Rows.** The note claimed none (`inventory_rows: []`) and closes none; DOC-A.1-71's text and witnesses are extended.

**Self-review.** Silent failure: a refused constant returns "" only under an already-failed compile and the entry reported;
the Unscoped environment answers NotAConstant so no null path. Test analysis: the golden's abnormal paths are derived from
sections 9.1.13.2 4), 9.1.13.4 1), 9.1.13.6 5) and 14.6.13.1.3 3). Comments: the determination is stated once on
`WordText`; the class summary points to it.
