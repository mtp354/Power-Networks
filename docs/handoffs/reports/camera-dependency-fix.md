# Camera session dependency regression — 2026-10-25

The owner reported live video briefly appearing, then a black camera background with motes and `SessionNotReady`. The failed session contained 432 records and no tracking. The process did not crash.

Native logcat identifies `AR_ERROR_GOOGLE_PLAY_SERVICES_LOCATION_LIBRARY_NOT_LINKED`, followed by a configuration error that pauses ARCore. The calibration APK omitted both Google location/auth dependency markers. The preserved working APK includes both. Earlier source cleanup treated resolver-managed dependency blocks as transient; a cached incremental build did not restore them. This was a build regression, not an alignment threshold problem. Google's [ARCore status reference](https://developers.google.com/ar/reference/c/group/shared-types) documents the required location-library class definitions.

The targeted fix declares `play-services-location:17.1.0` and `play-services-auth-base:16.0.0` explicitly outside resolver-managed blocks. These match the resolved versions from the working APK and the installed Extensions SDK's dependency ranges. No runtime recovery loop or session reset was added.

Validation:

- Android build `build_99fecdc2f11b` succeeded in 7.381 seconds, 0 errors, the same three vendor test-image warnings.
- APK size: 53,643,233 bytes; SHA-256: `508450cbecdc4423eeaa2723a10d06a8ae44bb731885b28d21c862daee71092d`.
- `Tools/diagnostics/verify_geospatial_apk.py` uses Android SDK `dexdump` to verify actual class definitions for `FusedLocationProviderClient` and `GoogleAuthUtil`, rather than string references or dependency markers. Both are present in the repaired APK. A markers-only negative fixture is rejected.
- Installed and launched on the S25. The specific missing-library/configuration-pause errors are absent in the new process. Native depth/feature warnings remain; no claim is made that all native diagnostics are quiet.
- The owner confirmed live camera video is back indoors after installing this repair. No additional outdoor test was requested; alignment tuning is deferred at the owner's request.

Run the APK dependency verifier after every future Android build. The pure C# suite remains at 121 passed cases; this Gradle-only fix was validated by the actual APK and device rather than repeating unchanged EditMode tests.
