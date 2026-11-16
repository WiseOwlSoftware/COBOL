      *>--- conformance-case
      *> id: g2-dde-cd-entry-order
      *> rules: [2002-SR-13.14.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR4 - "If the entry-name clause is specified,
      *> either the data-name format or the filler format may be
      *> specified and the entry-name clause shall immediately follow
      *> the level-number. If no entry-name clause is specified, it is
      *> as though the filler format of the entry-name clause were
      *> specified. The REDEFINES clause, if specified, shall immediately
      *> follow the entry-name clause, if specified; otherwise, the
      *> REDEFINES clause shall immediately follow the level-number. If
      *> the TYPEDEF clause is specified, the data-name format of the
      *> entry-name clause shall also be specified and theTYPEDEF clause
      *> shall immediately follow the entry-name clause.The remaining
      *> clauses may be written in any order."
      *> The granted forms, in a record after a CD (the communication
      *> facility, obsolete at 2002, draws warning COBOL0903) and for the
      *> type declaration in working storage:
      *>   B REDEFINES A PIC X(2)    REDEFINES right after the name
      *>   REDEFINES A PIC X(2)      no name: REDEFINES after the level
      *>   PIC X(2) VALUE "CD"       no name: an implied FILLER
      *>   FILLER VALUE "EF" PIC ..  filler format; VALUE before PICTURE
      *>   D VALUE "GH" PIC X(2) USAGE DISPLAY   the remaining clauses
      *>                             in an order other than the usual
      *>   T3 IS TYPEDEF PIC X(3)    TYPEDEF right after the name
      *> (§2002:13.16.42.2 SR10 keeps VALUE off the redefining entries;
      *> SR4 of that clause keeps REDEFINES off level 1 in this section.)
      *> DERIVATION.
      *>   CDREC is A(2) "AB" (B and the unnamed entry redefine it and
      *>   add no storage), the implied FILLER "CD", the FILLER "EF", D
      *>   "GH", then CT of type T3 = PIC X(3); MOVE "XYZW" TO CT stores
      *>   "XYZ" (alphanumeric MOVE, truncated on the right).
      *>   B shares A's storage (§2002:13.16.42.3 GR2): "B=[AB]".
      *>   "REC=[ABCDEFGHXYZ]" (2+2+2+2+3 = 11 characters).
      *>   D was written VALUE before PIC: "D=[GH]".
      *> A leg fails if any of these entries is refused, if an unnamed
      *> entry is given storage other than a FILLER's, if a redefining
      *> entry adds storage (REC would be longer), or if T3 is not X(3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE15.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T3 IS TYPEDEF PIC X(3).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A PIC X(2) VALUE "AB".
           05 B REDEFINES A PIC X(2).
           05 REDEFINES A PIC X(2).
           05 PIC X(2) VALUE "CD".
           05 FILLER VALUE "EF" PIC X(2).
           05 D VALUE "GH" PIC X(2) USAGE DISPLAY.
           05 CT TYPE T3.
       PROCEDURE DIVISION.
           MOVE "XYZW" TO CT
           DISPLAY "B=[" B "]"
           DISPLAY "REC=[" CDREC "]"
           DISPLAY "D=[" D "]"
           STOP RUN.
