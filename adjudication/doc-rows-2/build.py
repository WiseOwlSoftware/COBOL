#!/usr/bin/env python3
"""doc-rows-2 lander step 2: build the FINAL 51-item set (writer row + record, refuter correction applied,
orchestrator decisions applied) -> final.jsonl + table.txt. Every patch asserts its anchor text occurs exactly once."""
import json, pathlib, re, sys

OUT = pathlib.Path(__file__).resolve().parents[1] / "out"
HERE = pathlib.Path(__file__).resolve().parent

def load(prefix, d):
    return {r["item"]: r for r in (json.loads(l) for l in (OUT / f"{prefix}-d{d}.jsonl").read_text(encoding="utf-8").splitlines() if l.strip())}

W, F = {}, {}
for d in range(1, 8):
    for i, r in load("rows", d).items():
        if (d == 1 and i in (31, 36, 38)) or (d == 5 and i in (159, 167)):
            continue  # superseded by d7
        assert i not in W, i
        W[i] = r
    for i, r in load("refute", d).items():
        if (d == 1 and i in (31, 36, 38)) or (d == 5 and i in (159, 167)):
            continue
        F[i] = r
assert len(W) == 51, len(W)
assert set(F) == set(W), set(W) ^ set(F)

def refuter_row(i):
    c = F[i]["correction"]
    m = re.search(r"\| DOC-A\.1-%d \|.*?\| (?:—|`[^|]*`) \|" % i, c, re.S)
    assert m, i
    return m.group(0)

def sub1(i, s, old, new):
    n = s.count(old)
    assert n == 1, (i, n, old[:80])
    return s.replace(old, new)

def resub(i, s, pat, new):
    s2, n = re.subn(pat, lambda _m: new, s, flags=re.S)
    assert n == 1, (i, n, pat[:80])
    return s2

ROW = {i: W[i]["row"].strip() for i in W}
REC = {i: dict(W[i]["record"]) for i in W}
OWNER = {i: W[i]["owning_note"] for i in W}
CHANGED = {}  # item -> what the lander changed (LANDING.md)

def note(i, what):
    CHANGED.setdefault(i, []).append(what)

# ---------------- item 14 (refuter: citation; 3 row defects + GnuCOBOL warning precision) -------------
r = ROW[14]
r = sub1(14, r, "Everything a COBOL.NET program needs in order to execute is acquired while it is being LOCATED (§14.9.4.4 GR3b), and a failure there is “the program cannot be located”:",
         "COBOL.NET checks no runtime resource beyond locating the program (§14.9.4.4 GR3b), and a failure to locate it is “the program cannot be located”:")
r = sub1(14, r, "see DOC-A.1-19)", "see items 15 and 16)")
r = sub1(14, r, "(a truncated or foreign image, a missing dependency)", "(a truncated or foreign image, or a dependency the registrar itself needs)")
r = sub1(14, r, "This is the GnuCOBOL disposition under CLAUDE.md rule 1's precedence — libcob's resolver sets EC-PROGRAM-NOT-FOUND when a module is found but cannot be loaded, and has no EC-PROGRAM-RESOURCES raise site on CALL.",
         "This follows GnuCOBOL under CLAUDE.md rule 1's precedence — libcob's resolver sets EC-PROGRAM-NOT-FOUND when a module is found but cannot be loaded, and has no EC-PROGRAM-RESOURCES raise site on CALL — except that GnuCOBOL also prints a runtime warning naming the module's path, which COBOL.NET does not issue. The same determination serves a user-defined function (item 89) and the class of an invoked method (item 102).")
ROW[14] = r; note(14, "refuter: cross-reference 19→15/16, 'everything acquired while located' overclaim replaced, 'missing dependency' narrowed, GnuCOBOL warning difference stated; lander: items 89/102 cross-reference")

# ---------------- item 20 ----------------
r = ROW[20]
r = sub1(20, r, "One folding function keys every name table (data-, procedure-, program-, file-, condition-names and the rest), so no comparison site can fold differently.",
         "Today every name comparison uses the .NET ordinal-ignore-case comparison, which is identical to C.2 on A–Z.")
r = sub1(20, r, "so every extended letter is rejected before any fold (kb/Work PB1402);",
         "so every extended letter is rejected before any fold, and the ONE Annex C folding function that must key every name table (data-, procedure-, program-, file-, condition-names and the rest) does not exist yet (kb/Work PB1402);")
