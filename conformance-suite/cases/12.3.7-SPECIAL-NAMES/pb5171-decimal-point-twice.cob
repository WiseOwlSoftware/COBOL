      *>--- conformance-case
      *> id: pb5171-decimal-point-twice
      *> rules: [FMT-12.3.7.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5171. §2023:12.3.7.2 prints "[ DECIMAL-POINT IS COMMA ]"
      *> with no ellipsis, as §1985:VI.4.5.2, §2002:12.2.6.1 and
      *> §2014:12.3.6.1 do, and §2023:5.2.7 gives repetition only where an
      *> ellipsis stands. The second DECIMAL-POINT clause, line 18, is
      *> refused COBOL2423 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5171DP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA
           DECIMAL-POINT IS COMMA.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NOT REACHED".
           STOP RUN.
