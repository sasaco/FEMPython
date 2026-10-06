import singleStatic from "../../../../FrameWeb/tests/data/contracts/positive/single-static.json";
import multipleStatic from "../../../../FrameWeb/tests/data/contracts/positive/multiple-static.json";
import nonlinearSteps from "../../../../FrameWeb/tests/data/contracts/positive/nonlinear-steps.json";
import multipleNonlinear from "../../../../FrameWeb/tests/data/contracts/positive/multiple-nonlinear.json";
import modal from "../../../../FrameWeb/tests/data/contracts/positive/modal.json";
import emptyTopology from "../../../../FrameWeb/tests/data/contracts/positive/empty-topology.json";
import bridgeStatic from "../../../../FrameWeb/tests/data/contracts/positive/bridge-static.json";
import duplicateIds from "../../../../FrameWeb/tests/data/contracts/negative/duplicate-ids.json";
import duplicateCoordinates from "../../../../FrameWeb/tests/data/contracts/negative/duplicate-coordinates.json";
import forbiddenFields from "../../../../FrameWeb/tests/data/contracts/negative/forbidden-fields.json";
import mismatchedIds from "../../../../FrameWeb/tests/data/contracts/negative/mismatched-ids.json";
import nonFinite from "../../../../FrameWeb/tests/data/contracts/negative/non-finite.json";
import finalMarkerErrors from "../../../../FrameWeb/tests/data/contracts/negative/final-marker-errors.json";
import extraProperties from "../../../../FrameWeb/tests/data/contracts/negative/extra-properties.json";

import {
  AnalysisResultSetValidationError,
  requireStaticResults,
  selectAnalysisResult,
  selectShellResults,
  selectSolidResults,
  validateAndIndexAnalysisResultSet,
} from "./analysis-result-set";

