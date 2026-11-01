---
title: The cobol command
description: The cobol command line, its options, the files it writes, what it prints and the exit statuses it returns.
sidebar:
  order: 1
---

```text
cobol <source> [options]
cobol check-batch <manifest>
```

`cobol` compiles one COBOL compilation group, the source file named by `<source>`, to a .NET assembly. The default
output is the source path with the extension `.dll`. Every value option accepts both the form `--output out.dll` and
the form `--output=out.dll`.

## Options

| Option | Effect |
|---|---|
| `-o`, `--output <out.dll>` | the output assembly path |
| `--run` | runs the program after a successful compile and returns its exit status |
| `--std <85\|2002\|2014\|2023>` | the ISO edition to compile for (default 2023); see [Editions](../editions/) |
| `--permissive` | accepts a construct the chosen edition removed, with a warning in place of the error; see [Editions](../editions/) |
| `--flag-extensions` | warns (COBOL2894) at every use of a nonstandard extension the compiler accepts; see [Editions](../editions/) |
| `--source-format <fixed\|free\|auto>` | the reference format the source starts in (default `fixed`); see [Source format and COPY libraries](../source-format/) |
| `--copy <DIR>` | a directory searched for COPY library text; repeat the option for several |
| `--sign-encoding <ibm\|ascii>` | how DISPLAY stores the sign of a signed item whose sign is not SEPARATE (default `ibm`) |
| `--numeric-display <decimal\|digits>` | how DISPLAY shows, and ACCEPT reads, a numeric item (default `decimal`) |
| `--repository-update <on\|off>` | whether the assembly carries the signatures other compilations bind against (default `on`) |
| `--nist <NAME>` | the placeholder preprocessing the NIST CCVS test programs need; for running that suite, not for application code |
| `--version` | prints the compiler's version |
| `-?`, `-h`, `--help` | prints the options |

### The representation options

`--sign-encoding` and `--numeric-display` choose between two representations the standard leaves to the implementor
(ISO/IEC 1989:2023 section 13.18.52.4 general rules 4 and 5, and section 14.9.11.4 general rule 1; Annex A.1 items 177,
178 and 56). They apply to every edition alike.

With a signed `PIC S9(3)` item holding -123 redefined as `PIC X(3)`, the option decides which character DISPLAY shows
for the last digit:

| `--sign-encoding` | The characters shown |
|---|---|
| `ibm` (default) | `12L`, the IBM and Micro Focus overpunch: `{` and `A` to `I` for positive digits, `}` and `J` to `R` for negative |
| `ascii` | `12s`, plain digits for positive values and `p` to `y` for negative |

`--numeric-display decimal` (the default) shows a numeric item with its decimal point and a leading sign, so
`PIC S9(5)V99 COMP-3` holding -12.5 displays as `-00012.50`. `--numeric-display digits` shows the digit image
instead.

## Output files

For `cobol hello.cob -o out/hello.dll` the files `hello.dll`, `hello.runtimeconfig.json`, `WiseOwl.Cobol.Runtime.dll`
and `hello.g.cs` are written to `out/`. A compilation group with no main program (for instance, one that holds only
subprograms) is a library: `--run` does not apply to it, and a .NET host calls its programs; see
[.NET interoperability](../../dotnet-interop/).

## What the compiler prints

Diagnostics go to the standard error stream, one per line, in the form `file(line,column): severity CODE: message`.
Warnings print whether or not the compile succeeds, so a migration warning is visible on a build that passes. A
`>>DISPLAY` compiler directive writes its text to the stream it names (ISO section 7.3.12.4, Annex A.1 item 53).

## Exit status

| Status | Meaning |
|---|---|
| 0 | the compile succeeded (with `--run`: the program ended normally) |
| 64 | `--run` was given for a library, which has no main program to run |
| 65 | the source is wrong: a parse error or a conformance or binding diagnostic |
| 66 | the source file was not found |
| 70 | an internal error: the C# the compiler generated was refused by Roslyn |

With `--run` the status is the program's own: the value of `STOP RUN WITH ERROR STATUS n`, 1 for an abnormal
termination, 0 otherwise. A `.NET` host that starts the program itself receives the same information; see
[Run units and STOP RUN](../run-units/).

## check-batch

`cobol check-batch <manifest>` parses, edition-checks and binds many sources in one process without emitting
code, which is how the compiler's own test suites sweep thousands of programs. The manifest is a tab-separated file;
each line is `source<TAB>std<TAB>nist-name<TAB>permissive`, with `permissive` 0 or 1 and the last three fields
optional. The command prints `source<TAB>std<TAB>PASS` or `FAIL` for each line and exits 0.
