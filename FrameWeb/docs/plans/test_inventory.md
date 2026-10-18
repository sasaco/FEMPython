# 現行テスト台帳

2026-09-10。66ファイル・386関数定義・2,026展開ケース。`material_nonlinear` markerは909ケース。一般FEMの修復に加え、PQ-01〜10・PQ-12の固有値、配布、機能表、VTK、診断、メッシュ収束、単位不変性、性能計測契約を反映した。関数数とパラメータ展開数を混同しない。

2026-09-12の[支持条件・釣合い共通化](../report/support-equilibrium-refactoring.md)で、
支持条件11ケース・公開後処理6ケースを追加。上記の件数は2026-09-10時点の記録であり、
今回の全件実測は実施記録を参照する。

同日の[スリップ支持ばね実装](../report/slip-support-implementation.md)で5テストファイル・95ケースを追加。
履歴28、入力36、ソルバー17、混在要素6、公開履歴回帰8ケースを検証する。

2026-10-18の[JRSNAP互換 支持ばね実装](../report/jrsnap-support-implementation.md)で6テストファイル・50関数・126ケースを追加
（goldenの凍結試験`harness/test_jrsnap_golden.py`の29ケースは別）。
ばね則・golden照合52、スリップ特性固定とレジストリ10、ソルバー14、入力29、混在要素・2次元13、公開履歴回帰8ケースを検証する。

[項目表](test_items.md) / [実行方法](../../tests/README.md) / [移行・統合記録](../report/test-reorganization.md)。整理前の全 264 関数・112 ファイルの移管先と hash は [機械可読な移行記録](../report/test-reorganization-evidence.json) にある。

| ファイル | 関数 | ケース | 主区分 |
|---|---:|---:|---|
| [elements/beam/test_elastic.py](../../tests/elements/beam/test_elastic.py) | 6 | 11 | unit |
| [elements/beam/test_foundation.py](../../tests/elements/beam/test_foundation.py) | 3 | 6 | unit |
| [elements/beam/test_kinematics.py](../../tests/elements/beam/test_kinematics.py) | 3 | 15 | unit |
| [elements/beam/test_nonlinear_section.py](../../tests/elements/beam/test_nonlinear_section.py) | 8 | 33 | unit |
| [elements/beam/test_precision.py](../../tests/elements/beam/test_precision.py) | 4 | 8 | unit |
| [elements/shell/test_dkt.py](../../tests/elements/shell/test_dkt.py) | 3 | 14 | unit |
| [elements/shell/test_kinematics.py](../../tests/elements/shell/test_kinematics.py) | 15 | 44 | unit |
| [elements/shell/test_pressure.py](../../tests/elements/shell/test_pressure.py) | 3 | 22 | unit |
| [elements/solid/test_linear.py](../../tests/elements/solid/test_linear.py) | 7 | 18 | unit |
| [elements/solid/test_quadratic.py](../../tests/elements/solid/test_quadratic.py) | 3 | 9 | unit |
| [elements/test_base.py](../../tests/elements/test_base.py) | 7 | 9 | unit |
| [harness/test_comparison.py](../../tests/harness/test_comparison.py) | 2 | 2 | unit |
| [harness/test_general_fem_repairs.py](../../tests/harness/test_general_fem_repairs.py) | 1 | 4 | unit |
| [harness/test_sample_runner.py](../../tests/harness/test_sample_runner.py) | 5 | 14 | unit |
| [harness/test_source_repairs.py](../../tests/harness/test_source_repairs.py) | 7 | 37 | unit |
| [harness/test_suite_contracts.py](../../tests/harness/test_suite_contracts.py) | 7 | 15 | unit |
| [harness/test_release_workflows.py](../../tests/harness/test_release_workflows.py) | 3 | 3 | unit |
| [integration/test_axial_subdivision.py](../../tests/integration/test_axial_subdivision.py) | 4 | 9 | integration |
| [integration/test_beam_foundation.py](../../tests/integration/test_beam_foundation.py) | 2 | 2 | integration |
| [integration/test_beam_precision.py](../../tests/integration/test_beam_precision.py) | 10 | 16 | integration |
| [integration/test_beam_solutions.py](../../tests/integration/test_beam_solutions.py) | 3 | 14 | integration |
| [integration/test_curvature_output.py](../../tests/integration/test_curvature_output.py) | 7 | 31 | integration |
| [integration/test_input_routes.py](../../tests/integration/test_input_routes.py) | 3 | 3 | integration |
| [integration/test_jr_beam_history.py](../../tests/integration/test_jr_beam_history.py) | 3 | 21 | integration |
| [integration/test_linear_beam.py](../../tests/integration/test_linear_beam.py) | 12 | 17 | integration |
| [integration/test_load_transfer.py](../../tests/integration/test_load_transfer.py) | 3 | 22 | integration |
| [integration/test_model_contracts.py](../../tests/integration/test_model_contracts.py) | 3 | 5 | integration |
| [integration/test_nonlinear_convergence.py](../../tests/integration/test_nonlinear_convergence.py) | 2 | 8 | integration |
| [integration/test_nonlinear_reference.py](../../tests/integration/test_nonlinear_reference.py) | 8 | 423 | integration |
| [integration/test_plate_bending.py](../../tests/integration/test_plate_bending.py) | 2 | 12 | integration |
| [integration/test_pressure_solution.py](../../tests/integration/test_pressure_solution.py) | 2 | 2 | integration |
| [integration/test_solids.py](../../tests/integration/test_solids.py) | 2 | 12 | integration |
| [io/test_http.py](../../tests/io/test_http.py) | 6 | 21 | integration |
| [io/test_diagnostics.py](../../tests/io/test_diagnostics.py) | 11 | 11 | integration |
| [io/test_model_lifecycle.py](../../tests/io/test_model_lifecycle.py) | 4 | 6 | integration |
| [io/test_model_roundtrip.py](../../tests/io/test_model_roundtrip.py) | 2 | 2 | integration |
| [io/test_pressure.py](../../tests/io/test_pressure.py) | 8 | 8 | integration |
| [io/test_package_metadata.py](../../tests/io/test_package_metadata.py) | 2 | 2 | integration |
| [io/test_source_input.py](../../tests/io/test_source_input.py) | 3 | 13 | integration |
| [io/test_structural_input.py](../../tests/io/test_structural_input.py) | 9 | 28 | integration |
| [io/test_vtk.py](../../tests/io/test_vtk.py) | 5 | 7 | integration |
| [materials/test_jr_hysteresis.py](../../tests/materials/test_jr_hysteresis.py) | 26 | 152 | unit |
| [postprocess/test_beam_equilibrium.py](../../tests/postprocess/test_beam_equilibrium.py) | 4 | 11 | unit |
| [postprocess/test_result_processor.py](../../tests/postprocess/test_result_processor.py) | 9 | 10 | unit |
| [postprocess/test_shell_results.py](../../tests/postprocess/test_shell_results.py) | 10 | 33 | unit |
| [postprocess/test_solid_stress.py](../../tests/postprocess/test_solid_stress.py) | 1 | 3 | unit |
| [regression/test_cantilever_history.py](../../tests/regression/test_cantilever_history.py) | 4 | 5 | regression |
| [regression/test_displacement_control_history.py](../../tests/regression/test_displacement_control_history.py) | 4 | 7 | regression |
| [regression/test_structural_samples.py](../../tests/regression/test_structural_samples.py) | 1 | 323 | regression |
| [solvers/test_boundary_conditions.py](../../tests/solvers/test_boundary_conditions.py) | 6 | 6 | unit |
| [solvers/test_support_equilibrium.py](../../tests/solvers/test_support_equilibrium.py) | 7 | 11 | unit |
| [solvers/test_capabilities.py](../../tests/solvers/test_capabilities.py) | 9 | 14 | unit |
| [solvers/test_linear.py](../../tests/solvers/test_linear.py) | 3 | 5 | unit |
| [solvers/test_modal_analysis.py](../../tests/solvers/test_modal_analysis.py) | 12 | 17 | unit |
| [solvers/test_nonlinear.py](../../tests/solvers/test_nonlinear.py) | 9 | 15 | unit |
| [solvers/test_unification.py](../../tests/solvers/test_unification.py) | 16 | 21 | unit |
| [validation/test_mesh_convergence.py](../../tests/validation/test_mesh_convergence.py) | 4 | 4 | oracle |
| [validation/test_performance.py](../../tests/validation/test_performance.py) | 3 | 3 | integration |
| [validation/test_plane_frame_series.py](../../tests/validation/test_plane_frame_series.py) | 8 | 197 | oracle |
| [validation/test_provenance.py](../../tests/validation/test_provenance.py) | 6 | 13 | oracle |
| [validation/test_shell_variational.py](../../tests/validation/test_shell_variational.py) | 5 | 16 | oracle |
| [validation/test_solid_sources.py](../../tests/validation/test_solid_sources.py) | 3 | 9 | oracle |
| [validation/test_source_solid_reference.py](../../tests/validation/test_source_solid_reference.py) | 7 | 16 | oracle |
| [validation/test_space_frame_series.py](../../tests/validation/test_space_frame_series.py) | 10 | 128 | oracle |
| [validation/test_stored_references.py](../../tests/validation/test_stored_references.py) | 6 | 10 | oracle |
| [validation/test_triangle_sample_references.py](../../tests/validation/test_triangle_sample_references.py) | 5 | 12 | oracle |
| [validation/test_unit_invariance.py](../../tests/validation/test_unit_invariance.py) | 5 | 5 | oracle |

