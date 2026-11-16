"""The planted rows, reads and tree the code reads' cases build (kb/Work PB5759): no case reads a real store."""
import json

import selftest_cases
from scratch_repo import ScratchRepo, scratch_dir

C = selftest_cases.subject(__file__)   # scripts/spec/code_reads.py
SCHEMA = C.inventory_schema.load_schema()
ED = SCHEMA.edition(1985)
KEPT = SCHEMA.next.chained.name

#: Planted rows of 1985: three KEPT rows (two witnessed), one amendment rule, and one row with a verdict of its own.
CITES = {"1985-GR-XII.2.4-8": "§1985:XII.2.4 GR8", "1985-GR-XII.2.4-12": "§1985:XII.2.4 GR12",
         "1985-SR-VI.5.8.3-1": "§1985:VI.5.8.3 SR1", "1989-AR-A.2.2-1": "§1989:A.2.2 AR1",
         "1985-GR-XII.2.4-9": "§1985:XII.2.4 GR9"}
GR8, GR12, SR1, AR1, OWN = CITES


def rows(digest="0b2e58f8a250"):
    def row(rid, kept=True, witnessed=True):
        return {"rule-id": rid, "rule-digest": digest, "citation": CITES[rid],
                "next": {"kind": KEPT, "to": ["GR-7.2.2.5-3"]} if kept else {},
                "verdict": "" if kept else "CONFORMS",
                "test-ref": "conformance:cases/x/y" if witnessed else ""}
    return [row(GR8), row(GR12), row(SR1, witnessed=False), row(AR1), row(OWN, kept=False)]


def note(ident, status="open"):
    """The text of a kb/Work note with the status `status`: the frontmatter the register's reader (`work.read_note`) reads."""
    return f'---\ntitle: "{ident} - planted"\nid: {ident}\nkind: defect\nstatus: {status}\n---\n# {ident}\n'


#: The C# file every planted read names (`read`'s `code-location`), as the pin holds it: `BindEntry` in its code, and a
#: name only its comment holds.
CODE_FILE = "src/WiseOwl.Cobol.Compiler/Binding/DataBinder.cs"
LOCATION = f"{CODE_FILE}#BindEntry"
CODE = ("namespace WiseOwl.Cobol.Binding;\n// OnlyInAComment\npublic sealed partial class DataBinder\n{\n"
        "    internal void BindEntry() { }\n}\n")


def tree():
    """A scratch repository with one commit (the pin): an OPEN kb/Work note PB1, a tracked adjudication file and the code
    file the planted reads name (`CODE_FILE`)."""
    repo = ScratchRepo.init(scratch_dir("code-reads-") / "repo")
    repo.commit("the tree", {"kb/Work/PB1.md": note("PB1"), "kb/Work/PB1/adj.md": "the adjudication\n", CODE_FILE: CODE})
    return repo, repo.git("rev-parse", "HEAD").strip()


def context(root, digest="0b2e58f8a250"):
    return C.Context(ED, SCHEMA, {r["rule-id"]: r for r in rows(digest)}, root)


def read(rid, verdict="CONFORMS", **kw):
    """A reader's record as the reader brief writes it."""
    base = {"rule-id": rid, "verdict": verdict, "requirement": "the compiler must do what the rule says",
            "code-location": LOCATION,
            "edition-consulted": [{"site": "src/X.cs#Y", "switches": "the arm", "same-arm-as-latest": True}],
            "edition-search": "", "behaviour-differs-by-edition": "no", "witnesses-cover": "all of it",
            "probes": [{"class": "b", "file": "p.cob", "printed": "X=3", "expected": "X=3"}],
            "citations": [{"cite": CITES.get(rid, "§1985:XII.2.4 GR8"), "check": "OK"}],
            "notes": "planted", "seeded": True, "harm": "", "cause": "", "at-2023": "", "repro": "", "expected": "",
            "observed": "", "known_note": ""}
    if verdict != "CONFORMS":
        base.update({"harm": "wrong-answer", "cause": "a", "repro": "r.cob", "expected": "X=3", "observed": "X=4",
                     "known_note": "none"})
    return {**base, **kw}


def refute(rid, refutation="UPHELD", **kw):
    return {"rule-id": rid, "refutation": refutation, "classes": "a,c", "evidence": "planted",
            "edition-sites-missed": [], "corrected": "" if refutation == "UPHELD" else "PARTIAL: planted", **kw}


def plant(directory, name, reads, refutes=None, edition=1985):
    """Write the slice `name`: the reader's file and, when `refutes` is given, the refuter's."""
    directory.mkdir(parents=True, exist_ok=True)
    (directory / f"{name}.json").write_text(json.dumps({"edition": edition, "records": reads}), encoding="utf-8", newline="\n")
    if refutes is not None:
        (directory / f"{name}-refute.json").write_text(json.dumps({"cases": refutes}), encoding="utf-8", newline="\n")


def refused(fn, *needles):
    """Does `fn` raise a SystemExit whose message holds every needle?"""
    try:
        fn()
    except SystemExit as e:
        return all(n in str(e.code) for n in needles)
    return False
