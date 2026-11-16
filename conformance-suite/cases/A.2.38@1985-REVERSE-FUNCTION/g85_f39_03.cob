      *>--- conformance-case
      *> id: g85_f39_03
      *> rules: [1989-FMT-A.2.38.2, 1989-RV-A.2.38.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.38.2 - the general format FUNCTION REVERSE
      *> ( argument-1 ), with a literal (V1), an alphanumeric item (V2)
      *> and a one-character argument (V3). §1989:A.2.38.4 RV1 - "the
      *> character in position j of the returned value is the character
      *> from position n-j+1 of argument-1", of the same length n:
      *>   V1 REVERSE ("ABCDE")        = "EDCBA"
      *>   V2 REVERSE (W), W = "AB C " = " C BA" (5 characters)
      *>   V3 REVERSE ("Q")            = "Q"
      *> The brackets show V2's leading space and its kept length. A
      *> leg fails on a refused format or a wrong order or length.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3903.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W               PIC X(5) VALUE "AB C ".
       01  OUT5            PIC X(5).
       01  OUT1            PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION REVERSE ("ABCDE") TO OUT5.
           DISPLAY "V1=[" OUT5 "]".
           MOVE FUNCTION REVERSE (W) TO OUT5.
           DISPLAY "V2=[" OUT5 "]".
           MOVE FUNCTION REVERSE ("Q") TO OUT1.
           DISPLAY "V3=[" OUT1 "]".
           STOP RUN.