## 各ファイルが所有する検証

### elements/beam/test_elastic.py

elements/beam / elastic contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_elastic_limit_matches_closed_form_matrix` | 6 |
| `test_bernoulli_euler_force_output_uses_right_handed_moments` | 1 |
| `test_invalid_node_count` | 0 |
| `test_axial_stiffness_and_end_forces` | 0 |
| `test_mass_matrix` | 0 |
| `test_rotated_stiffness_is_symmetric` | 0 |

### elements/beam/test_foundation.py

elements/beam / foundation contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_axial_foundation_matches_hyperbolic_dynamic_stiffness` | 3 |
| `test_long_bending_foundation_matches_independent_half_infinite_solution` | 1 |
| `test_linear_foundation_particular_solution_preserves_consistent_loads` | 2 |

### elements/beam/test_kinematics.py

elements/beam / kinematics contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_rigid_body_motion_has_zero_force` | 12 |
| `test_endpoint_forces_and_moments_balance` | 2 |
| `test_rotated_response_matches_independent_basis_rotation` | 1 |

### elements/beam/test_nonlinear_section.py

elements/beam / nonlinear section contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_response_curvature_is_local_total_and_does_not_change_history` | 6 |
| `test_tangent_is_finite_difference_of_force` | 4 |
| `test_section_skeleton_uses_strain_or_curvature_not_endpoint_motion` | 12 |
| `test_named_bending_law_leaves_other_plane_elastic` | 2 |
| `test_trial_order_cannot_change_result_or_committed_history` | 1 |
| `test_tangent_and_output_do_not_replace_trial_to_be_committed` | 1 |
| `test_output_preserves_converged_force_after_commit_and_other_trials` | 1 |
| `test_tangent_with_fixed_committed_history_on_smooth_branches` | 6 |

### elements/beam/test_precision.py

