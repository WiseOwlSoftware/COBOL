      *>--- conformance-case
      *> id: g2_use_declarative_references
      *> rules: [2002-SR-14.8.45.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.45.2 SR3 - "Within a declarative procedure, there
      *>   shall be no reference to any nondeclarative procedures except
      *>   in a RESUME statement." The legal forms: the declarative
      *>   references declarative procedures only (PERFORM D1-NOTE,
      *>   GO TO D1-END) and reaches the nondeclarative ND-P only through
      *>   RESUME AT ND-P, the exemption the rule grants. WiseOwl warns
      *>   (COBOL2375) on the violating form, a PERFORM or GO TO of a
      *>   nondeclarative procedure (docs/CONFORMANCE.md D-DECLREF); this
      *>   program draws no such warning.
      *> Flow: COMPUTE Q = 5 / N with N = 0 and no SIZE ERROR phrase sets
      *>   EC-SIZE-ZERO-DIVIDE (§2002:14.6.4) and, checking being on, runs
      *>   the declarative (§2002:14.5.12.1.2 item 2). RESUME AT ND-P
      *>   transfers "as if a GO TO procedure-name-1 were executed"
      *>   (§2002:14.8.31.3 GR3), so AFTER-COMPUTE is never displayed.
      *> Each leg can fail: a refusal of the RESUME exemption or of the
      *>   in-declarative PERFORM/GO TO fails the compile; a RESUME that
      *>   returned to the next statement shows AFTER-COMPUTE; a GO TO
      *>   that fell through shows D1-NOTE twice.
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USE01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 Q PIC S9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE-ZERO-DIVIDE.
       D1-P.
           DISPLAY "D1 ENTERED"
           PERFORM D1-NOTE
           GO TO D1-END.
       D1-NOTE.
           DISPLAY "D1-NOTE".
       D1-END.
           RESUME AT ND-P.
       END DECLARATIVES.
       M SECTION.
       M1.
           DISPLAY "START"
           COMPUTE Q = 5 / N
           DISPLAY "AFTER-COMPUTE"
           STOP RUN.
       ND-P.
           DISPLAY "ND-P"
           STOP RUN.
