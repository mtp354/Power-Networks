# Fantasy Earth alignment calibration

Authorized by the owner on 2026-10-25 after the first S25 field run. This is the next calibration step; rebuilt geometry and map view remain separate work.

The baseline is commit `88481b3`: the painted camera, diagnostics and Earth tracking work, but the 1.5 m / 2° entry gate never locked. The final stable portion of the field run held near 1.9 m / 1.5°. The original Plan 1 field summary remains historical evidence.

- [x] Verify the current branch, working tree, Unity connection and S25 authorization.
- [x] Replay coordinate-free field accuracy through the actual AlignmentGate with horizontal candidates 1.5, 1.75, 2.0 and 2.25 m. Keep the 2° yaw gate, 1-second entry dwell, exit hysteresis and tracking-loss behavior.
- [x] Add behavioral regression coverage for measured S25 pairs and calibrated boundaries; confirm failure against the old entry limit.
- [x] Apply the smallest supported horizontal limit and pass all Unity EditMode tests.
- [x] Build, install and smoke-test the updated development APK. Preserve consent and private session logs.
- [x] Record the selected threshold, replay assumptions, tests and APK identity. Commit the implementation.
- [ ] Verify an actual lock on the updated build and record measured locked medians and time to first lock. Replay predictions do not complete this step.

Replay assumes each preceding sample holds until the next approximately 2 Hz record, using 0.1-second gate updates. Gaps greater than 1.25 seconds reset tracking. This checks the implementation against recorded measurements but cannot reconstruct intermediate frames or prove world alignment. No coordinate data is included in replay input or committed results.

Replay on all 1,797 coordinate-free samples: 1.5 m and 1.75 m produce no predicted lock; 2.0 m produces one predicted lock transition and 184 locked sample records (median 1.90 m / 1.50°). 2.25 m adds only one predicted locked record. Select 2.0 m as the smallest tested supported candidate; verify on device before treating it as accepted.

Regression result: the old 1.5 m gate fails the two measured S25 cases and the new 2 m boundary. After selecting 2.0 m, all 121 FantasyEarth EditMode cases pass, including yaw rejection, dwell, hysteresis, invalid accuracy and immediate tracking-loss behavior.

Owner steering: after the second attempt exposed a black camera dependency regression, the owner explicitly deferred alignment fine-tuning and requested progress on other tasks. The dependency repair is installed and camera video is confirmed restored. Keep actual lock acceptance pending; do not request another walk as part of the building-generation slice.
