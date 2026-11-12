      *>--- conformance-case
      *> id: pb5180-initialize-replacing-two-categories-85
      *> rules: [1985-FMT-VI.6.17.2]
      *> source-format: fixed
      *>---
      *> §1985:VI.6.17.2 (THE INITIALIZE STATEMENT) prints the five
      *> category words of a REPLACING item in plain braces with no
      *> choice indicators, and §1985:IV.2.1.4: "When braces, { } ,
      *> enclose a portion of a general format, one of the options
      *> contained within the braces must be either explicitly
      *> specified or implicitly selected." So the REPLACING item below,
      *> NUMERIC ALPHANUMERIC DATA BY ZERO, names one category too many
      *> at 85; the 1985 spelling of the request is two items. A
      *> refusal at 85: a compiler that accepts it fails (kb/Work
      *> PB5180).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5180IC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  N PIC 9(3) VALUE 123.
           05  X PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P1.
           INITIALIZE G REPLACING NUMERIC ALPHANUMERIC DATA BY ZERO.
           DISPLAY G.
           STOP RUN.
