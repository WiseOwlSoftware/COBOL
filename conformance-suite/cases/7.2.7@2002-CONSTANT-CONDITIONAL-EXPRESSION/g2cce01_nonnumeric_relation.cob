      *>--- conformance-case
      *> id: g2cce01_nonnumeric_relation
      *> rules: [2002-GR-7.2.7.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.7.2 GR2 - "For a simple relation condition where the
      *>   operands are not numeric or boolean, no collating sequence is
      *>   used for the comparison. A character by character comparison
      *>   for equality based on the binary value of each character's
      *>   encoding is used. If the literals are of unequal length they
      *>   are not equal." NOTE: "uppercase and lowercase letters are
      *>   not equivalent". The operators are the 2002 set, "IS [NOT]
      *>   EQUAL TO" or "IS [NOT] =" (§2002:7.2.7.1 SR1a2), over
      *>   alphanumeric and national literals and a compilation variable
      *>   that references an alphanumeric literal (§2002:7.2.9.3 GR7).
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   CASE-NE       "abc" = "ABC": lowercase and uppercase differ.
      *>   LEN-NE        "AB" = "AB ": unequal length, not equal (an
      *>                 ordinary relation pads with spaces and would be
      *>                 equal).
      *>   EQ            "AB" IS EQUAL TO "AB".
      *>   NE            "AB" IS NOT = "AC".
      *>   NOT-EQ-FALSE  "AB" IS NOT EQUAL TO "AB" is FALSE.
      *>   NAT-EQ        N"AB" = N"AB".
      *>   NAT-CASE-NE   N"ab" = N"AB".
      *>   NAT-LEN-NE    N"AB" = N"AB ".
      *>   VAR-CASE-NE   V (references "abc") = "ABC".
      *>   VAR-EQ        V = "abc".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CCE01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       >>IF "abc" = "ABC"
           DISPLAY "CASE-EQ-BAD"
       >>ELSE
           DISPLAY "CASE-NE"
       >>END-IF
       >>IF "AB" = "AB "
           DISPLAY "LEN-EQ-BAD"
       >>ELSE
           DISPLAY "LEN-NE"
       >>END-IF
       >>IF "AB" IS EQUAL TO "AB"
           DISPLAY "EQ"
       >>ELSE
           DISPLAY "EQ-BAD"
       >>END-IF
       >>IF "AB" IS NOT = "AC"
           DISPLAY "NE"
       >>ELSE
           DISPLAY "NE-BAD"
       >>END-IF
       >>IF "AB" IS NOT EQUAL TO "AB"
           DISPLAY "NOT-EQ-BAD"
       >>ELSE
           DISPLAY "NOT-EQ-FALSE"
       >>END-IF
       >>IF N"AB" = N"AB"
           DISPLAY "NAT-EQ"
       >>ELSE
           DISPLAY "NAT-EQ-BAD"
       >>END-IF
       >>IF N"ab" = N"AB"
           DISPLAY "NAT-CASE-BAD"
       >>ELSE
           DISPLAY "NAT-CASE-NE"
       >>END-IF
       >>IF N"AB" = N"AB "
           DISPLAY "NAT-LEN-BAD"
       >>ELSE
           DISPLAY "NAT-LEN-NE"
       >>END-IF
       >>DEFINE V AS "abc"
       >>IF V = "ABC"
           DISPLAY "VAR-CASE-BAD"
       >>ELSE
           DISPLAY "VAR-CASE-NE"
       >>END-IF
       >>IF V = "abc"
           DISPLAY "VAR-EQ"
       >>ELSE
           DISPLAY "VAR-EQ-BAD"
       >>END-IF
           STOP RUN.
