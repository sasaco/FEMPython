export type AnalysisType = "static" | "material_nonlinear" | "modal";
export type ResultStateKind = "static" | "load_step" | "mode";

export interface UnitSystem {
  readonly system: string;
  readonly length: string;
  readonly force: string;
  readonly mass: string;
  readonly time: string;
}

export interface CoordinateSystem {
  readonly name: "global_cartesian";
  readonly handedness: "right";
  readonly axes: readonly ["x", "y", "z"];
}

export interface Vector3 {
  readonly x: number;
  readonly y: number;
  readonly z: number;
}

export interface CoordinateFrame {
  readonly origin: Vector3;
  readonly x_axis: Vector3;
  readonly y_axis: Vector3;
  readonly z_axis: Vector3;
}

export interface ResultCase {
  readonly case_id: string;
  readonly name: string;
  readonly symbol: string;
  readonly analysis_type: AnalysisType;
  readonly support_node_ids: readonly string[];
}

export interface TopologyNode {
  readonly node_id: string;
  readonly coordinates: Vector3;
  readonly source_node_id: string | null;
  readonly generated: boolean;
}

export interface MemberStation {
  readonly station_id: string;
  readonly position: number;
}

export interface TopologyMember {
  readonly member_id: string;
  readonly node_i: string;
  readonly node_j: string;
  readonly local_frame: CoordinateFrame;
  readonly stations: readonly MemberStation[];
}

export interface ShellResultLocation {
  readonly location_id: "element_average";
  readonly kind: "element_average";
}

export interface TopologyShellElement {
  readonly element_id: string;
  readonly element_type: "triangle3" | "quadrilateral4";
  readonly node_ids: readonly string[];
  readonly local_frame: CoordinateFrame;
  readonly result_locations: readonly ShellResultLocation[];
}

export interface SolidResultLocation {
  readonly location_id: string;
  readonly natural_coordinates: {
    readonly xi: number;
    readonly eta: number;
    readonly zeta: number;
  };
}

export interface TopologySolidElement {
  readonly element_id: string;
  readonly element_type:
    | "tetra4"
    | "wedge6"
    | "hexa8"
    | "tetra10"
    | "wedge15"
    | "hexa20";
  readonly node_ids: readonly string[];
  readonly coordinate_frame: "global";
  readonly result_locations: readonly SolidResultLocation[];
}

export interface ResultTopology {
  readonly nodes: readonly TopologyNode[];
  readonly members: readonly TopologyMember[];
  readonly shell_elements: readonly TopologyShellElement[];
  readonly solid_elements: readonly TopologySolidElement[];
}

export interface DisplacementComponents {
  readonly dx: number;
  readonly dy: number;
  readonly dz: number;
  readonly rx: number;
  readonly ry: number;
  readonly rz: number;
}

export interface ForceComponents {
  readonly fx: number;
  readonly fy: number;
  readonly fz: number;
  readonly mx: number;
  readonly my: number;
  readonly mz: number;
}

export interface NodeDisplacement {
  readonly node_id: string;
  readonly components: DisplacementComponents;
}

export interface NodeModeShape {
  readonly node_id: string;
  readonly components: DisplacementComponents;
}

export interface SupportReaction {
  readonly node_id: string;
  readonly components: ForceComponents;
}

export interface MemberForceSegment {
  readonly segment_id: string;
  readonly station_i: string;
  readonly station_j: string;
  readonly length: number;
  readonly i_end: ForceComponents;
  readonly j_end: ForceComponents;
}

export interface MemberSectionForces {
  readonly member_id: string;
  readonly segments: readonly MemberForceSegment[];
}

export interface ShellResult {
  readonly element_id: string;
  readonly locations: readonly {
    readonly location_id: "element_average";
    readonly membrane_force: { readonly nx: number; readonly ny: number; readonly nxy: number };
    readonly bending_moment: { readonly mx: number; readonly my: number; readonly mxy: number };
    readonly transverse_shear: { readonly qx: number; readonly qy: number };
    readonly top_stress: { readonly sx: number; readonly sy: number; readonly txy: number };
    readonly bottom_stress: { readonly sx: number; readonly sy: number; readonly txy: number };
  }[];
}

export interface SolidResult {
  readonly element_id: string;
  readonly locations: readonly {
    readonly location_id: string;
    readonly stress: {
      readonly sx: number; readonly sy: number; readonly sz: number;
      readonly txy: number; readonly tyz: number; readonly tzx: number;
    };
    readonly strain: {
      readonly ex: number; readonly ey: number; readonly ez: number;
      readonly gxy: number; readonly gyz: number; readonly gzx: number;
    };
  }[];
}

interface ForceBearingResult {
  readonly case_id: string;
  readonly node_displacements: readonly NodeDisplacement[];
  readonly support_reactions: readonly SupportReaction[];
  readonly member_section_forces: readonly MemberSectionForces[];
  readonly shell_results: readonly ShellResult[];
  readonly solid_results: readonly SolidResult[];
}

export interface StaticAnalysisResult extends ForceBearingResult {
  readonly state: { readonly kind: "static"; readonly index: 0 };
  readonly diagnostics: { readonly warnings: readonly string[] };
}

