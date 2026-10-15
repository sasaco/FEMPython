"""Run real xUnit controls through the unchanged configured pytest runner."""
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
OUTPUT = ROOT / ".tmp/transverse-analysis-print-corrections/runner-selfcheck"
PROJECT = '''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><IsTestProject>true</IsTestProject><IsPackable>false</IsPackable></PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.10.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
  </ItemGroup>
  <Target Name="IntentionalBuildFailure" BeforeTargets="CoreCompile" Condition="'$(TRANSVERSE_SELFCHECK_BUILD_FAILURE)' == 'true'">
    <Error Text="Intentional bridge build-failure negative control" />
  </Target>
</Project>
'''
ASSERTIONS = '''using Xunit;
public class NativeBridgeSelfCheck
{
    [Fact] public void Passes() => Assert.Equal(2, 1 + 1);
    [Fact] public void Fails() => Assert.Equal(3, 1 + 1);
    [Fact(Skip = "Intentional skip negative control")] public void Skipped() => Assert.True(true);
    [Fact] public void Delays() => System.Threading.Thread.Sleep(60000);
    [Fact] public void Throws() => throw new System.InvalidOperationException("Not an assertion");
}
'''


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    (OUTPUT / "RunnerSelfCheck.csproj").write_text(PROJECT, encoding="utf-8")
    (OUTPUT / "Assertions.cs").write_text(ASSERTIONS, encoding="utf-8")
    results = []
    for label, test, expect, failure, timeout, required in [
        ("pass", "Passes", "pass", False, "180", "Passes"),
        ("fail", "Fails", "fail", False, "180", "Fails"),
        ("unmatched", "Absent", "fail", False, "180", "Absent"),
        ("skipped", "Skipped", "fail", False, "180", "Skipped"),
        ("buildfailure", "Passes", "fail", True, "180", "Passes"),
        ("wrongname", "Passes", "pass", False, "180", "NotExecuted"),
        ("exception", "Throws", "fail", False, "180", "Throws"),
        ("unrelated", "Passes|FullyQualifiedName~NativeBridgeSelfCheck.Fails", "fail", False, "180", "Passes"),
        ("timeout", "Delays", "fail", False, "8", "Delays"),
    ]:
        env = os.environ.copy()
        env.update(TRANSVERSE_NATIVE_PROJECT=str(OUTPUT / "RunnerSelfCheck.csproj"),
                   TRANSVERSE_NATIVE_FILTER=f"FullyQualifiedName~NativeBridgeSelfCheck.{test}",
                   TRANSVERSE_NATIVE_EXPECT=required, TRANSVERSE_NATIVE_LABEL=f"selfcheck-{label}",
                   TRANSVERSE_NATIVE_TIMEOUT=timeout, PYTHONIOENCODING="utf-8")
        if failure:
            env["TRANSVERSE_SELFCHECK_BUILD_FAILURE"] = "true"
        else:
            env.pop("TRANSVERSE_SELFCHECK_BUILD_FAILURE", None)
        command = [sys.executable, ".agents/skills/_shared/run_tests.py", "--component", "python",
                   "--target", "FrameWebforCS/tests/TransverseParity/test_native.py",
                   "--expect", expect, "--timeout", "600", "--label", f"transverse-selfcheck-{label}"]
        completed = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, encoding="utf-8", timeout=610)
        (OUTPUT / f"{label}.json").write_text(completed.stdout + completed.stderr, encoding="utf-8")
        payload = json.loads(completed.stdout)
        valid = label in ("pass", "fail")
        expected_exit = 0 if valid else 2
        accepted = completed.returncode == expected_exit and payload["observed"] == (
            ("passed" if label == "pass" else "failed") if valid else "collection_error")
        results.append({"label": label, "exit": completed.returncode, "observed": payload["observed"],
                        "accepted": accepted, "log": str(OUTPUT / f"{label}.json")})
        print(json.dumps(results[-1]), flush=True)
    (OUTPUT / "summary.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    return 0 if all(row["accepted"] for row in results) else 1


if __name__ == "__main__":
    sys.exit(main())
