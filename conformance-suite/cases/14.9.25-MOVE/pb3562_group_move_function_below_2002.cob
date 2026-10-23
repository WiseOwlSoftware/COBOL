*>--- conformance-case
*> id: pb3562_group_move_function_below_2002
*> rules: [GR-14.9.25.4-4, RV-15.13.4-1]
*> source-format: free
*>---
*> kb/Work PB3562, the negative of pb3562_group_move_function_values:
*> BOOLEAN-OF-INTEGER (ISO 15.13) and usage bit are COBOL-2002
*> introductions, so a MOVE of the function value to a group is refused
*> at COBOL-85 by name (COBOLNET1502, the intrinsic window gate) and
*> accepted from 2002 on.
identification division.
program-id. pb3562n1.
data division.
working-storage section.
01 g.
   05 gx pic x(8).
procedure division.
main-para.
    move function boolean-of-integer(165 8) to g
    stop run.
end program pb3562n1.