export interface NonlinearStepAnalysisResult extends ForceBearingResult {
  readonly state: {
    readonly kind: "load_step";
    readonly index: number;
    readonly load_factor: number;
    readonly is_final: boolean;
  };
  readonly diagnostics: {
    readonly warnings: readonly string[];
    readonly iterations: readonly {
      readonly index: number;
      readonly residual_norm: number;
      readonly correction_norm: number;
      readonly converged: boolean;
    }[];
  };
}

export interface ModalAnalysisResult {
  readonly case_id: string;
  readonly state: {
    readonly kind: "mode";
    readonly index: number;
    readonly eigenvalue: number;
    readonly frequency: number;
    readonly degeneracy_group: number;
  };
  readonly node_mode_shapes: readonly NodeModeShape[];
  readonly diagnostics: {
    readonly warnings: readonly string[];
    readonly normalization: "mass";
    readonly eigenvalue_tolerance: number;
    readonly degeneracy_relative_tolerance: 1e-8;
  };
}

export type ForceAnalysisResult = StaticAnalysisResult | NonlinearStepAnalysisResult;
export type AnalysisResult = ForceAnalysisResult | ModalAnalysisResult;

export interface AnalysisResultSet {
  readonly kind: "analysis_result_set";
  readonly schema_version: "1.0";
  readonly units: UnitSystem;
  readonly coordinate_system: CoordinateSystem;
  readonly cases: readonly ResultCase[];
  readonly topology: ResultTopology;
  readonly results: readonly AnalysisResult[];
}

export interface AnalysisResultSetIndex {
  readonly value: AnalysisResultSet;
  readonly caseOrder: readonly string[];
  readonly casesById: ReadonlyMap<string, ResultCase>;
  readonly resultsInOrder: readonly AnalysisResult[];
  readonly resultsByCase: ReadonlyMap<string, readonly AnalysisResult[]>;
  readonly resultsByCoordinate: ReadonlyMap<string, AnalysisResult>;
}

export interface AnalysisResultSelection {
  readonly case_id: string;
  readonly state_kind: ResultStateKind;
  readonly state_index: number;
}

export const INVALID_ANALYSIS_RESULT_SET_MESSAGE = "計算結果の形式が不正です。";

export class AnalysisResultSetValidationError extends Error {
  constructor(readonly path: string, readonly reason: string) {
    super(`${INVALID_ANALYSIS_RESULT_SET_MESSAGE} (${path}: ${reason})`);
    this.name = "AnalysisResultSetValidationError";
  }
}

type JsonObject = Record<string, unknown>;

const FRAME_TOLERANCE = 1e-8;
const DEGENERACY_TOLERANCE = 1e-8;
const MODAL_FREQUENCY_RELATIVE_TOLERANCE = 1e-8;

function fail(path: string, reason: string): never {
  throw new AnalysisResultSetValidationError(path, reason);
}

function objectAt(value: unknown, path: string): JsonObject {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    fail(path, "object required");
  }
  return value as JsonObject;
}

function arrayAt(value: unknown, path: string): unknown[] {
  if (!Array.isArray(value)) fail(path, "array required");
  return value;
}

function exactKeys(value: JsonObject, keys: readonly string[], path: string): void {
  const actual = Object.keys(value).sort();
  const expected = [...keys].sort();
  if (actual.length !== expected.length || actual.some((key, index) => key !== expected[index])) {
    fail(path, `required keys are ${expected.join(", ")}`);
  }
}

function stringAt(value: unknown, path: string, nonEmpty = false): string {
  if (typeof value !== "string" || (nonEmpty && value.trim().length === 0)) {
    fail(path, nonEmpty ? "non-empty string required" : "string required");
  }
  return value;
}

function finiteAt(value: unknown, path: string): number {
  if (typeof value !== "number" || !Number.isFinite(value)) fail(path, "finite number required");
  return value;
}

function integerAt(value: unknown, path: string): number {
  const result = finiteAt(value, path);
  if (!Number.isInteger(result) || result < 0) fail(path, "non-negative integer required");
  return result;
}

function booleanAt(value: unknown, path: string): boolean {
  if (typeof value !== "boolean") fail(path, "boolean required");
  return value;
}

function literalAt<T extends string>(value: unknown, allowed: readonly T[], path: string): T {
  if (typeof value !== "string" || !allowed.includes(value as T)) {
    fail(path, `expected ${allowed.join(" | ")}`);
  }
  return value as T;
}

function validateStringArray(value: unknown, path: string, nonEmptyItems = false): string[] {
  return arrayAt(value, path).map((item, index) => stringAt(item, `${path}[${index}]`, nonEmptyItems));
}

function assertUnique(values: readonly string[], path: string): void {
  const seen = new Set<string>();
  values.forEach((value, index) => {
    if (seen.has(value)) fail(`${path}[${index}]`, `duplicate ID ${value}`);
    seen.add(value);
  });
}

function validateVector3(value: unknown, path: string): Vector3 {
  const obj = objectAt(value, path);
  exactKeys(obj, ["x", "y", "z"], path);
  finiteAt(obj.x, `${path}.x`);
  finiteAt(obj.y, `${path}.y`);
  finiteAt(obj.z, `${path}.z`);
  return obj as unknown as Vector3;
}

function dot(a: Vector3, b: Vector3): number {
  return a.x * b.x + a.y * b.y + a.z * b.z;
}

function cross(a: Vector3, b: Vector3): Vector3 {
  return { x: a.y * b.z - a.z * b.y, y: a.z * b.x - a.x * b.z, z: a.x * b.y - a.y * b.x };
}

