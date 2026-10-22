## File PB3509: a carrier's DONE report hides its predecessor's review findings from the wave planner

Wave 1144's unit planned zero groups. `plan_wave.py` listed PB3489 as awaiting landing under
fix-pb3504-antlr-sll-merge-cache, while the branch survey holds that branch until PB3489's own review finisher answers
the one finding on fix-pb3489-nested-fn-hang. The planner credits the successor with every note it closed, including
the one it inherited by merging its predecessor, so the successor's newer DONE report replaces the predecessor's
FINDINGS report and no finisher is ever planned. kb/Work PB3509 records the mechanism, the expected rule (already stated
in the planner's own comment) and the sibling sweep. No code changes in this landing.
