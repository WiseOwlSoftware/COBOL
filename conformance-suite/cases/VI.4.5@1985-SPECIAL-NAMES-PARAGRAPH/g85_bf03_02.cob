      *>--- conformance-case
      *> id: g85_bf03_02
      *> rules: [1985-GR-VI.4.5.4-5, 1985-GR-VI.4.5.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.4 GR5 - when more than one character has the
      *> highest position in the program collating sequence, the LAST
      *> one specified is HIGH-VALUE; GR6 - when more than one has the
      *> lowest, the FIRST one specified is LOW-VALUE. AL names every
      *> native character once (ordinals 1-65536); its first position
      *> is "B" ALSO "A" and its last 65536 ALSO "Z" (GR4d 6: ALSO
      *> shares one position). LOW-VALUE is therefore "B" (not "A")
      *> and HIGH-VALUE "Z" (not the character of native ordinal
      *> 65536); taking the other member of either tie fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0302.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. WISEOWL
           PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "B" ALSO "A" 1 THRU 65 68 THRU 90
               92 THRU 65535 65536 ALSO "Z".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X2 PIC X(2).
       PROCEDURE DIVISION.
       P1.
           MOVE LOW-VALUES TO X2.
           DISPLAY "LV=" X2.
           MOVE HIGH-VALUES TO X2.
           DISPLAY "HV=" X2.
           DISPLAY "LV1=" LOW-VALUE " HV1=" HIGH-VALUE.
           STOP RUN.
