"""docchecks/facts.py — CONFORMANCE.md and the VCR against the facts the COMPILER holds (kb/Work PB4453).

Each check was a Unit or Conformance test that held a document to the compiler by reading the Markdown. The compiler's side
is `tests/doc-facts/compiler-facts.json` (written by DocumentedFactsDriftTests from the compiler itself and held equal to
it there) and the data files beside it; the Markdown is read only here.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

from . import common
from .common import Check, need

COBOL_WORDS = "tests/version-matrix/cobol-words.json"
RAISE_SITES = "tests/doc-facts/declined-raise-sites.json"
BEHAVIOR_ANCHOR = re.compile(r"<!--\s*behavior:([^\s>]+)\s*-->")
EC_IMP_NAME = re.compile(r"EC-[A-Z0-9]+(?:-[A-Z0-9]+)*-IMP\b")


def _row(body: str, key: str, findings: list[str]) -> str:
    row = common.table_row(body, key)
    if row is None:
        findings.append(f"{common.CONFORMANCE} §7 carries no `| {key} |` row")
        return ""
    return row


# ── a published number IS the number the compiler enforces (DOC-A.1-62, DOC-A.1-93) ───────────────────────────────

def maximum_findings(conformance: str, facts: dict) -> list[str]:
    """Annex A.1 item 62 (dynamic-length elementary items, maximum length) and item 93 (the returned-value maximum,
    ISO §2023:15.4) are REQUIRED and DOCUMENTED implementor-defined elements (§2023:4.2.5): the register publishes the number, in
    both spellings for item 62, and it is the carrier's ceiling, so the two are the same number."""
    findings: list[str] = []
    n62, n93 = facts["dynamicLengthMaximum"], facts["returnedValueMaximum"]
    need(findings, n62 == n93, f"the returned-value maximum {n93} is not the dynamic-length maximum {n62} (DOC-A.1-93 says it is)")
    row62 = _row(conformance, "DOC-A.1-62", findings)
    row93 = _row(conformance, "DOC-A.1-93", findings)
    need(findings, row62 == "" or f"{n62:,}" in row62, f"DOC-A.1-62 does not publish {n62:,}, the dynamic-length maximum")
    need(findings, row62 == "" or f"0x{n62:X}" in row62, f"DOC-A.1-62 does not publish 0x{n62:X}, the dynamic-length maximum")
    need(findings, row93 == "" or f"{n93:,}" in row93, f"DOC-A.1-93 does not publish {n93:,}, the returned-value maximum")
    return findings


# ── the implementor-defined exception-names (DOC-A.1-99, DOC-A.1-100) ─────────────────────────────────────────────

def exception_name_findings(conformance: str, facts: dict) -> list[str]:
    """ISO §2023:14.6.13.1.1 leaves an implementor-defined exception-name's action to the implementor and A.1 requires it
    documented: item 100 names exactly the catalog's level-2 `-IMP` names and says each is FATAL; item 99 says there is
    no `EC-IMP-suffix` name, and the catalog has none."""
    findings: list[str] = []
    provided = set(facts["ecImplementorLevel2Names"])
    row100 = _row(conformance, "DOC-A.1-100", findings)
    documented = set(EC_IMP_NAME.findall(row100))
    need(findings, len(provided) == 22, f"the catalog provides {len(provided)} level-2 -IMP names, Table 13 defines 22")
    need(findings, row100 == "" or provided == documented,
         f"DOC-A.1-100 and the catalog disagree about the -IMP names: catalog only {sorted(provided - documented)}, "
         f"documented only {sorted(documented - provided)}")
    need(findings, row100 == "" or "each is FATAL" in row100, "DOC-A.1-100 does not say each -IMP name is FATAL")
    row99 = _row(conformance, "DOC-A.1-99", findings)
    need(findings, row99 == "" or "**Not provided.**" in row99, "DOC-A.1-99 does not say EC-IMP-suffix is **Not provided.**")
    need(findings, not facts["ecImpSuffixNames"], f"the catalog has EC-IMP- names {facts['ecImpSuffixNames']}, DOC-A.1-99 says none")
    return findings


def device_form_findings(conformance: str, facts: dict) -> list[str]:
    """DOC-A.1-92 names every function whose catalog row gives its returned value a description of its own (PB3514)."""
    findings: list[str] = []
    row = _row(conformance, "DOC-A.1-92", findings)
    need(findings, len(facts["functionDeviceFormRowNames"]) >= 3, "the compiler facts name no device-form rows: the facts are broken")
    return findings + [f"DOC-A.1-92 does not name {n}, whose catalog row gives its returned value a description of its own "
                       "(kb/Work PB3514)" for n in facts["functionDeviceFormRowNames"] if row and n not in row]