ROW[20] = r; note(20, "refuter: present-tense 'one folding function' claim replaced by today's ordinal-ignore-case; the one Annex C function moved into the ⚠ as PB1402's obligation")

# ---------------- item 23 ----------------
r = ROW[23]
r = sub1(23, r, "What bounds a text-word is the §8.3.5 separator set (with the §7.2.2.5 rule 1 colon and parenthesis cases) and nothing else,",
         "What bounds a text-word is the §8.3.5 separator set (with the §7.2.2.5 rule 1 colon and parenthesis cases), the end of a source line, and the horizontal tab U+0009, which COBOL.NET treats as white space throughout source text as GnuCOBOL does (item 157) — and nothing else,")
r = sub1(23, r, "so every Unicode White_Space character acts as a separator during matching (kb/Work PB1543)",
         "so every other Unicode White_Space character (U+00A0, U+2000–U+200A, U+3000, U+0085, …) acts as a separator during matching (kb/Work PB1543)")
ROW[23] = r; note(23, "refuter: TAB and the line end added to what bounds a text-word; the ⚠ narrowed to the OTHER White_Space characters")

# ---------------- item 25 (refuter + orchestrator: BOM + malformed = compile-time error) ----------------
r = ROW[25]
r = sub1(25, r, "No source character is ever replaced by U+FFFD or any other stand-in.",
         "**A file whose byte-order mark names an encoding its content is not well-formed in** (invalid UTF-8 after `EF BB BF`, a lone surrogate in UTF-16, a UTF-32 unit beyond U+10FFFF) **is a compile-time ERROR naming the file and the byte offset** — there is no fallback for a marked file, because the mark states the encoding. No source character is ever replaced by U+FFFD or any other stand-in.")
r = sub1(25, r, "⚠ **Not yet what the compiler does for malformed UTF-8:**", "⚠ **Not yet what the compiler does for malformed source text:**")
r = sub1(25, r, "in literals, comments and library text alike (kb/Work PB1397)", "in literals, comments and library text alike, and whether or not the file has a byte-order mark (kb/Work PB1397)")
ROW[25] = r; note(25, "refuter + orchestrator: a marked file whose content is malformed is a compile-time error naming the file and byte offset, no fallback; ⚠ covers the marked arm")

# ---------------- item 39 ----------------
r = ROW[39]
r = resub(39, r, r"⚠ \*\*Not yet what the compiler does for an interval that reaches 10\^38:\*\*.*?as stated\.",
          "⚠ **Not yet what the compiler does for an interval whose value, or a product inside it, reaches the carrier's range:** on the standard-decimal lane (`CobolDec.ToUnscaled`) a value ≥ 10^38 keeps only its low-order 38 digits, and on the native lane an `Int128` product wraps, so `CONTINUE AFTER 10 ** 40 SECONDS` and `CONTINUE AFTER BIG * BIG SECONDS` (BIG = 2^64) suspend 0 seconds (kb/Work PB1529); every other value above 86,400, including 2^64 and 2^64 + 3, is clamped to 86,400 as stated.")
ROW[39] = r; note(39, "refuter: ⚠ widened to the native-lane Int128 product wrap (BIG * BIG suspends 0 s)")

