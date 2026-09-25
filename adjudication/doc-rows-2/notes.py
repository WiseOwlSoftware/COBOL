#!/usr/bin/env python3
"""doc-rows-2 lander step 5: register the landing in kb/Work (CLAUDE.md rule 8). Run from the worktree root."""
import json, pathlib, re
W = pathlib.Path("kb/Work")
STAMP = "2026-09-25"
HDR = f"\n\n## doc-rows-2 landing ({STAMP})\n\n"

def load(n):
    p = W / f"{n}.md"
    raw = p.read_bytes().decode("utf-8")
    return p, raw, ("\r\n" in raw)

def save(p, t, crlf):
    p.write_bytes((t.replace("\n", "\r\n") if crlf else t).encode("utf-8"))

def fm_list(t, key):
    m = re.search(rf"^{key}: (\[.*?\])\s*$", t, re.M | re.S)
    assert m, key
    return m, json.loads(m.group(1))

def edit(n, drop=(), add=(), append=None, fm=None, body_sub=None):
    p, raw, crlf = load(n)
    t = raw.replace("\r\n", "\n")
    head, sep, rest = t.partition("\n---\n")
    assert head.startswith("---\n"), n
    m, rows = fm_list(head, "inventory_rows")
    for d in drop:
        assert d in rows, (n, d, rows)
        rows.remove(d)
    for a in add:
        if a not in rows:
            rows.append(a)
    head = head[:m.start(1)] + json.dumps(rows, ensure_ascii=False) + head[m.end(1):]
    for k, v in (fm or {}).items():
        pat = rf"^{k}: .*$"
        if re.search(pat, head, re.M):
            head = re.sub(pat, lambda _m: f"{k}: {v}", head, count=1, flags=re.M)
        else:
            head = head.replace("\ntags:", f"\n{k}: {v}\ntags:", 1)
    if body_sub:
        for old, new in body_sub:
            assert rest.count(old) == 1, (n, old[:60])
            rest = rest.replace(old, new)
    if append:
        rest = rest.rstrip("\n") + HDR + append.strip() + "\n"
    save(p, head + sep + rest, crlf)
    print(n, "ok", rows if len(rows) < 12 else f"{len(rows)} rows")

LAND = "adjudication/doc-rows-2/LANDING.md"

edit("PB1422", drop=["DOC-A.1-14", "DOC-A.1-89"], append=f"""
**Scope narrowed to the INVOKE twin (A.1 item 102) and GR-8.4.3.2.4-6.** The doc-rows-2 §7 rows settle what
'located' means for a separately compiled module, once, for CALL and for a user-defined function: a module is
located when `ProgramTable.ProbeSiblingModule` has loaded it AND run its `__CobolModule.Register()` registrar, so
a module that exists but cannot be loaded, or whose registrar throws, is NOT located — EC-PROGRAM-NOT-FOUND for a
CALL (DOC-A.1-14), EC-FUNCTION-NOT-FOUND for a function (DOC-A.1-89) — and the set of runtime resources checked
after locating is EMPTY, so neither statement has an EC-PROGRAM-RESOURCES raise site to add. ISO fixes the
consequence of each classification but leaves the locating mechanics of a separately compiled module to the
implementor (§14.9.4.4 GR3 b / §8.4.3.2.4 GR6 b defer only the name scope to 8.4.6), so CLAUDE.md rule 1 goes to
GnuCOBOL: libcob/call.c:270-277 `set_resolve_error` (EC-PROGRAM-NOT-FOUND / EC-FUNCTION-NOT-FOUND on a
dlopen/dlsym failure, call.c:924-942), no EC-PROGRAM-RESOURCES raise site. The d1 writer and refuter decided item
14 that way; the d3 writer (item 89) had assumed the opposite, and the LANDER reconciled 89 to 14 (one lookup, one
meaning of 'located'; {LAND}). Both rows are CONFORMS, test-needed: DOC-A.1-14 and DOC-A.1-89 are released
from this note. What remains here: **DOC-A.1-102** (INVOKE — a class whose assembly cannot be loaded or whose type
initializer throws escapes as a .NET exception instead of EC-OO-METHOD; GR7 b gives both 'not found' and
'resources' EC-OO-METHOD, so the fix is the EC, not a resource set), and GR-8.4.3.2.4-6, which the next implementer
re-verdicts against DOC-A.1-89 (the determination it lacked is now written). The note body's premise that CALL
has a raise site is wrong — CALL has none either, and none is owed. GnuCOBOL also prints a runtime warning naming
the path on a load failure; COBOL.NET's bare catch is silent (DOC-A.1-14 says so) — adding that warning is optional
polish, not a conformance obligation.
""")