def implementor_name_findings(conformance: str, facts: dict) -> list[str]:
    """Annex A.1 items 189-191 document every implementor-name the table holds, as `NAME` (the diagnostic that names
    the unavailable ones is the compiler test's)."""
    findings: list[str] = []
    for kind, key in (("Device", "DOC-A.1-189"), ("Feature", "DOC-A.1-190"), ("Switch", "DOC-A.1-191")):
        row = _row(conformance, key, findings)
        names = facts["implementorNameElements"][kind]
        need(findings, bool(names), f"the compiler facts hold no {kind} implementor-names: the facts are broken")
        findings += [f"{key} does not name `{e}`" for e in names if row and f"`{e}`" not in row]
    return findings


def linage_claim_findings(conformance: str) -> list[str]:
    """Annex A.4.11's LINAGE claim and the file-description format it rests on (the compiler's table is the Unit test's)."""
    findings: list[str] = []
    row = _row(conformance, "A.4.11", findings)
    for phrase in ("The LINAGE clause (item 10, §2023:13.18.34) is CLAIMED",
                   "a LINAGE clause on a Format 2 (relative or indexed) or Format 3 (report) entry is refused COBOL2604"):
        need(findings, row == "" or phrase in row, f"A.4.11 no longer says: {phrase}")
    return findings


# ── nonstandard extensions (§3.2) and the reserved words they add (D-RW1) ──────────────────────────────────────────

def extension_register_findings(conformance: str, facts: dict) -> list[str]:
    """ISO §2023:4.2.10 documentation: §3.2 lists exactly the nonstandard-extension register's rows."""
    section = re.search(r"### 3\.2 Nonstandard extensions.*?(?=\n## )", conformance, re.DOTALL)
    if section is None:
        return [f"{common.CONFORMANCE} lost its §3.2 Nonstandard extensions section (ISO §2023:4.2.10 documentation)"]
    documented = re.findall(r"^\| `([a-z0-9-]+)` \|", section.group(0), re.MULTILINE)
    ids = facts["nonstandardExtensionIds"]
    findings: list[str] = []
    need(findings, len(documented) == len(set(documented)), "CONFORMANCE.md §3.2 lists an extension twice")
    need(findings, set(documented) == set(ids),
         f"CONFORMANCE.md §3.2 lists {sorted(documented)} != the register {sorted(ids)}")
    return findings


def reserved_word_findings(conformance: str, extension_reserved: set[str]) -> list[str]:
    """§2023:4.2.10: "Documentation … shall specify any reserved words added for nonstandard extensions." D-RW1 lists exactly
    cobol-words.json's `extensionReserved` words."""
    block = re.search(r"\*\*D-RW1 .*?\n\n", conformance, re.DOTALL)
    if block is None:
        return [f"{common.CONFORMANCE} lost its D-RW1 determination (§2023:4.2.10 documentation)"]
    documented = {w for m in re.finditer(r"\*\*([A-Z][A-Z0-9-]*(?:,\s+[A-Z][A-Z0-9-]*)+)\*\*", block.group(0))
                  for w in re.split(r",\s+", m.group(1))}
    if documented == extension_reserved:
        return []
    return [f"CONFORMANCE.md D-RW1 lists {len(documented)} word(s), cobol-words.json extensionReserved {len(extension_reserved)} "
            f"— §2023:4.2.10 requires the two to agree (documented only {sorted(documented - extension_reserved)}, "
            f"data only {sorted(extension_reserved - documented)})"]


# ── the declined raise sites (EcRaiseSiteRegistryDriftTests) ───────────────────────────────────────────────────────

def raise_site_findings(conformance: str, sites: list[dict]) -> list[str]:
    """Each exception-name no statement declares and no code raises is quoted by the CONFORMANCE.md row or section that
    says why: `DOC-A.1-n` is that Annex A.1 row (it must quote the name); `§4` is "Documented non-support facilities" (it
    must quote the name or its level-2 family); `§3:D-x` is the §3 determination bullet that begins `- **D-x`."""
    section4 = conformance[conformance.find("\n## 4. "):conformance.find("\n## 5. ")]
    problems: list[str] = []
    for site in sites:
        anchor = site["anchor"]
        for name in site["names"]:
            if anchor == "§4":
                family = site.get("family", "")
                need(problems, name in section4 or (family != "" and family in section4),
                     f"{name}: {common.CONFORMANCE} §4 quotes neither the name nor its family {family}")
            elif anchor.startswith("§3:"):
                bullet = "- **" + anchor[len("§3:"):]
                at = conformance.find(bullet)
                if at < 0:
                    problems.append(f"{name}: {common.CONFORMANCE} has no determination bullet beginning '{bullet}'")
                else:
                    end = conformance.find("\n- **", at + len(bullet))
                    need(problems, name in conformance[at:len(conformance) if end < 0 else end],
                         f"{name}: the {anchor} determination of {common.CONFORMANCE} does not quote the name")
            else:
                row = common.table_row(conformance, anchor)
                if row is None:
                    problems.append(f"{name}: {common.CONFORMANCE} has no row {anchor}")
                else:
                    need(problems, name in row, f"{name}: the {anchor} row of {common.CONFORMANCE} does not quote the name")
    return problems


