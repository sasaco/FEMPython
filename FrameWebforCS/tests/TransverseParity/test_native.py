"""One scoped pytest observation of executed native xUnit assertions."""
import pytest

from native_runner import observe


def test_native_assertions():
    try:
        result = observe()
        failed = bool(result["failed"])
        description = result["description"]
    except Exception as exc:
        # Infrastructure failures must never become pytest exit 1 (valid Red).
        pytest.exit(f"Invalid native observation: {exc}", returncode=2)
    if failed:
        pytest.fail(description, pytrace=False)
