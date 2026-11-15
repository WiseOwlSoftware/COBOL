      *>--- conformance-case
      *> id: g2acp01_chronological_items
      *> rules: [2002-GR-14.8.1.3-7, 2002-GR-14.8.1.3-8, 2002-GR-14.8.1.3-9, 2002-GR-14.8.1.3-10, 2002-GR-14.8.1.3-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.1.3 GR6 - format 2 transfers the conceptual item
      *> "according to the rules for the MOVE statement"; GR7..GR12
      *> give each conceptual item's description. The date is not
      *> fixed, so each line pins a SHAPE and a cross-check against
      *> the 2002 date functions, never a literal date:
      *>  §2002:15.16.2 RV1 CURRENT-DATE (1:8) is "Four numeric digits
      *>    of the year", month 01-12, day 01-31: the same YYYYMMDD.
      *>  §2002:15.36.3 RV1 INTEGER-OF-DATE is "the number of days the
      *>    date represented by argument-1 succeeds December 31, 1600";
      *>    day 1 (1601-01-01) was a Monday, so MOD (n - 1, 7) + 1 is
      *>    1 for Monday .. 7 for Sunday (ORACLE line: 2026-11-16 is a
      *>    Monday, so it prints 1).
      *>  §2002:15.19.3 RV2 DAY-OF-INTEGER is "an integer of the form
      *>    (YYYYDDD)".
      *> Every ACCEPT runs in one window bracketed by two ACCEPT DATE
      *> YYYYMMDD that must agree (retried across a midnight).
      *> §2002:14.8.1.3 GR8 DATE YYYYMMDD: an 8-digit unsigned display
      *>   integer equal to CURRENT-DATE (1:8); into 9(10) it is
      *>   zero-filled on the left (§2002:14.8.24.3 GR4b, §2002:14.5.8
      *>   rule 1): "00" then YYYYMMDD; into X(10) left-aligned
      *>   with two spaces (§2002:14.8.24.3 GR4a, §2002:14.5.8 rule
      *>   5), which pins the eight-digit width.
      *> §2002:14.8.1.3 GR7 DATE: six digits, the two low-order year
      *>   digits then MMDD = YYYYMMDD (3:6); into X(10) left-aligned
      *>   with four spaces (§2002:14.8.24.3 GR4a, §2002:14.5.8 rule
      *>   5); into 9(4) the high-order digits truncate: MMDD.
      *> §2002:14.8.1.3 GR9 DAY: five digits, YY then DDD (001-366),
      *>   DDD = INTEGER-OF-DATE (today) - INTEGER-OF-DATE (YYYY0101)
      *>   + 1; into X(7) left-aligned with two spaces, which pins
      *>   the five-digit width (a 9(5) receiver would truncate a
      *>   wider sender to the same YYDDD).
      *> §2002:14.8.1.3 GR10 DAY YYYYDDD: seven digits YYYY * 1000 +
      *>   DDD, equal to DAY-OF-INTEGER (INTEGER-OF-DATE (today)); into
      *>   X(9) left-aligned with two spaces.
      *> §2002:14.8.1.3 GR12 DAY-OF-WEEK: one digit 1..7 equal to the
      *>   MOD oracle; into X(3) the digit then two spaces, into 99
      *>   zero-filled on the left.
      *> Why each leg can fail: a wrong width (an alphanumeric
      *> receiver shows any width; a numeric one only a narrower
      *> width), a wrong field order, a day-of-year counted from
      *> 0, a week that starts on Sunday or counts 0-6, or a MOVE that
      *> aligns the conceptual item as other than an unsigned integer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACP01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D8A          PIC 9(8).
       01  G8.
           05  D8       PIC 9(8).
       01  G8R REDEFINES G8.
           05  Y4       PIC 9(4).
           05  M2       PIC 99.
           05  DD2      PIC 99.
       01  G6.
           05  D6       PIC 9(6).
       01  G4.
           05  D4       PIC 9(4).
       01  G5.
           05  D5       PIC 9(5).
       01  G7.
           05  D7       PIC 9(7).
       01  G10.
           05  DN10     PIC 9(10).
       01  GW.
           05  DW       PIC 9.
       01  GW99.
           05  DW99     PIC 99.
       01  DX10         PIC X(10).
       01  DX9          PIC X(9).
       01  DX10B        PIC X(10).
       01  DX7          PIC X(7).
       01  DWX          PIC X(3).
       01  CD21         PIC X(21).
       01  N-TODAY      PIC 9(7).
       01  N-JAN1       PIC 9(7).
       01  DDD          PIC 9(3).
       01  EXP5         PIC 9(5).
       01  EXP7         PIC 9(7).
       01  EXPW         PIC 9.
       01  TRIES        PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       P0.
           PERFORM CAPTURE WITH TEST AFTER
               UNTIL D8A = D8 OR TRIES > 5.
           IF D8A NOT = D8
               DISPLAY "CLOCK UNSTABLE " D8A " " D8
           END-IF.
           COMPUTE EXPW = FUNCTION MOD
               (FUNCTION INTEGER-OF-DATE (20261116) - 1, 7) + 1.
           DISPLAY "ORACLE 2026-11-16 IS DAY " EXPW.
      *> GR8
           IF G8 IS NUMERIC AND M2 >= 1 AND M2 <= 12
              AND DD2 >= 1 AND DD2 <= 31 AND G8 = CD21 (1:8)
               DISPLAY "GR8 DATE YYYYMMDD: 8 DIGITS = CURRENT-DATE"
           ELSE
               DISPLAY "GR8 WRONG [" G8 "] [" CD21 (1:8) "]"
           END-IF.
           IF G10 (1:2) = "00" AND G10 (3:8) = G8
               DISPLAY "GR8 INTO 9(10): 00 THEN YYYYMMDD"
           ELSE
               DISPLAY "GR8 9(10) WRONG [" G10 "]"
           END-IF.
           IF DX10B (1:8) = G8 AND DX10B (9:2) = SPACES
               DISPLAY "GR8 INTO X(10): YYYYMMDD THEN 2 SPACES"
           ELSE
               DISPLAY "GR8 X(10) WRONG [" DX10B "]"
           END-IF.
      *> GR7
           IF G6 IS NUMERIC AND G6 = G8 (3:6)
               DISPLAY "GR7 DATE: 6 DIGITS = YYMMDD"
           ELSE
               DISPLAY "GR7 WRONG [" G6 "] [" G8 "]"
           END-IF.
           IF DX10 (1:6) = G8 (3:6) AND DX10 (7:4) = SPACES
               DISPLAY "GR7 INTO X(10): YYMMDD THEN 4 SPACES"
           ELSE
               DISPLAY "GR7 X(10) WRONG [" DX10 "]"
           END-IF.
           IF G4 = G8 (5:4)
               DISPLAY "GR7 INTO 9(4): MMDD"
           ELSE
               DISPLAY "GR7 9(4) WRONG [" G4 "]"
           END-IF.
      *> GR9
           COMPUTE N-TODAY = FUNCTION INTEGER-OF-DATE (D8).
           COMPUTE N-JAN1 = FUNCTION INTEGER-OF-DATE
               (Y4 * 10000 + 0101).
           COMPUTE DDD = N-TODAY - N-JAN1 + 1.
           COMPUTE EXP5 = FUNCTION MOD (Y4, 100) * 1000 + DDD.
           IF G5 IS NUMERIC AND D5 = EXP5
              AND DDD >= 1 AND DDD <= 366
               DISPLAY "GR9 DAY: 5 DIGITS = YYDDD"
           ELSE
               DISPLAY "GR9 WRONG [" G5 "] " EXP5
           END-IF.
           IF DX7 (1:5) = G5 AND DX7 (6:2) = SPACES
               DISPLAY "GR9 INTO X(7): YYDDD THEN 2 SPACES"
           ELSE
               DISPLAY "GR9 X(7) WRONG [" DX7 "]"
           END-IF.
      *> GR10
           COMPUTE EXP7 = Y4 * 1000 + DDD.
           IF G7 IS NUMERIC AND D7 = EXP7
              AND D7 = FUNCTION DAY-OF-INTEGER (N-TODAY)
               DISPLAY "GR10 DAY YYYYDDD: 7 DIGITS = YYYYDDD"
           ELSE
               DISPLAY "GR10 WRONG [" G7 "] " EXP7
           END-IF.
           IF DX9 (1:7) = G7 AND DX9 (8:2) = SPACES
               DISPLAY "GR10 INTO X(9): YYYYDDD THEN 2 SPACES"
           ELSE
               DISPLAY "GR10 X(9) WRONG [" DX9 "]"
           END-IF.
      *> GR12
           COMPUTE EXPW = FUNCTION MOD (N-TODAY - 1, 7) + 1.
           IF GW IS NUMERIC AND DW = EXPW AND DW >= 1 AND DW <= 7
               DISPLAY "GR12 DAY-OF-WEEK: 1 DIGIT, 1 = MONDAY"
           ELSE
               DISPLAY "GR12 WRONG [" GW "] " EXPW
           END-IF.
           IF DWX (1:1) = GW AND DWX (2:2) = SPACES
              AND GW99 (1:1) = "0" AND GW99 (2:1) = GW
               DISPLAY "GR12 INTO X(3) AND 99: D THEN 2 SPACES, 0D"
           ELSE
               DISPLAY "GR12 MOVE WRONG [" DWX "] [" GW99 "]"
           END-IF.
           STOP RUN.
       CAPTURE.
           ADD 1 TO TRIES.
           ACCEPT D8A FROM DATE YYYYMMDD.
           ACCEPT D6 FROM DATE.
           ACCEPT DX10 FROM DATE.
           ACCEPT D4 FROM DATE.
           ACCEPT DN10 FROM DATE YYYYMMDD.
           ACCEPT DX10B FROM DATE YYYYMMDD.
           ACCEPT D5 FROM DAY.
           ACCEPT DX7 FROM DAY.
           ACCEPT D7 FROM DAY YYYYDDD.
           ACCEPT DX9 FROM DAY YYYYDDD.
           ACCEPT DW FROM DAY-OF-WEEK.
           ACCEPT DWX FROM DAY-OF-WEEK.
           ACCEPT DW99 FROM DAY-OF-WEEK.
           MOVE FUNCTION CURRENT-DATE TO CD21.
           ACCEPT D8 FROM DATE YYYYMMDD.