elements/beam / precision contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_stiff_beam_rigid_rotation_force_does_not_cancel_large_products` | 3 |
| `test_loaded_beam_internal_force_and_section_force_use_same_kinematics` | 1 |
| `test_polynomial_beam_virtual_work_and_force_resultants` | 3 |
| `test_beam_keeps_displacement_below_the_rigid_translation_ulp` | 1 |

### elements/shell/test_dkt.py

elements/shell / dkt contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_dkt_quadratic_bending_has_exact_thickness_cubed_energy` | 12 |
| `test_dkt_covariance_and_exactly_six_null_modes` | 1 |
| `test_dkt_requires_a_triangle` | 1 |

### elements/shell/test_kinematics.py

elements/shell / kinematics contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_shell_six_rigid_body_modes_have_no_internal_force` | 12 |
| `test_shell_stiffness_and_mass_rotate_with_the_plane` | 2 |
| `test_shell_affine_membrane_energy` | 2 |
| `test_degenerate_shell_is_rejected_without_fallback` | 2 |
| `test_shell_has_exactly_six_physical_rigid_zero_modes` | 2 |
| `test_drill_spin_constraint_has_independent_constant_field_energy` | 2 |
| `test_quad_constant_bending_energy_has_no_shear_locking` | 6 |
| `test_quad_rounding_tail_preserves_very_thin_plate_bending` | 2 |
| `test_initialization` | 2 |
| `test_shape_functions` | 2 |
| `test_shape_derivatives` | 2 |
| `test_gauss_points` | 2 |
| `test_jacobian_determinant` | 2 |
| `test_stiffness_matrix` | 2 |
| `test_mass_matrix` | 2 |

### elements/shell/test_pressure.py

Surface-pressure nodal values, face direction and force/moment balance.

| 検証関数 | 展開ケース |
|---|---:|
| `test_surface_pressure_nodal_values_and_equilibrium` | 15 |
| `test_invalid_surface_pressure_never_falls_back` | 4 |
| `test_invalid_triangle_pressure` | 3 |

### elements/solid/test_linear.py

Linear solid interpolation, quadrature and matrix contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_initialization` | 3 |
| `test_shape_functions` | 3 |
| `test_shape_derivatives` | 3 |
| `test_tetrahedron_volume` | 1 |
| `test_gauss_points` | 2 |
| `test_stiffness_matrix` | 3 |
| `test_mass_matrix` | 3 |

### elements/solid/test_quadratic.py

elements/solid / quadratic contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_quadratic_partition_polynomials_derivatives` | 3 |
| `test_quadratic_affine_energy_mass_and_six_rigid_modes` | 3 |
| `test_quadratic_inverted_geometry_is_rejected` | 3 |

### elements/test_base.py

elements / base contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_unimplemented_geometry_contract` | 3 |
| `test_initialization` | 0 |
| `test_invalid_input` | 0 |
| `test_set_node_coordinates` | 0 |
| `test_nonexistent_node` | 0 |
| `test_get_element_length` | 0 |
| `test_get_jacobian` | 0 |

### harness/test_comparison.py

harness / comparison contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_comparator_rejects_missing_extra_wrong_sign_and_number` | 1 |
| `test_legacy_cut_signs_and_labels_have_independent_physical_values` | 1 |

### harness/test_general_fem_repairs.py

修復参照・入力保持・独立計算ソースのハッシュを照合する。

| 検証関数 | 展開ケース |
|---|---:|
| `test_repaired_reference_input_and_source_hashes_are_current` | 4 |

### harness/test_sample_runner.py

harness / sample runner contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_missing_sample_reference_never_writes` | 1 |
| `test_sample_comparison_really_rejects_corrupted_reference` | 5 |
| `test_late_step_reference_mutation_is_detected` | 3 |
| `test_audit_visits_later_case_after_first_error_and_never_writes_reference` | 1 |
| `test_cantilever_step_zero_reference_is_compared_in_addition_to_all_step_oracle` | 4 |

### harness/test_source_repairs.py

harness / source repairs contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_source_repair_rejects_changed_model_or_unknown_reference` | 8 |
| `test_support_reference_repair_rejects_unverified_evidence` | 6 |
| `test_support_reference_completion_preserves_inputs_and_is_idempotent` | 1 |
| `test_tri1_reference_rejects_changed_input_conditions` | 7 |
| `test_tetra_input_repair_rejects_unverified_conditions` | 9 |
| `test_solid_source_repair_is_pure_and_idempotent` | 5 |
| `test_tetrahedron_reference_repair_preserves_input_and_is_idempotent` | 1 |

### harness/test_suite_contracts.py

Prevent collection drift, reference contamination and baseline masking.

| 検証関数 | 展開ケース |
|---|---:|
| `test_sample_manifest_covers_all_original_files_cases_and_snapshots` | 1 |
| `test_test_modules_do_not_import_each_other_or_use_historical_names` | 1 |
| `test_oracles_and_recursive_assertions_have_no_product_dependencies` | 1 |
| `test_baseline_classification_keeps_other_failure_causes_visible` | 3 |
| `test_public_model_and_http_import_the_same_classes` | 1 |
| `test_baseline_gate_rejects_collection_and_outcome_changes` | 6 |
| `test_empty_baseline_requires_every_sample_to_pass` | 2 |

### integration/test_axial_subdivision.py

軸力部材の生成点・荷重方向・補間出力と構造節点の機構の区別。

| 検証関数 | 展開ケース |
|---|---:|
| `test_axial_solution_is_invariant_under_subdivision_and_rotation` | 6 |
| `test_axial_subdivision_does_not_hide_a_transverse_point_load` | 1 |
| `test_original_axial_endpoint_mechanism_is_still_rejected` | 1 |
| `test_transverse_interpolation_is_explicit_and_does_not_create_reaction` | 1 |

### integration/test_beam_foundation.py

integration / beam foundation contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_distributed_axial_foundation_exact_solution` | 1 |
| `test_foundation_constant_patch_and_linear_nonlinear_route` | 1 |

