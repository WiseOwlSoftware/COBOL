---
title: WiseOwl COBOL
description: The user manual for the WiseOwl COBOL compiler, which translates ISO/IEC 1989:2023 COBOL (and the 1985, 2002 and 2014 editions) to .NET assemblies.
sidebar:
  order: 0
---

WiseOwl COBOL compiles COBOL to .NET. You write a COBOL program, the `cobol` command translates it to C# and Roslyn
builds that C# into an ordinary .NET assembly, and you run the assembly with `dotnet` or call it from a .NET program.
The compiler implements ISO/IEC 1989:2023 and, selected with `--std`, the 1985, 2002 and 2014 editions.

This manual is the user documentation that ISO/IEC 1989:2023 clause 4.2.16 requires of an implementation. That clause
names seven others (4.2.3, 4.2.4, 4.2.5, 4.2.6, 4.2.10, 4.2.12 and 4.2.13), and the table below says where each is
answered.

## Where to start

| If you want to | Read |
|---|---|
| install the compiler | [Install](install/) |
| compile and run a first program | [Getting started](getting-started/) |
| use the `cobol` command, its options and its editions | [User guide](user-guide/command-line/) |
| call a COBOL program from C#, F# or Visual Basic | [.NET interoperability](dotnet-interop/) |
| move a program from IBM Enterprise COBOL, Micro Focus or GnuCOBOL | [Migrating](migrating/) |
| see what changed in a release | [Release notes](release-notes/) |

## Where each documentation obligation is answered

| Clause | Subject | Answered in |
|---|---|---|
| 4.2.3 | Interaction with non-COBOL runtime modules | [.NET interoperability](dotnet-interop/) |
| 4.2.4 | Interaction between COBOL implementations | [Migrating](migrating/#interaction-between-cobol-implementations-clause-424) |
| 4.2.5 | Implementor-defined language elements | the conformance statement, section 7 (Annex A.1 register) |
| 4.2.6 | Processor-dependent language elements | the conformance statement, sections 2 and 4 |
| 4.2.10 | Nonstandard extensions | the conformance statement, section 3.2; [Editions](user-guide/editions/) for `--flag-extensions` |
| 4.2.12, 4.2.13 | Archaic and obsolete elements | [Editions](user-guide/editions/) |

The conformance statement is `docs/CONFORMANCE.md` in the source repository. The release pipeline will publish it, and
the generated reference chapters (command line, diagnostics, edition matrix), as part of this manual; until then the
file in the repository is the statement.

## How this manual is built

Every page here is written by hand and versioned with the compiler: the manual you read for a release is the one that
was in the source tree when that release was built. A statement about behavior cites the clause of ISO/IEC 1989:2023
that defines it, or the determination in the conformance statement where the standard leaves the choice to the
implementor.
