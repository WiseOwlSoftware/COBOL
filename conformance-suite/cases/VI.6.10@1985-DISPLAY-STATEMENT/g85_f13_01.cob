      *>--- conformance-case
      *> id: g85_f13_01
      *> rules: [1985-GR-VI.6.10.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.10.4 GR7 - a DISPLAY with no UPON phrase uses the
      *> implementor's standard display device. WiseOwl COBOL's device
      *> is standard output (docs/conformance/7/DOC-A.1-59.md), the
      *> stream the harness compares. SYSOUT names that same device and
      *> SYSERR names standard error, a different one.
      *> Why each leg can fail: the three lines with no UPON phrase and
      *> the SYSOUT line must appear in order; the SYSERR line must NOT
      *> appear, so an implementation that sent a DISPLAY without UPON
      *> anywhere but the standard device, or sent every device to one
      *> stream, prints a different file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1301.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS OUT-DEV
           SYSERR IS ERR-DEV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X(4) VALUE "ITEM".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "NOUPON=STDOUT".
           DISPLAY "SYSOUT=STDOUT" UPON OUT-DEV.
           DISPLAY "SYSERR=NOTSTDOUT" UPON ERR-DEV.
           DISPLAY "NOUPON " WS-A.
           DISPLAY "TAIL=SEEN".
           STOP RUN.
