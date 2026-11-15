#!/usr/bin/env python3
"""A run of the GnuCOBOL differential filtered by `--only` judges only the wrappers it selected (kb/Work PB5673): a
selection that selects no case is refused and names what exists; the per-case diff compares against the manifest rows of
the selected wrappers alone and says the run is filtered; `--write-baseline` is refused beside `--only`, and the manifest
stays as it was."""
import contextlib
import io
import subprocess
import sys

import fixtures as F
import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

D = selftest_cases.subject(__file__)   # scripts/gnucobol_differential.py
SCRIPT = D.__file__

BASE = {"syn_a:3": ("DEFAULT_DIALECT", "AGREE_ACCEPT"), "syn_a:9": ("DEFAULT_DIALECT", "AGREE_REJECT"),
        "syn_b:3": ("DEFAULT_DIALECT", "AGREE_ACCEPT"), "syn_abc:1": ("DEFAULT_DIALECT", "AGREE_ACCEPT"),
        "zzz_c:1": ("DEFAULT_DIALECT", "AGREE_ACCEPT")}


def _diff(results, only):
    """(the flip count, the printed diff) of `diff_baseline` over `BASE` and `results`."""
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        n = D.diff_baseline(BASE, results, "m.tsv", only)
    return n, out.getvalue()


def case_00_an_only_that_selects_no_case_is_an_error_naming_the_value_and_the_wrappers(expect):
    src = str(F.corpus())
    groups, err = D.select_cases(src, "syn_a")
    expect.equal("a prefix that selects: its two cases and no error", (len(groups), err), (2, ""))
    groups, err = D.select_cases(src, "")
    expect.equal("no filter: all three cases and no error", (len(groups), err), (3, ""))
    groups, err = D.select_cases(src, "syn_a:3")
    expect.equal("a case id is no wrapper prefix: no case selected", len(groups), 0)
    expect("the error names the value given and every wrapper file that exists",
           "'syn_a:3'" in err and "syn_a.at" in err and "syn_b.at" in err and "notes.txt" not in err, err)
    # The command line, end to end (no compiler is reached: the selection is refused first).
    r = subprocess.run([sys.executable, SCRIPT, "--src", src, "--only", "nothing", "--exe", sys.executable],
                       capture_output=True, text=True, timeout=120)
    expect("the script exits non-zero with the error on stderr, and prints no zero-flip line",
           r.returncode != 0 and "'nothing'" in r.stderr and "syn_a.at" in r.stderr
           and "PER-CASE FLIP" not in r.stdout, f"rc={r.returncode} stdout={r.stdout!r} stderr={r.stderr!r}")


def case_01_a_filtered_diff_compares_the_selected_wrappers_alone_and_says_so(expect):
    ok = [F.result("syn_a:3", "AGREE_ACCEPT"), F.result("syn_a:9", "AGREE_REJECT"), F.result("syn_abc:1", "AGREE_ACCEPT")]
    n, text = _diff(ok, "syn_a")
    expect.equal("the selected wrapper's rows agree: no flip", n, 0)
    expect("the other wrappers' rows are not reported REMOVED", "REMOVED" not in text and "corpus changed" not in text, text)
    expect("the heading says the run is filtered and names the prefix", "--only 'syn_a'" in text and "FILTERED" in text, text)
    n, text = _diff([F.result("syn_a:3", "AGREE_ACCEPT"), F.result("syn_a:9", "WE_ACCEPT_THEY_REJECT")], "syn_a")
    expect.equal("a flip inside the selected wrapper still counts", n, 1)
    n, text = _diff([F.result("syn_a:3", "AGREE_ACCEPT")], "syn_a")
    expect("a selected wrapper's row the run lost is still REMOVED", "REMOVED  syn_a:9" in text, text)
    n, text = _diff([F.result("syn_a:3", "AGREE_ACCEPT"), F.result("syn_a:9", "AGREE_REJECT"),
                     F.result("syn_a:12", "AGREE_ACCEPT")], "syn_a")
    expect("a case the manifest lacks in a selected wrapper is still NEW", "NEW      syn_a:12" in text, text)
    n, text = _diff([F.result("syn_a:3", "AGREE_ACCEPT"), F.result("syn_a:9", "AGREE_REJECT")], "syn_ab")
    expect("the prefix is the wrapper FILE's: `syn_ab` selects syn_abc, not syn_a", "REMOVED  syn_abc:1" in text, text)
    n, text = _diff([], "")
    expect("an unfiltered run keeps the whole-population diff: every row is REMOVED and the heading is plain",
           text.count("    REMOVED  ") == 5 and "FILTERED" not in text, text)


def case_02_write_baseline_is_refused_beside_only_and_the_manifest_stays(expect):
    manifest = scratch_dir("gcdiff-manifest-") / "baseline.tsv"
    manifest.write_text("syn_a:3\tDEFAULT_DIALECT\tAGREE_ACCEPT\n", encoding="utf-8", newline="\n")
    before = manifest.read_text(encoding="utf-8")
    r = subprocess.run([sys.executable, SCRIPT, "--only", "syn_a", "--write-baseline", "--record", "r",
                        "--baseline", str(manifest), "--exe", sys.executable],
                       capture_output=True, text=True, timeout=120)
    expect("argparse refuses it (exit 2) and names both options", r.returncode == 2
           and "--write-baseline" in r.stderr and "--only" in r.stderr and "cannot" in r.stderr,
           f"rc={r.returncode} stderr={r.stderr!r}")
    expect.equal("the manifest is unchanged", manifest.read_text(encoding="utf-8"), before)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
