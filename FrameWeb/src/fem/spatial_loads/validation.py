"""Input and geometry preparation without changing the mesh or load definitions."""
from dataclasses import dataclass

import numpy as np

from .definitions import SpatialLoadDefinitions
from .geometry import (
    PanelCell, PanelGeometry, PlanarFrame, clip_polygon, frozen_points,
    points_array, signed_area, split_line, triangulate_convex, triangulate_simple, validate_partition,
)
from .interpolation import build_strip, project_path


def validate_references(definitions, mesh):
    from ..capabilities import canonical_element_type

    if not isinstance(definitions, SpatialLoadDefinitions):
        raise ValueError('spatial_loads must be SpatialLoadDefinitions')
    structural_nodes = {n for e in mesh.elements.values() for n in e['nodes']}
    for panel in definitions.panels:
        for node in panel.nodes:
            if node not in mesh.nodes or node not in structural_nodes:
                raise ValueError(f'panel {panel.id}: missing structural node {node}')
        for element_id in panel.elements:
            element = mesh.elements.get(element_id)
            if element is None or canonical_element_type(element['type']) != 'shell':
                raise ValueError(f'panel {panel.id}: missing shell element {element_id}')
            if not set(element['nodes']) <= set(panel.nodes):
                raise ValueError(f'panel {panel.id}: shell {element_id} uses non-panel nodes')


def prepare_panel(panel, mesh):
    """Build a validated planar T3/Q4 partition from explicit connectivity."""
    label = f'panel {panel.id}'
    validate_references(SpatialLoadDefinitions(panels=(panel,)), mesh)
    vertices = points_array([mesh.nodes[n] for n in panel.nodes], 3, label)
    frame = PlanarFrame.from_points(vertices, panel.tolerance, label)
    if panel.plane is None:
        if np.ptp(vertices[:, 2]) > frame.eps:
            raise ValueError(f'{label}: only global XY parallel panels are supported without an explicit plane')
        frame = PlanarFrame(frame.origin, (1., 0., 0.), (0., 1., 0.), (0., 0., 1.), frame.scale, frame.eps)
    else:
        plane = panel.plane
        frame = PlanarFrame(plane.origin, plane.axis_u, plane.axis_v,
                            tuple(np.cross(plane.axis_u, plane.axis_v)), frame.scale, frame.eps)
    projected = frame.project(vertices, label)
    for i, point in enumerate(projected):
        if any(np.linalg.norm(point - other) <= frame.eps for other in projected[i + 1:]):
            raise ValueError(f'{label}: coincident structural nodes')
    coordinates = dict(zip(panel.nodes, frozen_points(projected)))
    cells = []
    connectivity = ((eid, tuple(mesh.elements[eid]['nodes']), eid) for eid in panel.elements)
    if panel.triangles:
        connectivity = ((i, nodes, None) for i, nodes in enumerate(panel.triangles))
    for key, nodes, element_id in connectivity:
        if len(nodes) not in (3, 4) or len(set(nodes)) != len(nodes):
            raise ValueError(f'{label} cell {key}: only distinct T3/Q4 nodes are supported')
        cells.append(PanelCell(key, nodes, tuple(coordinates[n] for n in nodes), element_id))
    if {n for cell in cells for n in cell.node_ids} != set(panel.nodes):
        raise ValueError(f'{label}: unused panel nodes')
    cells = tuple(sorted(cells, key=lambda cell: cell.key))
    if panel.loading_nodes:
        loading = {node.id: point for node in panel.loading_nodes
                   for point in [tuple(frame.project((node.point,), label + ' loading node')[0])]}
        domain_cells = tuple(PanelCell(i, triangle, tuple(loading[n] for n in triangle))
                             for i, triangle in enumerate(panel.loading_triangles))
        # Structural cells are the interpolation target. Their holes are
        # inferred here because panel.holes belongs to the independent domain.
        validate_partition(cells, frame.eps, label + ' structural target', holes=None)
        boundary = validate_partition(domain_cells, frame.eps, label, holes=panel.holes)
        holes = tuple(tuple(loading[n] for n in ring) for ring in panel.holes)
        for domain in domain_cells:
            covered = sum(abs(signed_area(clip_polygon(domain.points, cell.points, frame.eps)))
                          for cell in cells)
            if abs(covered - abs(signed_area(domain.points))) > frame.eps * frame.scale:
                raise ValueError(f'{label}: loading mesh lies outside structural target')
    else:
        boundary = validate_partition(cells, frame.eps, label, holes=panel.holes)
        holes = tuple(tuple(coordinates[n] for n in ring) for ring in panel.holes)
        # Empty means the structural cells themselves are the loading domain.
        domain_cells = ()
    return PanelGeometry(panel.id, frame, cells, boundary, holes, domain_cells)


