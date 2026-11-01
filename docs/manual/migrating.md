---
title: Migrating from IBM, Micro Focus and GnuCOBOL
description: What carries over from IBM Enterprise COBOL, Micro Focus and GnuCOBOL programs, what is a dialect extension WiseOwl COBOL accepts or refuses, and how WiseOwl COBOL relates to other COBOL implementations.
sidebar:
  order: 5
---

WiseOwl COBOL implements the standard. It has no IBM mode, Micro Focus mode or GnuCOBOL mode: one compiler accepts what the
chosen ISO edition defines, plus a short, documented list of vendor extensions, and refuses the rest by name. A program
written for another compiler therefore moves in three kinds of steps: switch on the options that match the source's
era and data, replace what is refused, and check the data the program shares with its old environment.

## Compile in migration mode first

```bash
cobol program.cob --std 85 --flag-extensions
```

- `--std` names the edition the program was written for. Programs from the 1980s and 1990s usually need `--std 85`,
  because later editions removed constructs such as `ALTER` and `STOP "literal"`. If you would rather build under the
  current edition while you replace the removed constructs, add `--permissive`; each removed construct then becomes a
  warning that names it (see [Editions](../user-guide/editions/)).
- `--flag-extensions` warns (COBOL2894) at each vendor extension the compiler accepts, and names the standard form to
  write instead. The list of warnings is your list of nonstandard constructs. The register is section 3.2 of the
  conformance statement (clause 4.2.10).
- `--sign-encoding` decides how a signed DISPLAY number stores its sign: `ibm` (the default) is what IBM Enterprise
  COBOL, Micro Focus and GnuCOBOL's `-fsign=ebcdic` write, and `ascii` is what GnuCOBOL writes by default on a PC (Annex
  A.1 item 177). Choose the one that matches the data files the program reads.
- Source written with the free-form layout common in Micro Focus and GnuCOBOL code needs `--source-format free`; see
  [Source format and COPY libraries](../user-guide/source-format/).

## What carries over

| Construct | Status in WiseOwl COBOL |
|---|---|
| `USAGE COMP-3`, `COMPUTATIONAL-3` | accepted extension; the standard form is `PACKED-DECIMAL`. |
| `USAGE COMP-1`, `COMP-2` | accepted extensions; the standard forms are `FLOAT-SHORT` and `FLOAT-LONG`. |
| `USAGE COMP`, `COMP-4`, `COMP-5` | `COMP` is the standard abbreviation of `BINARY`; `COMP-4` and `COMP-5` are accepted extensions (standard forms: `BINARY`, and the fixed-width `BINARY-CHAR`, `BINARY-SHORT`, `BINARY-LONG`, `BINARY-DOUBLE`). |
| `GOBACK RETURNING` | accepted extension (GnuCOBOL, Micro Focus). |
| `SET program-pointer TO ENTRY` | accepted extension (IBM, Micro Focus). |
| Signed DISPLAY numbers | both conventions, chosen with `--sign-encoding`. |
| Binary items | two's complement, most significant byte first, in 1 byte for 1 to 2 digits, 2 for 3 to 4, 4 for 5 to 9, 8 for 10 to 18 and 16 for 19 to 38 (Annex A.1 item 205). |
| COPY libraries | supported; see the search order in [Source format and COPY libraries](../user-guide/source-format/). |

Three layout differences to check in records that are written to files or passed to other programs. A binary item of 1 or
2 digits takes one byte, where IBM Enterprise COBOL stores 1 to 4 digits in two bytes: `PIC S9(2) COMP` is 1 byte and
`PIC S9(4) COMP` is 2. A `SYNCHRONIZED` binary item adds no padding (Annex A.1 item 205): in `01 REC.  05 A PIC X.
05 B PIC S9(9) COMP SYNC.` the record is 5 bytes, where a compiler that aligns would make it 8. And a `HIGH-VALUE` byte read back from a fixed-record file is the
character U+FFFF, which also stands for a real `ÿ` that passed through a byte (Annex A.1 item 31).

## What is refused, and what to write instead

Each of these is an error with a code of its own. None is a construct an edition removed, so `--permissive` does not
change them.

| Source construct | Diagnostic | What to do |
|---|---|---|
| `ENTRY "name"` secondary entry points | COBOL2269 | make the entry point a separate program, or a nested program, and CALL it |
| `JSON GENERATE`, `JSON PARSE`, `XML GENERATE`, `XML PARSE` | COBOL0313 | not supported; generate or parse the text in a .NET host that calls the program |
| SCREEN SECTION, `ACCEPT` and `DISPLAY` of a screen | COBOL1560, COBOL1707 | not supported (Annex A.4.2 is not claimed); move the screen handling to the host |
| `EXEC SQL`, `EXEC CICS` and other embedded-language blocks | COBOL0001 (a parse error) | run the vendor's precompiler first; the compiler reads COBOL only |
| `FORMAT` and `SELECT WHEN` clauses | COBOL1705 | not supported (Annex A.4.8 is not claimed) |
| `WRITE FILE` and `REWRITE FILE` | COBOL1706 | not supported (Annex A.4.13 is not claimed) |
| a class that inherits from more than one class | COBOL0849 | not supported (Annex A.4.10 item 1 is not claimed); an interface may inherit from several |

For example, a program that writes `ENTRY "OTHER"` is refused:

```text
  migentry.cob(6,12): error COBOL2269: ENTRY: ISO/IEC 1989 defines no ENTRY statement — a secondary entry point is a vendor extension, and this implementation admits none (ISO §4.2.2). Make the entry point a separate program, or a nested program, and CALL it
```

The full list of optional facilities WiseOwl COBOL does not claim, with the diagnostic for each, is section 4 and
section 5 of the conformance statement. A construct that is neither accepted nor listed there is either not standard
COBOL or a defect; report it.

## Interaction between COBOL implementations (clause 4.2.4)

ISO/IEC 1989:2023 clause 4.2.4 says that when an implementation supports the transfer of control and the sharing of
external items between runtime elements translated by different COBOL implementations, its documentation names the
implementations supported.

**WiseOwl COBOL supports no such interaction.** A CALL resolves only WiseOwl COBOL programs, so control never passes to or
from a runtime element another COBOL implementation translated, and a program shares EXTERNAL items only with other
WiseOwl COBOL programs of the same run unit (Annex A.1 items 13, 15 and 141; see [.NET interoperability](../dotnet-interop/)).
A COBOL program compiled for .NET by another vendor is, to WiseOwl COBOL, a non-COBOL module.

What can move between WiseOwl COBOL and another implementation is data, and the representation choices above exist for
that. Files and records laid out by the declarations the two programs share (the data description entries, the
`--sign-encoding` convention, the binary layout above, the character encoding in [Files and I-O](../user-guide/files/))
are the interface. To keep two programs that must exchange control in step, migrate them together and compile them
into one WiseOwl COBOL run unit.
