      *>--- conformance-case
      *> id: g2mrg03-merge-collating-precedence
      *> rules: [2002-GR-14.8.23.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.23.3 GR5 - the alphanumeric and the national
      *> collating sequences for MERGE keys are "separately determined
      *> ... a) First, the collating sequence established by the
      *> COLLATING SEQUENCE phrase, if specified, in this MERGE
      *> statement. The collating sequence associated with
      *> alphabet-name-1 applies to key data items of class alphabetic
      *> and alphanumeric; the collating sequence associated with
      *> alphabet-name-2 applies to key data items of class national.
      *> b) Second, the collating sequences established as the program
      *> collating sequences."
      *> Five sequences, all different, so no leg passes on another's:
      *>   native A<B<C; program PRG-AN "BCA": B<C<A; program PRG-NAT
      *>   N"CAB": C<A<B; statement REV-AN "CBA": C<B<A; statement
      *>   REV-NAT N"BAC": B<A<C.
      *> (§2002:12.2.5.3 GR9 establishes PRG-AN/PRG-NAT as the
      *> program collating sequences.) Each leg writes its expected
      *> order s1 s2 s3 as F1 = s1 s3 and F2 = s2, in the order the
      *> leg's own sequence gives (§2002:14.8.23.3 GR6: input not
      *> ordered by "the collating sequence associated with the MERGE
      *> statement" leaves the merge undefined), and DISPLAYs the
      *> merged tags:
      *>   L1 alnum key, no phrase           -> GR5b PRG-AN : B C A
      *>   L2 alnum key, FOR ALPHANUMERIC    -> GR5a REV-AN : C B A
      *>   L3 national key, no phrase        -> GR5b PRG-NAT: C A B
      *>   L4 national key, FOR NATIONAL     -> GR5a REV-NAT: B A C
      *>   L5 national key, FOR ALPHANUMERIC only -> alphabet-name-1
      *>      does not reach a national key, GR5b PRG-NAT : C A B
      *>   L6 alnum key, IS REV-AN REV-NAT   -> GR5a REV-AN : C B A
      *>   L7 national key, IS REV-AN REV-NAT-> GR5a REV-NAT: B A C
      *> A merge under any other sequence takes a different head first
      *> (e.g. L1 natively gives B A C, L3 natively A C B), so each
      *> line fails if the wrong sequence is chosen for its class.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MRG03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER.
           PROGRAM COLLATING SEQUENCE IS PRG-AN PRG-NAT.
       SPECIAL-NAMES.
           ALPHABET PRG-AN IS "BCA"
           ALPHABET PRG-NAT FOR NATIONAL IS N"CAB"
           ALPHABET REV-AN IS "CBA"
           ALPHABET REV-NAT FOR NATIONAL IS N"BAC".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "g2mrg03w.tmp".
           SELECT F1 ASSIGN TO "g2mrg031.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "g2mrg032.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SNK PIC N(1).
          05 SAK PIC X(1).
          05 STG PIC X(1).
       FD F1.
       01 R1.
          05 R1N PIC N(1).
          05 R1A PIC X(1).
          05 R1T PIC X(1).
       FD F2.
       01 R2.
          05 R2N PIC N(1).
          05 R2A PIC X(1).
          05 R2T PIC X(1).
       WORKING-STORAGE SECTION.
       01 W-S1 PIC X.
       01 W-S2 PIC X.
       01 W-S3 PIC X.
       01 W-NAT PIC X VALUE "N".
       01 W-LINE PIC X(3).
       01 W-POS PIC 9.
       01 EOF-M PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "N" TO W-NAT
           MOVE "B" TO W-S1 MOVE "C" TO W-S2 MOVE "A" TO W-S3
           PERFORM BUILD
           MERGE SW ON ASCENDING KEY SAK USING F1 F2
               OUTPUT PROCEDURE IS MOUT
           DISPLAY "L1=" W-LINE
           MOVE "C" TO W-S1 MOVE "B" TO W-S2 MOVE "A" TO W-S3
           PERFORM BUILD
           MERGE SW ON ASCENDING KEY SAK
               COLLATING SEQUENCE FOR ALPHANUMERIC IS REV-AN
               USING F1 F2
               OUTPUT PROCEDURE IS MOUT
           DISPLAY "L2=" W-LINE
           MOVE "Y" TO W-NAT
           MOVE "C" TO W-S1 MOVE "A" TO W-S2 MOVE "B" TO W-S3
           PERFORM BUILD
           MERGE SW ON ASCENDING KEY SNK USING F1 F2
               OUTPUT PROCEDURE IS MOUT
           DISPLAY "L3=" W-LINE
           MOVE "B" TO W-S1 MOVE "A" TO W-S2 MOVE "C" TO W-S3
           PERFORM BUILD
           MERGE SW ON ASCENDING KEY SNK
               COLLATING SEQUENCE FOR NATIONAL IS REV-NAT
               USING F1 F2
               OUTPUT PROCEDURE IS MOUT
           DISPLAY "L4=" W-LINE
           MOVE "C" TO W-S1 MOVE "A" TO W-S2 MOVE "B" TO W-S3
           PERFORM BUILD
           MERGE SW ON ASCENDING KEY SNK
               COLLATING SEQUENCE FOR ALPHANUMERIC IS REV-AN
               USING F1 F2
               OUTPUT PROCEDURE IS MOUT
           DISPLAY "L5=" W-LINE
           MOVE "N" TO W-NAT
           MOVE "C" TO W-S1 MOVE "B" TO W-S2 MOVE "A" TO W-S3
           PERFORM BUILD
           MERGE SW ON ASCENDING KEY SAK
               COLLATING SEQUENCE IS REV-AN REV-NAT
               USING F1 F2
               OUTPUT PROCEDURE IS MOUT
           DISPLAY "L6=" W-LINE
           MOVE "Y" TO W-NAT
           MOVE "B" TO W-S1 MOVE "A" TO W-S2 MOVE "C" TO W-S3
           PERFORM BUILD
           MERGE SW ON ASCENDING KEY SNK
               COLLATING SEQUENCE IS REV-AN REV-NAT
               USING F1 F2
               OUTPUT PROCEDURE IS MOUT
           DISPLAY "L7=" W-LINE
           STOP RUN.
      *> F1 = s1 s3, F2 = s2. The key not under test is held constant.
       BUILD.
           OPEN OUTPUT F1
           MOVE W-S1 TO R1T
           PERFORM SET-R1
           WRITE R1
           MOVE W-S3 TO R1T
           PERFORM SET-R1
           WRITE R1
           CLOSE F1
           OPEN OUTPUT F2
           MOVE W-S2 TO R2T
           IF W-NAT = "Y"
               MOVE "x" TO R2A
               EVALUATE R2T
                   WHEN "A" MOVE N"A" TO R2N
                   WHEN "B" MOVE N"B" TO R2N
                   WHEN "C" MOVE N"C" TO R2N
               END-EVALUATE
           ELSE
               MOVE N"x" TO R2N
               MOVE R2T TO R2A
           END-IF
           WRITE R2
           CLOSE F2.
       SET-R1.
           IF W-NAT = "Y"
               MOVE "x" TO R1A
               EVALUATE R1T
                   WHEN "A" MOVE N"A" TO R1N
                   WHEN "B" MOVE N"B" TO R1N
                   WHEN "C" MOVE N"C" TO R1N
               END-EVALUATE
           ELSE
               MOVE N"x" TO R1N
               MOVE R1T TO R1A
           END-IF.
       MOUT.
           MOVE SPACES TO W-LINE
           MOVE 0 TO W-POS
           MOVE "N" TO EOF-M
           PERFORM UNTIL EOF-M = "Y"
               RETURN SW AT END MOVE "Y" TO EOF-M
                   NOT AT END
                       ADD 1 TO W-POS
                       MOVE STG TO W-LINE (W-POS:1)
               END-RETURN
           END-PERFORM.