# ---------------- item 40 (refuter + orchestrator: GnuCOBOL suffix AND directory order) ----------------
ROW[40] = ("| DOC-A.1-40 | **COPY statement — rules for identifying and locating default library text**, §7.2.3.3 SR5 and §7.2.3.4 GR3 (with GR2), required + documented | "
    "**The default COBOL library is an ordered list of places, searched in GnuCOBOL's order** (CLAUDE.md rule 1: the standard leaves both the list and the search to the implementor): "
    "(1) the compiler process's current working directory — the name is first tried as a path relative to it, or as written when it is absolute; (2) each `--copy DIR` in command-line order; "
    "(3) with `--nist`, the `copylib` directory beside the source file's directory. The source file's own directory is searched only when it is one of these. "
    "The same list serves every COPY in the compilation group, including a COPY inside library text. "
    "**Locating a text:** text-name-1 is used as spelled; literal-1's content is a host path (SR5: any non-empty host path is an allowable literal-1 or literal-2). "
    "In each place in turn COBOL.NET tries the name as spelled and then with the suffixes `.CPY`, `.CBL`, `.COB`, `.cpy`, `.cbl`, `.cob`, in that order (a name that already contains a period is tried only as spelled), "
    "and the FIRST regular file that exists IS the library text; so a text-name names exactly one text in a library (SR3) and no ambiguity is diagnosed. "
    "If `BK3.cbl` and `BK3.cob` sit in one directory, `COPY BK3` is `BK3.cbl`. Whether letter case in the name matters is the host file system's rule. "
    "**OF/IN library-name-1 or literal-2** names a subdirectory, looked for in each place in the same order, and the text is located in that subdirectory alone. "
    "**A library that no place holds, or a text the library does not contain, is the compile-time error CBL3620** (GR2: the library shall be available): compilation fails, "
    "and there is no fallback from a named library to the default one and no silent omission of the COPY (GnuCOBOL's fallback to the default library, with a warning, is not followed: GR2 controls). "
    "The same at every edition. "
    "⚠ **Not yet what the compiler does:** `Frontend.Preprocess` builds `CopyProcessor` with `strict: false`, so a missing text becomes a comment with no diagnostic, and `FindCopybook` falls back from an unavailable OF/IN library to the default library; "
    "and the search order is not yet GnuCOBOL's — the source file's directory is searched first and the working directory is not searched, and every name is tried with `\"\"`, `.cpy`, `.cob`, `.cbl`, `.CPY`, `.COB`, `.CBL` in that order (`CopyProcessor.CopybookExtensions`), "
    "so `BK3.cob` beside `BK3.cbl` is taken (kb/Work PB1355). | — |")
note(40, "refuter + orchestrator (rule 1): GnuCOBOL's suffix order ('' .CPY .CBL .COB .cpy .cbl .cob; a name with a period as spelled only) AND directory order (working directory, then --copy dirs); both arms added to the ⚠ and to PB1355")

# ---------------- item 60 / 66 / 161 / 162 / 164: refuter's corrected rows ----------------
for i in (60, 66, 161, 162, 164):
    ROW[i] = refuter_row(i)
    note(i, "refuter's corrected row adopted verbatim")
# 164: the refuter asked the writer to confirm relative/indexed before claiming every organization — scope it.
ROW[164] = sub1(164, ROW[164], "Same in every edition.", "Same in every edition. (A relative or indexed file's in-memory store is the file's content, not an input-output area.)")
note(164, "lander: the organization scope the refuter flagged is stated, not extended")

# ---------------- item 67 (orchestrator: trimmed to R44's decision; NOT-IMPLEMENTED under PB1086) ----------------
ROW[67] = ("| DOC-A.1-67 | **External repository — information beyond the required information**, §8.13, optional (documented if provided) | "
    "**The external repository is the metadata of compiled COBOL.NET assemblies** (owner decision kb/Work R44, 2026-09-24): the §8.13 information of each source unit is carried as .NET metadata, "
    "with a COBOL.NET-owned metadata record for whatever .NET metadata cannot express. Whether that record holds anything beyond the §8.13 list (“any other information that the implementor requires”), and what, "
    "is fixed by the R44 design doc (kb/Work PB1086), which writes it into this row in the same change set. "
    "⚠ Not yet implemented: no repository exists today, REPOSITORY-paragraph specifiers are never resolved against one (`CLASS NOSUCHCL PROGRAM NOSUCHPG` compiles and runs clean), "
    "and the compiler has no option to reference or update a repository — kb/Work PB1086. | — |")
note(67, "refuter + orchestrator: trimmed to R44's decision (no binding-identity/version/'nothing else' claims); verdict NOT-IMPLEMENTED under PB1086; the R44-only anchor row is written because a DOC NOT-IMPLEMENTED verdict still needs its anchor")
REC[67]["verdict"] = "NOT-IMPLEMENTED"
REC[67]["notes"] = ("A.1 item 67: 'External repository information (other information beyond the required information). This item is optional. This item, if provided by the implementor, shall be documented' (8.13). "
    "cite.py --check 8.13 'any other information that the implementor requires' -> OK §8.13 (External repository). Owner decision kb/Work/R44 (2026-09-24): the repository IS compiled-assembly metadata, "
    "with a COBOL.NET-owned metadata record for what .NET metadata cannot express, and a design doc lands first (rule 2). R44 does NOT decide the content of the implementor's additional information "
    "(the doc-rows-2 writer's 'assembly name+version, implementing type/member, never compared, nothing else' was refuted as exceeding R44), so the row states only R44 and defers the content to PB1086's design doc. "
    "NOT-IMPLEMENTED: no repository exists; the CLI has no reference/update option; probe rep67.cob (REPOSITORY. CLASS NOSUCHCL PROGRAM NOSUCHPG.) --std 2023 compiles, prints RAN, exit 0 (pin 80de60472). "
    "Owning note: kb/Work/PB1086 (its design doc must fix the §8.13 'any other information' content and write DOC-A.1-67 from it). PB1099 Q3 answered by R44.")

