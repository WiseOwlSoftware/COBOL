*>--- conformance-case
*> id: pb5640-invoke-bit-refmod-data-length
*> rules: [2002-SR-14.8.22.2-12]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5640. §2002:14.8.22.2 SR12 (and §2014:14.9.22.2 SR12): the whole reference modification of a bit argument, its leftmost
      *> position and its length, consists of only fixed-point numeric literals or all-literal arithmetic expressions, so B8(1:L) with the
      *> data-name L is refused at 2002 and 2014 (COBOL2453). §2023:14.9.23.3 SR12 narrows the rule to "subscripting and the leftmost
      *> position in a reference modification", so the same program is accepted at 2023 (pb5640-invoke-bit-refmod-data-length-2023). Control: pb5640-invoke-bit-refmod-literal-length.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5640B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       01 L PIC 9(2) VALUE 8.
       01 BG.
          05 B8 PIC 1(16) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           IF O NOT = NULL
               INVOKE O "TAKEB" USING B8(1:L)
           END-IF.
           DISPLAY "COMPILED".
           STOP RUN.
       END PROGRAM PB5640B.
