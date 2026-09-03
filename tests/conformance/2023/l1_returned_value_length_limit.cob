      *> ISO 15.4 Returned values - a returned value whose LENGTH
      *> exceeds the implementor's maximum (Annex A.1 item 93;
      *> docs/CONFORMANCE.md row 93: the maximum is 8 191 character
      *> positions, and the value returned is the 15.3 rule 14
      *> checking-off default, a ZERO-LENGTH value).
      *> 15.4: "The evaluation of a function produces a returned value
      *> in a temporary elementary data item. If the length of the
      *> returned value exceeds the maximum length specified by the
      *> implementor for a returned value, an EC-ARGUMENT-FUNCTION
      *> exception condition is set to exist."  15.3 rule 14: "If the
      *> EC-ARGUMENT-FUNCTION exception condition is set to exist and
      *> checking for EC-ARGUMENT-FUNCTION is not enabled, the
      *> implementor defines the result of the function reference."
      *> Checking is not enabled here, so the documented substituted
      *> result is what a program sees.
      *> BASECONVERT is the function whose returned length grows without
      *> an argument bound: 15.12.4 r1 returns the value "expressed as a
      *> string of characters" in the base of argument-3, and 15.12.3 r3
      *> makes an input length legal only while "neither it nor the
      *> returned value would exceed that defined by the implementor for
      *> a data item" - the same 8 191 bound, so both rules land on one
      *> answer.
      *> ATMAX "7" followed by 2 047 "F" is 3 + 8 188 = 8 191 binary
      *>       digits, EXACTLY at the maximum: converted whole, all
      *>       ones, and the INSPECT count IS the returned length. The
      *>       boundary is INSIDE.
      *> OVER  2 048 "F" is 8 192 binary digits, ONE past the maximum:
      *>       15.4 sets EC-ARGUMENT-FUNCTION and the documented result
      *>       is a zero-length value, which space-fills the receiver.
      *>       The receiver is deliberately pre-filled with "*" first,
      *>       so "all spaces" is a POSITIVE observation of the
      *>       zero-length transfer and not a leftover from the previous
      *>       MOVE. The space-fill is the standard's own, not an
      *>       inference: python scripts/spec/cite.py --check 14.6.8.5
      *>       "If the sending data item or literal is zero-length, the
      *>       entire receiving data item is space filled."  ->  OK
      *>       §14.6.8.5 - so "all spaces in a receiver pre-filled with
      *>       asterisks" IS "the returned value has length zero", and
      *>       the leg cannot be satisfied by a returned "0" (which
      *>       would leave OVERHEAD=[0   ] and OVER=00000 both, hence
      *>       the two legs together).
      *>
      *> WARNING - THE SECOND ENFORCEMENT SITE NAMED ON ROW 93 DISAGREES
      *> WITH THE ROW. CobolIntrinsics.BooleanOfInteger answers
      *> argument-2 > 8 191 with the one-position boolean "0", not with
      *> the zero-length value this row documents (and not with item
      *> 90's "the zero value of the type", whose own text puts a
      *> function whose returned LENGTH is derived from the rejected
      *> argument in the zero-length class). That divergence is reported
      *> as a defect and its repro is
      *> l1_returned_value_limit_boolean_repro; THIS golden measures the
      *> arm that agrees with the documentation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RVLIM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-OK  PIC X(2048).
       01 W-OVR PIC X(2048).
       01 W-OUT PIC X(8191).
       01 W-CNT PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE ALL "F" TO W-OK
           MOVE "7" TO W-OK(1:1)
           MOVE ALL "F" TO W-OVR
           MOVE FUNCTION BASECONVERT(W-OK, 16, 2) TO W-OUT
           MOVE ZERO TO W-CNT
           INSPECT W-OUT TALLYING W-CNT FOR ALL "1"
           DISPLAY "ATMAX=" W-CNT
           DISPLAY "ATMAXHEAD=[" W-OUT(1:4) "]"
           MOVE ALL "*" TO W-OUT
           MOVE FUNCTION BASECONVERT(W-OVR, 16, 2) TO W-OUT
           MOVE ZERO TO W-CNT
           INSPECT W-OUT TALLYING W-CNT FOR ALL "1"
           DISPLAY "OVER=" W-CNT
           DISPLAY "OVERHEAD=[" W-OUT(1:4) "]"
           STOP RUN.