# ── the dialect-behavior registry against the VCR's anchors (DialectBehaviorRegistryDriftTests) ────────────────────

def behavior_anchors(vcr_lines: list[str]) -> list[tuple[str, str, str]]:
    out = []
    for ln in vcr_lines:
        rm = common.ROW_NUMBER.match(ln)
        if rm:
            out += [(rm.group(1), m.group(1), ln) for m in BEHAVIOR_ANCHOR.finditer(ln)]
    return out


def behavior_findings(vcr: str, behaviors: list[dict]) -> list[str]:
    anchors = behavior_anchors(vcr.split("\n"))
    problems: list[str] = []
    need(problems, len(behaviors) >= 1, "the compiler facts hold no dialect behaviors: the facts are broken")
    for b in behaviors:
        hit = next((a for a in anchors if a[0] == b["vcrRow"] and a[1] == b["id"]), None)
        if hit is None:
            problems.append(f"{b['id']}: VERSION_CHANGE_REFERENCE row {b['vcrRow']} carries no <!-- behavior:{b['id']} -->")
            continue
        need(problems, "<!-- todo -->" not in hit[2], f"{b['id']}: row {b['vcrRow']} is gated in code but still marked <!-- todo -->")
        need(problems, "gate-behavior-by-dialect" in hit[2], f"{b['id']}: row {b['vcrRow']}'s gating action is not gate-behavior-by-dialect")
    known = {b["id"] for b in behaviors}
    return problems + [f"VCR row {a[0]}: behavior:{a[1]} names no DialectBehavior" for a in anchors if a[1] not in known]


# ── the checks ────────────────────────────────────────────────────────────────────────────────────────────────────

def _conf(root: Path) -> str:
    return common.text(root, common.CONFORMANCE)


def _extension_reserved(root: Path) -> set[str]:
    return {e["word"] for e in json.loads(common.text(root, COBOL_WORDS))["extensionReserved"]}


CHECKS = (
    Check("PUBLISHED MAXIMA (DOC-A.1-62, DOC-A.1-93) are the numbers the compiler enforces",
          lambda r: maximum_findings(_conf(r), common.facts(r))),
    Check("IMPLEMENTOR-DEFINED EXCEPTION-NAMES (DOC-A.1-99, DOC-A.1-100) are the catalog's",
          lambda r: exception_name_findings(_conf(r), common.facts(r))),
    Check("FUNCTION DEVICE FORMS (DOC-A.1-92) are all named",
          lambda r: device_form_findings(_conf(r), common.facts(r))),
    Check("IMPLEMENTOR-NAMES (DOC-A.1-189..191) are all documented",
          lambda r: implementor_name_findings(_conf(r), common.facts(r))),
    Check("A.4.11 LINAGE claim is stated", lambda r: linage_claim_findings(_conf(r))),
    Check("NONSTANDARD EXTENSIONS (§3.2) are the register's rows",
          lambda r: extension_register_findings(_conf(r), common.facts(r))),
    Check("EXTENSION RESERVED WORDS (D-RW1) are cobol-words.json's",
          lambda r: reserved_word_findings(_conf(r), _extension_reserved(r))),
    Check("DECLINED RAISE SITES are quoted by the row that says why",
          lambda r: raise_site_findings(_conf(r), json.loads(common.text(r, RAISE_SITES))["sites"])),
    Check("DIALECT BEHAVIORS are anchored on their VCR rows",
          lambda r: behavior_findings(common.text(r, common.VCR), common.facts(r)["dialectBehaviors"])),
)


