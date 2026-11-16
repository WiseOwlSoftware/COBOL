---
heading: "## 1. Conformance summary (§2023:4.2.16)"
---
WiseOwl COBOL is a **standard-conforming** COBOL implementation targeting ISO/IEC 1989:2023, with correct support for
the 1985, 2002, and 2014 editions selected by `--std`. It implements the required nucleus and the standard modules
except the optional/processor-dependent facilities listed as **not supported** below; the per-module
optional-element dispositions are §5 (only Claimed/Partial rows there are claimed). Per §2023:4.2.6, an
implementation need not implement processor-dependent elements for which support is not claimed; per §2023:4.2.7, an
optional element is implemented only when support is claimed. The **documented non-support facilities**
(§4; the asynchronous messaging facility left them with kb/Work PB4744, which provides it) are recognized at compile time with a NAMED diagnostic.
The VALIDATE facility is built in stages (§4 item 3; kb/Work PB5577): its statement runs and its clauses, until their stage lands, are refused by name (**COBOL1708**). **SCREEN handling is a REFUSAL** —
**COBOL1560** (the data / environment surface) and **COBOL1707** (the procedure surface), both Errors,
at every edition where the words are reserved — because Annex A.4.1 admits an optional element's syntax
"only when support for that language element is claimed", which is a prohibition on ACCEPTING and not a
licence to accept-and-flag. The A.4.13 module (WRITE / REWRITE FILE, **Claimed**: kb/Work PB5579) is provided. The A.4.8 module (FORMAT / SELECT WHEN) is **Partial** from kb/Work PB5578: provided except for the two record shapes §5 names. Commit and rollback (A.4.3, **Claimed**) is
**provided for every organization §2023:9.1.18.2 admits** — RELATIVE and INDEXED files in the keyed store engine, SEQUENTIAL
and LINE SEQUENTIAL files (the print stream included) in a transaction journal — complete for the sequential
and keyed organizations alike: an OPEN OUTPUT that replaces a RELATIVE or INDEXED file, a DELETE FILE that removes one and an OPEN that creates one are journaled in the same transaction journal (the keyed CAPTURE of DESIGN-runtime-library.md 2.2.2.14, kb/Work PB3492), so a ROLLBACK restores such a file as at the last COMMIT (§2023:14.9.36.4 GR3; the NOTE under §2023:14.9.10.4 GR15) and a DELETE FILE after a CLOSE under commit control proceeds (§5, A.4.3; §4 item 2).

> ⚖ **The warning band and the hard errors are DELIBERATELY asymmetric, and the axis is whether a program can
> still mean what it says.** For an Annex A.3 element §2023:4.2.6 ¶3 requires a warning mechanism and adds that "The
> implementor is not required to produce executable code when unsupported processor-dependent language elements
> are used" — so both severities conform there, and the choice is ours to make and to explain. For an Annex A.4
> element neither sentence applies: §2023:4.2.7 asks only for the documentation, and A.4.1 makes refusing the default
> reading — so warning on the A.4.14 VALIDATE statement is a choice we owe an explanation for, and it is below.
> **The 1578–1580 band warns** because those facilities are *additive*: a program with a SEND, a COMMIT or a
> VALIDATE has a well-defined meaning with the facility inert, and the rest of the program is unaffected.
> Compiling and running it is more useful than refusing it. **SCREEN handling left that band on 2026-09-02**
> (kb/Work PB260): it is not additive in practice — `ACCEPT screen-name-1` and `DISPLAY screen-name-1` are
> token-identical to their format-1 DEVICE forms, so "inert" meant *silently binding the other statement* and
> moving the wrong data — and A.4.1's licence for an unclaimed OPTIONAL module reads the other way from
> §2023:4.2.6's for a PROCESSOR-DEPENDENT one. A.4 unclaimed ⇒ the syntax is not accepted at all.
> **`ARITHMETIC IS STANDARD-BINARY` is a hard error (COBOL0806)** because it is *pervasive*: §2023:8.8.1.4.1 makes
> it "a method of evaluating an arithmetic expression, an arithmetic statement, the SUM clause, and certain
> integer and numeric functions", in effect for the whole unit the moment the phrase is written, so there is
> no inert reading — compiling it with the clause ignored would silently run the whole program under a different
> arithmetic than the source selected, and the divergence would appear as wrong VALUES, not as a missing
> facility. A warning nobody reads plus wrong answers is the worst of the available outcomes; refusing is the
> honest one. `ARITHMETIC IS STANDARD` (removed 2023) and the removed/introduced edition gates are errors for
> the same reason.

> ℹ **The former warning dead spot is closed (kb/Work PB137).** A bare `COMMIT`/`ROLLBACK` after any
> open operand list — a `DISPLAY` operand tail, an `EVALUATE … WHEN` arm's first statement, the
> arithmetic receiver lists — now parses as its statement at the editions where §2023:8.9 reserves the words:
> the `cobolWord` alternatives for the two words carry a reservation predicate, so no operand list can
> absorb them at 2023 while both remain ordinary user-defined words below 2023 (which is itself the §2023:8.9
> rule). The named warning fires in every position, the statements bind with IDENTITY
> (`BoundCommitRollback`), and §2023:14.9.7.3/§2023:14.9.36.3 SR1 (recursive source elements, functions and
> methods included) and SR2 (SORT/MERGE input/output procedures) reject at compile time (COBOL1690).
> `VALIDATE` keeps the old absorption behavior (its own facility row).
