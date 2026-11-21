      *>--- conformance-case
      *> id: pb5924-udw-digit-procedure-names-85
      *> rules: [1985-SR-IV.4.2.2.1.1-p5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.1 (cite.py: OK): "must contain at least one alphabetic character"
      *> Section-names and paragraph-names may be digits only; 0200 and
      *> 200 are different names (IV.4.2.2.1.1.3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UDWDPN.
       PROCEDURE DIVISION.
       0100 SECTION.
       0110.
           PERFORM 0200.
           PERFORM 200.
           STOP RUN.
       0200.
           DISPLAY "0200".
       200.
           DISPLAY "200".
