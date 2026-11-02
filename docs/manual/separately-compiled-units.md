---
title: Separately compiled units and the repository
description: How a REPOSITORY paragraph finds a program or function compiled in another compilation, which assemblies are searched, and what the compiler reports.
sidebar:
  order: 5
---

ISO/IEC 1989:2023 section 8.13 defines the external repository: the information a compilation needs to activate
programs, functions and methods compiled elsewhere and to check that a call conforms to them. Section 12.3.8 lets a
REPOSITORY paragraph name such a unit. This page describes how WiseOwl COBOL keeps that information and finds it
(Annex A.1 item 66, docs/CONFORMANCE.md DOC-A.1-66).

## The repository is the compiled assembly

Every assembly `cobol` produces is the repository entry for every program, function and program or function prototype
it compiled. When your REPOSITORY paragraph names a program or function this compilation group does not define, the
compiler looks in the repository:

1. the `--repository DIR` directories, in the order given (by default, the output directory);
2. then the `--reference FILE.dll` assemblies, in the order given.

In a directory, the assembly named after the unit (`PAY.dll` for `PROGRAM PAY`) is read first; when it does not
publish the unit, every assembly of the directory is. The first entry found is used. The assembly being produced is
never searched: its previous build is the information this compilation updates.

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CALLER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PAY.
       ...
           CALL PAY USING AMOUNT
```

```
cobol pay.cob -o PAY.dll
cobol caller.cob -o CALLER.dll      # PAY.dll in the output directory supplies PAY's signature
dotnet CALLER.dll
```

A program or function found in the repository is checked at compile time exactly as one defined in your compilation
group: the arguments of `CALL PAY USING AMOUNT` are compared with `PAY`'s formal parameters (ISO 14.8.2). At run time
the program is activated by name from the run unit's application directory, and because it was compiled elsewhere the
call is checked again when it is activated, if EC-PROGRAM-ARG-MISMATCH checking is enabled in both programs (ISO
14.9.4.4 general rule 3 d)): a callee rebuilt with another signature after its caller was compiled is found then.

A specifier that names a definition that follows the REPOSITORY paragraph in the same compilation group is not
satisfied by it (ISO 12.3.8.3 syntax rule 14: "specified previously"); write a program prototype definition of it
ahead of every other unit, or compile it apart.

## Options

| Option | Effect |
|---|---|
| `--repository <DIR>` | A repository directory, searched in order (repeatable). Default: the output directory. |
| `--reference <FILE.dll>` | An assembly searched after the directories, in order (repeatable). |
| `--repository-update <on\|off>` | `on` (default) records this compilation's signatures in the assembly it produces; `off` produces an assembly other compilations cannot bind against (it can still be called by name at run time). |
| `--repository-check <on\|off>` | `on` (default) warns when a unit compiled here differs in signature from the previous build of the same output or from another entry of the same name the compilation read; `off` does not compare. |

## What the compiler reports

| Code | Severity | When |
|---|---|---|
| COBOL5962 | error | A specifier names a program or function that neither the compilation group nor the repository holds; the message lists every place searched. |
| COBOL5963 | warning | A later entry in the search order publishes the same name from another build with a different signature; the first is used. |
| COBOL5964 | error | The file named after the unit is not a readable .NET assembly. |
| COBOL5965 | error | An entry's repository record is malformed or exceeds the reader's limits. |
| COBOL5966 | error | An entry describes a parameter the compiler cannot rebuild. |
| COBOL5967 | error | An entry was written for another repository schema, runtime major version or call ABI; recompile it. |
| COBOL5968 | error | The entry was compiled with `--repository-update off`. |
| COBOL5969 | warning | A unit compiled here differs in signature from the previous build of the output or from another entry of its name; callers compiled against that entry must be recompiled. |
| COBOL5970 | error | A `--repository` directory or `--reference` file does not exist. |
| COBOL5971 | error | An entry's parameter is an object reference to a class or interface this compilation group does not have. |
| COBOL5972 | warning | The previous build of the output was written for another schema, runtime major version or call ABI, so it is not compared. |

A REPOSITORY `FUNCTION F` entry is satisfied, in order, by the function externalized `F` in the group, then in the
repository, then by the function whose user-function-name is `F` (a definition `FUNCTION-ID. F AS "G"`), in the group
and then in the repository. When the externalized name selects one function and a different function carries the
word, the externalized name is used and COBOL2969 names both (docs/CONFORMANCE.md D-R3).
