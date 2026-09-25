import pathlib, re, json
p = pathlib.Path("kb/Work/PB1522.md")
raw = p.read_bytes().decode("utf-8")
crlf = "\r\n" in raw
t = raw.replace("\r\n", "\n")
def sub1(old, new):
    global t
    assert t.count(old) == 1, old[:60]
    t = t.replace(old, new)
sub1("(26 overturned and corrected)", "(21 overturned and corrected)")
sub1("(DIVERGES 25 · PARTIAL 14 · CONFORMS 11 · NOT-IMPLEMENTED 1; GAP 1247 → 1241)",
     "(DIVERGES 24 · PARTIAL 14 · CONFORMS 11 · NOT-IMPLEMENTED 1 · DOCUMENTED-NON-SUPPORT 1; GAP 1247 → 1241)")
m = re.search(r'^inventory_rows: (\[.*?\])\s*$', t, re.M)
rest = json.loads(m.group(1))
t = t[:m.start(1)] + "[]" + t[m.end(1):]
t = re.sub(r"^status: open$", "status: landed", t, count=1, flags=re.M)
t = t.rstrip("\n") + "\n\n**LANDED — every inventory row is adjudicated** (4,347 rows, 0 verdict-less, measured after the doc-rows-2 batch). " \
    "The last rows this note claimed (" + ", ".join(rest) + ") carry their verdicts — the `a1-condition-absent` / " \
    "`a1-optional-not-provided` derived verdicts of train 62 and doc-rows-1 — and are released; every defective row is claimed " \
    "by its mechanism owner (DefectiveRowCoverageDriftTests).\n"
p.write_bytes((t.replace("\n", "\r\n") if crlf else t).encode("utf-8"))
print("PB1522 landed; released", rest)
