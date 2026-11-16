      *>--- conformance-case
      *> id: g85_f08_05
      *> rules: [1985-GR-VI.5.12.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.12.4 GR1 - the SIGN clause specifies the position
      *> and mode of the operational sign "for the numeric data
      *> description entry to which it applies, or for each numeric data
      *> description entry subordinate to the group to which it
      *> applies", and "applies only to numeric data description entries
      *> whose PICTURE contains the character 'S'".  SEPARATE is used so
      *> the mode is the standard '+'/'-' of §1985:VI.5.12.4 GR6c and
      *> the image is not implementor-defined.
      *> Expected (derived):
      *>   G (SIGN LEADING SEPARATE): A S9(3) -12 -> "-012"; B 9(3) 7
      *>     has no
      *>   'S', the clause does not apply -> "007" (3 positions); C S99
      *>     +5,
      *>   two levels below G -> "+05".  G = "-012007+05".
      *>   GD: D S99 SIGN TRAILING SEPARATE -3 (elementary entry) ->
      *>     "03-".
      *> A leg fails if the group clause misses a subordinate (C),
      *> touches the unsigned B, or the elementary clause is ignored.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0805.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G                  SIGN IS LEADING SEPARATE CHARACTER.
           05  A              PIC S9(3) VALUE -12.
           05  B              PIC 9(3) VALUE 7.
           05  H.
               10  C          PIC S99 VALUE +5.
       01  GD.
           05  D              PIC S99 SIGN IS TRAILING SEPARATE
                              CHARACTER VALUE -3.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "G=[" G "]".
           DISPLAY "GD=[" GD "]".
           STOP RUN.
