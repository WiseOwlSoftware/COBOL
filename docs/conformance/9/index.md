---
heading: "## 9. The COBOL-85 conformance statement (`--std 85`; §1985:I.1.5)"
generated: "conformance_1985"
---
At `--std 85` WiseOwl COBOL compiles the language of ANSI X3.23-1985 as amended by X3.23a-1989 (Amendment 1, which adds the
Intrinsic Function module) and X3.23b-1993 (Amendment 2); `specs/ANSI_COBOL_1985.md` holds that text with both amendments
applied. This section is the statement §1985:I.1.5 asks of an implementation of it ("Paragraph 1.5 and its subparagraphs
identify the criteria which must be met in order for a valid claim to be made that an implementation conforms to Standard
COBOL"). Sections 3.3 to 3.13 and the 1985 rows of §3, §7 and §8 are its determinations.

**The claim** (the owner's decision, kb/Work PB4885): WiseOwl COBOL at `--std 85` is a conforming implementation of the
**high subset** of Standard COBOL, "composed of all language elements of the highest level of all required modules"
(§1985:I.1.5.1), **with the highest level of every optional module**: Report Writer, Communication, Debug, Segmentation
and Intrinsic Function, the fifth optional module since Amendment 1 ("Standard COBOL consists of 12 modules, seven of which
are required and five of which are optional", §1985:I.1.5). The claim takes the fifth module because PB4885's "every level of
all four optional modules" was decided on the unamended count, and every rule of the module is in the inventory below (kb/Work
PB4997). No option selects a lower subset or level, so no level 1 restriction is enforced (§3.9).

**The claim is computed, not written.** The table after this paragraph is rendered from every rule of the 1985 catalog
(`docs/rearchitecture/spec-rule-catalog-1985.json`) and its state in the 1985 inventory each time this document is
rendered (`scripts/spec/conformance_1985.py`), and `python scripts/spec/conformance_doc.py --check` fails when the rules
the claim does not support and the rows of §9.1 differ. A rule that opens is named here as a GAP the moment it opens.

Each requirement of §1985:I.1.5.2 and where this statement meets it:

| Requirement | Where it is met |
|---|---|
| §1985:I.1.5.2: the claimed subset and optional module levels supported in full, "except as qualified in paragraph 1.5.2.5" | The computed table below: every rule of each claimed module level and its state; §9.1 names each rule the claim does not support, with its ground |
| §1985:I.1.5.2.1: no substitute or additional language elements required | None is: a program of standard elements compiles with `--std 85` and nothing else, and no implementor-defined element (§1985:C.2; §3, §7, §8) is specified by requiring an added element |
| §1985:I.1.5.2.2: the syntax and function of the claimed levels | Every level of every module is claimed, so no element of the standard is accepted outside the claim |
| §1985:I.1.5.2.3: obsolete elements supported, identified and flagged | The list below, item by item from §1985:C.1, with the state of the rules each item marks; the warning mechanism is stated under it |
| §1985:I.1.5.2.4: externally provided functions | No standard element requires an external specification in its place or beside it. The specifications outside the source program interface with the operating environment, which the paragraph allows: the ASSIGN target (A.1 item 24), the message control system's definitions (§3.3), and the runtime's environment variables |
| §1985:I.1.5.2.5.1: the hardware components | §9.2 |
| §1985:I.1.5.2.5.2: extensions identified and flagged | The nonstandard extensions are §3.2 and D-RW1, and `--flag-extensions` warns on each (COBOL2894). There is no standard extension at `--std 85`: every level of every module is claimed |
| §1985:I.1.5.2.5.3: the reserved words of all twelve modules | All are reserved at `--std 85` (`tests/version-matrix/reserved-words.json`) |
| §1985:I.1.5.2.5.4: character substitution | §9.3 |
| §1985:I.1.5.2.5.5: the ENTER statement | Not included (§3.7.1); its rules are rows of §9.1 |
| §1985:XV.1.3.4: the object time switch, controlled outside the program, with no recompilation | D-DEBUG-SWITCH-85: `WISEOWL_COBOL_DEBUG` |
