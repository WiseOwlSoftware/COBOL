      *>--- conformance-case
      *> id: g2-invoke-universal-by-content
      *> rules: [2002-SR-14.8.22.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR6 - "If identifier-1 references a universal
      *> object reference, neither the BY CONTENT nor the BY VALUE phrase
      *> shall be specified". U is a universal object reference, so BY
      *> CONTENT at line 22 is refused at 2002 (COBOL0866). The implied
      *> BY REFERENCE half is g2-invoke-cd-receiving-operands A2 and A6.
      *> Why it can fail: the compiler passes the argument by content.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE.
       01 X PIC X(4) VALUE "AAAA".
       PROCEDURE DIVISION.
       MAIN.
           IF U NOT = NULL
               INVOKE U "SETIT" USING BY CONTENT X
           END-IF.
           DISPLAY "COMPILED".
           STOP RUN.
       END PROGRAM G2INV03.
