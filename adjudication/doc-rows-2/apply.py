#!/usr/bin/env python3
"""doc-rows-2 lander step 3: write final.jsonl's rows into docs/CONFORMANCE.md §7 (replace in place when the item
already has a row, else append in item order after the last §7 row), dedupe DOC-A.1-56, and apply the also_changes
edits to other rows and §3. Every edit asserts exactly one occurrence. Run from the worktree root."""
import json, pathlib, re, sys
HERE = pathlib.Path(__file__).resolve().parent
DOC = pathlib.Path("docs/CONFORMANCE.md")
raw = DOC.read_bytes()
assert not raw.startswith(b"\xef\xbb\xbf")
crlf = b"\r\n" in raw
text = raw.decode("utf-8").replace(chr(13) + chr(10), chr(10))
final = [json.loads(l) for l in (HERE / "final.jsonl").read_text(encoding="utf-8").splitlines() if l.strip()]

def sub1(s, old, new, tag):
    n = s.count(old)
    assert n == 1, (tag, n, old[:90])
    return s.replace(old, new)

def resub(s, pat, new, tag):
    s2, n = re.subn(pat, lambda _m: new, s, flags=re.S)
    assert n == 1, (tag, n, pat[:90])
    return s2

lines = text.split("\n")
start = next(i for i, l in enumerate(lines) if l.startswith("## 7. Annex A.1"))
end = next(i for i, l in enumerate(lines) if i > start and l.startswith("## 8."))
def rowidx():
    idx = {}
    for i in range(start, end):
        m = re.match(r"^\| DOC-A\.1-(\d+) \|", lines[i])
        if m:
            idx.setdefault(int(m.group(1)), []).append(i)
    return idx

# --- dedupe DOC-A.1-56: merge the second row's strongly-typed-group paragraph and witness into the first ---
idx = rowidx()
assert len(idx[56]) == 2, idx[56]
a, b = idx[56]
ra, rb = lines[a], lines[b]
para = rb[rb.index("⛔ **A strongly-typed group with a class pointer/object leaf**"):rb.index(" | `conformance:2002/pb148_display_forms`")]
ra = sub1(ra, "**DISPLAY statement — conversion of data**, §14.9.11 GR1,", "**DISPLAY statement — conversion of data**, §14.9.11.4 GR1,", "56a")
ra = sub1(ra, "The variable-length-group format (A.1 item 57) is kb/Work PB164's open half. |",
          "The variable-length-group format is A.1 item 57. " + para + " |", "56b")
ra = sub1(ra, "`conformance:2023/comp5_display_beyond_picture` |", "`conformance:2023/comp5_display_beyond_picture`; `conformance:2002/pb244_pointer_group_transfer` |", "56c")
lines[a] = ra
del lines[b]
end -= 1

# --- the 51 rows ---
idx = rowidx()
append = []
replaced = []
for x in sorted(final, key=lambda x: x["item"]):
    i = x["item"]
    if i in idx:
        assert len(idx[i]) == 1, (i, idx[i])
        lines[idx[i][0]] = x["row"]
        replaced.append(i)
    else:
        append.append(x["row"])
last = max(max(v) for v in idx.values())
lines[last + 1:last + 1] = append
text = "\n".join(lines)
print(f"replaced {replaced}; appended {len(append)}")

# --- also_changes on other rows ---
def row_of(n):
    m = re.search(r"^\| DOC-A\.1-%d \|.*$" % n, text, re.M)
    assert m, n
    return m

def edit_row(n, fn):
    global text
    rows = re.findall(r"^\| DOC-A\.1-%d \|.*$" % n, text, re.M)
    assert len(rows) == 1, (n, len(rows))
    old = rows[0]
    new = fn(old)
    assert new != old, n
    text = text.replace(old, new)

# 26 (item 25): the no-mark Latin-1 fallback and the marked-malformed error
edit_row(26, lambda r: sub1(r, "a file with no byte-order mark is read as UTF-8.",
    "a file with no byte-order mark is read as UTF-8 or, when it is not well-formed UTF-8, as ISO/IEC 8859-1 with a warning naming the file (item 25); a file whose mark names an encoding its content is not well-formed in is a compile-time error naming the file and the byte offset (item 25).", "26"))