### integration/test_beam_precision.py

integration / beam precision contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_extremely_short_segment_preserves_cantilever_solution` | 2 |
| `test_precise_frame_keeps_a_weak_support_spring_and_balanced_member_load` | 1 |
| `test_high_precision_does_not_regularize_an_unrestrained_rigid_rotation` | 1 |
| `test_stiff_beam_on_weak_foundation_preserves_rigid_translation` | 2 |
| `test_precise_snapshots_are_cleared_before_reusing_model` | 1 |
| `test_rigid_end_uses_the_coalesced_notice_boundary` | 2 |
| `test_precise_tiny_axial_load_survives_thermal_force_postprocessing` | 1 |
| `test_short_oblique_child_keeps_the_original_member_axis` | 1 |
| `test_public_axial_force_survives_prescribed_rigid_translation` | 3 |
| `test_short_segment_sample_reaches_constitutive_equilibrium` | 2 |

### integration/test_beam_solutions.py

integration / beam solutions contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_cantilever_tip_load_matches_hand_solution_and_mesh_refinement` | 8 |
| `test_fem_model_publishes_section_forces_with_nonconsecutive_nodes` | 2 |
| `test_nonlinear_pure_bending_solution_and_output_under_refinement` | 4 |

### integration/test_curvature_output.py

非線形要素の応答曲率出力。局所軸・単位・状態非変更の単体保証は `elements/beam/test_nonlinear_section.py` が所有し、このファイルは解析履歴と公開出力の対応を所有する。

| 検証関数 | 展開ケース |
|---|---:|
| `test_curvature_output_tracks_total_cyclic_response` | 24 |
| `test_curvature_output_survives_result_save_and_section_cut_view` | 1 |
| `test_curvature_output_does_not_report_strain_or_twist_as_bending` | 2 |
| `test_static_output_does_not_publish_nonlinear_curvature` | 1 |
| `test_curvature_includes_elastic_bending_axis_of_nonlinear_element` | 1 |
| `test_nonlinear_analysis_with_only_elastic_elements_has_empty_curvature` | 1 |
| `test_cantilever_curvature_output_matches_independent_all_step_reference` | 1 |

### integration/test_input_routes.py

integration / input routes contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_python_json_file_http_equivalence` | 1 |
| `test_omitted_nu_has_same_nonlinear_default_in_python_json_and_http` | 1 |
| `test_omitted_g_file_http_and_saved_model_agree` | 1 |

### integration/test_jr_beam_history.py

integration / jr beam history contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_jr_beam_reversal_commit_rollback_and_saved_output` | 8 |
| `test_jr_beam_history_tangent_matches_all_force_columns` | 12 |
| `test_real_jr_failure_restores_last_commit_and_fresh_run` | 1 |

### integration/test_linear_beam.py

integration / linear beam contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_legacy_bernoulli_section_and_cg` | 1 |
| `test_generated_point_coordinates_keep_submillimetre_precision` | 1 |
| `test_uniform_and_triangular_load_consistent_forces` | 3 |
| `test_released_tip_propped_beam` | 1 |
| `test_legacy_support_spring_is_not_prescribed_motion` | 3 |
| `test_rigid_zone_uses_its_assigned_section` | 1 |
| `test_point_load_and_negative_width_split` | 1 |
| `test_public_distributed_load_and_joint_apis` | 1 |
| `test_timoshenko_uniform_load_includes_shear_and_fixed_end_forces` | 1 |
| `test_fully_released_unloaded_rotation_is_absent_not_stabilized` | 1 |
| `test_split_roundoff_and_other_case_subdivisions_keep_loads_separate` | 1 |
| `test_temperature_load_free_expansion_and_restrained_force` | 2 |

### integration/test_load_transfer.py

剛性ゼロの荷重配分専用部材の合力・一次モーメント・座標変換と構造の機構拒否。

| 検証関数 | 展開ケース |
|---|---:|
| `test_transfer_conserves_force_and_first_moment` | 20 |
| `test_transfer_rotates_local_load_and_preserves_point_couple` | 1 |
| `test_transfer_does_not_support_an_unrestrained_structure` | 1 |

### integration/test_model_contracts.py

integration / model contracts contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_public_legacy_ids_are_insertion_indices_and_modern_ids_are_retained` | 1 |
| `test_public_shell_output_with_noncontiguous_ids` | 2 |
| `test_nonconsecutive_node_ids_preserve_reactions_and_displacements` | 2 |

### integration/test_nonlinear_convergence.py

integration / nonlinear convergence contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_load_increment_and_tolerance_sensitivity` | 3 |
| `test_nonuniform_bending_discrete_compliance_and_continuum_limit` | 5 |

### integration/test_nonlinear_reference.py

integration / nonlinear reference contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_uniform_modes_independent_inverse_skeleton` | 96 |
| `test_cyclic_polygon_residual_deformation_and_work` | 96 |
| `test_public_spring_api_participates_in_equilibrium` | 3 |
| `test_nested_reversal_and_damage_independent_polygon` | 192 |
| `test_elastic_cantilever_force_moment_balance` | 6 |
| `test_explicit_G_elastic_torsion` | 3 |
| `test_two_members_with_intermediate_load_have_independent_equilibrium` | 3 |
| `test_rotated_member_global_displacements_and_equilibrium` | 24 |

### integration/test_plate_bending.py

integration / plate bending contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_dkt_cantilever_constant_moment` | 4 |
| `test_quad_cantilever_constant_moment_full_solver` | 8 |

### integration/test_pressure_solution.py

integration / pressure solution contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_pressure_fix_is_identical_in_json_and_original_format_and_balances_load` | 1 |
| `test_releasing_pressure_fixture_rotations_creates_a_loaded_rigid_mode` | 1 |

### integration/test_solids.py

