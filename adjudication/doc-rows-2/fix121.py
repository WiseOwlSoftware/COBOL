import pathlib, re
p = pathlib.Path("docs/CONFORMANCE.md")
t = p.read_bytes().decode("utf-8")
rows = re.findall(r"^\| DOC-A\.1-121 \|.*$", t, re.M)
assert len(rows) == 1
old = rows[0]
assert old.rstrip("\r").endswith("COBOL 2002 and later. | — |"), old[-60:]
new = old.replace("COBOL 2002 and later. | — |", "COBOL 2002 and later. | `conformance:2002/oo_property_methods`; `conformance:2002/oo_property_ref` |")
t = t.replace(old, new)
p.write_bytes(t.encode("utf-8"))
print("121 ok")
