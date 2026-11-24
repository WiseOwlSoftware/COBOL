      *>--- conformance-case
      *> id: 1985-SR-XII_3_3-7
      *> rules: [1985-SR-XII.3.3-7]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:XII.3.3 SR7: "Pseudo-text-1 must not consist entirely of
      *> a separator comma or a separator semicolon." The legal form:
      *> pseudo-text-1 holds a separator comma between two words.
      *> Matching treats each separator comma and each space as one
      *> space (§1985:XII.3.4 GR6b), so ==ALPHA, BETA== matches the source
      *> text ALPHA, BETA and the statement ends up
      *> DISPLAY "REPLACED". A leg fails if the pair is refused or
      *> left unreplaced.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3178SR1237.
       PROCEDURE DIVISION.
       MAIN-PARA.
       REPLACE ==ALPHA, BETA== BY ==DISPLAY "REPLACED"==.
           ALPHA, BETA.
       REPLACE OFF.
           STOP RUN.
