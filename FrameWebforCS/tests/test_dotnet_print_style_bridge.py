"""Expose one completed xUnit assertion to the repository TDD gate."""

import os
from pathlib import Path
import subprocess
import tempfile
import xml.etree.ElementTree as ET

import pytest


ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "FrameWebforCS/tests/PrintProjection.Tests/PrintProjection.Tests.csproj"


def test_filtered_print_projection_style():
    name = os.environ.get("FRAMEPRINT_TDD_FILTER")
    if not name:
        pytest.skip("xUnit bridge runs only when FRAMEPRINT_TDD_FILTER is set")
    if not name.startswith("FrameWebforCS.Tests.TransversePrintMetadataTests."):
        pytest.exit("FRAMEPRINT_TDD_FILTER must name a print metadata test", returncode=2)
    with tempfile.TemporaryDirectory(prefix="print-style-tdd-") as output:
        try:
            run = subprocess.run(
                ["dotnet", "test", str(PROJECT), "--filter", f"FullyQualifiedName={name}",
                 "--logger", "trx;LogFileName=result.trx", "--results-directory", output,
                 "--nologo", "--verbosity", "quiet"],
                cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=180,
            )
        except (OSError, subprocess.TimeoutExpired) as error:
            pytest.exit(f"xUnit runner unavailable: {error}", returncode=2)
        trx = Path(output) / "result.trx"
        if not trx.exists():
            pytest.exit(f"xUnit did not produce TRX (build/collection error): {run.stdout}\n{run.stderr}", returncode=2)
        tree = ET.parse(trx)
        counters = tree.find(".//{*}Counters")
        if counters is None or int(counters.attrib["total"]) != 1:
            pytest.exit(f"Expected exactly one xUnit test: {run.stdout}\n{run.stderr}", returncode=2)
        failed = int(counters.attrib["failed"])
        passed = int(counters.attrib["passed"])
        if failed + passed != 1 or passed == 1 and run.returncode != 0:
            pytest.exit(f"xUnit did not complete cleanly: {run.stdout}\n{run.stderr}", returncode=2)
        failure = tree.find(".//{*}UnitTestResult/{*}Output/{*}ErrorInfo/{*}Message")
        assert failed == 0, failure.text if failure is not None else "xUnit assertion failed"