function validateCoordinateFrame(value: unknown, path: string): CoordinateFrame {
  const obj = objectAt(value, path);
  exactKeys(obj, ["origin", "x_axis", "y_axis", "z_axis"], path);
  validateVector3(obj.origin, `${path}.origin`);
  const x = validateVector3(obj.x_axis, `${path}.x_axis`);
  const y = validateVector3(obj.y_axis, `${path}.y_axis`);
  const z = validateVector3(obj.z_axis, `${path}.z_axis`);
  for (const [name, axis] of [["x_axis", x], ["y_axis", y], ["z_axis", z]] as const) {
    if (Math.abs(dot(axis, axis) - 1) > FRAME_TOLERANCE) fail(`${path}.${name}`, "unit vector required");
  }
  if (Math.abs(dot(x, y)) > FRAME_TOLERANCE || Math.abs(dot(x, z)) > FRAME_TOLERANCE || Math.abs(dot(y, z)) > FRAME_TOLERANCE) {
    fail(path, "axes must be mutually orthogonal");
  }
  const xy = cross(x, y);
  if (Math.abs(xy.x - z.x) > FRAME_TOLERANCE || Math.abs(xy.y - z.y) > FRAME_TOLERANCE || Math.abs(xy.z - z.z) > FRAME_TOLERANCE) {
    fail(path, "right-handed axes required");
  }
  return obj as unknown as CoordinateFrame;
}

function validateNamedNumbers(value: unknown, keys: readonly string[], path: string): void {
  const obj = objectAt(value, path);
  exactKeys(obj, keys, path);
  keys.forEach((key) => finiteAt(obj[key], `${path}.${key}`));
}

function validateWarnings(value: unknown, path: string): void {
  validateStringArray(value, path);
}

function assertOrderedCoverage(actual: readonly string[], expected: readonly string[], path: string): void {
  if (actual.length !== expected.length || actual.some((id, index) => id !== expected[index])) {
    fail(path, "IDs and order must exactly match topology");
  }
}

