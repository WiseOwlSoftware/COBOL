*>--- conformance-case
*> id: pb5856-accept-binary-packed-window
*> rules: [GR-14.9.1.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*> stdin: pb5856-accept-binary-packed-window.in
*>---
      *> §2023:14.9.1.4 GR4b: when the device transfers more than the receiving item holds, "only the leftmost
      *> characters of the transferred data are stored"; the rest are ignored. The size compared is the item's size
      *> (PIC 9(4) is four characters as DISPLAY, COMP or COMP-3); the conversion of the stored characters
      *> is the implementor's (GR1, docs/conformance/7/DOC-A.1-1.md), but ignored characters cannot decide the value.
      *> From the record 12345678, D4, C4 and K4 each hold 1234; S4 (PIC S9(4) SIGN LEADING SEPARATE, five
      *> characters) reads 12345 and keeps its four low-order digits, 2345; SP (PIC S9(4) COMP-3) reads -123 from
      *> -123456 and holds -123; CV (PIC 9(3)V99 COMP, five characters) reads 12.50 from 12.50CR, as DV (DISPLAY) does.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5856W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  D4  PIC 9(4).
       77  C4  PIC 9(4) COMP.
       77  K4  PIC 9(4) COMP-3.
       77  S4  PIC S9(4) SIGN LEADING SEPARATE.
       77  SP  PIC S9(4) COMP-3.
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
           ACCEPT SP.
           DISPLAY "SP[" SP "]".
           ACCEPT CV.
           DISPLAY "CV[" CV "]".
           ACCEPT DV.
           DISPLAY "DV[" DV "]".
           STOP RUN.
