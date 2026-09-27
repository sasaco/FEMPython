"""Shared AnalysisResultSet v1 schema and semantic fixture contract."""

import copy
import json
from pathlib import Path

import pytest

from fem.result_contracts import ResultContractError, validate_analysis_result_set


CONTRACTS = Path(__file__).parents[1] / "data" / "contracts"


def _load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def test_schema_is_draft_2020_12_and_defines_the_sole_root() -> None:
    schema = _load(CONTRACTS / "analysis-result-set-v1.schema.json")
    assert schema["$schema"] == "https://json-schema.org/draft/2020-12/schema"
    assert schema["$ref"] == "#/$defs/analysisResultSet"
    root = schema["$defs"]["analysisResultSet"]
    assert root["additionalProperties"] is False
    assert root["properties"]["kind"] == {"const": "analysis_result_set"}
    assert root["properties"]["schema_version"] == {"const": "1.0"}
    assert "maxItems" not in root["properties"]["cases"]


def test_more_than_256_cases_validate_without_a_count_ceiling() -> None:
    single = _load(CONTRACTS / "positive" / "single-static.json")
    result_set = copy.deepcopy(single)
    result_set["cases"] = []
    result_set["results"] = []
    for index in range(257):
        case_id = f"case-{index}"
        case = copy.deepcopy(single["cases"][0])
        result = copy.deepcopy(single["results"][0])
        case["case_id"] = case_id
        result["case_id"] = case_id
        result_set["cases"].append(case)
        result_set["results"].append(result)

    validate_analysis_result_set(result_set)

    result_set["results"][-1]["case_id"] = "unknown"
    with pytest.raises(ResultContractError, match="case_id"):
        validate_analysis_result_set(result_set)


@pytest.mark.parametrize(
    "path", sorted((CONTRACTS / "positive").glob("*.json")), ids=lambda path: path.stem
)
def test_positive_shared_fixtures_validate(path: Path) -> None:
    validate_analysis_result_set(_load(path))


NEGATIVE_ERRORS = {
    "duplicate-ids.json": "duplicate node_id",
    "duplicate-coordinates.json": "duplicates result coordinate",
    "forbidden-fields.json": "extra ['node_displacements']",
    "mismatched-ids.json": "must exactly cover node IDs",
    "non-finite.json": "must be finite",
    "final-marker-errors.json": "mark only its last step final",
    "extra-properties.json": "extra ['legacy']",
}


@pytest.mark.parametrize("filename, message", NEGATIVE_ERRORS.items())
def test_negative_shared_fixtures_fail_for_the_intended_reason(
    filename: str, message: str
) -> None:
    with pytest.raises(ResultContractError, match=message.replace("[", r"\[").replace("]", r"\]")):
        validate_analysis_result_set(_load(CONTRACTS / "negative" / filename))