function validateTopology(value: unknown): ResultTopology {
  const path = "topology";
  const obj = objectAt(value, path);
  exactKeys(obj, ["nodes", "members", "shell_elements", "solid_elements"], path);

  const nodes = arrayAt(obj.nodes, `${path}.nodes`);
  const nodeIds = nodes.map((item, index) => {
    const itemPath = `${path}.nodes[${index}]`;
    const node = objectAt(item, itemPath);
    exactKeys(node, ["node_id", "coordinates", "source_node_id", "generated"], itemPath);
    const id = stringAt(node.node_id, `${itemPath}.node_id`, true);
    validateVector3(node.coordinates, `${itemPath}.coordinates`);
    if (node.source_node_id !== null) stringAt(node.source_node_id, `${itemPath}.source_node_id`, true);
    booleanAt(node.generated, `${itemPath}.generated`);
    return id;
  });
  assertUnique(nodeIds, `${path}.nodes`);
  const nodeSet = new Set(nodeIds);
  nodes.forEach((item, index) => {
    const source = (item as JsonObject).source_node_id;
    if (source !== null && !nodeSet.has(source as string)) fail(`${path}.nodes[${index}].source_node_id`, "unknown node ID");
  });

  const members = arrayAt(obj.members, `${path}.members`);
  const memberIds = members.map((item, index) => {
    const itemPath = `${path}.members[${index}]`;
    const member = objectAt(item, itemPath);
    exactKeys(member, ["member_id", "node_i", "node_j", "local_frame", "stations"], itemPath);
    const id = stringAt(member.member_id, `${itemPath}.member_id`, true);
    const nodeI = stringAt(member.node_i, `${itemPath}.node_i`, true);
    const nodeJ = stringAt(member.node_j, `${itemPath}.node_j`, true);
    if (!nodeSet.has(nodeI) || !nodeSet.has(nodeJ) || nodeI === nodeJ) fail(itemPath, "member endpoints must reference distinct topology nodes");
    validateCoordinateFrame(member.local_frame, `${itemPath}.local_frame`);
    const stations = arrayAt(member.stations, `${itemPath}.stations`);
    if (stations.length < 2) fail(`${itemPath}.stations`, "at least two stations required");
    const stationIds = stations.map((stationValue, stationIndex) => {
      const stationPath = `${itemPath}.stations[${stationIndex}]`;
      const station = objectAt(stationValue, stationPath);
      exactKeys(station, ["station_id", "position"], stationPath);
      const stationId = stringAt(station.station_id, `${stationPath}.station_id`, true);
      if (stationId !== `S${stationIndex}`) fail(`${stationPath}.station_id`, `expected S${stationIndex}`);
      const position = finiteAt(station.position, `${stationPath}.position`);
      if (position < 0 || (stationIndex > 0 && position <= finiteAt((stations[stationIndex - 1] as JsonObject).position, `${itemPath}.stations[${stationIndex - 1}].position`))) {
        fail(`${stationPath}.position`, "strictly ascending non-negative position required");
      }
      return stationId;
    });
    assertUnique(stationIds, `${itemPath}.stations`);
    return id;
  });
  assertUnique(memberIds, `${path}.members`);

  const shells = arrayAt(obj.shell_elements, `${path}.shell_elements`);
  const shellIds = shells.map((item, index) => {
    const itemPath = `${path}.shell_elements[${index}]`;
    const shell = objectAt(item, itemPath);
    exactKeys(shell, ["element_id", "element_type", "node_ids", "local_frame", "result_locations"], itemPath);
    const id = stringAt(shell.element_id, `${itemPath}.element_id`, true);
    const type = literalAt(shell.element_type, ["triangle3", "quadrilateral4"] as const, `${itemPath}.element_type`);
    const shellNodeIds = validateStringArray(shell.node_ids, `${itemPath}.node_ids`, true);
    if (shellNodeIds.length !== (type === "triangle3" ? 3 : 4) || shellNodeIds.some((nodeId) => !nodeSet.has(nodeId))) {
      fail(`${itemPath}.node_ids`, "invalid shell node coverage");
    }
    assertUnique(shellNodeIds, `${itemPath}.node_ids`);
    validateCoordinateFrame(shell.local_frame, `${itemPath}.local_frame`);
    const locations = arrayAt(shell.result_locations, `${itemPath}.result_locations`);
    if (locations.length !== 1) fail(`${itemPath}.result_locations`, "exactly one element_average required");
    const location = objectAt(locations[0], `${itemPath}.result_locations[0]`);
    exactKeys(location, ["location_id", "kind"], `${itemPath}.result_locations[0]`);
    literalAt(location.location_id, ["element_average"] as const, `${itemPath}.result_locations[0].location_id`);
    literalAt(location.kind, ["element_average"] as const, `${itemPath}.result_locations[0].kind`);
    return id;
  });
  assertUnique(shellIds, `${path}.shell_elements`);

  const solidNodeCounts: Record<string, number> = { tetra4: 4, wedge6: 6, hexa8: 8, tetra10: 10, wedge15: 15, hexa20: 20 };
  const solids = arrayAt(obj.solid_elements, `${path}.solid_elements`);
  const solidIds = solids.map((item, index) => {
    const itemPath = `${path}.solid_elements[${index}]`;
    const solid = objectAt(item, itemPath);
    exactKeys(solid, ["element_id", "element_type", "node_ids", "coordinate_frame", "result_locations"], itemPath);
    const id = stringAt(solid.element_id, `${itemPath}.element_id`, true);
    const type = literalAt(solid.element_type, Object.keys(solidNodeCounts), `${itemPath}.element_type`);
    literalAt(solid.coordinate_frame, ["global"] as const, `${itemPath}.coordinate_frame`);
    const solidNodeIds = validateStringArray(solid.node_ids, `${itemPath}.node_ids`, true);
    if (solidNodeIds.length !== solidNodeCounts[type] || solidNodeIds.some((nodeId) => !nodeSet.has(nodeId))) {
      fail(`${itemPath}.node_ids`, "invalid solid node coverage");
    }
    assertUnique(solidNodeIds, `${itemPath}.node_ids`);
    const locations = arrayAt(solid.result_locations, `${itemPath}.result_locations`);
    if (locations.length === 0) fail(`${itemPath}.result_locations`, "at least one result location required");
    const locationIds = locations.map((locationValue, locationIndex) => {
      const locationPath = `${itemPath}.result_locations[${locationIndex}]`;
      const location = objectAt(locationValue, locationPath);
      exactKeys(location, ["location_id", "natural_coordinates"], locationPath);
      const locationId = stringAt(location.location_id, `${locationPath}.location_id`, true);
      if (locationId !== `GP${locationIndex}`) fail(`${locationPath}.location_id`, `expected GP${locationIndex}`);
      const natural = objectAt(location.natural_coordinates, `${locationPath}.natural_coordinates`);
      exactKeys(natural, ["xi", "eta", "zeta"], `${locationPath}.natural_coordinates`);
      ["xi", "eta", "zeta"].forEach((key) => finiteAt(natural[key], `${locationPath}.natural_coordinates.${key}`));
      return locationId;
    });
    assertUnique(locationIds, `${itemPath}.result_locations`);
    return id;
  });
  assertUnique(solidIds, `${path}.solid_elements`);
  return obj as unknown as ResultTopology;
}

function validateCases(value: unknown, nodeIds: readonly string[]): ResultCase[] {
  const cases = arrayAt(value, "cases");
  if (cases.length < 1) fail("cases", "at least one case required");
  const nodeSet = new Set(nodeIds);
  const caseIds = cases.map((item, index) => {
    const path = `cases[${index}]`;
    const resultCase = objectAt(item, path);
    exactKeys(resultCase, ["case_id", "name", "symbol", "analysis_type", "support_node_ids"], path);
    const caseId = stringAt(resultCase.case_id, `${path}.case_id`, true);
    stringAt(resultCase.name, `${path}.name`, true);
    stringAt(resultCase.symbol, `${path}.symbol`, true);
    literalAt(resultCase.analysis_type, ["static", "material_nonlinear", "modal"] as const, `${path}.analysis_type`);
    const supportIds = validateStringArray(resultCase.support_node_ids, `${path}.support_node_ids`, true);
    assertUnique(supportIds, `${path}.support_node_ids`);
    let previousIndex = -1;
    supportIds.forEach((id, supportIndex) => {
      const topologyIndex = nodeIds.indexOf(id);
      if (!nodeSet.has(id) || topologyIndex <= previousIndex) fail(`${path}.support_node_ids[${supportIndex}]`, "supports must follow topology node order");
      previousIndex = topologyIndex;
    });
    return caseId;
  });
  assertUnique(caseIds, "cases");
  return cases as unknown as ResultCase[];
}