describe("AnalysisResultSet v1", () => {
  const positiveFixtures: [string, unknown][] = [
    ["single static", singleStatic],
    ["multiple static", multipleStatic],
    ["nonlinear steps", nonlinearSteps],
    ["multiple nonlinear", multipleNonlinear],
    ["modal", modal],
    ["empty topology", emptyTopology],
    ["bridge static with assembly audit", bridgeStatic],
  ];

  it("validates spatial audit node references, values, and exact fields", () => {
    const mutations = [
      (a: any) => a.node_loads[0].node_id = "unknown",
      (a: any) => a.node_loads.push(a.node_loads[0]),
      (a: any) => a.loads.push(a.loads[0]),
      (a: any) => a.resultant.z = Infinity,
      (a: any) => a.force_error = -1,
      (a: any) => a.loads[0].feature = "unknown",
      (a: any) => a.loads[0].clipped_area = -1,
      (a: any) => a.extra = 1,
    ];
    mutations.forEach(mutate => {
      const fixture = JSON.parse(JSON.stringify(bridgeStatic));
      mutate(fixture.results[0].diagnostics.spatial_loads);
      expect(() => validateAndIndexAnalysisResultSet(fixture)).toThrowError(AnalysisResultSetValidationError);
    });
  });

  it("rejects null or nonstatic spatial audits", () => {
    const fixture = JSON.parse(JSON.stringify(bridgeStatic));
    fixture.results[0].diagnostics.spatial_loads = null;
    expect(() => validateAndIndexAnalysisResultSet(fixture)).toThrowError(AnalysisResultSetValidationError);
    [modal, nonlinearSteps].forEach(source => {
      const value = JSON.parse(JSON.stringify(source));
      value.results[0].diagnostics.spatial_loads = bridgeStatic.results[0].diagnostics.spatial_loads;
      expect(() => validateAndIndexAnalysisResultSet(value)).toThrowError(AnalysisResultSetValidationError);
    });
  });

  positiveFixtures.forEach(([name, fixture]) => {
    it(`accepts shared positive fixture: ${name}`, () => {
      expect(() => validateAndIndexAnalysisResultSet(fixture)).not.toThrow();
    });
  });

  it("accepts more than 256 ordered cases and still checks result references", () => {
    const fixture = JSON.parse(JSON.stringify(singleStatic));
    fixture.cases = [];
    fixture.results = [];
    for (let index = 0; index < 257; index++) {
      const caseId = `case-${index}`;
      fixture.cases.push({ ...singleStatic.cases[0], case_id: caseId });
      fixture.results.push({ ...singleStatic.results[0], case_id: caseId });
    }

    const result = validateAndIndexAnalysisResultSet(fixture);
    expect(result.caseOrder.length).toBe(257);
    expect(result.caseOrder[256]).toBe("case-256");

    fixture.results[256].case_id = "unknown";
    expect(() => validateAndIndexAnalysisResultSet(fixture)).toThrowError(
      AnalysisResultSetValidationError
    );
  });

  const negativeFixtures: [string, unknown][] = [
    ["duplicate IDs", duplicateIds],
    ["duplicate coordinates", duplicateCoordinates],
    ["forbidden fields", forbiddenFields],
    ["mismatched IDs", mismatchedIds],
    ["non-finite values", nonFinite],
    ["invalid final markers", finalMarkerErrors],
    ["extra properties", extraProperties],
  ];

  negativeFixtures.forEach(([name, fixture]) => {
    it(`rejects shared negative fixture before dispatch: ${name}`, () => {
      expect(() => validateAndIndexAnalysisResultSet(fixture)).toThrowError(
        AnalysisResultSetValidationError
      );
    });
  });

  it("preserves integer-like case order from the arrays", () => {
    const index = validateAndIndexAnalysisResultSet(multipleStatic);

    expect(index.caseOrder).toEqual(["10", "2"]);
    expect(index.resultsInOrder.map((result) => result.case_id)).toEqual(["10", "2"]);
  });

  it("indexes every nonlinear state by explicit case/state coordinates", () => {
    const index = validateAndIndexAnalysisResultSet(nonlinearSteps);

    expect(index.resultsByCase.get("NL")?.map((result) => result.state.index)).toEqual([0, 1]);
    expect(index.resultsByCoordinate.get("NL\u0000load_step\u00001")?.state.kind).toBe("load_step");
  });

  it("selects canonical snapshots and element results by an explicit coordinate", () => {
    const index = validateAndIndexAnalysisResultSet(singleStatic);
    const selection = { case_id: index.caseOrder[0], state_kind: "static" as const, state_index: 0 };

    expect(selectAnalysisResult(index, selection)).toBe(index.resultsInOrder[0]);
    expect(selectShellResults(index, selection)).toEqual([]);
    expect(selectSolidResults(index, selection)).toEqual([]);
  });

  it("rejects element-result selection for a modal coordinate", () => {
    const index = validateAndIndexAnalysisResultSet(modal);
    const mode = index.resultsInOrder[0];
    const selection = {
      case_id: mode.case_id,
      state_kind: "mode" as const,
      state_index: mode.state.index,
    };

    expect(() => selectShellResults(index, selection)).toThrowError(
      "Shell results are unavailable for modal analysis results."
    );
    expect(() => selectSolidResults(index, selection)).toThrowError(
      "Solid results are unavailable for modal analysis results."
    );
  });

  it("keeps the validated base response immutable", () => {
    const index = validateAndIndexAnalysisResultSet(JSON.parse(JSON.stringify(singleStatic)));

    expect(Object.isFrozen(index.value)).toBeTrue();
    expect(Object.isFrozen(index.value.results[0])).toBeTrue();
    expect(Object.isFrozen(index.value.results[0].state)).toBeTrue();
  });

  it("allows derived combinations only when every result is static", () => {
    expect(requireStaticResults(validateAndIndexAnalysisResultSet(multipleStatic)).length).toBe(2);
    expect(() => requireStaticResults(validateAndIndexAnalysisResultSet(nonlinearSteps))).toThrowError(
      "DEFINE/COMBINE/PICKUP は静的解析結果にのみ使用できます。"
    );
    expect(() => requireStaticResults(validateAndIndexAnalysisResultSet(modal))).toThrowError(
      "DEFINE/COMBINE/PICKUP は静的解析結果にのみ使用できます。"
    );
  });
});