@dataclass(frozen=True)
class AreaPiece:
    cell_index: int
    strip_index: int
    triangle: tuple


@dataclass(frozen=True)
class PreparedLoad:
    """Geometry only. This does not enable analysis or contain assembled loads."""
    load: object
    panel: PanelGeometry
    paths: tuple
    line_pieces: tuple = ()
    strip_cells: tuple = ()
    area_pieces: tuple[AreaPiece, ...] = ()


def prepare_geometry(definitions, mesh):
    """Validate all panels and referenced load paths, returning immutable geometry.

    The static solver calls this through the compiler before stiffness assembly.
    """
    validate_references(definitions, mesh)
    panels = {p.id: prepare_panel(p, mesh) for p in definitions.panels}
    path_definitions = {p.id: p for p in definitions.paths}
    prepared = []
    for load in definitions.loads:
        panel = panels[load.panel_id]
        label = f'load {load.id} panel {load.panel_id}'
        paths = tuple(project_path(path_definitions[pid], panel, f'{label} path {pid}')
                      for pid in load.path_ids)
        if len(paths) == 1:
            pieces = split_line(panel, paths[0].points, paths[0].parameters, label)
            prepared.append(PreparedLoad(load, panel, paths, line_pieces=pieces))
            continue
        strips = build_strip(*paths, load.end_intensities, panel.frame.eps, label)
        # Hole interiors are excluded explicitly. The outer boundary remains a
        # hard domain limit, so strips cannot bridge an exterior concave notch.
        outer_triangles = triangulate_simple(panel.boundary, panel.frame.eps, label)
        hole_triangles = tuple(t for ring in panel.holes
                               for t in triangulate_simple(ring, panel.frame.eps, label))
        pieces = []
        for strip_index, strip in enumerate(strips):
            triangles = []
            for cell_index, cell in enumerate(panel.cells):
                # Validated structural cells do not overlap, so without an
                # independent loading mesh a cell is its only domain.
                for domain in panel.domain_cells or (cell,):
                    clipped = clip_polygon(strip.points, domain.points, panel.frame.eps)
                    clipped = clip_polygon(clipped, cell.points, panel.frame.eps)
                    for triangle in triangulate_convex(clipped, panel.frame.eps):
                        pieces.append(AreaPiece(cell_index, strip_index, triangle))
                        triangles.append(triangle)
            integrated_area = sum(abs(signed_area(t)) for t in triangles)
            strip_area = abs(signed_area(strip.points))
            outer_area = sum(abs(signed_area(clip_polygon(strip.points, t, panel.frame.eps)))
                             for t in outer_triangles)
            hole_area = sum(abs(signed_area(clip_polygon(strip.points, t, panel.frame.eps)))
                            for t in hole_triangles)
            area_eps = panel.frame.eps * panel.frame.scale
            if abs(outer_area - strip_area) > area_eps:
                raise ValueError(f'{label}: area leaves panel outer boundary; extrapolation is forbidden')
            if abs(integrated_area - (strip_area - hole_area)) > area_eps:
                raise ValueError(f'{label}: clipped area is incomplete or duplicated')
        if not pieces:
            raise ValueError(f'{label}: area has no overlap with the panel outside holes')
        prepared.append(PreparedLoad(load, panel, paths, strip_cells=strips, area_pieces=tuple(pieces)))
    return tuple(prepared)
