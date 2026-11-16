      *>--- conformance-case
      *> id: g85_bf09_03
      *> rules: [1985-FMT-VI.5.10.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.10.2 - the format "level-number [data-name-1 |
      *> FILLER] REDEFINES data-name-2": the subject may be a data-name,
      *> FILLER, or omitted (the brackets are optional), at level 05 and
      *> at level 01.
      *>   cite.py: OK  §1985:VI.5.10.2 "Level-number, data-name-1, and
      *>     FILLER are not part of the REDEFINES clause"
      *> DERIVATION. W is ABCDEF. The unnamed 05 REDEFINES W puts W-MID
      *> on its 3rd and 4th characters: CD. The FILLER REDEFINES W puts
      *> W-HEAD on its first three: ABC. The unnamed 01 REDEFINES NUMS
      *> (4321) splits it into NUM-HI 43 and NUM-LO 21. MOVE "XY" TO
      *> W-MID changes W to ABXYEF. A compiler that refuses a
      *> subject-less REDEFINES fails to translate; one that allocates
      *> the redefinition elsewhere shows other characters.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0903.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AREA-1.
           05  W PIC X(6) VALUE "ABCDEF".
           05  REDEFINES W.
               10  PIC X(2).
               10  W-MID PIC X(2).
               10  PIC X(2).
           05  FILLER REDEFINES W.
               10  W-HEAD PIC X(3).
               10  PIC X(3).
       01  NUMS PIC 9(4) VALUE 4321.
       01  REDEFINES NUMS.
           05  NUM-HI PIC 99.
           05  NUM-LO PIC 99.
       PROCEDURE DIVISION.
       P1.
           DISPLAY W-MID.
           DISPLAY W-HEAD.
           DISPLAY NUM-HI " " NUM-LO.
           MOVE "XY" TO W-MID.
           DISPLAY W.
           STOP RUN.