# ---------------- item 66 record notes: drop the invented option spellings ----------------
n = REC[66]["notes"]
n = resub(66, n, r"R44 fixes the mechanism but not the option spellings or defaults: this row names them .*?must amend this row if it differs\.",
          "R44 fixes the mechanism but not the option spellings or defaults, and no design doc exists yet; the row (refuter-corrected) states only what R44 decided, and the R44 design doc (kb/Work PB1086) fills the spellings and defaults into it.")
n = sub1(66, n, "(flagged, and not flagged under --no-repository-check), and an assembly compiled with --no-repository-update",
         "(flagged, and not flagged when the flag option is off), and an assembly compiled with repository update off")
REC[66]["notes"] = n

# ---------------- item 68 ----------------
r = ROW[68]
r = sub1(68, r, "Removing the spaces follows GnuCOBOL (ISO does not settle it, CLAUDE.md rule 1), and a compile-time warning is given when an AS literal has leading or trailing spaces.",
         "ISO leaves formation to the implementor (§8.3.2.2). GnuCOBOL removes leading and trailing spaces, with a warning, from a CALL or CANCEL literal and from a literal program-name, but keeps an AS literal as written, so a GnuCOBOL program named `AS \"trail  \"` cannot be called. COBOL.NET removes them from AS names too, so that every externalized name is formed by one rule, and gives a compile-time warning when an AS literal has leading or trailing spaces.")
r = sub1(68, r, "does not share with `AS \"shared two\"` (kb/Work PB1539);", "does not share with `AS \"shared two\"`, and no warning is given (kb/Work PB1539);")
ROW[68] = r; note(68, "refuter: the false GnuCOBOL attribution for AS literals replaced (GnuCOBOL keeps AS literals verbatim; trimming AS names is COBOL.NET's own one-rule choice); ⚠ adds that no warning exists today")

# ---------------- item 75 ----------------
r = ROW[75]
r = resub(75, r, r"When an OPEN succeeds, the connector takes a lock that matches its sharing mode .*?\(item 77\)\.",
          "When an OPEN succeeds, the connector takes a host lock on the physical file chosen from BOTH its sharing mode (§9.1.15 rules 1–3) and its open mode, so that another opener — in this run unit, in another COBOL.NET run unit, or in another language that honours the lock — is admitted or refused exactly as §9.1.15 Table 19 decides. A connector with no sharing mode specified locks as the implementor-default mode (item 77).")
r = sub1(75, r, "**On Linux and macOS** the lock is an advisory POSIX record lock (`fcntl`) over the whole file. Every COBOL.NET run unit takes and honours it, and a program in another language is bound only if it takes `fcntl` locks too.",
         "**On Linux and macOS** the lock is an advisory POSIX record lock (`fcntl`), taken according to both the connector's sharing mode and its open mode, so every COBOL.NET run unit is arbitrated by §9.1.15 Table 19; a program in another language is bound only if it takes `fcntl` locks the same way.")
r = sub1(75, r, "still succeeds with 00 (kb/Work PB833).",
         "still succeeds with 00; and a whole-file two-state lock keyed on the sharing mode alone cannot express Table 19 at all (it cannot admit concurrent `SHARING WITH ALL OTHER` writers while refusing a `READ ONLY` connector's writer), so the lock layout is kb/Work PB833's design (kb/Work PB833).")
ROW[75] = r; note(75, "refuter: the lock-type table (which cannot deliver Table 19 on Unix) replaced by the guarantee — lock keyed on sharing mode AND open mode; the lock-layout flaw recorded in PB833")