integration / solids contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_quadratic_public_prescribed_affine_solution` | 3 |
| `test_quadratic_reactions_with_large_rigid_displacement` | 9 |

### io/test_http.py

io / http contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_http_explicit_analysis_type` | 3 |
| `test_http_invalid_input_is_not_success` | 7 |
| `test_http_failed_analysis_and_reanalysis` | 4 |
| `test_invalid_load_factors_rejected` | 4 |
| `test_failed_postprocessing_clears_result_and_http_is_error` | 2 |
| `test_explicit_static_spring_reactions_balance` | 1 |

### io/test_model_lifecycle.py

io / model lifecycle contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_failed_file_reload_does_not_leave_previous_success` | 1 |
| `test_failed_fem_run_clears_previous_results` | 1 |
| `test_programmatic_fem_model_has_analysis_defaults` | 1 |
| `test_missing_topology_has_an_input_error_before_linear_algebra` | 3 |

### io/test_model_roundtrip.py

io / model roundtrip contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_model_save_load_preserves_nonlinear_input_and_result` | 1 |
| `test_saved_legacy_member_features_retain_the_same_physical_model` | 1 |

### io/test_pressure.py

io / pressure contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_pressure_class_creation` | 0 |
| `test_pressure_class_string_representation` | 0 |
| `test_boundary_condition_pressure_management` | 0 |
| `test_boundary_condition_clear` | 0 |
| `test_shell_with_pressure_load_json` | 0 |
| `test_shell_with_pressure_load_fem` | 0 |
| `test_multiple_pressure_loads` | 0 |
| `test_pressure_file_io_roundtrip` | 0 |

### io/test_source_input.py

io / original source input contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_source_shell_material_thickness_restraint_load_records` | 1 |
| `test_source_bad_input_rejected_with_line_number` | 11 |
| `test_original_hexa_input_preserves_all_records` | 1 |

### io/test_structural_input.py

io / structural input contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_json_does_not_add_unrequested_rotational_restraint` | 1 |
| `test_g_omission_and_explicit_shear_flag` | 12 |
| `test_selected_case_and_rigid_zone_use_their_own_g` | 1 |
| `test_nonbeam_elements_require_no_beam_section` | 4 |
| `test_disabled_member_load_ignores_stale_values_and_does_not_split` | 2 |
| `test_member_and_shell_number_namespaces_do_not_overwrite_each_other` | 1 |
| `test_explicit_shell_thickness_and_mindlin_override_are_preserved` | 1 |
| `test_missing_or_invalid_legacy_shell_thickness_is_rejected` | 4 |
| `test_shell_area_field_supplies_thickness` | 2 |

### materials/test_jr_hysteresis.py

materials / jr hysteresis contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_monotonic_skeleton_all_segments_and_outgoing_tangent` | 14 |
| `test_reversal_uses_latest_committed_point_and_is_continuous` | 2 |
| `test_zero_force_crossing_at_same_sign_displacement_and_target_continuity` | 2 |
| `test_yielded_source_aims_at_opposite_second_breakpoint` | 2 |
| `test_zero_beyond_nominal_target_uses_forward_skeleton_intersection` | 1 |
| `test_asymmetric_loading_both_directions` | 1 |
| `test_nested_inner_loops_restore_return_points_and_suspended_outer_path` | 2 |
| `test_reversal_before_force_zero_retraces_unloading_line` | 2 |
| `test_region_specific_reduced_stiffness_and_limits` | 20 |
| `test_trial_reproducibility_input_immutability_and_hold` | 4 |
| `test_force_finite_difference_matches_tangent_on_smooth_branches` | 9 |
| `test_transition_continuity_and_one_sided_tangent` | 7 |
| `test_subdividing_same_path_preserves_response_and_future_history` | 9 |
| `test_nonfinite_parameters_are_explicitly_rejected` | 42 |
| `test_invalid_parameter_order_and_incompatible_stiffness_rejected` | 9 |
| `test_nonfinite_trial_deformation_is_rejected` | 3 |
| `test_zero_plateau_is_not_replaced_by_artificial_stiffness` | 1 |
| `test_material_api_validates_before_division_or_analysis` | 10 |
| `test_symmetric_constructor_rejects_zero_before_default_stiffness_division` | 1 |
| `test_reversing_at_exact_zero_retraces_and_restores_zero_crossing_path` | 2 |
| `test_internal_unloading_retrace_does_not_leave_a_stale_reversal` | 2 |
| `test_small_nonzero_increment_is_not_treated_as_hold` | 1 |
| `test_asymmetric_elastic_reversal_and_origin_one_sided_tangent` | 1 |
| `test_irregular_cyclic_path_refinement_with_asymmetry` | 2 |
| `test_maximum_deformation_api_includes_off_skeleton_experience` | 1 |
| `test_reduced_stiffness_inside_loop_keeps_outer_damage_and_returns_exactly` | 2 |

### postprocess/test_beam_equilibrium.py

postprocess / beam equilibrium contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_free_end_recovery_preserves_applied_moments_and_force_balance` | 6 |
| `test_recovery_does_not_hide_a_constitutive_or_solver_error` | 1 |
| `test_supported_or_foundation_member_is_not_statically_recovered` | 2 |
| `test_recovery_with_consistent_distributed_load_and_reversed_loading` | 2 |

### postprocess/test_shell_results.py

postprocess / shell results contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_dkt_transverse_resultants_follow_moment_equilibrium` | 1 |
| `test_shell_rotated_membrane_patch` | 4 |
| `test_legacy_engineering_shear_energy_density_and_separate_surfaces` | 1 |
| `test_legacy_view_uses_point_mean_not_area_weighted_mean` | 1 |
| `test_surface_tensors` | 16 |
| `test_shell_integrated_energy_and_resultants` | 1 |
| `test_drilling_energy_is_separate_from_physical_energy` | 1 |
| `test_shell_output_rejects_incomplete_or_nonfinite_displacement` | 3 |
| `test_edge_tractions_from_uniform_membrane_stress` | 4 |
| `test_edge_bending_couple_has_physical_rotation_sign` | 1 |