# 62 (item 60): the table constant is not the carrier's-headroom rule
edit_row(62, lambda r: resub(r, r" — the same .the carrier's headroom IS the implementor maximum. rule a dynamic-capacity table's constant follows \(`CobolDynTable<T>\.MaxOccurrences`; item 60's own determination is not yet written — the table's runtime maximum is the open defect kb/Work PB1410\)\.",
    ". A dynamic-capacity table's maximum is NOT this rule: item 60's constant (`CobolDynTable<T>.MaxOccurrences`) is about half the .NET array-length limit, chosen so its doubling growth never overflows a 32-bit length, and its runtime resource bound is kb/Work PB1410.", "62"))

# 65 (item 167): the run-unit ending rule
edit_row(65, lambda r: sub1(r, "(a staged RAISING condition is never raised, because the host enables no checking). Every edition. |",
    "(a staged RAISING condition is never raised, because the host enables no checking). STOP RUN and a fatal exception condition end the run unit only and return to the host as item 167 states. Every edition. |", "65"))

# 76 (items 75, 77)
edit_row(76, lambda r: sub1(r, "How strongly the file lock excludes other run units depends on the host, and that is open under kb/Work PB833. The mode used when no sharing mode is specified (item 77) is open under kb/Work PB322.",
    "How the file lock excludes other run units and other languages is item 75 (open on Linux and macOS under kb/Work PB833). The mode used when no sharing mode is specified is item 77 (its implementation is open under kb/Work PB322).", "76"))

# 106 / 110 (item 31: '91')
edit_row(106, lambda r: sub1(r, "and its only implementor-defined I-O status is the fatal '90' (item 110).",
    "and its implementor-defined I-O statuses are the fatal '90' and '91' (item 110).", "106"))
def e110(r):
    r = sub1(r, "**COBOL.NET defines exactly ONE implementor-defined I-O status: `'90'`, the LINAGE value-rule violation of §13.18.34.4 GR6 b)**",
             "**COBOL.NET defines two implementor-defined I-O statuses. `'90'` is the LINAGE value-rule violation of §13.18.34.4 GR6 b)**", "110a")
    r = sub1(r, " via `FileStatusCode.LinageValueViolation`. |",
             " via `FileStatusCode.LinageValueViolation`. **`'91'` is a record character with no byte image in the file's coded character set** (owner decision kb/Work R47; item 31): the WRITE or REWRITE of a record area holding a character above U+00FF to a record sequential or report file with no CODE-SET clause is unsuccessful, nothing reaches the medium and the record area is unchanged (a line sequential file answers the standard's '71' instead, item 115). Like '90' it is FATAL (a '9' first digit, §9.1.13.1) and its status→EC correspondence is EC-I-O-IMP. No other status fits: §9.1.13.10 confines '71' to line sequential files; a '3x' permanent error would stay in effect for every later operation on the connector (item 105) although the next record may be writable; and every '0x', '4x' and '5x' value asserts a different condition. ⚠ Not yet implemented: the character is written as `?` with '00' (kb/Work PB690). |", "110b")
    return r
edit_row(110, e110)

