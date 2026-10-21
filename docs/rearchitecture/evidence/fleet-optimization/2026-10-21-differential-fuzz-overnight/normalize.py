"""Estimate how much of OUTPUT_DIFFERS is the DISPLAY latitude (sign form + implied decimal point).

GnuCOBOL's default DISPLAY of a numeric DISPLAY-usage item prints a leading separate sign for signed items
and inserts '.' at the implied decimal point. WiseOwl prints the storage form: digits only, the sign
overpunched on the last digit. Canonicalize BOTH sides to <sign><digits> (no '.', no overpunch) and compare.
"""
import json, re, sys, collections

OVER = {}
for i, c in enumerate("{ABCDEFGHI"):
    OVER[c] = ("+", str(i))
for i, c in enumerate("}JKLMNOPQR"):
    OVER[c] = ("-", str(i))

GNU_TOKEN = re.compile(r'(?<![\w.])([+-]?)(\d+)(?:\.(\d+))?(?![\w.])')
WE_TOKEN = re.compile(r'(?<![\w.])(\d*)([{}A-R])(?![\w])')


def canon_gnu(s):
    def f(m):
        sign, ip, fp = m.groups()
        return (sign or "") + ip + (fp or "")
    return "\n".join(GNU_TOKEN.sub(f, line) for line in s.splitlines())


def canon_we(s):
    def f(m):
        digits, z = m.groups()
        sign, d = OVER[z]
        return sign + digits + d
    out = []
    for line in s.splitlines():
        line = WE_TOKEN.sub(f, line)
        out.append(canon_gnu(line))
    return "\n".join(out)


def main(path):
    rows = [json.loads(l) for l in open(path, encoding="utf-8")]
    od = [r for r in rows if r["verdict"] == "OUTPUT_DIFFERS"]
    explained, residual = [], []
    for r in od:
        we, they = r["we"]["output"], r["they"]["output"]
        if canon_we(we).rstrip() == canon_gnu(they).rstrip():
            explained.append(r)
        else:
            residual.append(r)
    print(f"OUTPUT_DIFFERS {len(od)}: explained by DISPLAY latitude {len(explained)}, residual {len(residual)}")
    sig = collections.Counter(r["signature"] for r in residual)
    print("residual by signature:")
    for s, n in sig.most_common():
        first = next(r["id"] for r in residual if r["signature"] == s)
        print(f"{n:4d} {s:28s} {first}")
    json.dump([r["id"] for r in residual], open(sys.argv[2], "w"))


if __name__ == "__main__":
    main(sys.argv[1])