def self_test() -> list[str]:
    imp = [f"EC-X{i}-IMP" for i in range(22)]
    facts = {"dynamicLengthMaximum": 65536, "returnedValueMaximum": 65536, "ecImplementorLevel2Names": imp, "ecImpSuffixNames": [],
             "functionDeviceFormRowNames": ["FormA", "FormB", "FormC"], "nonstandardExtensionIds": ["one", "two"],
             "implementorNameElements": {"Device": ["CSP"], "Feature": ["F1"], "Switch": ["SWITCH-1"]}}
    doc = ("| DOC-A.1-62 | e | 65,536 and 0x10000 | — |\n| DOC-A.1-93 | e | 65,536 | — |\n"
           f"| DOC-A.1-100 | e | {' '.join(imp)} each is FATAL | — |\n| DOC-A.1-99 | e | **Not provided.** | — |\n"
           "| DOC-A.1-92 | e | FormA FormB FormC | — |\n| DOC-A.1-189 | e | `CSP` | — |\n| DOC-A.1-190 | e | `F1` | — |\n"
           "| DOC-A.1-191 | e | `SWITCH-1` | — |\n| A.4.11 | m | The LINAGE clause (item 10, §2023:13.18.34) is CLAIMED; a LINAGE clause on a "
           "Format 2 (relative or indexed) or Format 3 (report) entry is refused COBOL2604 | x |\n"
           "### 3.2 Nonstandard extensions\n| `one` | a |\n| `two` | b |\n\n## 4. Documented non-support\nEC-MCS quoted\n"
           "## 5. next\n- **D-RW1 words.** **ALPHA, BETA** end.\n\nafter\n")
    sites = [{"names": ["EC-MCS-X"], "anchor": "§4", "family": "EC-MCS"}, {"names": ["EC-X0-IMP"], "anchor": "DOC-A.1-100"}]
    beh = [{"id": "alpha", "vcrRow": "7.20"}]
    vcr = "| 7.20 | x | <!-- behavior:alpha --> gate-behavior-by-dialect |\n"
    return common.self_test_arms([
        ("the published maxima agree", maximum_findings(doc, facts), []),
        ("a changed number is named", len(maximum_findings(doc, {**facts, "dynamicLengthMaximum": 99999, "returnedValueMaximum": 99999})), 3),
        ("differing maxima are a finding", len(maximum_findings(doc, {**facts, "returnedValueMaximum": 65537})), 2),
        ("the exception names agree", exception_name_findings(doc, facts), []),
        ("a documented-only -IMP name is a finding", len(exception_name_findings(doc.replace("EC-X21-IMP", "EC-Y-IMP"), facts)), 1),
        ("an EC-IMP- name in the catalog is a finding", len(exception_name_findings(doc, {**facts, "ecImpSuffixNames": ["EC-IMP-Z"]})), 1),
        ("device forms are named", device_form_findings(doc, facts), []),
        ("a missing device form is named", len(device_form_findings(doc.replace("FormA FormB FormC", "FormA FormB"), facts)), 1),
        ("implementor names are documented", implementor_name_findings(doc, facts), []),
        ("a missing implementor-name is named", len(implementor_name_findings(doc.replace("`F1`", ""), facts)), 1),
        ("the LINAGE claim is stated", linage_claim_findings(doc), []),
        ("a dropped LINAGE sentence is a finding", len(linage_claim_findings(doc.replace("is CLAIMED", ""))), 1),
        ("the extension register agrees", extension_register_findings(doc, facts), []),
        ("an undocumented extension is a finding", len(extension_register_findings(doc, {**facts, "nonstandardExtensionIds": ["one", "two", "three"]})), 1),
        ("the reserved words agree", reserved_word_findings(doc, {"ALPHA", "BETA"}), []),
        ("a reserved word the document omits is a finding", len(reserved_word_findings(doc, {"ALPHA", "BETA", "GAMMA"})), 1),
        ("a raise site quoted by its anchor is clean", raise_site_findings(doc, sites), []),
        ("a raise site its row omits is a finding", len(raise_site_findings(doc, [{"names": ["EC-X0-IMP"], "anchor": "DOC-A.1-99"}])), 1),
        ("a missing determination bullet is a finding", len(raise_site_findings(doc, [{"names": ["EC-Z"], "anchor": "§3:D-NONE"}])), 1),
        ("a §3 bullet quoting the name is clean", raise_site_findings(doc.replace("**ALPHA, BETA**", "EC-Z"),
                                                                       [{"names": ["EC-Z"], "anchor": "§3:D-RW1"}]), []),
        ("a behavior anchored on its row is clean", behavior_findings(vcr, beh), []),
        ("a behavior with no anchor is a finding", len(behavior_findings("| 7.20 | x |\n", beh)), 1),
        ("a todo-marked row is a finding", len(behavior_findings(vcr.replace("alpha -->", "alpha --> <!-- todo -->"), beh)), 1),
        ("an anchor naming no behavior is a finding", len(behavior_findings(vcr + "| 7.21 | <!-- behavior:ghost --> |\n", beh)), 1),
    ])