edit("PB1369", drop=["DOC-A.1-23", "DOC-A.1-40"], append="""
DOC-A.1-23 and DOC-A.1-40 now have their §7 rows (doc-rows-2): the missing-determination finding (F3) is
discharged for both. DOC-A.1-23's remaining divergence (text-words split on every Unicode White_Space) is
PB1543's; DOC-A.1-40's (missing text not an error, library fallback, and GnuCOBOL's suffix/directory order not yet
followed) is PB1355's. DOC-A.1-53/-54/-55 are claimed by PB1538/PB807 as before.
""")

edit("PB547", drop=["DOC-A.1-36", "DOC-A.1-38"], append="""
DOC-A.1-36 (the composite-set mapping) and DOC-A.1-38 (UTF-8 / mixed data in class alphanumeric: not provided,
none needed) are CONFORMS on the standing UTF-16 repertoire determination (kb/Work R48 second section) and are
released. This note keeps DOC-A.1-46 (DIVERGES): a DISPLAY character above U+00FF reaching a byte channel is
substituted `?` WITHOUT EC-DATA-CONVERSION at some channels. The FILE arm of the same repertoire question is
owner decision R47 and belongs to PB690 (DOC-A.1-31, -159).
""")

edit("PB322", drop=["DOC-A.1-52", "DOC-A.1-218"], append="""
**DOC-A.1-52** is CONFORMS (all devices allow concurrent access, the item-76 determination) and is released.
**Determination E is DOCUMENTATION ONLY, not a code defect** (doc-rows-2 refuter of item 218): §14.6.13.1.3
rule 3 routes a fatal EC-I-O condition through §9.1.13 and then, absent an implementor override, through rules
4–8, which key on the checking state — so the checked/unchecked split E called 'two behaviours selected by a
directive the rule does not mention' is the standard's own. DOC-A.1-218 now documents it (CONFORMS; test-needed:
the checked arm has no witness) and is released. **E-extended (A.1 item 103) stays here**: a fatal status with no
FILE STATUS and no USE procedure must TERMINATE (DOC-A.1-103, GnuCOBOL codegen.c:11091), and today continues.
**Leads for determination A (items 77, 131)**, all consequences of the landed rows, owed in A's landing:
DOC-A.1-153's example (two clause-less connectors open together, B READs and REWRITEs) is what the code does
TODAY and becomes impossible under item 131 (a second clause-less I-O opener gets '61') — re-derive it with the
golden `2002/pb669_lock_visibility_plain_connector`; golden `2002/pb316_open_group_scope`'s first two lines move;
docs/PHASE4_RECONCILIATION.md:1523 ('default sharing NO OTHER') becomes the INPUT-excepted form;
`FileRegistry.ImplementorDefaultSharing`'s summary, FileLockPosture's 'owner-facing question' remark and
docs/COBOLNET_FILES_DESIGN.md (~l.701 'UNDETERMINED') follow DOC-A.1-77 / -131.
""")

edit("PB1178", drop=["DOC-A.1-192"], append="""
DOC-A.1-192 is CONFORMS with its four StopGobackExitCodeTests witnesses (doc-rows-2) and is released.
""")

edit("PB1534", drop=["DOC-A.1-20", "DOC-A.1-36", "DOC-A.1-38", "DOC-A.1-46", "DOC-A.1-75", "DOC-A.1-105", "DOC-A.1-114", "DOC-A.1-124"],
     fm={"status": "landed", "closes_rows": '["DOC-A.1-36", "DOC-A.1-38"]'}, append="""
**LANDED — every determination this note tracked now has its §7 row** (doc-rows-2, PB1522 step 3; §7's
documentation obligations are 182 of 182 discharged). DOC-A.1-36 and DOC-A.1-38 closed CONFORMS on their
witnesses. The rows whose determination today's compiler does not yet implement moved to their mechanism owners:
DOC-A.1-20 → PB1402, DOC-A.1-46 → PB547, DOC-A.1-75 → PB833, DOC-A.1-105 → PB1541, DOC-A.1-114 → PB1540,
DOC-A.1-124 → PB1526.
""")

