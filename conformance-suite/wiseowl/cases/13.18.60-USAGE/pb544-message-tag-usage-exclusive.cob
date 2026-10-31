*>--- conformance-case
*> id: pb544-message-tag-usage-exclusive
*> rules: [SR-13.18.60.3-21]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB544 — §13.18.60.3 syntax rule 21: "If MESSAGE-TAG is specified, no other usage clauses
      *> shall be specified in the data description entry."
      *> ⛔ THE RULE HAD NO SUBJECT. USAGE MESSAGE-TAG is an Annex A.3 item-4 processor-dependent element this
      *> compiler declines BY NAME (COBOL1943) — but a declined usage still has syntax rules over it, and
      *> this entry used to compile with NO DIAGNOSTIC AT ALL, not even the decline: the binder kept a single
      *> `usageText` that each successive USAGE clause overwrote, so the entry bound as a plain DISPLAY item
      *> and SR21's subject — the SET of usage clauses in the entry — did not exist. COBOL2158 is raised
      *> from the clause LIST, BEFORE and independently of the decline, so the rule is enforced whichever
      *> order the two clauses are written in.
      *> All four editions refuse it (kb/Work PB916 took the sweep PB544 had to hold back). MESSAGE-TAG is an
      *> Annex E.2 item-25 COBOL-2023 addition, so below 2023 the entry ALSO draws the COBOL0900 introduction gate
      *> (usage-message-tag-2023) - and no longer the COBOL1943 decline, which names a 2023 facility; SR21 is a rule
      *> of the usage clause and is reported beside the gate, as COBOL1958 is beside FUNCTION-POINTER's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB544EX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  M USAGE MESSAGE-TAG USAGE DISPLAY PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
