#!/bin/bash
# COBOL-85 compliance dashboard. Reports, per NIST module, how many programs are present vs baselined
# (a baseline = tests/nist/valid/<T>.txt, created only at 0 FAIL* + non-vacuous), and the overall % toward
# the literal-100% target (every in-scope module baselined). See docs/COBOL85_COMPLIANCE_PLAN.md.
#
# Usage: bash scripts/compliance.sh
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT" || exit 1

prog=tests/nist/programs
base=tests/nist/valid

# module key | display name | spec status
MODULES=(
  "NC|Nucleus|core"
  "IF|Intrinsic Functions|core"
  "SM|Source Text (COPY/REPLACE)|core"
  "IC|Inter-Program|core"
  "SQ|Sequential I-O|core"
  "OBSQ|Sequential I-O (obsolete)|core"
  "IX|Indexed I-O|core"
  "RL|Relative I-O|core"
  "ST|Sort-Merge|core"
  "RW|Report Writer|live"
  "DB|Debug|removed"
  "SG|Segmentation|removed"
  "CM|Communication|removed"
  "OBNC|Obsolete Nucleus|removed"
  "OBIC|Obsolete Inter-Program|removed"
  "EXEC|EXEC85 (EXCLUDED)|excluded"
)
# status: core/live = baseline target; removed = parse+dialect-flag only (NOT a baseline target,
# their …A NIST tests stay un-baselined by design — see docs/COBOL85_COMPLIANCE_PLAN.md §4); excluded = out.

count() { ls $1 2>/dev/null | grep -E "/${2}[0-9]" | wc -l | tr -d ' '; }

printf "\n  COBOL-85 COMPLIANCE DASHBOARD  (literal-100%% target — docs/COBOL85_COMPLIANCE_PLAN.md)\n\n"
printf "  %-28s %8s %10s %7s  %s\n" "MODULE" "PRESENT" "BASELINED" "%" "STATUS"
printf "  %s\n" "--------------------------------------------------------------------------------"

tot_present=0; tot_base=0; tot_present_inscope=0; tot_base_inscope=0
for m in "${MODULES[@]}"; do
  IFS='|' read -r key name status <<< "$m"
  p=$(count "$prog/${key}*.cob" "$key"); b=$(count "$base/${key}*.txt" "$key")
  [ "$p" -eq 0 ] && continue
  pct=$([ "$p" -gt 0 ] && echo "$((100*b/p))" || echo 0)
  mark="✗"; [ "$b" -eq "$p" ] && mark="✅"; [ "$b" -gt 0 ] && [ "$b" -lt "$p" ] && mark="◐"
  [ "$status" = "excluded" ] && mark="—"
  printf "  %-28s %8s %10s %6s%%  %s %s\n" "$name" "$p" "$b" "$pct" "$mark" "$status"
  tot_present=$((tot_present+p)); tot_base=$((tot_base+b))
  # Baseline target = the LIVE feature set: core modules + Report Writer. Removed/obsolete modules are
  # parse+dialect-flag only (their …A tests don't baseline), so they're NOT in the baseline-target denominator.
  if [ "$status" = "core" ] || [ "$status" = "live" ]; then
    tot_present_inscope=$((tot_present_inscope+p)); tot_base_inscope=$((tot_base_inscope+b)); fi
done
printf "  %s\n" "--------------------------------------------------------------------------------"
printf "  %-28s %8s %10s %6s%%\n" "ALL NIST programs" "$tot_present" "$tot_base" "$((100*tot_base/tot_present))"
printf "  %-28s %8s %10s %6s%%   (core + Report Writer)\n" "BASELINE TARGET (live)" "$tot_present_inscope" "$tot_base_inscope" "$((100*tot_base_inscope/tot_present_inscope))"
echo "  (Removed modules DB/SG/CM/OBNC/OBIC = parse + dialect-flag only — not a baseline target.)"
echo
echo "  Note: PRESENT counts every NIST .cob; some non-baselined are excluded-by-design within a ✅ module"
echo "  (flagging '…M' modules emit no CCVS report; NO_OUTPUT producers feed chains; PROCEDURE DIVISION USING"
echo "  callee-halves; non-deterministic ACCEPT FROM DATE/TIME). Those are accounted for in the plan, not gaps."
echo
