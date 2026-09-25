import pathlib, re
p = pathlib.Path("kb/Work/PB833.md")
raw = p.read_bytes().decode("utf-8")
old = 'inventory_rows: ["GR-9.1.15-2", "DOC-A.1-52", "DOC-A.1-75"]'
assert raw.count(old) == 1
raw = raw.replace(old, 'inventory_rows: ["GR-9.1.15-2", "DOC-A.1-75"]')
nl = "\r\n" if "\r\n" in raw else "\n"
raw = raw.rstrip() + nl + nl + ("DOC-A.1-52 (which devices allow concurrent access: all of them) is CONFORMS on "
    "`conformance:2002/l1c11_sharing_every_device_shareable` and scopes the lock strength out to DOC-A.1-75; it is released." ) + nl
p.write_bytes(raw.encode("utf-8"))
print("PB833 ok")
