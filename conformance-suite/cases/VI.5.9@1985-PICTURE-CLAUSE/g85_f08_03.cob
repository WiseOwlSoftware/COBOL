      *>--- conformance-case
      *> id: g85_f08_03
      *> rules: [1985-GR-VI.5.9.5-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.5 GR5 - the DECIMAL-POINT IS COMMA leg: a final
      *> insertion character is followed by the separator period, giving
      *> "the combination of ',.' if the DECIMAL-POINT IS COMMA clause
      *> is used".  §1985:VI.4.5.4 GR12 exchanges the functions of comma
      *> and period in the PICTURE character-string and in numeric
      *> literals, so ',' is the special insertion character (actual
      *> decimal point).
      *> Expected (derived):
      *>   E1 PIC 9999,99 <- 1234,5: "1234,50"; G1 = E1 + "|" =
      *>     "1234,50|" (the ',' is counted: 7 characters).
      *>   E2 PIC 9.999,99 <- 1234,5: '.' is now simple insertion,
      *>     "1.234,50".
      *>   E3 PIC 9999, (',' last, then the separator: ",.") <- 1234,5:
      *>     "1234,".
      *> A leg fails if ',' is not the aligned decimal point, if it is
      *> not counted, or if the ",." spelling is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0803.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC                PIC 9(4)V99 VALUE 1234,5.
       01  G1.
           05  E1             PIC 9999,99.
           05  M1             PIC X VALUE "|".
       01  E2                 PIC 9.999,99.
       01  E3                 PIC 9999,.
       PROCEDURE DIVISION.
       P1.
           MOVE SRC TO E1.
           DISPLAY "G1=[" G1 "]".
           MOVE SRC TO E2.
           DISPLAY "E2=[" E2 "]".
           MOVE SRC TO E3.
           DISPLAY "E3=[" E3 "]".
           STOP RUN.
