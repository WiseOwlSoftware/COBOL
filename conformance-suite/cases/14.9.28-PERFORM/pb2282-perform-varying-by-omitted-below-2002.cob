*>--- conformance-case
*> id: pb2282-perform-varying-by-omitted-below-2002
*> rules: [GR-14.9.28.4-12]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2282 - COBOL-85's PERFORM writes BY in its VARYING phrase and in every AFTER phrase; the
      *> omission ISO §14.9.28.4 GR12 permits ("For any BY phrase that is omitted, the augment value is 1") is a
      *> later form, stated in the 2002 text (VCR row 7.42 records the derived 1985 edge). The VARYING phrase writes
      *> BY and only the AFTER phrase omits it, which is where the gate reports (the constructs.json row's own
      *> source witnesses the VARYING site).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2282N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9.
       01 M PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM SHOW-PARA VARYING N FROM 1 BY 1 UNTIL N > 2
               AFTER M FROM 1 UNTIL M > 2
           STOP RUN.
       SHOW-PARA.
           DISPLAY N " " M.