# 115 (items 31, 114)
def e115(r):
    r = sub1(r, "**Every character whose code point is U+0020 (SPACE) or above is a member of the line sequential character set; the characters BELOW U+0020 — the C0 controls U+0000–U+001F, which include the two line delimiters CR U+000D and LF U+000A, and the tab U+0009 — are outside it.** DEL (U+007F) and everything above it, the whole Latin-1 supplement included, are members,",
             "**The members of the line sequential character set are the characters from U+0020 (SPACE) through U+00FF in an alphanumeric record area, and every character from U+0020 up in a national one; the characters BELOW U+0020 — the C0 controls U+0000–U+001F, among them LF U+000A (the line delimiter, with CR U+000D only as part of a CR LF pair — item 114) and the tab U+0009 — are outside it, and so is a character above U+00FF in an alphanumeric record area, which has no one-byte image (owner decision kb/Work R47; item 31).** DEL (U+007F) through U+00FF, the whole Latin-1 supplement included, are members,", "115a")
    r = sub1(r, "so a record area holding CR or LF cannot round-trip as one record.",
             "so a record area holding CR or LF cannot round-trip as one record (the delimiter is LF, with CR only as part of a CR LF pair — item 114; a lone CR read into a record area is record data and answers '09').", "115b")
    r = sub1(r, "**THE DERIVATION, IN FOUR STEPS.**", "**THE DERIVATION, IN FIVE STEPS.**", "115c")
    r = sub1(r, " **CHARACTERS, NOT BYTES.**",
             " (5) The ceiling U+00FF in an alphanumeric record area is the file's one-byte coded character set (owner decision kb/Work R47; item 31): a character above it has no byte image, so it cannot be written, and a national record area's characters are written as UTF-16BE and keep no such ceiling. ⚠ Not yet implemented for a character above U+00FF in an alphanumeric record area: it is written as `?` with '00' (kb/Work PB690). **CHARACTERS, NOT BYTES.**", "115d")
    return r
edit_row(115, e115)

# 143 (item 68)
edit_row(143, lambda r: sub1(r, "there is no CALL-CONVENTION directive and no way to declare a non-COBOL program (item 19),",
    "no call-convention-name is defined (only `>>CALL-CONVENTION COBOL` is accepted, item 68) and there is no way to declare a non-COBOL program (item 19),", "143"))

# 153 (item 131): the example's stale parenthetical
edit_row(153, lambda r: sub1(r, "(that two clause-less connectors can have the file open at the same time is the DEFAULT SHARING MODE, item 131, which is not yet determined — kb/Work PB322; the example shows the lock behaviour only)",
    "(it shows the lock behaviour only, as COBOL.NET runs it TODAY: under the item 131 determination a clause-less OPEN I-O establishes SHARING WITH NO OTHER and the second connector's OPEN is refused '61', so this example and its golden are re-derived when kb/Work PB322 lands)", "153"))

# 160 (item 159)
edit_row(160, lambda r: sub1(r, "⚠ Which bytes a national character becomes in the report FILE is the record-structure question (item 159), open under kb/Work PB690.",
    "Which bytes a national character becomes in the report FILE is item 159's record structure.", "160"))

# §3: >>CALL-CONVENTION and >>DISPLAY bullet, the non-COBOL return bullet, SORT/MERGE, D-FRA (iv)
text = sub1(text, "**>>CALL-CONVENTION**\n  (§7.3.9) — CALL uses the single .NET managed calling convention (a native-interop convention selector has no\n  target);",
    "**>>CALL-CONVENTION**\n  (§7.3.9) — CALL uses the single .NET managed calling convention, so only `>>CALL-CONVENTION COBOL` is accepted and\n  any other call-convention-name is an error (A.1 item 68 in §7; ⚠ today any word is accepted, kb/Work PB1383);", "s3-cc")
text = sub1(text, "displays the operands at compile time).", "displays the operands at compile time; A.1 items 53–55 in §7 state the determination).", "s3-disp")
text = sub1(text, "COBOL.NET declares no §7.3.9 CALL-CONVENTION directive, so a\n  non-COBOL element",
    "COBOL.NET defines no §7.3.9 call-convention-name (only `>>CALL-CONVENTION COBOL`), so a\n  non-COBOL element", "s3-noncobol")
text = sub1(text, "(COBOL.NET does, with\n  checking off)", "(COBOL.NET does, with\n  checking off, when the file has a FILE STATUS clause or a USE procedure applies — A.1 item 103 in §7)", "s3-sort")
text = resub(text, r"\(iv\) the implicit RECORD clause\n  \(§13\.18\.43\.4 GR5, \*\"defined by the implementor\"\*\)",
    "(iv) the implicit RECORD clause\n  (§13.18.43.4 GR5, *\"defined by the implementor\"*; A.1 item 147 in §7, for an FD and an SD entry alike)", "s3-dfra")

if len(sys.argv) > 1 and sys.argv[1] == "--fp210":
    pass
DOC.write_bytes((text.replace(chr(10), chr(13) + chr(10)) if crlf else text).encode("utf-8"))
print("also_changes applied")
