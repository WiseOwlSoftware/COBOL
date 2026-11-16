      *>--- conformance-case
      *> id: g85_bf12_01
      *> rules: [1985-GR-VI.5.13.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.13.4 GR6 - with any form of the SYNCHRONIZED clause
      *> the operational sign appears in the sign position the SIGN
      *> clause specifies, explicitly (on the item) or implicitly (by
      *> inheritance from its group, §1985:VI.5.12.4 GR1).
      *> The rule: "the sign of the item appears in the sign position
      *> explicitly or implicitly specified by the SIGN clause."
      *> DERIVED, from the 1985 text alone:
      *>  §1985:VI.5.12.4 GR6a: with SEPARATE CHARACTER the sign is the
      *>  leading (trailing) character position, "this character
      *>  position is not a digit position"; GR6b: the S "is counted in
      *>  determining the size of the item", so S9(4) SEPARATE is 5
      *>  positions; GR6c: the signs are "+" and "-".
      *>  Each image is read by reference modification of the item
      *>  itself: §1985:IV.4.3.8.4.4 GR2 treats a numeric item "as if it
      *>  were redefined as an alphanumeric data item of the same size"
      *>  and GR1 gives "the sign position ... an ordinal number within
      *>  that data item". So no implementor alignment choice
      *>  (§1985:VI.5.13.4 GR2) enters any line.
      *>  EXPL: -42 LEADING SYNC [-0042]; -42 TRAILING SYNCHRONIZED
      *>        [0042-]; +42 LEADING SYNC LEFT [+0042]; +42 TRAILING
      *>        SYNC RIGHT [0042+].
      *>  CTRL: the LEADING SYNC image equals the same item without
      *>        SYNC.
      *>  IMPL: I1V has no SIGN clause of its own; its group says
      *>        TRAILING SEPARATE (GR1), so -42 is [0042-].
      *>  IMPLEQ: the same without SYNC agrees.
      *>  EMBED: TRAILING without SEPARATE (GR5a fixes the position,
      *>        GR5c leaves the sign's form to the implementor), so only
      *>        equality with the item without SYNC is pinned.
      *> Each leg fails if SYNC moves or drops the sign or changes the
      *> size the SIGN clause gives the item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1201.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P1.
           05  P1A  PIC X VALUE "p".
           05  P1V  PIC S9(4) SIGN IS LEADING SEPARATE CHARACTER
                    SYNC VALUE -42.
       01  P2.
           05  P2A  PIC X VALUE "p".
           05  P2V  PIC S9(4) SIGN IS TRAILING SEPARATE
                    SYNCHRONIZED VALUE -42.
       01  P3.
           05  P3A  PIC X VALUE "p".
           05  P3V  PIC S9(4) SIGN IS LEADING SEPARATE
                    SYNC LEFT VALUE +42.
       01  P4.
           05  P4A  PIC X VALUE "p".
           05  P4V  PIC S9(4) SIGN IS TRAILING SEPARATE
                    SYNC RIGHT VALUE +42.
       01  P5.
           05  P5A  PIC X VALUE "p".
           05  P5V  PIC S9(4) SIGN IS LEADING SEPARATE VALUE -42.
       01  I1 SIGN IS TRAILING SEPARATE.
           05  I1A  PIC X VALUE "p".
           05  I1V  PIC S9(4) SYNC VALUE -42.
       01  I2 SIGN IS TRAILING SEPARATE.
           05  I2A  PIC X VALUE "p".
           05  I2V  PIC S9(4) VALUE -42.
       01  J1.
           05  J1A  PIC X VALUE "p".
           05  J1V  PIC S9(4) SIGN IS TRAILING SYNC VALUE -42.
       01  J2.
           05  J2A  PIC X VALUE "p".
           05  J2V  PIC S9(4) SIGN IS TRAILING VALUE -42.
       01  FLG  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "EXPL=[" P1V(1:5) "][" P2V(1:5) "][" P3V(1:5)
               "][" P4V(1:5) "]".
           MOVE "NO" TO FLG.
           IF P1V(1:5) = P5V(1:5)
               MOVE "YES" TO FLG
           END-IF.
           DISPLAY "CTRL=" FLG.
           DISPLAY "IMPL=[" I1V(1:5) "]".
           MOVE "NO" TO FLG.
           IF I1V(1:5) = I2V(1:5)
               MOVE "YES" TO FLG
           END-IF.
           DISPLAY "IMPLEQ=" FLG.
           MOVE "NO" TO FLG.
           IF J1V(1:4) = J2V(1:4)
               MOVE "YES" TO FLG
           END-IF.
           DISPLAY "EMBED=" FLG.
           STOP RUN.
