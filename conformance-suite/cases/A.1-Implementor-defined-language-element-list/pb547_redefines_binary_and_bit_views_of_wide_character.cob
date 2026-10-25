*>--- conformance-case
*> id: pb547_redefines_binary_and_bit_views_of_wide_character
*> rules: [GR-8.1.2-1, GR-13.18.60.4-7, DOC-A.1-31, DOC-A.1-46]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 §8.1.2 1) (every character of the alphanumeric coded character set occupies the same number of
      *> bytes), §13.18.44.4 GR1 (a REDEFINES entry shares ONE storage area) and §13.18.60.4 GR7 (USAGE DISPLAY: one
      *> byte per position, DOC-A.1-209): the binary item B and the bit item BITS below read the storage image of the
      *> alphanumeric item A.
      *>
      *> The determination (docs/CONFORMANCE.md DOC-A.1-31, -33, -46; kb/Work PB547): a code unit with no one-byte image
      *> (above U+00FF, other than HIGH-VALUE) reads at an in-memory byte channel as the byte '?' (0x3F) and sets
      *> EC-DATA-CONVERSION. Before PB547 every numeric and bit view took the code unit's low-order byte: the euro
      *> (U+20AC) was the byte 0xAC, so B held 0xAC414243 = 2889957955 and nothing was raised.
      *>
      *> Why each leg can fail:
      *>  CTL-B   - "ABCD" is 41 42 43 44 = 1094861636 and nothing is raised.
      *>  EURO-B  - A = euro + "ABC": bytes 3F 41 42 43 = 1061241411 (not 0xAC414243 = 2889957955), and
      *>            EC-DATA-CONVERSION is the last exception.
      *>  EURO-BIT - the bit view of the same four bytes: 0011 1111 0100 0001 0100 0010 0100 0011.
identification division.
program-id. PB547BB.
data division.
working-storage section.
01 g.
   05 a pic x(4).
01 vb redefines g.
   05 b pic 9(9) comp.
01 vt redefines g.
   05 bits pic 1(32) usage bit.
01 outb pic 9(10).
01 outbits pic 1(32) usage bit.
procedure division.
main.
>>TURN EC-DATA-CONVERSION CHECKING ON
    move "ABCD" to a
    move b to outb
    display "CTL-B " outb
    display "EC0=[" function exception-status "]"
    move "€ABC" to a
    move b to outb
    display "EURO-B " outb
    display "EC1=[" function exception-status "]"
    move bits to outbits
    display "EURO-BIT " outbits
    stop run.
