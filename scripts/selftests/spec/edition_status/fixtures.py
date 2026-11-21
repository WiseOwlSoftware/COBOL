"""The planted sources and register notes the edition status cases build (kb/Work PB5924): no case reads the real register."""
import selftest_cases

C = selftest_cases.subject(__file__)   # scripts/spec/edition_status.py

NAMES = ("inventory GAP", "code reads", "open work notes", "catalog prose holes")


def planted(*counts):
    """Four sources that answer `counts` (None: a source whose reader refuses)."""
    def source(name, n):
        def read(year):
            if n is None:
                raise SystemExit(f"{name}: planted refusal")
            return C.Part(name, n)
        return read
    return tuple(source(n, c) for n, c in zip(NAMES, counts))


def line(*counts):
    return C.verdict(1985, C.status(1985, planted(*counts)))


def note(ident, status="open", **fm):
    body = {"title": f'"{ident} - planted"', "id": ident, "kind": "defect", "status": status, **fm}
    return "---\n" + "".join(f"{k}: {v}\n" for k, v in body.items()) + f"---\n# {ident}\n"