### postprocess/test_solid_stress.py

postprocess / solid stress contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_solid_affine_stress_patch` | 3 |

### regression/test_cantilever_history.py

regression / cantilever history contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_cantilever_all_stored_steps_keep_strict_zero_moment_reference` | 1 |
| `test_public_cantilever_keeps_a_real_sub_tolerance_end_moment` | 1 |
| `test_cantilever_independent_flexibility_reference` | 2 |
| `test_all_stored_history_steps` | 1 |

### regression/test_structural_samples.py

Every registered structural sample load/reference case is independently visible.

| 検証関数 | 展開ケース |
|---|---:|
| `test_saved_sample_case` | 323 |

### solvers/test_boundary_conditions.py

solvers / boundary conditions contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_only_last_node_is_fixed_in_four_node_frame` | 1 |
| `test_prescribed_displacement_corrects_free_rhs_and_preserves_symmetry` | 1 |
| `test_solid_boundary_uses_explicit_three_dof_stride` | 1 |
| `test_spring_support_is_not_a_fixed_displacement` | 1 |
| `test_reaction_uses_actual_node_stride` | 1 |
| `test_newton_bc_enforces_remaining_prescribed_motion_and_spring_force` | 1 |

### solvers/test_linear.py

solvers / linear contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_sparse_product_retains_real_sub_ulp_force_and_python311_fallback` | 1 |
| `test_linear_solver_scales_without_artificial_stiffness` | 1 |
| `test_linear_solver_rejects_rigid_mode` | 3 |

### solvers/test_nonlinear.py

solvers / nonlinear contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_linear_bar_through_newton_matches_hand_calculation` | 1 |
| `test_prescribed_motion_without_external_force_is_applied_once_per_step` | 1 |
| `test_spring_force_participates_in_residual_and_reactions` | 1 |
| `test_unconverged_step_rolls_back_and_stops_before_callback` | 1 |
| `test_unrestrained_model_is_rejected_not_regularized` | 1 |
| `test_fixed_node_load_does_not_hide_free_dof_imbalance` | 1 |
| `test_cubic_spring_converges_to_known_nonlinear_root` | 1 |
| `test_failed_second_step_restores_first_converged_state` | 1 |
| `test_invalid_controls_rejected_before_analysis` | 7 |

### solvers/test_unification.py

共通静解析フロー・独立解・互換境界の契約。

| 検証関数 | 展開ケース |
|---|---:|
| `test_common_entry_obeys_axial_hand_solution` | 2 |
| `test_static_does_not_enter_newton` | 1 |
| `test_one_step_nonlinear_still_iterates_to_independent_cubic_root` | 1 |
| `test_failed_step_restores_load_factor_force_and_allows_reuse` | 1 |
| `test_model_uses_common_solver_and_final_snapshots_are_independent` | 1 |
| `test_legacy_solve_dispatches_to_nonlinear_and_exposes_shared_state` | 1 |
| `test_newton_linear_algebra_does_not_replace_accepted_displacement` | 1 |
| `test_internal_six_key_format_and_legacy_three_key_projection` | 1 |
| `test_assembly_layout_does_not_depend_on_load_assembly` | 1 |
| `test_modal_still_solves_generalized_eigenproblem` | 1 |
| `test_same_material_elements_restart_history_after_failure_and_type_switch` | 1 |
| `test_snapshot_reads_once_and_callback_mutation_cannot_change_results` | 1 |
| `test_shared_spring_and_prescribed_displacement_balance` | 4 |
| `test_legacy_callback_error_leaves_accepted_step_and_stops` | 1 |
| `test_invalid_reanalysis_does_not_expose_previous_solver_state` | 1 |
| `test_mixed_element_widths_share_layout_after_three_dof_analysis` | 2 |

### validation/test_plane_frame_series.py

独立したスカラー級数による平面梁の解析解・荷重・地盤ばね・剛域と、188保存ケースの全必須量。

| 検証関数 | 展開ケース |
|---|---:|
| `test_series_cantilever_tip_force` | 1 |
| `test_series_uniform_foundation_rigid_translation` | 1 |
| `test_series_free_thermal_expansion` | 1 |
| `test_series_interior_point_force` | 1 |
| `test_series_endpoint_member_force_matches_nodal_force` | 2 |
| `test_series_piecewise_rigidity_matches_compliance_integral` | 2 |
| `test_series_axial_only_member_preserves_axial_solution` | 1 |
| `test_saved_plane_frame_reference_matches_independent_series` | 188 |

### validation/test_provenance.py

validation / provenance contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_complete_external_solid_input_and_repaired_displacement` | 5 |
| `test_missing_json_connectivity_is_proved_against_original_fem` | 4 |
| `test_tetra_printed_reference_is_incomplete_and_not_same_model_proof` | 1 |
| `test_source_audit_detects_input_change_instead_of_trusting_same_file_name` | 1 |
| `test_stale_notice_record_does_not_stop_the_numerical_audit` | 1 |
| `test_selected_material_case_and_rounded_geometry_are_reported` | 1 |

### validation/test_shell_variational.py

独立した変分式による三角形／四角形シェルの膜・曲げ・せん断・回転整合と、8保存モデル。

| 検証関数 | 展開ケース |
|---|---:|
| `test_independent_shell_six_rigid_modes` | 2 |
| `test_independent_shell_constant_membrane_energy` | 2 |
| `test_independent_shell_constant_curvature_and_surface_signs` | 2 |
| `test_independent_shell_drilling_energy_is_separate` | 2 |
| `test_saved_shell_reference_matches_independent_variational_solution` | 8 |