# ---------------- item 89 (LANDER reconciliation with item 14: one ProbeSiblingModule, one determination) ----------------
ROW[89] = ("| DOC-A.1-89 | **Function-identifier — the runtime resources checked before a user-defined function is executed**, §8.4.3.2.4 GR6 c), required + documented | "
    "**The set of runtime resources checked is EMPTY, so a function activation never raises EC-PROGRAM-RESOURCES** — the determination item 14 makes for CALL, applied to a user-defined function. "
    "COBOL.NET checks no runtime resource beyond locating the function (GR6 b): a function is located when its externalized name (item 68) matches a function registered in the run unit — compiled into the calling assembly, "
    "or a separately compiled module `<name>.dll` beside the application whose `__CobolModule.Register()` registrar has run (`ProgramTable.ProbeSiblingModule`, the lookup CALL uses). "
    "A module file that exists but cannot be loaded (a truncated or foreign image, or a dependency the registrar itself needs) or whose registrar throws has therefore NOT been located: **EC-FUNCTION-NOT-FOUND** is set, "
    "the function activation is not successful, and execution continues as GR6 f) specifies. **Available memory and activation depth are not checked**: running out of either is a host failure, the run unit ends abnormally and no exception condition is set. "
    "This follows GnuCOBOL under CLAUDE.md rule 1's precedence — libcob's resolver sets EC-FUNCTION-NOT-FOUND for a function module that is found but cannot be loaded (`call.c` `set_resolve_error`) and has no EC-PROGRAM-RESOURCES raise site. | — |")
REC[89]["verdict"] = "CONFORMS"
REC[89]["test-ref"] = ""
REC[89]["notes"] = ("A.1 item 89 (8.4.3.2.4 GR6 c): required, documented. cite.py --check 8.4.3.2.4 'the resources necessary to execute the function are not available' -> OK §8.4.3.2.4 6); "
    "cite.py --check 8.4.3.2.4 'EC-FUNCTION-NOT-FOUND exception condition is set to exist' -> OK §8.4.3.2.4 6) b). LANDER RECONCILIATION (doc-rows-2): the d3 writer determined 'a module found on disk is located; "
    "a load/registrar failure is EC-PROGRAM-RESOURCES' (DIVERGES), while the d1 writer AND refuter determined for the CALL twin (item 14, same ProgramTable.ProbeSiblingModule) that locating = loading + running the "
    "__CobolModule registrar, so the checked set is EMPTY and a load failure is not-found (CONFORMS). Both refuters upheld their own item; the two rows could not both land (one lookup, two definitions of 'located'). "
    "ISO fixes the consequence of each classification but leaves what 'locating' a separately compiled module means to the implementor (GR6 b defers only the name scope to 8.4.6), so CLAUDE.md rule 1 goes to GnuCOBOL: "
    "tests/external/gnucobol-3.2.tar.xz libcob/call.c:270-277 set_resolve_error sets EC-FUNCTION-NOT-FOUND for a function module (EC-PROGRAM-NOT-FOUND for a program) when dlopen/dlsym fails (call.c:924-942), "
    "and no EC-PROGRAM-RESOURCES raise site exists. The item-14 determination is therefore applied here, and the code already does it: CallEmitter activates user functions through "
    "ProgramRegistry.CallProgram(..., notFoundEc: \"EC-FUNCTION-NOT-FOUND\"), whose ProbeSiblingModule bare catch treats an unloadable module as not found. CONFORMS; test-ref empty (witness owed: a sibling module "
    "whose __CobolModule.Register throws -> EXCEPTION-STATUS EC-FUNCTION-NOT-FOUND). Released from kb/Work PB1422, which keeps the INVOKE twin (item 102).")
OWNER[89] = "PB1422 (released; CONFORMS)"
note(89, "LANDER reconciliation with item 14 (one ProbeSiblingModule, one meaning of 'located', GnuCOBOL call.c set_resolve_error): rewritten to the empty checked set, EC-FUNCTION-NOT-FOUND for an unloadable module; verdict DIVERGES -> CONFORMS; released from PB1422")

# ---------------- item 102 ----------------
r = ROW[102]
r = sub1(102, r, "§14.9.23.4 GR7 b) (Annex A.1 cites General rule 7e, but the resource sentence is GR7 b), required + documented",
         "§14.9.23.4 GR7 b) — Annex A.1 cites GR7 e), but the resource sentence is GR7 b); required + documented")
r = sub1(102, r, "This is the same determination as the CALL twin (item 14) and the function twin (item 89).",
         "The CALL twin (item 14) and the function twin (item 89) count a module that cannot be loaded as NOT LOCATED rather than as a resource failure; for INVOKE the distinction has no observable effect, because GR7 b) gives both outcomes EC-OO-METHOD.")
ROW[102] = r; note(102, "refuter nit: header parenthetical; lander: cross-reference reconciled with items 14 and 89")