function validateNodeRows(value: unknown, expectedIds: readonly string[], path: string): void {
  const rows = arrayAt(value, path);
  const ids = rows.map((rowValue, index) => {
    const rowPath = `${path}[${index}]`;
    const row = objectAt(rowValue, rowPath);
    exactKeys(row, ["node_id", "components"], rowPath);
    const id = stringAt(row.node_id, `${rowPath}.node_id`, true);
    validateNamedNumbers(row.components, ["dx", "dy", "dz", "rx", "ry", "rz"], `${rowPath}.components`);
    return id;
  });
  assertUnique(ids, path);
  assertOrderedCoverage(ids, expectedIds, path);
}

function validateReactionRows(value: unknown, expectedIds: readonly string[], path: string): void {
  const rows = arrayAt(value, path);
  const ids = rows.map((rowValue, index) => {
    const rowPath = `${path}[${index}]`;
    const row = objectAt(rowValue, rowPath);
    exactKeys(row, ["node_id", "components"], rowPath);
    const id = stringAt(row.node_id, `${rowPath}.node_id`, true);
    validateNamedNumbers(row.components, ["fx", "fy", "fz", "mx", "my", "mz"], `${rowPath}.components`);
    return id;
  });
  assertUnique(ids, path);
  assertOrderedCoverage(ids, expectedIds, path);
}

function validateMemberRows(value: unknown, topology: ResultTopology, path: string): void {
  const rows = arrayAt(value, path);
  const ids = rows.map((rowValue, index) => {
    const rowPath = `${path}[${index}]`;
    const row = objectAt(rowValue, rowPath);
    exactKeys(row, ["member_id", "segments"], rowPath);
    const id = stringAt(row.member_id, `${rowPath}.member_id`, true);
    const member = topology.members[index];
    if (member === undefined || id !== member.member_id) fail(`${rowPath}.member_id`, "must follow topology member order");
    const segments = arrayAt(row.segments, `${rowPath}.segments`);
    if (segments.length !== member.stations.length - 1) fail(`${rowPath}.segments`, "segment count must cover consecutive stations");
    segments.forEach((segmentValue, segmentIndex) => {
      const segmentPath = `${rowPath}.segments[${segmentIndex}]`;
      const segment = objectAt(segmentValue, segmentPath);
      exactKeys(segment, ["segment_id", "station_i", "station_j", "length", "i_end", "j_end"], segmentPath);
      const stationI = member.stations[segmentIndex];
      const stationJ = member.stations[segmentIndex + 1];
      const expectedSegmentId = `${stationI.station_id}-${stationJ.station_id}`;
      if (segment.segment_id !== expectedSegmentId || segment.station_i !== stationI.station_id || segment.station_j !== stationJ.station_id) {
        fail(segmentPath, "segment station identity mismatch");
      }
      const length = finiteAt(segment.length, `${segmentPath}.length`);
      const expectedLength = stationJ.position - stationI.position;
      if (length < 0 || Math.abs(length - expectedLength) > FRAME_TOLERANCE * Math.max(1, Math.abs(expectedLength))) {
        fail(`${segmentPath}.length`, "segment length mismatch");
      }
      validateNamedNumbers(segment.i_end, ["fx", "fy", "fz", "mx", "my", "mz"], `${segmentPath}.i_end`);
      validateNamedNumbers(segment.j_end, ["fx", "fy", "fz", "mx", "my", "mz"], `${segmentPath}.j_end`);
    });
    return id;
  });
  assertUnique(ids, path);
  assertOrderedCoverage(ids, topology.members.map((member) => member.member_id), path);
}

function validateShellRows(value: unknown, topology: ResultTopology, path: string): void {
  const rows = arrayAt(value, path);
  const ids = rows.map((rowValue, index) => {
    const rowPath = `${path}[${index}]`;
    const row = objectAt(rowValue, rowPath);
    exactKeys(row, ["element_id", "locations"], rowPath);
    const id = stringAt(row.element_id, `${rowPath}.element_id`, true);
    const element = topology.shell_elements[index];
    if (element === undefined || id !== element.element_id) fail(`${rowPath}.element_id`, "must follow topology shell order");
    const locations = arrayAt(row.locations, `${rowPath}.locations`);
    if (locations.length !== element.result_locations.length) fail(`${rowPath}.locations`, "location coverage mismatch");
    locations.forEach((locationValue, locationIndex) => {
      const locationPath = `${rowPath}.locations[${locationIndex}]`;
      const location = objectAt(locationValue, locationPath);
      exactKeys(location, ["location_id", "membrane_force", "bending_moment", "transverse_shear", "top_stress", "bottom_stress"], locationPath);
      if (location.location_id !== element.result_locations[locationIndex].location_id) fail(`${locationPath}.location_id`, "location order mismatch");
      validateNamedNumbers(location.membrane_force, ["nx", "ny", "nxy"], `${locationPath}.membrane_force`);
      validateNamedNumbers(location.bending_moment, ["mx", "my", "mxy"], `${locationPath}.bending_moment`);
      validateNamedNumbers(location.transverse_shear, ["qx", "qy"], `${locationPath}.transverse_shear`);
      validateNamedNumbers(location.top_stress, ["sx", "sy", "txy"], `${locationPath}.top_stress`);
      validateNamedNumbers(location.bottom_stress, ["sx", "sy", "txy"], `${locationPath}.bottom_stress`);
    });
    return id;
  });
  assertUnique(ids, path);
  assertOrderedCoverage(ids, topology.shell_elements.map((element) => element.element_id), path);
}