edit("PB1527", drop=["DOC-A.1-217"], fm={
        "title": '"PB1527 — STRING naming a strongly-typed group that holds an object reference (or any pointer-class leaf) as a sending item compiles clean and then ABORTS at run time (the Tier-C whole-group refusal), where DOC-A.1-214 makes the group\'s character image the D-SLOT placeholder image every other consumer reads"',
        "wrong_answer": "false", "crashes": "true"}, append="""
**RE-SCOPED to the STRING crash only** (doc-rows-2 item 214, refuter upheld). The MOVE half of this note's
original premise is resolved in favour of the CODE: DOC-A.1-214 (rewritten in place) now states that an object
reference in a strongly-typed group counts 8 positions (BYTE-LENGTH 11 for `PIC X(3)` + one reference) whose
character image is the D-SLOT placeholder — eight SPACES — exactly DOC-A.1-56's group-image determination, and the
old §7 sentence 'contributes no character positions to any group image' was the error. What remains is the STRING
arm: `STRING g DELIMITED SIZE INTO x` compiles and then stops at run time instead of reading that image (or being
refused at compile time) — `crashes: true`, `wrong_answer: false`. The doc-rows-1 integrator's 'DOC-A.1-214's
existing row stays' is superseded. DOC-A.1-217 (PROGRAM-POINTER) is CONFORMS and released. Witness owed: the
STRING golden and a DOC-A.1-214 image golden.
""")

edit("PB1529", append="""
**A second arm (doc-rows-2 refuter of item 39): the native-lane PRODUCT wrap.** `01 BIG PIC 9(20) VALUE
18446744073709551616.` then `CONTINUE AFTER BIG * BIG SECONDS` emits
`ContinueAfterExact((double)((Int128)(BIG) * (BIG)), …)` — the unchecked Int128 product of 2^64 × 2^64 wraps to 0 —
and the program suspends 0 seconds (repro `doc2/out/run-refute/d1/r39b.cob`, pin 80de60472). The Scale-0 / scaled /
Real lanes already clamp a value that FITS (PB1033 HostInt64 saturation); the open defect is (a) the Dec lane's
`CobolDec.ToUnscaled` low-digit truncation (10**40 → 0 s) and (b) any intermediate product that overflows the
carrier before the clamp sees it. DOC-A.1-39's ⚠ names both.
""")

edit("PB1355", append="""
**A third arm (doc-rows-2 item 40, orchestrator decision under CLAUDE.md rule 1): the search ORDER is
GnuCOBOL's.** DOC-A.1-40 documents (1) the working directory first (the name as a relative path, or as written when
absolute), then (2) each `--copy DIR` in order, then (3) the `--nist` copylib; in each place the name as spelled,
then `.CPY`, `.CBL`, `.COB`, `.cpy`, `.cbl`, `.cob` — and a name that contains a period only as spelled
(GnuCOBOL 3.2 cobc/pplex.l:1439-1530 `ppcopy_try_open` / `ppcopy_find_file`, cobc/cobc.c:9033-9039 the extension
list; the source file's own directory is NOT searched — pplex.l's own TODO). COBOL.NET today searches the source
file's directory FIRST (`CopyProcessor.RegisterSourceDir`), never the working directory, and tries
`""`, `.cpy`, `.cob`, `.cbl`, `.CPY`, `.COB`, `.CBL` for every name (`CopyProcessor.CopybookExtensions`,
CopyProcessor.cs:56): `BK3.cob` + `BK3.cbl` → COBOL.NET takes `.cob`, GnuCOBOL `.cbl`. ⚠ The implementer must
sweep the goldens and test harnesses that rely on the source-directory search (the compiler's working directory
becomes load-bearing) — measure the fan-out before changing the order. GnuCOBOL's fallback from an unavailable
OF/IN library to the default library (with a warning) is NOT followed: §7.2.3.4 GR2 controls (error CBL3620).
""")