### validation/test_solid_sources.py

validation / solid sources contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_quadratic_solid_all_outputs_against_independent_decimal_source` | 3 |
| `test_tetrahedron_all_outputs_against_source_input_solution` | 1 |
| `test_solid_displacements_and_global_equilibrium` | 5 |

### validation/test_source_solid_reference.py

validation / source solid reference contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_original_shape_stiffness_has_exact_affine_work_and_rigid_modes` | 3 |
| `test_decimal_reference_inverse_on_skew_jacobian` | 1 |
| `test_source_input_only_tetra_solves_hand_axial_patch` | 1 |
| `test_source_input_rejects_unsupported_dangling_and_duplicate_records` | 3 |
| `test_original_source_tetra_uniform_axial_stress_supports` | 1 |
| `test_original_source_rejects_partial_output_without_echo` | 1 |
| `test_refined_source_solves_load_not_inaccurate_output_displacement` | 6 |

### validation/test_space_frame_series.py

立体梁の独立式、荷重方向・ねじり・支持不足・剛域、および梁／シェル混在を含む114保存ケース。

| 検証関数 | 展開ケース |
|---|---:|
| `test_spatial_series_cantilever_compliance` | 6 |
| `test_spatial_series_interior_bending_couple` | 1 |
| `test_spatial_series_uniform_temperature` | 1 |
| `test_spatial_reference_rejects_an_unrestrained_global_rotation` | 1 |
| `test_spatial_load_transfer_uses_endpoint_statics` | 1 |
| `test_spatial_uniform_foundation_rigid_translation` | 1 |
| `test_spatial_released_uniform_beam_matches_simple_support_solution` | 1 |
| `test_spatial_long_foundation_preserves_uniform_translation` | 1 |
| `test_spatial_couple_at_material_interface_matches_compliance_integral` | 1 |
| `test_saved_spatial_reference_matches_independent_series` | 114 |

### validation/test_stored_references.py

validation / stored references contracts.

| 検証関数 | 展開ケース |
|---|---:|
| `test_cantilever_reference_inverse_at_hand_calculated_ordinates` | 5 |
| `test_stored_cantilever_all_steps_against_independent_scalar_reference` | 1 |
| `test_cantilever_hand_calculated_branch_crossings_and_tip_motion` | 1 |
| `test_pressure_saved_reference_is_independent_exact_polynomial_solution` | 1 |
| `test_tri1_conditions_match_the_original_fem` | 1 |
| `test_all_tri1_outputs_match_original_operators_without_production_imports` | 1 |

### validation/test_triangle_sample_references.py

元のDKT演算子と独立した回転整合エネルギー、4保存モデル、原資料との入力同一性と改変拒否。

| 検証関数 | 展開ケース |
|---|---:|
| `test_saved_triangle_sample_uses_independent_source_reference` | 4 |
| `test_triangle_source_identity_rejects_changed_input` | 5 |
| `test_independent_spin_operator_has_six_physical_rigid_modes` | 1 |
| `test_independent_spin_operator_constant_membrane_energy` | 1 |
| `test_independent_spin_operator_relative_rotation_energy` | 1 |

## 2026-09-10までの品質ロードマップ追加分

### harness/test_release_workflows.py

同一artifactを検証から公開へ渡すCI・provenance契約。

| 検証関数 | 展開ケース |
|---|---:|
| `test_verification_builds_once_and_gates_every_required_check` | 1 |
| `test_publish_uses_only_the_successful_reusable_workflow_artifact` | 1 |
| `test_distribution_manifest_rejects_changed_bytes_or_commit` | 1 |

### io/test_diagnostics.py

診断コード、結果メタデータ、入力hash、ログ、保存往復。

| 検証関数 | 展開ケース |
|---|---:|
| `test_static_result_metadata_and_model_result_roundtrip` | 1 |
| `test_input_hash_changes_with_analysis_input` | 1 |
| `test_static_metadata_residual_includes_spring_equilibrium` | 1 |
| `test_model_metadata_cannot_override_provenance_fields` | 1 |
| `test_modal_metadata_uses_eigenpair_residual` | 1 |
| `test_high_precision_path_is_declared_in_metadata` | 1 |
| `test_nonlinear_metadata_summarizes_accepted_history` | 1 |
| `test_unknown_analysis_has_same_python_and_http_code` | 1 |
| `test_mechanism_has_same_python_and_http_code_and_dof_details` | 1 |
| `test_numerical_rank_failure_is_distinct_from_input_and_mechanism` | 1 |
| `test_nonconvergence_exposes_stable_python_details` | 1 |

### io/test_package_metadata.py

配布・実行時versionの単一源とHTTP旧名互換。

| 検証関数 | 展開ケース |
|---|---:|
| `test_distribution_and_runtime_versions_have_one_source` | 1 |
| `test_legacy_http_function_name_is_a_compatibility_alias` | 1 |

### io/test_vtk.py

Legacy VTKのセル型、ID、節点順、結果整列と明示的失敗。

| 検証関数 | 展開ケース |
|---|---:|
| `test_mixed_topology_ids_and_quadratic_node_order_roundtrip` | 1 |
| `test_result_quantities_remain_distinct_and_cell_aligned` | 1 |
| `test_public_shell_analysis_results_roundtrip` | 1 |
| `test_missing_generic_cell_component_is_nan_not_zero` | 1 |
| `test_unsupported_or_malformed_cell_fails_explicitly` | 3 |

### postprocess/test_result_processor.py

3／6DOF、混在、非連続IDと変位長不整合の公開後処理。

