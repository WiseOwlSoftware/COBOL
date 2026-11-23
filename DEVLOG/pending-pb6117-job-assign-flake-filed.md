## kb/Work PB6117 filed: a test child intermittently fails to join its host's kill-on-close job

A RepoDrift gate run on branch md85-summary-titles failed one test (`ArchReviewFleetDriftTests.TheWorkflow_DryRunDrivesEveryArm`)
with `AssignProcessToJobObject failed for process 44404` from `KillOnCloseJob.Assign`. The branch touched nothing that test
reads. The exception drops the Win32 error code, so the cause cannot be classified from the log; PB6117 records the evidence,
the candidates and the fix (surface the code, then remove the start-to-bind window).