function validateSolidRows(value: unknown, topology: ResultTopology, path: string): void {
  const rows = arrayAt(value, path);
  const ids = rows.map((rowValue, index) => {
    const rowPath = `${path}[${index}]`;
    const row = objectAt(rowValue, rowPath);
    exactKeys(row, ["element_id", "locations"], rowPath);
    const id = stringAt(row.element_id, `${rowPath}.element_id`, true);
    const element = topology.solid_elements[index];
    if (element === undefined || id !== element.element_id) fail(`${rowPath}.element_id`, "must follow topology solid order");
    const locations = arrayAt(row.locations, `${rowPath}.locations`);
    if (locations.length !== element.result_locations.length) fail(`${rowPath}.locations`, "location coverage mismatch");
    locations.forEach((locationValue, locationIndex) => {
      const locationPath = `${rowPath}.locations[${locationIndex}]`;
      const location = objectAt(locationValue, locationPath);
      exactKeys(location, ["location_id", "stress", "strain"], locationPath);
      if (location.location_id !== element.result_locations[locationIndex].location_id) fail(`${locationPath}.location_id`, "location order mismatch");
      validateNamedNumbers(location.stress, ["sx", "sy", "sz", "txy", "tyz", "tzx"], `${locationPath}.stress`);
      validateNamedNumbers(location.strain, ["ex", "ey", "ez", "gxy", "gyz", "gzx"], `${locationPath}.strain`);
    });
    return id;
  });
  assertUnique(ids, path);
  assertOrderedCoverage(ids, topology.solid_elements.map((element) => element.element_id), path);
}

function validateForceFields(result: JsonObject, resultCase: ResultCase, topology: ResultTopology, path: string): void {
  validateNodeRows(result.node_displacements, topology.nodes.map((node) => node.node_id), `${path}.node_displacements`);
  validateReactionRows(result.support_reactions, resultCase.support_node_ids, `${path}.support_reactions`);
  validateMemberRows(result.member_section_forces, topology, `${path}.member_section_forces`);
  validateShellRows(result.shell_results, topology, `${path}.shell_results`);
  validateSolidRows(result.solid_results, topology, `${path}.solid_results`);
}

