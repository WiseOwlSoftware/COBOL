      *>--- conformance-case
      *> id: 1985-SR-XII_2_3-9
      *> rules: [1985-SR-XII.2.3-9]
      *> accept: [85]
      *> files: [SR2339.cpy]
      *> source-format: fixed
      *>---
      *> §1985:XII.2.3 SR9: "Pseudo-text-1 must not consist entirely of
      *> a separator comma or a separator semicolon." The legal form:
      *> pseudo-text-1 holds a separator comma between two words.
      *> Matching treats each separator comma and each space as one
      *> space (§1985:XII.2.4 GR5b), so ==ALPHA, BETA== matches the
      *> library text ALPHA, BETA and the program ends up
      *> DISPLAY "COPIED". A leg fails if the pair is refused or left
      *> unreplaced.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3178SR2239.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COPY SR2339
               REPLACING ==ALPHA, BETA== BY ==DISPLAY "COPIED"==.
           STOP RUN.
