      *>--- conformance-case
      *> id: g2call06_literal1_categories
      *> rules: [2002-SR-14.8.4.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR2 (ALL FORMATS) - "Literal-1 shall be an
      *> alphanumeric or national literal." Both categories are written
      *> as literal-1 in Format 1 (on-overflow), Format 2 (on-exception)
      *> and, for the alphanumeric literal, Format 3 (AS NESTED). By
      *> §2002:14.8.4.3 GR3 "the value of literal-1 ... is the
      *> program-name of the program being called", so the national
      *> literal N"G2LTS" names the same program as "G2LTS".
      *> Each call reaches the contained program G2LTS, which prints
      *> "IN G2LTS"; the success path ignores ON OVERFLOW / ON EXCEPTION
      *> (GR3h) and runs NOT ON EXCEPTION. Derived lines:
      *>   IN G2LTS / 1 ALNUM F1
      *>   IN G2LTS / 2 NATIONAL F1
      *>   IN G2LTS / 3 ALNUM F2 OK
      *>   IN G2LTS / 4 NATIONAL F2 OK
      *>   IN G2LTS / 5 ALNUM F3
      *> Fails if: a national literal-1 is refused or names another
      *> program (an OVF / EXC line instead of IN G2LTS), or literal-1
      *> is refused in the on-overflow format. The prohibiting side
      *> (a numeric literal-1) is g2call07_literal1_numeric_refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LTM.
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2LTS" ON OVERFLOW DISPLAY "1 OVF" END-CALL
           DISPLAY "1 ALNUM F1"
           CALL N"G2LTS" ON OVERFLOW DISPLAY "2 OVF" END-CALL
           DISPLAY "2 NATIONAL F1"
           CALL "G2LTS"
               ON EXCEPTION DISPLAY "3 EXC"
               NOT ON EXCEPTION DISPLAY "3 ALNUM F2 OK"
           END-CALL
           CALL N"G2LTS"
               ON EXCEPTION DISPLAY "4 EXC"
               NOT ON EXCEPTION DISPLAY "4 NATIONAL F2 OK"
           END-CALL
           CALL "G2LTS" AS NESTED END-CALL
           DISPLAY "5 ALNUM F3"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LTS.
       PROCEDURE DIVISION.
       S1.
           DISPLAY "IN G2LTS"
           GOBACK.
       END PROGRAM G2LTS.
       END PROGRAM G2LTM.