function validateResults(value: unknown, cases: readonly ResultCase[], topology: ResultTopology): AnalysisResult[] {
  const results = arrayAt(value, "results");
  if (results.length === 0) fail("results", "at least one result required");
  const casesById = new Map(cases.map((resultCase) => [resultCase.case_id, resultCase]));
  const grouped = new Map<string, JsonObject[]>();
  let previousCaseIndex = -1;
  let previousStateIndex = -1;
  const coordinates = new Set<string>();

  results.forEach((resultValue, index) => {
    const path = `results[${index}]`;
    const result = objectAt(resultValue, path);
    const caseId = stringAt(result.case_id, `${path}.case_id`, true);
    const resultCase = casesById.get(caseId);
    if (resultCase === undefined) fail(`${path}.case_id`, "unknown case ID");
    const state = objectAt(result.state, `${path}.state`);
    const kind = literalAt(state.kind, ["static", "load_step", "mode"] as const, `${path}.state.kind`);
    const stateIndex = integerAt(state.index, `${path}.state.index`);
    const coordinate = `${caseId}\u0000${kind}\u0000${stateIndex}`;
    if (coordinates.has(coordinate)) fail(path, "duplicate case/state coordinate");
    coordinates.add(coordinate);

    const caseIndex = cases.indexOf(resultCase);
    if (caseIndex < previousCaseIndex || (caseIndex === previousCaseIndex && stateIndex <= previousStateIndex)) {
      fail(path, "results must use case-major ascending state order");
    }
    if (caseIndex !== previousCaseIndex) previousStateIndex = -1;
    previousCaseIndex = caseIndex;
    previousStateIndex = stateIndex;

    if (kind === "static") {
      if (resultCase.analysis_type !== "static") fail(path, "static state does not match case analysis_type");
      exactKeys(result, ["case_id", "state", "node_displacements", "support_reactions", "member_section_forces", "shell_results", "solid_results", "diagnostics"], path);
      exactKeys(state, ["kind", "index"], `${path}.state`);
      if (stateIndex !== 0) fail(`${path}.state.index`, "static index must be zero");
      validateForceFields(result, resultCase, topology, path);
      const diagnostics = objectAt(result.diagnostics, `${path}.diagnostics`);
      exactKeys(diagnostics, ["warnings"], `${path}.diagnostics`);
      validateWarnings(diagnostics.warnings, `${path}.diagnostics.warnings`);
    } else if (kind === "load_step") {
      if (resultCase.analysis_type !== "material_nonlinear") fail(path, "load_step state does not match case analysis_type");
      exactKeys(result, ["case_id", "state", "node_displacements", "support_reactions", "member_section_forces", "shell_results", "solid_results", "diagnostics"], path);
      exactKeys(state, ["kind", "index", "load_factor", "is_final"], `${path}.state`);
      finiteAt(state.load_factor, `${path}.state.load_factor`);
      booleanAt(state.is_final, `${path}.state.is_final`);
      validateForceFields(result, resultCase, topology, path);
      const diagnostics = objectAt(result.diagnostics, `${path}.diagnostics`);
      exactKeys(diagnostics, ["warnings", "iterations"], `${path}.diagnostics`);
      validateWarnings(diagnostics.warnings, `${path}.diagnostics.warnings`);
      const iterations = arrayAt(diagnostics.iterations, `${path}.diagnostics.iterations`);
      iterations.forEach((iterationValue, iterationIndex) => {
        const iterationPath = `${path}.diagnostics.iterations[${iterationIndex}]`;
        const iteration = objectAt(iterationValue, iterationPath);
        exactKeys(iteration, ["index", "residual_norm", "correction_norm", "converged"], iterationPath);
        if (integerAt(iteration.index, `${iterationPath}.index`) !== iterationIndex) {
          fail(`${iterationPath}.index`, "iteration indices must start at zero and be consecutive");
        }
        if (finiteAt(iteration.residual_norm, `${iterationPath}.residual_norm`) < 0) {
          fail(`${iterationPath}.residual_norm`, "non-negative value required");
        }
        if (finiteAt(iteration.correction_norm, `${iterationPath}.correction_norm`) < 0) {
          fail(`${iterationPath}.correction_norm`, "non-negative value required");
        }
        booleanAt(iteration.converged, `${iterationPath}.converged`);
      });
    } else {
      if (resultCase.analysis_type !== "modal") fail(path, "mode state does not match case analysis_type");
      exactKeys(result, ["case_id", "state", "node_mode_shapes", "diagnostics"], path);
      exactKeys(state, ["kind", "index", "eigenvalue", "frequency", "degeneracy_group"], `${path}.state`);
      const eigenvalue = finiteAt(state.eigenvalue, `${path}.state.eigenvalue`);
      const frequency = finiteAt(state.frequency, `${path}.state.frequency`);
      if (eigenvalue <= 0 || frequency <= 0) fail(`${path}.state`, "positive eigenvalue and frequency required");
      const expectedFrequency = Math.sqrt(eigenvalue) / (2 * Math.PI);
      if (
        Math.abs(frequency - expectedFrequency) >
        MODAL_FREQUENCY_RELATIVE_TOLERANCE * Math.max(1, Math.abs(expectedFrequency))
      ) {
        fail(`${path}.state.frequency`, "must equal sqrt(eigenvalue) / (2*pi)");
      }
      integerAt(state.degeneracy_group, `${path}.state.degeneracy_group`);
      validateNodeRows(result.node_mode_shapes, topology.nodes.map((node) => node.node_id), `${path}.node_mode_shapes`);
      const diagnostics = objectAt(result.diagnostics, `${path}.diagnostics`);
      exactKeys(diagnostics, ["warnings", "normalization", "eigenvalue_tolerance", "degeneracy_relative_tolerance"], `${path}.diagnostics`);
      validateWarnings(diagnostics.warnings, `${path}.diagnostics.warnings`);
      literalAt(diagnostics.normalization, ["mass"] as const, `${path}.diagnostics.normalization`);
      if (finiteAt(diagnostics.eigenvalue_tolerance, `${path}.diagnostics.eigenvalue_tolerance`) < 0) fail(`${path}.diagnostics.eigenvalue_tolerance`, "non-negative value required");
      if (diagnostics.degeneracy_relative_tolerance !== DEGENERACY_TOLERANCE) fail(`${path}.diagnostics.degeneracy_relative_tolerance`, "must equal 1e-8");
    }
    const caseResults = grouped.get(caseId) ?? [];
    caseResults.push(result);
    grouped.set(caseId, caseResults);
  });

  cases.forEach((resultCase) => {
    const caseResults = grouped.get(resultCase.case_id) ?? [];
    if (caseResults.length === 0) fail("results", `case ${resultCase.case_id} has no results`);
    if (resultCase.analysis_type === "static") {
      if (caseResults.length !== 1) fail("results", `static case ${resultCase.case_id} must have one result`);
      return;
    }
    caseResults.forEach((result, index) => {
      const state = result.state as JsonObject;
      if (state.index !== index) fail("results", `case ${resultCase.case_id} state indices must start at zero and be consecutive`);
    });
    if (resultCase.analysis_type === "material_nonlinear") {
      const finals = caseResults.filter((result) => (result.state as JsonObject).is_final === true);
      if (finals.length !== 1 || (caseResults[caseResults.length - 1].state as JsonObject).is_final !== true) {
        fail("results", `nonlinear case ${resultCase.case_id} must mark only its last step final`);
      }
    } else if (resultCase.analysis_type === "modal") {
      const states = caseResults.map((result) => result.state as JsonObject);
      const eigenvalues = states.map((state) => state.eigenvalue as number);
      if (eigenvalues.some((value, index) => index > 0 && value < eigenvalues[index - 1])) {
        fail("results", `modal case ${resultCase.case_id} eigenvalues must be ascending`);
      }
      const groups = states.map((state) => state.degeneracy_group as number);
      if (
        groups[0] !== 0 ||
        groups.some((group, index) => index > 0 && group !== groups[index - 1] && group !== groups[index - 1] + 1)
      ) {
        fail("results", `modal case ${resultCase.case_id} degeneracy groups must be contiguous`);
      }
    }
  });
  return results as unknown as AnalysisResult[];
}

