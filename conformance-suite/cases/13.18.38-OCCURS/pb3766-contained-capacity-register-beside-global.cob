      *>--- conformance-case
      *> id: pb3766-contained-capacity-register-beside-global
      *> rules: [SR-13.18.38.3-30, GR-13.18.27.4-2]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3766, review finding R2-1. A contained program's CAPACITY register is not "defined elsewhere in
      *> the source element" because its container happens to define a GLOBAL name of the same spelling.
      *>
      *> THE RULE. ISO/IEC 1989:2023 13.18.38.3 SR30: "Data-name-3 shall not be defined elsewhere in the source
      *> element." The container PB3766CO defines the GLOBAL data-name WCAP and a GLOBAL dynamic table whose
      *> register is KCAP; the contained program PB3766CI is a different source element, and its OCCURS DYNAMIC
      *> entries implicitly define their own WCAP and KCAP (SR30). Where a name is declared in the contained
      *> program, 8.4.6.2.1 3) a) makes the contained program's item the referenced item: "If the name is
      *> declared in source element B, the item in source element B is the referenced item." The compiler
      *> inherited the container's GLOBAL names before the SR30 screen ran and refused the contained program
      *> with COBOL1523.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT. FROM 3 opens three occurrences (13.18.38.4, Format 4), so the
      *> contained program's own register WCAP holds 3 and DISPLAY shows its nine-digit unsigned binary image,
      *> 0000000003. The container's WCAP is the data item it declared with VALUE 7 and is not touched. The same
      *> holds for KCAP, a register spelled like the register of the container's GLOBAL table (a register of a
      *> GLOBAL record is a global name, 8.4.6.2.2): KCAP is 2 for the container's table TG (FROM 2) and 3 for
      *> the contained table TK (FROM 3), and each program's reference finds the register of the nearest source
      *> element that declares the name: INNER KCAP=0000000003, OUTER KCAP=0000000002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3766CO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WCAP PIC 9 GLOBAL VALUE 7.
       01 TG GLOBAL.
          05 EG PIC X OCCURS DYNAMIC CAPACITY IN KCAP FROM 2.
       PROCEDURE DIVISION.
           CALL "PB3766CI"
           DISPLAY "OUTER WCAP=" WCAP
           DISPLAY "OUTER KCAP=" KCAP
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3766CI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS DYNAMIC CAPACITY IN WCAP FROM 3.
       01 TK.
          05 EK PIC X OCCURS DYNAMIC CAPACITY IN KCAP FROM 3.
       PROCEDURE DIVISION.
           DISPLAY "INNER WCAP=" WCAP
           DISPLAY "INNER KCAP=" KCAP
           EXIT PROGRAM.
       END PROGRAM PB3766CI.
       END PROGRAM PB3766CO.
