import re, sys, collections
t = open(sys.argv[1], encoding='utf-8', errors='replace').read()
sigs = re.findall(r'^### `([^`]+)` × (\d+) — first: (\S+) \(clause ([^)]*)\)', t, re.M)
print(len(sigs))
cls = collections.Counter()
for s, n, f, c in sigs:
    cls[s.split(':')[0]] += int(n)
print(cls)
for s, n, f, c in sorted(sigs, key=lambda x: -int(x[1])):
    print(f"{int(n):4d} {s:50s} {f} {c[:70]}")
