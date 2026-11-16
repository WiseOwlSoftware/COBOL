      *>--- conformance-case
      *> id: g85_bf41_03
      *> rules: [1989-FMT-A.2.29.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.29.2 - the general format FUNCTION NUMVAL
      *> ( argument-1 ): one argument, a nonnumeric literal (N1, N3,
      *> N4, N5) or an alphanumeric data item (N2). Each value is
      *> "the numeric value represented by argument-1" (§1989:A.2.29.4
      *> RV1):
      *>   N1 "  -12.5 "   leading sign, spaces ignored  = -12.5
      *>   N2 ARG " 123 "  data item                     =  123
      *>   N3 ".75"        the ". digit" form            =   0.75
      *>   N4 "8.5-"       trailing sign (format 2)      =  -8.5
      *>   N5 "+ 4"        sign, space, digits           =   4
      *> V is PIC -9(3).99, so each line shows a sign position (space
      *> or minus), three digits, a point and two digits. A leg fails
      *> on a refused format or argument, or a wrong value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4103.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ARG             PIC X(8) VALUE " 123 ".
       01  V               PIC -9(3).99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE V = FUNCTION NUMVAL ("  -12.5 ").
           DISPLAY "N1=" V.
           COMPUTE V = FUNCTION NUMVAL (ARG).
           DISPLAY "N2=" V.
           COMPUTE V = FUNCTION NUMVAL (".75").
           DISPLAY "N3=" V.
           COMPUTE V = FUNCTION NUMVAL ("8.5-").
           DISPLAY "N4=" V.
           COMPUTE V = FUNCTION NUMVAL ("+ 4").
           DISPLAY "N5=" V.
           STOP RUN.
