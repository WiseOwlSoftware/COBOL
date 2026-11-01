---
title: Editions, --permissive and extensions
description: How --std selects COBOL 1985, 2002, 2014 or 2023, what the compiler does with a construct the edition lacks or removed, and how to find nonstandard extensions.
sidebar:
  order: 2
---

WiseOwl COBOL is four compilers in one command. `--std` names the edition of ISO/IEC 1989 the compilation group is
written for, and the compiler accepts what that edition defines, no more and no less.

| `--std` | Edition |
|---|---|
| `85` | ISO/IEC 1989:1985 |
| `2002` | ISO/IEC 1989:2002 |
| `2014` | ISO/IEC 1989:2014 |
| `2023` | ISO/IEC 1989:2023, the default |

## A construct the edition does not have yet

A construct introduced by a later edition is an error, naming the edition that introduced it. `EXIT PERFORM` came in
with COBOL 2002:

```text
$ cobol loop.cob --std 85
  loop.cob(10,20): error COBOL0900: the EXIT PERFORM statement requires COBOL-2002 (targeting COBOL-85) — ...
```

The same source compiles with `--std 2002` and later. `--permissive` does not change this: it never accepts a
construct from a later edition.

## A construct the edition removed

A construct a later edition deleted is an error at the editions that no longer have it, and the message names the
edition that removed it. Most removals draw COBOL0902, and a few have a code of their own. `ALTER` is in COBOL 85 and
was removed by COBOL 2002:

| `--std` | Result for a program that uses `ALTER` |
|---|---|
| `85` | compiles |
| `2002`, `2014`, `2023` | `error COBOL0810: the ALTER statement was removed in COBOL-2002` |

With `--permissive` the removed construct compiles at every edition and the error becomes a warning with the same
code (`STOP "pause"`, removed by COBOL 2002, is COBOL0902 as an error and as a warning). `--permissive` is the
migration mode: it lets a program that uses deleted constructs build under a current edition while you replace them, and the warnings are the list of what is left to replace. It is orthogonal to `--std`.

## An archaic or obsolete construct

Archaic elements are those Annex F.1 of ISO/IEC 1989:2023 lists, and obsolete elements are those in Annex F.2 (clauses
4.2.12 and 4.2.13). They should not be used in new programs, and an obsolete element will normally be removed from the
next edition. The compiler supports them in the editions that have them. Where it flags one, the flag is warning
COBOL0903, which does not change the program. For example, at `--std 2023`, `NEXT SENTENCE` is archaic (Annex F.1):

```text
  hello.cob(8,16): warning COBOL0903: the NEXT SENTENCE phrase is obsolete as of COBOL-2023 — ...
```

The same use compiles silently with `--std 2002`, where the standard does not yet list it. A construct that the chosen
edition has removed is the previous section's error.

## Nonstandard extensions

Clause 4.2.10 requires the documentation to identify the nonstandard extensions the implementation supports and the
reserved words they add, and requires a warning mechanism for them. The register is section 3.2 of the conformance
statement (`docs/CONFORMANCE.md`). The warning mechanism is `--flag-extensions`:

```bash
cobol payroll.cob --flag-extensions
```

With the option, every use of an extension the compiler accepts draws warning COBOL2894, naming the construct, where it
comes from and the standard form to write instead. For example, `USAGE COMP-3` draws a warning that names `USAGE
PACKED-DECIMAL` (ISO section 13.18.60.4). The option is off by default, a compile without it is the same as a compile
that never had the option, and it never changes whether a program compiles or what it does. It is orthogonal to `--std`
and `--permissive`.

Constructs the compiler recognizes only to refuse them, such as `JSON GENERATE` or `ENTRY`, are errors, not
extensions; see [Migrating](../../migrating/).

## Where each edition's rules come from

Every edition-dependent behavior is derived from the standards themselves. The conformance statement's section 3 records
the choices the standard leaves to the implementor, edition by edition. The edition matrix, which states for each
construct the edition that introduced it, the edition that removed or obsoleted it and the diagnostic code involved, is
the repository file `tests/version-matrix/constructs.json`, and `docs/VERSION_CHANGE_REFERENCE.md` lists each change
between editions with the clause that makes it. A generated edition matrix is planned as a chapter of this manual and
will replace the file as the place to look up a construct.
