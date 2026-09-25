import pathlib
p = pathlib.Path("docs/CONFORMANCE.md")
raw = p.read_bytes().decode("utf-8")
crlf = "\r\n" in raw
t = raw.replace("\r\n", "\n")
def sub1(old, new):
    global t
    assert t.count(old) == 1, (t.count(old), old[:60])
    t = t.replace(old, new)
sub1("> **⛔ STATUS: INCOMPLETE — this is a known, registered v1.0 conformance gap, not an oversight.**\n",
     "> **STATUS: every documentation obligation is discharged (2026-09-25, doc-rows-2) — but a documented determination is\n"
     "> not always what the compiler does yet: a row whose ⚠ names a kb/Work note states the INTENDED behaviour, and its\n"
     "> traceability-inventory verdict (DIVERGES / PARTIAL / NOT-IMPLEMENTED) is held open by that note.**\n")
sub1("> **MEASURED 2026-09-24 — this register discharges 125 of the 182 in scope, and 57 obligations remain**;\n"
     "> 143 items carry a determination, eighteen of them (**35**, **61**, **69**, **90**, **92**, **104**, **118**,\n"
     "> **119**, **123**, **125**, **138**, **144**, **169**, **170**, **186**, **194**, **201**, **220**) documented\n"
     "> voluntarily where A.1 does not require it.",
     "> **MEASURED 2026-09-25 — this register discharges 182 of the 182 in scope, and 0 obligations remain**;\n"
     "> 205 items carry a determination, twenty-three of them (**35**, **60**, **61**, **63**, **69**, **81**, **90**, **92**,\n"
     "> **104**, **113**, **118**, **119**, **123**, **125**, **138**, **144**, **169**, **170**, **186**, **194**, **200**,\n"
     "> **201**, **220**) documented voluntarily where A.1 does not require it.")
sub1("> Completing this register is **PHASE-14 Step 0** work — the four-edition traceability inventory enumerates\n"
     "> every A.1 row and drives it to zero-GAP; do not attempt it piecemeal here. Items are added below as, and only\n"
     "> as, the compiler's behaviour for them is actually settled — an undocumented determination and a\n"
     "> wrongly-documented one are both non-conformance, and the second is worse.",
     "> Driving this register's rows to CONFORMS is **PHASE-14 Step 0** work — the four-edition traceability inventory\n"
     "> enumerates every A.1 row and drives it to zero-GAP. A row documents the INTENDED determination and never the\n"
     "> wrong behaviour (kb/Work PB1535): where today's compiler differs, the row's ⚠ says how and names the kb/Work\n"
     "> note that owns the fix — an undocumented determination and a wrongly-documented one are both non-conformance,\n"
     "> and the second is worse.")
p.write_bytes((t.replace("\n", "\r\n") if crlf else t).encode("utf-8"))
print("preamble ok")
