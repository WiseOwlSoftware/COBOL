*>--- conformance-case
*> id: pb5123-not-before-extended-relop-refused
*> rules: [FMT-8.8.4.12.2]
*> source-format: free
*>---
      *> kb/Work PB5123 — the 2002+ half. ISO §2023:8.8.4.12.2's general format puts NOT, a simple-relational-operator
      *> or an extended-relational-operator after the connective, and the NOTE to §2023:8.8.4.12.4 GR1 says "NOT cannot
      *> immediately precede an extended-relational operator": ISO 2002 prohibited the 1985 logical NOT before the
      *> relational operators meaning 'greater than or equal to' and 'less than or equal to' (§2002:F.1 item 22), so
      *> that every implementor diagnoses it. `A > B AND NOT >= C` (accepted at 85, where it is
      *> (A > B) AND (NOT (A >= C)) — cases/VI.6.3@1985-CONDITIONAL-EXPRESSIONS/pb5123_logical_not_before_relop) is
      *> refused from 2002 with the removal diagnostic, COBOL0902, rather than a parse error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5123R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 1.
       01 C PIC 9 VALUE 6.
       PROCEDURE DIVISION.
       MAIN.
           IF A > B AND NOT >= C
               DISPLAY "T"
           END-IF
           STOP RUN.
