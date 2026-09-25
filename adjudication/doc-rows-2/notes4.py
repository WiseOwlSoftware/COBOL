import pathlib, re
p = pathlib.Path("kb/Work/PB1522.md")
raw = p.read_bytes().decode("utf-8")
crlf = "\r\n" in raw
t = raw.replace("\r\n", "\n")
def sub1(old, new):
    global t
    assert t.count(old) == 1, old[:70]
    t = t.replace(old, new)
sub1("status: landed\n", "status: open\n")
sub1('inventory_rows: []\n', 'inventory_rows: ["DOC-A.1-201"]\n')
sub1("**LANDED — every inventory row is adjudicated** (4,347 rows, 0 verdict-less, measured after the doc-rows-2 batch). "
     "The last rows this note claimed (DOC-A.1-13, DOC-A.1-15, DOC-A.1-30, DOC-A.1-34, DOC-A.1-37, DOC-A.1-88, DOC-A.1-96, DOC-A.1-98, DOC-A.1-142, DOC-A.1-201) carry their verdicts",
     "**Every inventory row is adjudicated** (4,347 rows, 0 verdict-less, measured after the doc-rows-2 batch). "
     "The rows this note still claimed (DOC-A.1-13, DOC-A.1-15, DOC-A.1-30, DOC-A.1-34, DOC-A.1-37, DOC-A.1-88, DOC-A.1-96, DOC-A.1-98, DOC-A.1-142) carry their verdicts")
sub1("and are released; every defective row is claimed by its mechanism owner (DefectiveRowCoverageDriftTests).\n",
     "and are released; every defective row is claimed by its mechanism owner (DefectiveRowCoverageDriftTests). "
     "**One row keeps this note open: DOC-A.1-201** (PARTIAL — parameterized-class expansion runs after REPLACE; the golden "
     "`2002/pb759_parameterized_class` pins the expansion, but the before/after ordering edge is witnessed only by the "
     "probes r201a/r201b/pc2, and its recorded owner PB1535 has landed). Its closure is a committed witness of that ordering "
     "(a REPLACE applied to the definition before expansion, and one matching only post-expansion text left unapplied); "
     "the doc-rows-2 lander's first attempt to land this note turned DefectiveRowCoverageDriftTests red on exactly this row.\n")
p.write_bytes((t.replace("\n", "\r\n") if crlf else t).encode("utf-8"))
print("PB1522 kept open on DOC-A.1-201")