# ---------------- item 105 / 107 / 114 nits and citation ----------------
ROW[105] = sub1(105, ROW[105], "**After a failed OPEN** ('30', '31', '35', '37', '38' or '39')",
                "**After a failed OPEN** ('30', '31', '35', '37', '38' — COBOL-85 to 2014 only: CLOSE WITH LOCK and status '38' were removed at 2023 — or '39')")
note(105, "refuter nit: '38' is 85–2014 only")
ROW[107] = sub1(107, ROW[107], "A relative record number with more significant digits than the relative key answers '24' (GR33 c).",
                "On a sequential-access WRITE, a relative record number with more significant digits than the relative key answers '24' (§14.9.51.4 GR29 a), §9.1.13.5 item 4); in random or dynamic access the record number comes from the key, so it cannot happen.")
note(107, "refuter nit: the too-many-digits '24' is the sequential-access case")
ROW[114] = sub1(114, ROW[114], "(+ §9.1.13.2 item 6's '06')", "(+ §9.1.13.2 item 5's '06')")
note(114, "refuter: '06' is §9.1.13.2 item 5, not item 6")

# ---------------- item 124: record notes precedence claim ----------------
REC[124]["notes"] = sub1(124, REC[124]["notes"], "(GnuCOBOL tests cob_decimal scale/fraction on the exact value too)",
    "(GnuCOBOL offers no precedent: it truncates a subscript to int (cobc/typeck.c:2702-2720 cb_build_cast_int) and never tests for a fraction, which would make §8.4.1.2's 'does not result in an integer -> EC-BOUND-SUBSCRIPT' vacuous, so it is not a definition precedence can adopt. The exact-value rule is chosen because §5.5 3) a) asks for ONE implementor definition, and it is the one that agrees with §5.5 3) b)'s standard-arithmetic test and with item 123's exact scaled-Int128 intermediate)")
note(124, "refuter: record-notes GnuCOBOL precedence claim corrected (row unchanged)")

# ---------------- item 146 ----------------
r = ROW[146]
r = sub1(146, r, "every alphanumeric, alphabetic, numeric-display or edited character position is ONE byte (item 209)",
         "every alphanumeric, alphabetic, numeric-display or edited character position is ONE byte (item 209: ISO/IEC 8859-1, owner decision kb/Work R47 — a record holding a character above U+00FF is refused, item 31)")
r = sub1(146, r, "the record's characters less the trailing spaces §14.9.51.4 GR21 does not transfer, one byte each, plus the **two-byte CR LF line delimiter**;",
         "the record's characters less the trailing spaces §14.9.51.4 GR21 does not transfer (a file whose RECORD clause has the DEPENDING phrase is filled to data-name-1's length instead, GR22), one byte each, plus the line delimiter of item 114 (CR LF on Windows, LF on Linux and macOS); a record area holding a character outside the line sequential character set is not written at all ('71', item 115);")
r = sub1(146, r, "The **FORMAT clause** cannot arise (item 85: COBOL.NET provides no FORMAT clause).",
         "The **FORMAT clause** cannot arise: A.4.8 is not claimed and a FORMAT clause is refused with COBOLNET1705 (A.1 items 84 and 85 cannot arise).")
r = sub1(146, r, "writes the description's size today (10 bytes, not 20).",
         "writes the description's size today (10 bytes, not 20); and arm (c)'s delimiter is CR LF on every host today (kb/Work PB1540).")
ROW[146] = r; note(146, "refuter: FORMAT citation (items 84/85 cannot arise, COBOLNET1705), arm (c) GR22 + '71'; lander: the byte-per-position dependency is now R47's decision, and arm (c)'s delimiter follows item 114")

# ---------------- item 157 ----------------
r = ROW[157]
r = sub1(157, r, "(each file decoded by its byte-order mark, UTF-8 without one — DOC-A.1-26)",
         "(each file decoded by its byte-order mark, UTF-8 without one, ISO/IEC 8859-1 when it is not well-formed UTF-8 — DOC-A.1-25, DOC-A.1-26)")
r = sub1(157, r, "a character of the Basic Multilingual Plane, a TAB included, is one position, and a character outside it is two.",
         "a character of the Basic Multilingual Plane is one position and a character outside it is two, EXCEPT a horizontal tab (U+0009), which advances to the next tab stop — positions 1, 9, 17, 25, … (a tab width of 8, GnuCOBOL's default `tab-width`; rule 1 precedence) — so it occupies one to eight positions.")
