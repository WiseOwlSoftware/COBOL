      *>--- conformance-case
      *> id: g2cd05_fna_after_text_refused
      *> rules: [2002-SR-7.2.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.2 SR2 - "A compiler directive shall be preceded only
      *> by zero, one, or more space characters." The 2002
      *> FLAG-NATIVE-ARITHMETIC directive (§2002:7.2.12) below follows
      *> program text (DISPLAY "B") on its line; refused at 2002 at that
      *> line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           DISPLAY "A"
           DISPLAY "B" >>FLAG-NATIVE-ARITHMETIC ON
           STOP RUN.