function deepFreeze<T>(value: T): T {
  if (typeof value !== "object" || value === null || Object.isFrozen(value)) return value;
  Object.getOwnPropertyNames(value).forEach((name) => deepFreeze((value as JsonObject)[name]));
  return Object.freeze(value);
}

export function analysisResultCoordinate(result: AnalysisResult): string {
  return `${result.case_id}\u0000${result.state.kind}\u0000${result.state.index}`;
}

export function analysisResultSelectionKey(result: AnalysisResult): string {
  if (result.state.kind === "static") return result.case_id;
  return `${result.case_id}@${result.state.kind}:${result.state.index}`;
}

export function analysisResultSelectionLabel(resultCase: ResultCase, result: AnalysisResult): string {
  const caseLabel = resultCase.name.length > 0 ? resultCase.name : resultCase.case_id;
  if (result.state.kind === "static") return `${resultCase.case_id}.${caseLabel}`;
  if (result.state.kind === "load_step") {
    return `${resultCase.case_id}.${caseLabel} / Step ${result.state.index + 1}`;
  }
  return `${resultCase.case_id}.${caseLabel} / Mode ${result.state.index + 1}`;
}

export function validateAndIndexAnalysisResultSet(value: unknown): AnalysisResultSetIndex {
  const root = objectAt(value, "$" );
  exactKeys(root, ["kind", "schema_version", "units", "coordinate_system", "cases", "topology", "results"], "$" );
  literalAt(root.kind, ["analysis_result_set"] as const, "$.kind");
  literalAt(root.schema_version, ["1.0"] as const, "$.schema_version");

  const units = objectAt(root.units, "units");
  exactKeys(units, ["system", "length", "force", "mass", "time"], "units");
  ["system", "length", "force", "mass", "time"].forEach((key) => stringAt(units[key], `units.${key}`, true));

  const coordinateSystem = objectAt(root.coordinate_system, "coordinate_system");
  exactKeys(coordinateSystem, ["name", "handedness", "axes"], "coordinate_system");
  literalAt(coordinateSystem.name, ["global_cartesian"] as const, "coordinate_system.name");
  literalAt(coordinateSystem.handedness, ["right"] as const, "coordinate_system.handedness");
  const axes = validateStringArray(coordinateSystem.axes, "coordinate_system.axes");
  if (axes.length !== 3 || axes[0] !== "x" || axes[1] !== "y" || axes[2] !== "z") fail("coordinate_system.axes", "must equal [x, y, z]");

  const topology = validateTopology(root.topology);
  const cases = validateCases(root.cases, topology.nodes.map((node) => node.node_id));
  const results = validateResults(root.results, cases, topology);
  const resultSet = deepFreeze(root as unknown as AnalysisResultSet);
  const casesById = new Map(cases.map((resultCase) => [resultCase.case_id, resultCase]));
  const resultsByCase = new Map<string, readonly AnalysisResult[]>();
  const resultsByCoordinate = new Map<string, AnalysisResult>();
  cases.forEach((resultCase) => {
    resultsByCase.set(resultCase.case_id, results.filter((result) => result.case_id === resultCase.case_id));
  });
  results.forEach((result) => resultsByCoordinate.set(analysisResultCoordinate(result), result));
  return Object.freeze({
    value: resultSet,
    caseOrder: Object.freeze(cases.map((resultCase) => resultCase.case_id)),
    casesById,
    resultsInOrder: resultSet.results,
    resultsByCase,
    resultsByCoordinate,
  });
}

export function isForceAnalysisResult(result: AnalysisResult): result is ForceAnalysisResult {
  return result.state.kind === "static" || result.state.kind === "load_step";
}

export function selectAnalysisResult(
  index: AnalysisResultSetIndex,
  selection: AnalysisResultSelection
): AnalysisResult {
  const coordinate = `${selection.case_id}\u0000${selection.state_kind}\u0000${selection.state_index}`;
  const result = index.resultsByCoordinate.get(coordinate);
  if (result === undefined) {
    throw new Error(
      `Analysis result was not found for case ${selection.case_id}, ${selection.state_kind} ${selection.state_index}.`
    );
  }
  return result;
}

export function selectShellResults(
  index: AnalysisResultSetIndex,
  selection: AnalysisResultSelection
): readonly ShellResult[] {
  const result = selectAnalysisResult(index, selection);
  if (!isForceAnalysisResult(result)) {
    throw new Error("Shell results are unavailable for modal analysis results.");
  }
  return result.shell_results;
}

export function selectSolidResults(
  index: AnalysisResultSetIndex,
  selection: AnalysisResultSelection
): readonly SolidResult[] {
  const result = selectAnalysisResult(index, selection);
  if (!isForceAnalysisResult(result)) {
    throw new Error("Solid results are unavailable for modal analysis results.");
  }
  return result.solid_results;
}

export function requireStaticResults(index: AnalysisResultSetIndex): readonly StaticAnalysisResult[] {
  const nonStatic = index.resultsInOrder.find((result) => result.state.kind !== "static");
  if (nonStatic !== undefined) {
    throw new Error("DEFINE/COMBINE/PICKUP は静的解析結果にのみ使用できます。");
  }
  return index.resultsInOrder as readonly StaticAnalysisResult[];
}