| 検証関数 | 展開ケース |
|---|---:|
| `test_three_dof_displacements_follow_sorted_noncontiguous_node_layout` | 1 |
| `test_six_dof_displacements_preserve_existing_schema` | 1 |
| `test_mixed_three_and_six_dof_elements_use_one_six_dof_layout` | 1 |
| `test_displacement_length_mismatch_is_not_hidden_by_zero_fill` | 1 |
| `test_stress_extraction_uses_full_mesh_including_unconnected_nodes` | 2 |
| `test_stress_extraction_for_mixed_element_widths_uses_global_six_dof_stride` | 1 |
| `test_two_argument_stress_api_preserves_legacy_six_dof_node_numbering` | 1 |
| `test_stress_extraction_rejects_truncated_mesh_displacements` | 1 |
| `test_beam_end_force_api_is_not_hidden_by_unimplemented_base_stress_api` | 1 |

### solvers/test_support_equilibrium.py

2026-09-12追加。支持ばねと制御方式の共通釣合い契約。
[実施記録](../report/support-equilibrium-refactoring.md)にTDDの失敗・成功結果を記載する。

| 検証関数 | 展開ケース |
|---|---:|
| `test_load_and_displacement_control_balance_translation_and_rotation_springs` | 4 |
| `test_displacement_control_keeps_spring_balance_past_the_load_peak` | 1 |
| `test_normalized_boundary_keeps_sparse_ids_legacy_springs_and_free_rotations` | 1 |
| `test_support_tangent_is_the_negative_derivative_of_equilibrium_residual` | 1 |
| `test_constraint_elimination_uses_already_assembled_support_equilibrium_once` | 1 |
| `test_compensated_support_residual_retains_sub_float_displacement` | 1 |
| `test_equilibrium_measure_excludes_fixed_loads_and_scales_rotational_springs` | 2 |

### regression/test_displacement_control_history.py

JR K4負勾配の保存変位制御履歴と参照改変拒否。

| 検証関数 | 展開ケース |
|---|---:|
| `test_jr_k4_fixture_stores_every_requested_displacement` | 1 |
| `test_all_stored_displacement_control_steps` | 1 |
| `test_jr_k4_history_comparison_rejects_old_all_zero_curvature_fixture` | 1 |
| `test_jr_k4_history_comparison_rejects_changed_softening_values` | 4 |

### solvers/test_capabilities.py

機械可読な機能対応表、alias、未対応組合せの解析前拒否、生成文書。

| 検証関数 | 展開ケース |
|---|---:|
| `test_registry_is_complete_json_data_and_returned_as_a_copy` | 1 |
| `test_public_and_legacy_names_resolve_from_registry` | 4 |
| `test_public_wedge_modal_is_rejected_with_ids_before_solver` | 1 |
| `test_legacy_wedge_name_uses_actual_public_path_in_rejection` | 1 |
| `test_saved_json_public_input_rejects_unsupported_wedge_modal` | 1 |
| `test_stub_element_paths_are_rejected_before_matrix_calls` | 3 |
| `test_solid_pressure_is_rejected_before_load_assembly` | 1 |
| `test_registry_does_not_accept_the_other_same_named_wedge_implementation` | 1 |
| `test_readme_and_wiki_tables_are_generated_from_registry` | 1 |

### solvers/test_modal_analysis.py

固有値の自由DOF縮約、要求数、ゼロ・負値、再試行、残差・直交性と実要素参照。

| 検証関数 | 展開ケース |
|---|---:|
| `test_public_modal_wrapper_uses_requested_count_and_restores_configuration` | 1 |
| `test_v0_modal_element_construction_uses_controllable_logging` | 1 |
| `test_modal_mode_count_must_be_a_positive_integer` | 4 |
| `test_modal_reduces_stiffness_and_mass_to_the_same_free_dofs` | 1 |
| `test_modal_does_not_silently_reduce_requested_mode_count` | 1 |
| `test_zero_mode_has_infinite_python_period_and_json_safe_null` | 1 |
| `test_significant_negative_eigenvalue_is_reported_as_instability` | 1 |
| `test_modal_retry_keeps_low_mode_selection` | 1 |
| `test_modal_reports_residual_and_mass_orthogonality_checks` | 1 |
| `test_wiki_spring_supported_beam_modal_example_has_closed_form_frequency` | 1 |
| `test_public_modal_path_matches_independent_one_dof_element_roots` | 3 |
| `test_large_public_shell_model_retry_returns_same_low_modes` | 1 |

### validation/test_mesh_convergence.py

閉形式・製造解に対する梁、DKT、MITC4、Hexa8の収束率と適用限界。

| 検証関数 | 展開ケース |
|---|---:|
| `test_closed_form_references_and_expected_asymptotic_rates` | 1 |
| `test_distortion_and_aspect_sweeps_remain_within_measured_scope` | 1 |
| `test_mitc4_thickness_sweep_has_no_shear_locking_in_strip_problem` | 1 |
| `test_hexa8_nearly_incompressible_stress_and_energy_are_an_explicit_limit` | 1 |

### validation/test_performance.py

小規模smoke model、位相・メモリschema、同一環境回帰判定、保存済み13 workload baseline。

| 検証関数 | 展開ケース |
|---|---:|
| `test_smoke_profile_covers_phases_and_workload_dimensions` | 1 |
| `test_regression_gate_rejects_environment_workload_and_limit_changes` | 1 |
| `test_committed_baseline_covers_all_pq12_dimensions` | 1 |

### validation/test_unit_invariance.py

N–m、N–mm、kN–mの一般化ノルム、非線形応答・判定、ばね、強制変位、固有値。

| 検証関数 | 展開ケース |
|---|---:|
| `test_generalized_norms_follow_force_and_length_scaling` | 1 |
| `test_load_control_mixed_translation_rotation_has_same_decision_and_response` | 1 |
| `test_cyclic_spring_and_prescribed_motion_are_unit_invariant` | 1 |
| `test_displacement_control_decision_is_unit_invariant` | 1 |
| `test_density_and_translational_rotational_springs_preserve_frequency` | 1 |
