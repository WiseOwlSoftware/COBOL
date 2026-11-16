*>--- conformance-case
*> id: pb5640-invoke-bit-refmod-literal-length
*> rules: [2002-SR-14.8.22.2-12]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5640. §2002:14.8.22.2 SR12: "If identifier-3 or identifier-4 references a bit data item, it shall be described such that:
      *> a) subscripting and reference modification in that identifier consist of only fixed-point numeric literals or arithmetic expressions
      *> in which all operands are fixed-point numeric literals and the exponentiation operator is not specified; and b) it is aligned on a byte
      *> boundary." B8(1:8) writes a literal leftmost position AND a literal length and starts on a byte boundary, so the rule is met.
      *> The same argument with a data-name length is pb5640-invoke-bit-refmod-data-length. Why it can fail: the screen refuses the literal
      *> length, or refuses the argument at an edition whose rule it satisfies.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5640A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 BG.
          05 B8 PIC 1(16) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           IF O NOT = NULL
               INVOKE O "TAKEB" USING B8(1:8)
           END-IF.
           DISPLAY "COMPILED".
           STOP RUN.
       END PROGRAM PB5640A.