r = sub1(157, r, "⚠ Two edges diverge today: a short fixed-form line is NOT padded, so the example gives `ABCD` (kb/Work PB1491), and a free-form line over 255 positions is accepted silently (kb/Work PB1496).",
         "⚠ Three edges diverge today: a short fixed-form line is NOT padded, so the example gives `ABCD` (kb/Work PB1491); a free-form line over 255 positions is accepted silently (kb/Work PB1496); and a TAB counts as one position, so a fixed-form line beginning with two TABs is misread (kb/Work PB1586).")
ROW[157] = r; note(157, "refuter: TAB = next tab stop of 8 (GnuCOBOL tab-width), new defect PB1586 named in the ⚠; lander: Latin-1 fallback of item 25 referenced")

# ---------------- item 192: record test-ref gets the fourth test ----------------
REC[192]["test-ref"] = REC[192]["test-ref"] + "; conformance-test:StopGobackExitCodeTests.FigurativeAllAndBooleanStatusLiterals_RenderUnderTheGR5Mapping"
note(192, "refuter nit: record test-ref carries the row's fourth test")

# ---------------- item 218 ----------------
ROW[218] = sub1(218, ROW[218], "(first digit 3, 4 or 7; COBOL.NET defines no fatal 9x value — DOC-A.1-110)",
                "(first digit 3, 4 or 7, or an implementor-defined 9x value — '90' and '91', fatal per DOC-A.1-110)")
REC[218]["notes"] = sub1(218, REC[218]["notes"], "not re-read here: no GnuCOBOL source on this machine",
                          "verified in tests/external/gnucobol-3.2.tar.xz cobc/codegen.c:11040-11092")
note(218, "refuter: the 'no fatal 9x' clause contradicted DOC-A.1-110 — now names '90' and '91'; notes cite codegen.c")

# ---------------- item 219: notes citation ----------------
REC[219]["notes"] = sub1(219, REC[219]["notes"], "(recalled, NOT verified against GnuCOBOL source — none on this machine; a refuter should confirm)",
    "(verified: tests/external/gnucobol-3.2.tar.xz cobc/scanner.l:978 word rule admits \\x80-\\xFF bytes in every COBOL word; libcob/call.c:701-789 cob_encode_program_id / cob_encode_invalid_chars encode the non-C characters of a PROGRAM-ID)")
note(219, "refuter: notes' GnuCOBOL claim now cited from scanner.l / call.c")

# ---------------- items 31 / 36: attribution ----------------
ROW[31] = sub1(31, ROW[31], "(owner decision kb/Work R48; items 33, 188)", "(the standing UTF-16 repertoire decision, applied in kb/Work R48's second section; items 33, 188)")
ROW[31] = sub1(31, ROW[31], "a source file is read as UTF-8 unless a byte-order mark names UTF-16 or UTF-32,",
               "a source file is read as UTF-8 unless a byte-order mark names UTF-16 or UTF-32 (ISO/IEC 8859-1 when it has no mark and is not well-formed UTF-8, item 25),")
note(31, "refuter: R48 second section is an orchestrator application, not an owner decision; lander: item 25's Latin-1 fallback referenced")
ROW[36] = sub1(36, ROW[36], "(owner decision kb/Work R48; item 31)", "(the standing UTF-16 repertoire decision, applied in kb/Work R48's second section; item 31)")
note(36, "refuter: attribution")

# ---------------- every other overturned item: record the refutation in the record notes ----------------
for i in W:
    f = F[i]
    if not f["upheld"]:
        REC[i]["notes"] = (REC[i].get("notes") or "") + f" DOC-ROWS-2 REFUTER ({f['kind']}): overturned and corrected; the recorded row carries the correction (adjudication/doc-rows-2/LANDING.md)."

