using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Fail-closed check that a test build really is a Development build (Editor only; changes nothing in any player).
//
// Test isolation (Photon AppVersion "uxtest_<PUN version>", Unity Services environment "development" with sign-in
// profile "uxtest", no survey redirect, TEST MODE badge) is compiled only under DEVELOPMENT_BUILD, which Unity defines
// only when the build's own BuildOptions contain Development. A scripted build through
// BuildPipeline.BuildPlayer(BuildPlayerOptions) - for example the Unity CLI "build run" command - takes that flag only
// from its options argument, not from the active build profile: naming the "Web - Desktop - Development" profile
// applies the profile's player settings (e.g. no compression) but still produces a Release player that connects to the
// production Photon pool and the production Unity Services environment (incident: PROJECT_CONTEXT.md section 17).
//
// Refuses the build before anything is compiled when:
//   1. the build is labelled as a test build (Development Build switched on in the active profile or platform
//      settings, a build profile named "...Development...", or an output folder named "uxtest...") but its
//      BuildOptions do not contain Development;
//   2. the build is a Development build while a build profile named "...Release..." is active (a test-mode player
//      under the participant-build profile).
// A Release build made with the Release profile active, and a Development build made with the Development profile,
// pass unchanged.
class DevelopmentBuildGuard : IPreprocessBuildWithReport
{
    const string TestOutputPrefix = "uxtest";

    public int callbackOrder => int.MinValue;

    public void OnPreprocessBuild(BuildReport report)
    {
        BuildSummary summary = report.summary;
        bool development = (summary.options & BuildOptions.Development) != 0;

        BuildProfile profile = BuildProfile.GetActiveBuildProfile();
        string profileName = profile != null ? profile.name : "";
        string profileLabel = profile != null ? "'" + profileName + "'" : "(none: platform settings)";
        bool settingsDevelopment = EditorUserBuildSettings.development;
        bool profileSaysDevelopment = Contains(profileName, "Development");
        bool profileSaysRelease = Contains(profileName, "Release");
        bool outputSaysTest = HasTestFolder(summary.outputPath);

        string state = "target=" + summary.platform + ", options=" + summary.options + ", active profile=" + profileLabel
                       + ", Development Build setting=" + settingsDevelopment + ", output=" + summary.outputPath;

        if (!development && (settingsDevelopment || profileSaysDevelopment || outputSaysTest))
        {
            throw new BuildFailedException(
                "[DevelopmentBuildGuard] REFUSED: this build is labelled as a test build but its BuildOptions do not "
                + "contain Development, so test mode (#if DEVELOPMENT_BUILD) would be compiled out and the player would "
                + "connect to PRODUCTION (Photon \"_2.42\", Unity Services \"production\"). A scripted build does not take "
                + "the Development flag from the build profile. Rebuild with BuildOptions.Development, e.g. "
                + "unity command \"build run\" --profileName \"Web - Desktop - Development\" "
                + "--options '[\"Development\",\"DetailedBuildReport\"]' --outputPath Builds/uxtest_<name> --confirm true "
                + "(PROJECT_CONTEXT.md section 13.2). For a participant build, activate the Release profile with "
                + "Development Build off and an output folder that does not start with \"" + TestOutputPrefix + "\". " + state);
        }

        if (development && profileSaysRelease)
        {
            throw new BuildFailedException(
                "[DevelopmentBuildGuard] REFUSED: Development (test mode) build while the Release profile is active. "
                + "It would run in TEST MODE (development data, no survey redirect) under the participant-build profile. "
                + "Use the \"Web - Desktop - Development\" profile for test builds, or remove the Development option for a "
                + "participant build (PROJECT_CONTEXT.md section 13.3). " + state);
        }

        Debug.Log("[DevelopmentBuildGuard] " + (development ? "Development (TEST MODE) build" : "Release build")
                  + " accepted: " + state
                  + (development ? ". Verify the artifact before serving it: python -I tools/webgl_test/serve_test_build.py <build folder> --check-only" : ""));
    }

    static bool Contains(string text, string word)
    {
        return !string.IsNullOrEmpty(text) && text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // True when any folder of the output path starts with "uxtest" (Builds/uxtest_x, or Builds/uxtest_x/Game.exe).
    static bool HasTestFolder(string outputPath)
    {
        if (string.IsNullOrEmpty(outputPath)) return false;
        foreach (string part in outputPath.Split('/', '\\'))
        {
            if (part.StartsWith(TestOutputPrefix, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
