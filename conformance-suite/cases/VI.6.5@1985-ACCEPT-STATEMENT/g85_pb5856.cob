      *>--- conformance-case
      *> id: g85_pb5856
      *> rules: [1985-GR-VI.6.5.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> stdin: g85_pb5856.in
      *>---
      *> §1985:VI.6.5.4 GR4b - when the device transfers more than the
      *> receiving item holds, only the leftmost characters of the
      *> transferred data are stored and the rest are ignored. The
      *> size compared is the item's size in standard data format,
      *> §1985:IV.4.3.4, whatever its USAGE: four characters for
      *> PIC 9(4) as DISPLAY, COMP or COMP-3. The conversion of those
      *> characters is the implementor's (GR1, docs/conformance/7/
      *> DOC-A.1-1.md), but characters five to eight cannot decide
      *> the value. From the record 12345678, D4, C4 and K4 each
      *> hold 1234; S4 (PIC S9(4) SIGN LEADING SEPARATE, five
      *> characters) reads 12345 and keeps its four low-order digits
      *> 2345 with the sign positive. SC4 (PIC S9(4) COMP) reads the
      *> leftmost four characters of -123456, which are -123, and
      *> holds -123. CV (PIC 9(3)V99 COMP, five characters) reads
      *> 12.50 from 12.50CR and holds 12.50, as DV (DISPLAY) does.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85PB5856.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  D4  PIC 9(4).
       77  C4  PIC 9(4) COMP.
       77  K4  PIC 9(4) COMP-3.
       77  S4  PIC S9(4) SIGN LEADING SEPARATE.
       77  SC4 PIC S9(4) COMP.
       77  CV  PIC 9(3)V99 COMP.
       77  DV  PIC 9(3)V99.
       PROCEDURE DIVISION.
       P1.
           ACCEPT D4.
           DISPLAY "D4[" D4 "]".
           ACCEPT C4.
           DISPLAY "C4[" C4 "]".
           ACCEPT K4.
           DISPLAY "K4[" K4 "]".
           ACCEPT S4.
           DISPLAY "S4[" S4 "]".
           ACCEPT SC4.
           DISPLAY "SC4[" SC4 "]".
           ACCEPT CV.
           DISPLAY "CV[" CV "]".
           ACCEPT DV.
           DISPLAY "DV[" DV "]".
           STOP RUN.
