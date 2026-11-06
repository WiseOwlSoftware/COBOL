## Owner decision: the exactness ruling stops at a float item (kb/Work PB4731, PB4751)

The owner ruled on 2026-11-05 that the 2026-11-04 exactness ruling (PB4569) does not reach NUMERIC_DESIGN D16: an expression
with a float item operand or receiver stays wholly IEEE binary64, including a float item beside an exact-valued term such as
FUNCTION EXP10 (-6). `A1 + A2` over FLOAT-LONG 0.1 and 0.2 stores +0.3000000000000000444, and `ZF + FUNCTION EXP10 (-6)`
stores 0.000000 in 9V9(6), both documented native results. D16 records the boundary; DOC-A.1-123 already publishes the rule.
PB4731 and PB4751 close with no code change.