edit("PB1539", append="""
**Sibling arm (doc-rows-2 item 68): the EXTERNAL AS name.** `01 X PIC X(3) EXTERNAL AS "Shared Two "` does not
share with `AS "shared two"` (probe `cc68.cob`): the same untrimmed-AS-literal mechanism on the EXTERNAL data/file
name path. DOC-A.1-68 determines that EVERY externalized name is formed by one rule — leading and trailing spaces
removed — and that a compile-time WARNING is given when an AS literal has leading or trailing spaces; **no such
warning exists today** (grep of src finds none; `as68.cob` prints no warning before EC-PROGRAM-NOT-FOUND). Note:
GnuCOBOL keeps AS literals verbatim (cobc/parser.y `_as_literal` / `_as_extname`, never passed to
`cb_trim_program_id`), so trimming AS names is COBOL.NET's own one-rule choice, not a GnuCOBOL precedent.
""")

edit("PB1520", append="""
**Second mechanism (doc-rows-2 item 121): a property-name spelled like a C# keyword.** `GET PROPERTY long` fails in
Roslyn with CS1002 at five sites because `NamingConvention.cs:41-46` concatenates `"__GET_"` + `DataItem.Sanitize(P)`
and Sanitize has already `@`-escaped the keyword (`__GET_@long`). DOC-A.1-121 determines `__GET_LONG` /
`__SET_LONG` — the upper-cased, hyphen-to-underscore name, never escaped. Repro `doc2/out/run/d4/p121c.cob`.
""")

edit("PB833", append="""
**A design flaw the doc-rows-2 refuter of item 75 found in this note's own 'fcntl … map directly' paragraph**
(which the note itself marks a derivation, not a measurement): a whole-file two-state lock keyed on the SHARING
MODE alone cannot deliver §9.1.15 Table 19 — with `SHARING WITH ALL OTHER` taking no lock and `READ ONLY` a read
lock, another run unit's `OPEN I-O` under ALL OTHER takes no lock and is not refused, and an `OPEN I-O` under
READ ONLY takes a compatible read lock — both violating rule 2 ('restricts … to input mode'). The lock must be
keyed on the opener's OPEN MODE as well (GnuCOBOL libcob/fileio.c:1739 takes F_WRLCK unless INPUT), and whole-file
locks cannot also admit concurrent ALL OTHER writers, so the design needs e.g. separate lock regions per mode.
DOC-A.1-75 now states the guarantee (every opener arbitrated by Table 19, the lock keyed on sharing mode AND open
mode) rather than a lock-type table; this note owns the mechanism.
""")

edit("PB1086", append="""
**Design-doc obligations added by doc-rows-2** (items 66, 67, 161, 162 — each row was trimmed by its refuter to
exactly what R44 decided): the R44 design doc must fix, and write into DOC-A.1-66/-67/-161/-162 in the same change
set, (a) the spellings and DEFAULTS of the reference, update and flag options (DOC-A.1-66 names none); (b) the
§8.13 'any other information that the implementor requires' content (A.1 item 67: the doc-rows-2 writer's
'.NET binding identity — assembly name + version, implementing type/member, never compared, nothing else' is a
candidate, not a decision; a version-mismatch policy is a real flagging question) — DOC-A.1-67 is recorded
NOT-IMPLEMENTED on an R44-only row until then; (c) the precedence between the compilation group and the referenced
repository, how externalized names are compared across assemblies (the AS-literal comparison is PB661's open
§8.3.2.2 (b) question: PB303 recorded 'verbatim, compared ordinally' while `OoClassTable.FindByExternalizedName`
uses OrdinalIgnoreCase), a name more than one referenced assembly answers, and where an unresolvable specifier is
diagnosed. PB1099 Q3 is answered by R44.
""")

