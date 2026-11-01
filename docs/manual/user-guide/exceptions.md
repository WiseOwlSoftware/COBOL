---
title: Exceptions and checking
description: How to turn exception checking on, handle an exception condition with a declarative, and what happens to a fatal condition nobody handles.
sidebar:
  order: 6
---

An exception condition is a run-time event the standard names, such as a subscript outside its table or a numeric
field that holds characters that are not digits. Each has an exception-name that starts `EC-`, for example
`EC-BOUND-SUBSCRIPT`.

## Checking is off until you turn it on

An exception status indicator is set only when checking for the condition is enabled, either by a TURN compiler directive
or by a WHEN phrase in a PERFORM statement (ISO/IEC 1989:2023 section 14.6.13.1.1). The compiler turns nothing on by
default, so a program that does not ask for checking is not checked, and the condition's effect is whatever the
standard says it is when checking is off. The conformance statement (`docs/CONFORMANCE.md`, section 3) records, case
by case, what WiseOwl COBOL does then.

The TURN directive (section 7.3.25) enables checking from where it is written. This program checks subscripts:

```cobol
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 ITEM PIC 9 OCCURS 3 TIMES VALUE 7.
       01 I PIC 9 VALUE 5.
       PROCEDURE DIVISION.
           DISPLAY ITEM(I)
           DISPLAY "after"
           STOP RUN.
```

With the directive, the subscript 5 raises `EC-BOUND-SUBSCRIPT` and the run unit ends abnormally; without the line,
checking is off and the program carries on unchecked. Because a fatal exception condition that nothing handles ends the
run unit abnormally (section 14.6.13.1.3, rules 4 to 7), the checked program prints this on the standard error stream
and exits with status 1:

```text
abnormal run-unit termination: EC-BOUND-SUBSCRIPT (fatal): subscript 5 is outside 1..3 (ISO 8.4.2.3.4 GR2)
```

## Handle a condition with a declarative

A `USE AFTER EXCEPTION CONDITION` declarative runs when its condition is raised and checking for it is enabled.
`FUNCTION EXCEPTION-STATUS` names the condition that caused it:

```cobol
       PROCEDURE DIVISION.
       DECLARATIVES.
       HANDLE SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H1.
           DISPLAY "caught " FUNCTION EXCEPTION-STATUS
           .
       END DECLARATIVES.
```

For a fatal condition, a declarative that ends normally is followed by the abnormal termination (section 14.6.13.1.3,
rule 5): the program above prints `caught EC-BOUND-SUBSCRIPT` and then ends with status 1. The declarative can continue
the program instead with a RESUME statement (the standard notes this might not be advisable for most fatal conditions):

```cobol
       H1.
           DISPLAY "caught " FUNCTION EXCEPTION-STATUS
           RESUME NEXT STATEMENT
           .
```

With `RESUME NEXT STATEMENT` the statement that raised the condition is abandoned, the program prints `after`, and the
run unit ends normally. RESUME is the optional module of Annex A.4.12, which WiseOwl COBOL claims.

## Exceptions and a .NET host

A program called from a .NET host that raises an exception condition no declarative handles ends its run unit, and the
host receives a `RunUnitEndedException` with `Abnormal` true; see [Run units and STOP RUN](../run-units/). A condition
that propagates out of a program the host called, or a program that cannot be activated, reaches the host as a
`CobolCallException` whose `EcName` is the exception-name; see [.NET interoperability](../../dotnet-interop/).

## I-O status and the conditional phrases

File status values and the AT END and INVALID KEY phrases are part of file handling (ISO section 9.1.13), not of the
TURN directive. The status values a statement can produce, and the ones WiseOwl COBOL defines where the standard leaves
the value to the implementor, are in the conformance statement's section 7 (Annex A.1 items 107, 129 and the rows around
them).
