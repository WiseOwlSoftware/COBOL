"""docchecks/register.py — docs/CONFORMANCE.md's registers (§2 Annex A.3, §5 Annex A.4, §7 Annex A.1, §8 derivations),
the README that summarizes them, and the conformance suite's profile (kb/Work PB4453).

Four Unit tests and one Conformance test read these registers as Markdown (AnnexA3RegisterDriftTests,
ReadmeDeclinedSummaryDriftTests, ConformanceSuiteProfileDriftTests, DeclinedFacilityAnnexDriftTests, and the
ConformanceRegister reader the inventory tests share). No compiler test opens a Markdown file now: the checks that are
about the documents alone live here, and the rows the compiler tests need are the EXTRACT
`tests/version-matrix/conformance-register.json`, written from the Markdown by `doc_audits.py --write-register` and held
fresh by `register_extract_fresh` (a stale extract is a finding), so a change to a register row reaches the tests that
depend on it and a change to the prose around it does not.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

from . import common  # first: it puts scripts/spec on sys.path
from .common import Check, need

import inventory_schema as schema  # noqa: E402

A3_HEADING = "## 2. Annex A.3"
A4_HEADING = "## 5. Annex A.4"
EXTRACT_PATH = schema.REGISTER_EXTRACT_REL
PROFILE_PATH = "conformance-suite/profiles/wiseowl.json"
SCHEMA_PATH = schema.SCHEMA_REL
EDITIONS = ["85", "2002", "2014", "2023"]
CODE = re.compile(r"COBOL[0-9]{4}")
#: An unresolved citation: a clause path whose last component is the literal `x` standing in for the part nobody derived.
PLACEHOLDER = re.compile(r"[0-9]\.x(?![0-9A-Za-z])", re.IGNORECASE)
A3_REFERENCE = re.compile(r"A\.3 items? (?P<list>[0-9]+(?:–[0-9]+)?(?:(?:,| and) [0-9]+(?:–[0-9]+)?)*)")
A3_ITEM = re.compile(r"[0-9]+(?:–[0-9]+)?")
A4_REFERENCE = re.compile(r"A\.4\.[0-9]+")
#: Annex A.3 lists 46 numbered items (ISO Annex A.3), and §2 documents every one (§2023:4.2.6).
A3_ITEMS = range(1, 47)
#: The characters of a §7 determination the extract keeps. The C# selector (`DerivedVerdictDriftTests`, `said.StartsWith(p)`)
#: reads this lead while the Python engine (`inventory_schema.DerivedSelector`) reads the whole determination, so a schema
#: `determination-prefix` longer than the lead would leave the C# half testing nothing, with no failure:
#: `lead_covers_schema_prefixes` fails the audit instead (kb/Work PB4475). The test files' own `StartsWith` literals bind the
#: same way and fail in their own test.
LEAD_LENGTH = 64


def a3_rows(conformance: str) -> list[dict[str, str]]:
    """§2's table: the A.3 item cell (which may name a range, `28–30`), the element, the § citation, the disposition and
    the note. Stops at the next `## ` heading, so a pipe table elsewhere is never mistaken for the register."""
    rows = []
    for line in common.section(conformance, A3_HEADING).split("\n"):
        if not line.startswith("| "):
            continue
        cells = line.strip().strip("|").split("|")
        if len(cells) < 5:
            continue
        items = cells[0].strip()
        if items == "A.3 #" or items.startswith("---"):
            continue
        rows.append({"items": items, "element": cells[1].strip(), "section": cells[2].strip(),
                     "disposition": cells[3].strip(), "note": "|".join(cells[4:]).strip()})
    return rows


def a4_rows(conformance: str) -> list[dict[str, str]]:
    """§5's rows, one per `| A.4.` line: the module's clause and its disposition."""
    rows = []
    for line in common.section(conformance, A4_HEADING).split("\n"):
        if not line.startswith("| A.4."):
            continue
        cells = line.strip().strip("|").split("|")
        if len(cells) >= 4:
            rows.append({"clause": cells[0].strip(), "disposition": cells[2].strip()})
    return rows


def codes_under(conformance: str, heading: str) -> list[str]:
    return sorted(set(CODE.findall(common.section(conformance, heading))))


def is_claimed(disposition: str) -> bool:
    return disposition.lstrip("* ").startswith("Claimed")


def claims_support(disposition: str) -> bool:
    d = disposition.lstrip("* ")
    return d.startswith("Claimed") or d.startswith("Partial")


# ── the extract the compiler tests read ───────────────────────────────────────────────────────────────────────────

def build_extract(conformance: str, derivation_heading: str, derivation_header: str) -> dict:
    """The register rows as data: §2, §5, §7 (the A.1 determinations) and §8 (the owner-signed derivations), and the
    diagnostic codes §2 and §5 name."""
    return {
        "comment": "GENERATED from docs/CONFORMANCE.md by `python scripts/doc_audits.py --write-register`; held fresh by "
                   "the DOC AUDITS gate audit. Do not edit: edit the register and regenerate (kb/Work PB4453). annexA1 "
                   f"carries each §7 row's key and the first {LEAD_LENGTH} characters of its determination, enough for "
                   "every schema determination-prefix; the prose behind it is not data and is not here.",
        "annexA1": [{"key": r.key, "lead": schema.plain(r.determination)[:LEAD_LENGTH]}
                    for r in schema.section7_rows(conformance)],
        "derivations": [{"key": r.key, "arm": r.arm, "names": r.names, "signature": r.signature}
                        for r in schema.derivation_rows(conformance, derivation_heading, derivation_header)],
        "declinedCodes": {"A3": codes_under(conformance, A3_HEADING), "A4": codes_under(conformance, A4_HEADING)},
    }


def render_extract(extract: dict) -> str:
    return json.dumps(extract, indent=1, ensure_ascii=False) + "\n"


def _derivation_arguments(root: Path) -> tuple[str, str]:
    raw = json.loads(common.text(root, SCHEMA_PATH))["derivation"]
    return raw["register-heading"], raw["register-header-cell"]


def current_extract(root: Path) -> str:
    return render_extract(build_extract(common.text(root, common.CONFORMANCE), *_derivation_arguments(root)))


def write_extract(root: Path) -> None:
    (root / EXTRACT_PATH).write_text(current_extract(root), encoding="utf-8", newline="\n")


def schema_prefixes(node: object) -> list[str]:
    """Every `determination-prefix` entry anywhere in the inventory schema (a selector's arm names them)."""
    if isinstance(node, dict):
        return [p for key, value in node.items()
                for p in (value if key == "determination-prefix" else schema_prefixes(value))]
    if isinstance(node, list):
        return [p for value in node for p in schema_prefixes(value)]
    return []


def prefixes_over_lead(prefixes: list[str], lead_length: int = LEAD_LENGTH) -> list[str]:
    return [f"{SCHEMA_PATH}: the determination-prefix '{p}' is {len(p)} characters, over the extract's LEAD_LENGTH {lead_length} "
            f"({EXTRACT_PATH} keeps only that many of each §7 determination, so the C# selector would test nothing for it): "
            "raise LEAD_LENGTH in scripts/docchecks/register.py and regenerate the extract" for p in prefixes if len(p) > lead_length]


def lead_covers_schema_prefixes(root: Path) -> list[str]:
    return prefixes_over_lead(schema_prefixes(json.loads(common.text(root, SCHEMA_PATH))))


def register_extract_fresh(root: Path) -> list[str]:
    committed = common.text(root, EXTRACT_PATH) if (root / EXTRACT_PATH).is_file() else ""
    if committed != current_extract(root):
        return [f"{EXTRACT_PATH} is out of step with {common.CONFORMANCE}: regenerate it with "
                "`python scripts/doc_audits.py --write-register` (the compiler tests read the extract, not the register)"]
    return []


# ── §2 shape (AnnexA3RegisterDriftTests) ──────────────────────────────────────────────────────────────────────────

def a3_covers_every_item(rows: list[dict[str, str]]) -> list[str]:
    """§2023:4.2.6: the implementor shall document every processor-dependent element, claimed or absent."""
    findings: list[str] = []
    need(findings, len(rows) >= 40, f"{common.CONFORMANCE} §2 parsed to only {len(rows)} row(s): the table scanner is broken")
    covered: set[int] = set()
    for r in rows:
        covered.update(int(n) for n in re.findall(r"[0-9]+", r["items"]))
        m = re.fullmatch(r"([0-9]+)\s*[–—-]\s*([0-9]+)", r["items"])
        if m:
            covered.update(range(int(m.group(1)), int(m.group(2)) + 1))
    missing = [n for n in A3_ITEMS if n not in covered]
    need(findings, not missing, f"{common.CONFORMANCE} §2 has no row for Annex A.3 item(s) {', '.join(map(str, missing))}")
    return findings


def a3_cites_resolved_clauses(rows: list[dict[str, str]]) -> list[str]:
    return [f"{common.CONFORMANCE} §2 item {r['items']} ({r['element']}) cites '{r['section']}', an unresolved clause "
            "placeholder: derive the clause from specs/ISO_COBOL_2023.md and validate it with `python scripts/spec/cite.py "
            "--check`" for r in rows if PLACEHOLDER.search(r["section"])]


def a3_codes_are_emitted(rows: list[dict[str, str]], emitted: set[str]) -> list[str]:
    """Every COBOL#### §2 names is a code some source file spells (a row that cites a diagnostic tells the reader how to
    OBSERVE the disposition, so a stale code turns an observable claim into an unobservable one)."""
    findings: list[str] = []
    need(findings, len(emitted) >= 200, f"only {len(emitted)} diagnostic code(s) found in src/: the source scan is broken")
    cited = [(r["items"], c) for r in rows for c in CODE.findall(r["note"] + " " + r["disposition"])]
    need(findings, len(cited) >= 4, f"only {len(cited)} diagnostic code(s) found in §2: the table scanner is broken")
    findings += sorted({f"{common.CONFORMANCE} §2 item {item} names {c}, which nothing in src/ spells"
                        for item, c in cited if c not in emitted})
    return findings


def source_codes(root: Path) -> set[str]:
    found: set[str] = set()
    for rel in common.tracked(root, "src/*.cs"):
        if "/obj/" not in rel and "/bin/" not in rel:
            found.update(CODE.findall(common.text(root, rel)))
    return found


# ── the README's Status section against §2 and §5 (ReadmeDeclinedSummaryDriftTests) ───────────────────────────────

def readme_status(readme: str) -> str:
    out: list[str] = []
    inside = False
    for line in readme.split("\n"):
        if line == "## Status":
            inside = True
        elif inside and line.startswith("## "):
            break
        elif inside:
            out.append(line)
    return "\n".join(out)


def readme_disagreements(status: str, a3: list[dict[str, str]], a4: list[dict[str, str]]) -> list[str]:
    named_a3 = {c for m in A3_REFERENCE.finditer(status) for c in A3_ITEM.findall(m.group("list"))}
    named_a4 = {m.group(0) for m in A4_REFERENCE.finditer(status)}
    declined_a3 = {r["items"] for r in a3 if not is_claimed(r["disposition"])}
    problems: list[str] = []
    for r in a3:
        if not is_claimed(r["disposition"]) and r["items"] not in named_a3:
            problems.append(f"§2 item {r['items']} ({r['element']}) is '{r['disposition']}' and the README does not name "
                            f"'A.3 item {r['items']}'")
    for item in sorted(i for i in named_a3 if i not in declined_a3):
        problems.append(f"the README names 'A.3 item {item}', which is no row of §2 that declines anything (a claimed row, "
                        "or not a row's item cell as §2 writes it)")
    for r in a4:
        if not is_claimed(r["disposition"]) and r["clause"] not in named_a4:
            problems.append(f"§5 {r['clause']} is '{r['disposition']}' and the README does not name it")
        if is_claimed(r["disposition"]) and r["clause"] in named_a4:
            problems.append(f"the README names {r['clause']}, which §5 claims")
    return problems


def readme_matches_registers(conformance: str, readme: str) -> list[str]:
    a3, a4, status = a3_rows(conformance), a4_rows(conformance), readme_status(readme)
    findings: list[str] = []
    need(findings, len(a3) >= 40, f"§2 parsed to {len(a3)} row(s); the scanner is broken")
    need(findings, len(a4) >= 13, f"§5 parsed to {len(a4)} row(s); the scanner is broken")
    need(findings, sum(not is_claimed(r["disposition"]) for r in a3) >= 10
         and sum(not is_claimed(r["disposition"]) for r in a4) >= 5,
         "too few declined rows parsed: the disposition reading is broken and the check would pass vacuously")
    need(findings, bool(status), "README.md has no '## Status' section to check")
    return findings + [f"README.md's Status section disagrees with {common.CONFORMANCE} §2/§5: {p}"
                       for p in readme_disagreements(status, a3, a4)]


# ── the conformance suite's profile against §2 and §5 (ConformanceSuiteProfileDriftTests) ─────────────────────────

def item_names(items: str) -> list[str]:
    ends = items.split("–")
    first = int(ends[0].strip())
    last = int(ends[1].strip()) if len(ends) > 1 else first
    return [f"A.3-{n}" for n in range(first, last + 1)]


def profile_disagreements(conformance: str, profile: dict) -> list[str]:
    a3, a4 = a3_rows(conformance), a4_rows(conformance)
    findings: list[str] = []
    need(findings, len(a3) >= 40, f"{common.CONFORMANCE} §2 parsed to {len(a3)} row(s); the scanner is broken")
    need(findings, len(a4) >= 13, f"{common.CONFORMANCE} §5 parsed to {len(a4)} row(s); the scanner is broken")
    need(findings, sum(claims_support(r["disposition"]) for r in a3) >= 20
         and sum(not claims_support(r["disposition"]) for r in a3) >= 5,
         "too few claiming or declining §2 rows: the disposition reading is broken")
    want_modules = {r["clause"] for r in a4 if claims_support(r["disposition"])}
    want_elements = {n for r in a3 if claims_support(r["disposition"]) for n in item_names(r["items"])}
    modules, elements = set(profile["modules"]), set(profile["elements"])
    findings += [f"§5 claims {m}; the profile's modules do not" for m in sorted(want_modules - modules)]
    findings += [f"the profile claims {m}, which §5 does not claim" for m in sorted(modules - want_modules)]
    findings += [f"§2 claims {e}; the profile's elements do not" for e in sorted(want_elements - elements)]
    findings += [f"the profile claims {e}, which §2 does not claim" for e in sorted(elements - want_elements)]
    if profile["editions"] != EDITIONS:
        findings.append(f"the profile supports {profile['editions']}, not every edition {EDITIONS}")
    return [f"{PROFILE_PATH} disagrees with {common.CONFORMANCE} §2/§5 (the registers are the authority): {f}"
            for f in findings]


# ── the checks ────────────────────────────────────────────────────────────────────────────────────────────────────

def _a3(root: Path) -> list[str]:
    rows = a3_rows(common.text(root, common.CONFORMANCE))
    return a3_covers_every_item(rows) + a3_cites_resolved_clauses(rows) + a3_codes_are_emitted(rows, source_codes(root))


def _readme(root: Path) -> list[str]:
    return readme_matches_registers(common.text(root, common.CONFORMANCE), common.text(root, common.README))


def _profile(root: Path) -> list[str]:
    return profile_disagreements(common.text(root, common.CONFORMANCE), json.loads(common.text(root, PROFILE_PATH)))


CHECKS = (
    Check("ANNEX A.3 REGISTER (§2 covers every item, cites resolved clauses, names emitted codes)", _a3),
    Check("README STATUS against §2/§5", _readme),
    Check("CONFORMANCE SUITE PROFILE against §2/§5", _profile),
    Check("CONFORMANCE REGISTER EXTRACT is fresh", register_extract_fresh),
    Check("CONFORMANCE REGISTER EXTRACT lead covers every schema determination-prefix", lead_covers_schema_prefixes),
)


# ── the self-test: every guard fails on a planted case, for its own reason ────────────────────────────────────────

def _fabricated_register() -> str:
    a3 = "\n".join(f"| {n} | element {n} | 13.18.{n} | **Claimed** | note |" for n in range(1, 47))
    return (f"{A3_HEADING} x\n\n| A.3 # | Element | § | Disposition | Note |\n| --- | --- | --- | --- | --- |\n{a3}\n\n"
            f"## 3. next\n\n{A4_HEADING} x\n\n| A.4.1 | m | Not claimed | n |\n| A.4.2 | m | **Claimed** | n |\n")


def self_test() -> list[str]:
    reg = _fabricated_register()
    rows = a3_rows(reg)
    short = reg.replace("| 7 | element 7 | 13.18.7 | **Claimed** | note |\n", "")
    placeholder = reg.replace("| 9 | element 9 | 13.18.9 |", "| 9 | element 9 | 12.4.x |")
    ranged = reg.replace("| 28 | element 28 |", "| 28–30 | element 28 |").replace("| 29 | element 29 | 13.18.29 | **Claimed** | note |\n", "") \
        .replace("| 30 | element 30 | 13.18.30 | **Claimed** | note |\n", "")
    declined = reg.replace("| 3 | element 3 | 13.18.3 | **Claimed** |", "| 3 | element 3 | 13.18.3 | Not claimed |")
    readme = "## Status\nA.3 items 3; A.4.1.\n## Next\n"
    profile = {"editions": EDITIONS, "modules": ["A.4.2"], "elements": [f"A.3-{n}" for n in range(1, 47)]}
    return common.self_test_arms([
        ("§2 parses 46 rows", len(rows), 46),
        ("§2 stops at the next heading (the §5 rows are not §2 rows)", len(a3_rows(reg + "| 99 | x | y | z | w |\n")), 46),
        ("§5 rows", [r["clause"] for r in a4_rows(reg)], ["A.4.1", "A.4.2"]),
        ("a complete §2 covers every item", a3_covers_every_item(rows), []),
        ("a missing item is named", len(a3_covers_every_item(a3_rows(short))), 1),
        ("a range row covers its endpoints' span", a3_covers_every_item(a3_rows(ranged)), []),
        ("a placeholder clause is a finding", len(a3_cites_resolved_clauses(a3_rows(placeholder))), 1),
        ("a real clause path is not", a3_cites_resolved_clauses(rows), []),
        ("the placeholder pattern fires on 13.x, 8.x, 12.4.x and not on real paths",
         [bool(PLACEHOLDER.search(s)) for s in ("13.x", "8.x", "12.4.x", "12.4.5.12.2 / 12.4.5.6.2", "13.18.43.3 SR7/SR8")],
         [True, True, True, False, False]),
        ("a dangling code is a finding",
         len(a3_codes_are_emitted([{"items": "2", "element": "e", "section": "1", "disposition": "d", "note": "COBOL9999"}] * 4,
                                  {f"COBOL{n:04d}" for n in range(1, 300)} - {"COBOL9999"})), 1),
        ("a live code is not", a3_codes_are_emitted([{"items": "2", "element": "e", "section": "1", "disposition": "d",
                                                      "note": "COBOL0100"}] * 4, {f"COBOL{n:04d}" for n in range(1, 300)}), []),
        ("the README names every declined row", readme_disagreements(readme_status(readme), a3_rows(declined), a4_rows(reg)), []),
        ("a declined row the README omits is a finding",
         len(readme_disagreements(readme_status("## Status\n## Next\n"), a3_rows(declined), a4_rows(reg))), 2),
        ("a claimed row the README names is a finding",
         len(readme_disagreements(readme_status("## Status\nA.3 items 3 and 5.\n"), a3_rows(declined), a4_rows(reg))), 2),
        ("the profile equal to the claims agrees", [f for f in profile_disagreements(reg, profile) if "parsed" not in f
                                                    and "too few" not in f], []),
        ("a profile claiming a declined module is a finding",
         any("which §5 does not claim" in f for f in profile_disagreements(reg, {**profile, "modules": ["A.4.1", "A.4.2"]})), True),
        ("a profile that drops an edition is a finding",
         any("not every edition" in f for f in profile_disagreements(reg, {**profile, "editions": ["2023"]})), True),
        ("an item range names each item", item_names("28–30"), ["A.3-28", "A.3-29", "A.3-30"]),
        # The escaped-pipe case, which is why the split is not a bare '|': item 82's determination writes an absolute value
        # as \|v\|, and a naive split gives that row two extra cells and mis-places the rest.
        ("an escaped pipe stays inside its cell",
         [r.determination for r in schema.section7_rows("## 7. Annex A.1\n| DOC-A.1-82 | E | rounds \\|v\\| away from zero | — |\n")],
         ["rounds |v| away from zero"]),
        ("every determination-prefix the real schema carries fits the extract's lead",
         prefixes_over_lead(schema_prefixes(json.loads((Path(__file__).resolve().parents[2] / SCHEMA_PATH).read_text(encoding="utf-8")))), []),
        ("the prefixes are found in nested selector arms and in a list of arms, and only those",
         schema_prefixes({"a": [{"arms": [{"determination-prefix": ["P1", "P2"], "kinds": ["DOC"]}]}], "determination-prefix": ["P3"],
                          "b": "determination-prefix"}), ["P1", "P2", "P3"]),
        ("a prefix over the lead is a finding naming it", ["x" * 65 in f for f in prefixes_over_lead(["x" * 65, "y" * 64])], [True]),
        ("a prefix exactly at the lead is not", prefixes_over_lead(["y" * 64]), []),
        ("the extract keeps each §7 key with the determination's lead, emphasis stripped",
         build_extract("## 7. Annex A.1\n| DOC-A.1-9 | E | **Not provided.** because | — |\n", "## 8.", "Rule")["annexA1"],
         [{"key": "DOC-A.1-9", "lead": "Not provided.** because"}]),
    ])