edit("PB690", append="""
**doc-rows-2 wrote the §7 rows on owner decision R47** (DOC-A.1-31, -159; also -110, -106, -115, -146). The status
the refusal sets: **'71'** for a LINE SEQUENTIAL file (the standard's own value — §9.1.13.10 1), §14.9.51.4 GR23,
§14.9.35.4 GR17 d) — reached by narrowing item 115's character set to U+0020–U+00FF in an alphanumeric record
area) and **'91'** for a record sequential file and a report file (§9.1.13.11 1): 7x is confined to line
sequential, a 3x permanent error would be sticky for a refusable record, every other value asserts a different
condition; '91' is FATAL (a '9' first digit) and maps to EC-I-O-IMP — DOC-A.1-110 now defines '90' and '91').
The implementer also owes: `FileStatusCode` gains '91'; the `FileStatus.cs` doc comment 'THE ONE
IMPLEMENTOR-DEFINED I-O STATUS' changes; `LineSequentialCharacterSet` gains the U+00FF ceiling for an
alphanumeric record area (a NATIONAL record area keeps no ceiling — its characters are UTF-16BE); and a report
file's GENERATE/TERMINATE is not in §9.1.13.1's list of statements that set an I-O status, so the '91' lands in
the report file connector's implicit write and the landing golden pins what the rest of that GENERATE does.
""")

edit("PB1094", drop=["DOC-A.1-63"], append="""
DOC-A.1-63 (the unstructured dynamic-length item: a native string, no length field, no delimiter, the extent table
beside a record) is CONFORMS on `conformance:2014/pb1053_dynamic_members_both_sides` (doc-rows-2) and is released;
this note keeps the DYNAMIC LENGTH STRUCTURE layout arm (GR-12.3.7.4-19).
""")

edit("PB1586", add=["DOC-A.1-157"], append="""
DOC-A.1-157 (doc-rows-2, refuter of item 157) now determines the TAB: it advances to the next tab stop — positions
1, 9, 17, 25, … (tab width 8, GnuCOBOL's default `tab-width`; CLAUDE.md rule 1) — and its ⚠ names this note.
PB1491 claims the same row for the short-line padding arm.
""")

edit("PB1397", append="""
**The fix is now DETERMINED (doc-rows-2 item 25, refuter + orchestrator):** a source or library file with no
byte-order mark that is not well-formed UTF-8 is read as ISO/IEC 8859-1 (every byte the character with that code
point) WITH a warning naming the file; a file whose byte-order mark names an encoding its content is not
well-formed in (invalid UTF-8 after EF BB BF, a lone surrogate in UTF-16, a UTF-32 unit beyond U+10FFFF) is a
compile-time ERROR naming the file and the byte offset — no fallback for a marked file. No source character is
ever replaced by U+FFFD. DOC-A.1-25 and DOC-A.1-26 state it; the 'pass-through or strict decoder' alternatives in
this note's body are superseded. Code site: `CompilationInputs.ReadAllText` (`File.ReadAllText`).
""")

edit("PB1587", append="""
Context from doc-rows-2: DOC-A.1-164 (RESERVE, no clause) now determines ONE input-output area per connector,
separate from the record area — the stream's 4096-byte host buffer — except when the sharing mode admits another
writer (`FileLockPosture.AdmitsAnotherWriter` → unbuffered). Its row is CONFORMS but test-needed: the RSV/RSV2
probes (`doc2/out/run-refute/d5/`) are the witness shape, so the unobservable-rule closure (PB386) does not apply.
""")

# PB1522: the 51 rows now carry verdicts and are claimed by their owners where defective
items = [14, 20, 23, 25, 31, 36, 38, 39, 40, 46, 49, 52, 53, 54, 55, 60, 63, 66, 67, 68, 75, 77, 81, 89, 99, 102, 103, 105, 107, 108, 113, 114, 116, 121, 124, 131, 146, 147, 156, 157, 159, 161, 162, 164, 167, 168, 192, 214, 217, 218, 219]
p, raw, crlf = load("PB1522")
m, rows = fm_list(raw.replace("\r\n", "\n"), "inventory_rows")
drop = [f"DOC-A.1-{i}" for i in items if f"DOC-A.1-{i}" in rows]
edit("PB1522", drop=drop, append=f"""
**Step 3 continued — doc-rows-2 landed the last 51 A.1 determinations** ({LAND}): every one was written by a
writer, attacked by a refuter (26 overturned and corrected), and recorded with its §7 row in the same commit
(DIVERGES 25 · PARTIAL 14 · CONFORMS 11 · NOT-IMPLEMENTED 1; GAP 1247 → 1241). §7's documentation obligations are
now 182 of 182 discharged. The {len(drop)} rows are released from this note; each defective one is claimed by its
mechanism owner. Owner decisions R47 (file encoding: Latin-1 + refuse) and R48 (STOP RUN ends the run unit only)
were taken during the round.
""")
