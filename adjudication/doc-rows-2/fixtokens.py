import pathlib, re
p = pathlib.Path("docs/CONFORMANCE.md")
raw = p.read_bytes().decode("utf-8")
crlf = "\r\n" in raw
t = raw.replace("\r\n", "\n")
lines = t.split("\n")
start = next(i for i, l in enumerate(lines) if l.startswith("## 7. Annex A.1"))
end = next(i for i, l in enumerate(lines) if i > start and l.startswith("## 8."))
n = 0
for i in range(start, end):
    l = lines[i]
    m = re.match(r"^\| DOC-A\.1-\d+ \|", l)
    if not m:
        continue
    head, rest = l[:m.end()], l[m.end():]
    rest2, k = re.subn(r"DOC-A\.1-(\d+)", r"item \1", rest)
    if k:
        n += k
        lines[i] = head + rest2
# item 20's element must cite the clause A.1 cross-references (8.1.3)
old = "**Case mapping (compile-time, of the COBOL character repertoire)**, §8.1.3.2 GR1 with GR3 b) and GR4 b),"
assert sum(l.count(old) for l in lines) == 1
lines = [l.replace(old, "**Case mapping (compile-time, of the COBOL character repertoire)**, §8.1.3 (§8.1.3.2 GR1 with GR3 b) and GR4 b)),") for l in lines]
t = "\n".join(lines)
p.write_bytes((t.replace("\n", "\r\n") if crlf else t).encode("utf-8"))
print("replaced", n)
