*>--- conformance-case
*> id: pb1034-abbreviated-not-gteq
*> rules: [FMT-8.7.5.1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1034, the abbreviated-combined-relation consumer of the one operator rule. ISO 1989:2023
      *> 8.8.4.12.2 prints the abbreviated tail's operator as `{ NOT | simple-relational-operator |
      *> extended-relational-operator }`, and 8.7.5.1 prints those two operator sets: neither has `>=` with a NOT,
      *> and the NOTE to 8.8.4.12.4 GR1 says "NOT cannot immediately precede an extended-relational operator".
      *> Before the operator rule was narrowed this compiled and expanded as A NOT >= C at every edition.
      *> kb/Work PB5123: at 85 the NOT is the LOGICAL operator (§1985:VI.6.3.3 GRL2.2), so the program is the
      *> 1985 form (A > B) AND (NOT (A >= C)) — cases/VI.6.3@1985-CONDITIONAL-EXPRESSIONS/
      *> pb5123_logical_not_before_relop runs it — which ISO 2002 prohibited (§2002:F.1 item 22): from 2002 it is
      *> refused by name, COBOL0902 (construct abbreviated-not-logical-removed-2002), not as a parse error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034NEG6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 3.
       01 C PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           IF A > B AND NOT >= C
               DISPLAY "X"
           ELSE
               DISPLAY "Y"
           END-IF
           STOP RUN.
