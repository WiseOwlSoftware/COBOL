      *>--- conformance-case
      *> id: g2-call-cd-arguments
      *> rules: [2002-GR-14.1.3-2, 2002-GR-14.1.3-6, 2002-GR-14.1.3-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Arguments and the returning item of a CALL are items of a
      *> record under a CD entry (§2002:A.1.1.2), which 2002 admits:
      *> §2002:14.8.4.2 SR3 "a data item defined in the file,
      *> working-storage, local-storage, linkage, or communication
      *> section" and SR7 for the RETURNING identifier-3.
      *> G2PDS01S is a separately compiled program, named by a literal,
      *> with no program-specifier and no NESTED phrase.
      *> §2002:14.1.3 GR2 - "The correspondence between the arguments
      *> and the formal parameters is established on a positional
      *> basis": the four arguments are distinct, so any other pairing
      *> shows in the callee's line.
      *> §2002:14.1.3 GR9 - by content "as if this record does not
      *> occupy the same storage area as the argument", "of the same
      *> length as the argument", and "That argument is moved to this
      *> allocated record without conversion": CN (PIC S9(4) SIGN
      *> LEADING SEPARATE, -7) lands in the PIC X(5) formal L-N as its
      *> five characters "-0007".  A MOVE would drop the separate sign
      *> (§2002:14.8.24.3 GR4a) and give "0007 ", so the line tells a
      *> raw copy from a conversion.  The callee's stores into L-X and
      *> L-N leave CX and CN unchanged.
      *> By reference (§2002:14.1.3 GR8) CK and CY are the formals, so
      *> the callee's ADD 1 and MOVE "QQQ" are seen by the caller.
      *> §2002:14.1.3 GR6 - data-name-2 L-R is "the name used in the
      *> function, method, or program for the result that is returned
      *> to the activating element"; §2002:14.5.5 "the result is placed
      *> in the data item referenced by that RETURNING phrase": CR gets
      *> L-K * 2 = 22 * 2 = 44.
      *> Signed items are SIGN LEADING SEPARATE (§2002:13.16.50.3 GR6b),
      *> so they display as "+0021".  Formals have the arguments'
      *> lengths (§2002:14.7.1.2.1, 14.7.1.2.2 rule 1).
      *>   CALLEE X=[ABCDEFGHIJ] K=+0021 N=[-0007] Y=[XYZ]
      *>   CALLER X=[ABCDEFGHIJ] K=+0022 N=-0007 Y=[QQQ] R=+0044
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PDS01.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  CREC.
           05 CX PIC X(10).
           05 CN PIC S9(4) SIGN IS LEADING SEPARATE.
           05 CK PIC S9(4) SIGN IS LEADING SEPARATE.
           05 CY PIC X(3).
           05 CR PIC S9(4) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABCDEFGHIJ" TO CX
           MOVE -7 TO CN
           MOVE 21 TO CK
           MOVE "XYZ" TO CY
           MOVE 0 TO CR
           CALL "G2PDS01S" USING BY CONTENT CX BY REFERENCE CK
                BY CONTENT CN BY REFERENCE CY
                RETURNING CR
           DISPLAY "CALLER X=[" CX "] K=" CK " N=" CN
                   " Y=[" CY "] R=" CR
           STOP RUN.
       END PROGRAM G2PDS01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PDS01S.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-X PIC X(10).
       01  L-K PIC S9(4) SIGN IS LEADING SEPARATE.
       01  L-N PIC X(5).
       01  L-Y PIC X(3).
       01  L-R PIC S9(4) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION USING L-X L-K L-N L-Y RETURNING L-R.
       SUB-P.
           DISPLAY "CALLEE X=[" L-X "] K=" L-K " N=[" L-N
                   "] Y=[" L-Y "]"
           MOVE "ZZZZZZZZZZ" TO L-X
           MOVE "99999" TO L-N
           ADD 1 TO L-K
           MOVE "QQQ" TO L-Y
           COMPUTE L-R = L-K * 2
           GOBACK.
       END PROGRAM G2PDS01S.
