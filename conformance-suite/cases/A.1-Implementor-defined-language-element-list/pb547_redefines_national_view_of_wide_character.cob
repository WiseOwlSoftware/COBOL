*>--- conformance-case
*> id: pb547_redefines_national_view_of_wide_character
*> rules: [GR-8.1.2-1, GR-13.18.60.4-7, DOC-A.1-31, DOC-A.1-33, DOC-A.1-46]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §8.1.2 1): "The number of bytes used in the memory of the computer to represent characters in
      *> the alphanumeric coded character set shall be the same for all characters in that coded character set", and
      *> §13.18.44.4 GR1: a REDEFINES entry's storage association is ONE storage area, so the national item N1 below
      *> reads the very bytes of the alphanumeric item A1 (§13.18.60.4 GR7 / GR8: one byte per DISPLAY position, two per
      *> NATIONAL position, UTF-16BE).
      *>
      *> The determination (docs/CONFORMANCE.md DOC-A.1-31, -33, -46; kb/Work PB547): an alphanumeric position holds any
      *> UTF-16 code unit in memory; at an in-memory byte channel a code unit with no one-byte image (above U+00FF, other
      *> than HIGH-VALUE, which is the byte 0xFF) is the byte '?' (0x3F) and EC-DATA-CONVERSION is set, "visible under
      *> checking and never silent" — the disposition decided for CONVERT's byte legs, applied to the storage image
      *> every view reads. Before PB547 the national view read the euro (U+20AC) as its low-order byte 0xAC, so N1 held
      *> U+AC41 and a write back through N1 turned the euro into U+00AC, with no condition.
      *>
      *> Why each leg can fail:
      *>  CTL-A  - "ABCD" are bytes 41 42 43 44: N1 is U+4142 U+4344 (ORD 16707, 17221) and nothing is raised.
      *>  CTL-HV - HIGH-VALUES is the byte 0xFF (§8.3.3.6.4 GR6 with the storage-byte law): N1 is U+FFFF U+FFFF (ORD 65536
      *>           twice) and nothing is raised — a character WITH a byte image never trips the condition.
      *>  EURO-N - A1 = euro + "ABC": the euro's byte is 3F, so N1(1) is U+3F41 (ORD 16194) not U+AC41 (ORD 44098), and
      *>           N1(2) is U+4243 (ORD 16964); EC-DATA-CONVERSION is the last exception.
      *>  EURO-W - writing N1 back (MOVE SAVEN TO N1) stores the bytes it read: A1 is "?ABC", the euro's image, and no
      *>           longer the unrelated U+00AC "¬" the defect stored.
identification division.
program-id. PB547NV.
data division.
working-storage section.
01 g1.
   05 a1 pic x(4).
01 r1 redefines g1.
   05 n1 pic n(2).
01 saven pic n(2).
procedure division.
main.
>>TURN EC-DATA-CONVERSION CHECKING ON
    move "ABCD" to a1
    move n1 to saven
    display "CTL-A " function ord(saven(1:1)) " " function ord(saven(2:1))
    move high-values to a1
    move n1 to saven
    display "CTL-HV " function ord(saven(1:1)) " " function ord(saven(2:1))
    display "EC0=[" function exception-status "]"
    move "€ABC" to a1
    move n1 to saven
    display "EURO-N " function ord(saven(1:1)) " " function ord(saven(2:1))
    display "EC1=[" function exception-status "]"
    move saven to n1
    display "EURO-W [" a1 "]"
    stop run.
