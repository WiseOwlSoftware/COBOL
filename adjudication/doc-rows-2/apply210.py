import pathlib
p = pathlib.Path("docs/CONFORMANCE.md")
t = p.read_bytes().decode("utf-8")
old = ("**ALIGNMENT: none.** A function-pointer occupies NO character positions and is never part of a group's §13.18.60.4 GR4 character image — it rides a MANAGED SLOT (`StorageForm.FunctionPointerRef`, image width 0), the identical posture `POINTER` and `PROGRAM-POINTER` already have — so there is no boundary for it to be aligned on and §13.18.60.3 SR14 confines it to a level-1/77 elementary item or a STRONG type declaration, where no sibling can be displaced by it.")
new = ("**ALIGNMENT: none** (the item 205 rule). §13.18.60.3 SR14 confines a function-pointer to a level-1/77 elementary item or an elementary item of a STRONG type declaration. Its value rides a MANAGED SLOT (`StorageForm.FunctionPointerRef`), and inside a strongly-typed group it occupies 8 character positions whose character image is the D-SLOT placeholder — eight SPACES, whether the pointer is NULL or not — exactly as a `POINTER` or `PROGRAM-POINTER` leaf does (DOC-A.1-56): `01 G TYPEDEF STRONG` of `PIC X(3)`, a `FUNCTION-POINTER TO` leaf and `PIC X(2)` has `FUNCTION LENGTH` 13 and displays `abc` + eight spaces + `zz` before and after `SET … TO ADDRESS OF FUNCTION`.")
assert t.count(old) == 1, t.count(old)
t = t.replace(old, new)
p.write_bytes(t.encode("utf-8"))
print("210 ok")