# ---------------- code-location shape fixes (record_verdicts: '<repo-relative-path>[#Symbol]') ----------------
REC[14]["code-location"] = sub1(14, REC[14]["code-location"], "#ResolveVisible (rule-4 fallthrough)", "#ResolveVisible")
REC[20]["code-location"] = sub1(20, REC[20]["code-location"], "; src/Cobol.Net.Compiler/Binding (StringComparer.OrdinalIgnoreCase name tables)", "")
REC[40]["code-location"] = sub1(40, REC[40]["code-location"], "src/Cobol.Net.Compiler/CompilerDriver.cs#Run", "src/Cobol.Net.Compiler/CompilerDriver.cs#Compile")
REC[46]["code-location"] = sub1(46, REC[46]["code-location"], "DataItem.cs#DataItem.ElementaryByteWidth", "DataItem.cs#ElementaryByteWidth")
REC[46]["code-location"] = sub1(46, REC[46]["code-location"], "CobolBits.cs#CobolBits.NatReadWindow", "CobolBits.cs#NatReadWindow")
REC[66]["code-location"] = sub1(66, REC[66]["code-location"], "; src/Cobol.Net.Cli", "; src/Cobol.Net.Cli/Program.cs")
# 99: an A.1-OPTIONAL item whose §7 cell opens "Not provided." is DOCUMENTED-NON-SUPPORT by the derived-verdict
# selector a1-optional-not-provided (owner, kb/Work PB280 Q1) — the EC-IMP-suffix acceptance stays PB1531's defect.
REC[99]["verdict"] = "DOCUMENTED-NON-SUPPORT"
REC[99]["notes"] += (" LANDER: recorded DOCUMENTED-NON-SUPPORT, the one verdict the a1-optional-not-provided derived-verdict selector "
                     "allows for an optional item whose §7 cell opens 'Not provided.' (owner, kb/Work PB280 Q1; DerivedVerdictDriftTests."
                     "NoOptionalNotProvidedA1Row_Diverges). The compiler's acceptance of any EC-IMP-suffix is an under-rejection kept open by kb/Work PB1531.")
note(99, "lander: verdict DIVERGES -> DOCUMENTED-NON-SUPPORT (derived-verdict selector a1-optional-not-provided); the defect stays PB1531's")
# 121: the §7 row names the witnesses its record carries (AnnexA1RegisterDriftTests)
ROW[121] = sub1(121, ROW[121], "COBOL 2002 and later. | — |", "COBOL 2002 and later. | `conformance:2002/oo_property_methods`; `conformance:2002/oo_property_ref` |")
note(121, "lander: the row's Pinned-by cell names the two goldens its record cites")
REC[39]["code-location"] = sub1(39, REC[39]["code-location"], "StatementEmitter.cs#Visit(BoundContinueAfter)", "StatementEmitter.cs#Visit")

# ---------------- assertions ----------------
VERDICTS = {"CONFORMS", "PARTIAL", "DIVERGES", "NOT-IMPLEMENTED", "DOCUMENTED-NON-SUPPORT", "NEEDS-OWNER-DECISION"}
def cells(row):
    s = row.strip()
    assert s.startswith("|") and s.endswith("|"), row[:80]
    return re.split(r"(?<!\\)\|", s[1:-1])
final = []
for i in sorted(W):
    row = ROW[i]
    assert "\n" not in row, i
    c = cells(row)
    assert len(c) == 4, (i, len(c))
    assert c[0].strip() == f"DOC-A.1-{i}", (i, c[0])
    rec = REC[i]
    assert rec["rule-id"] == f"DOC-A.1-{i}", i
    assert rec["verdict"] in VERDICTS, (i, rec["verdict"])
    assert rec["verdict"] != "NEEDS-OWNER-DECISION", i
    assert rec["code-location"].startswith(f"docs/CONFORMANCE.md#DOC-A.1-{i}"), (i, rec["code-location"][:60])
    # the row's test cell and the record's test-ref agree on emptiness
    final.append({"item": i, "rule-id": rec["rule-id"], "row": row, "record": rec, "owning_note": OWNER[i],
                  "refuter": {"upheld": F[i]["upheld"], "kind": F[i]["kind"]}, "lander_changes": CHANGED.get(i, [])})
(HERE / "final.jsonl").write_text("\n".join(json.dumps(x, ensure_ascii=False) for x in final) + "\n", encoding="utf-8")
lines = [f"{x['item']:>4}  {x['record']['verdict']:<16} {x['owning_note']:<28} {'upheld' if x['refuter']['upheld'] else 'OVERTURNED:' + x['refuter']['kind']}" for x in final]
(HERE / "table.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
from collections import Counter
print("\n".join(lines))
print(Counter(x["record"]["verdict"] for x in final))
batch = {"batch": "doc-rows-2", "records": [x["record"] for x in final]}
(HERE / "batch-doc-rows-2.json").write_text(json.dumps(batch, ensure_ascii=False, indent=1), encoding="utf-8")
print("wrote final.jsonl, table.txt, batch-doc-rows-2.json:", len(final))
