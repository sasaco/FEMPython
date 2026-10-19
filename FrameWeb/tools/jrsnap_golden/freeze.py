"""Freeze one JRSNAP run as a golden JSON for FrameWeb tests.

Inputs are the JRSNAP.Headless run output directory (with its envelope JSON)
and the raw dump written by extract_db1.ps1. Values are copied unchanged;
only the layout and provenance are added.

Schema ``jrsnap_golden_v2`` (2026-10-19) adds the analysis control, the
basic data JRSNAP stored (``kihon``), the member data (``member_info``), the
member state items (``SCFlg``, ``Outou_FAI_Kaiseki``, ...) and the hashes of
every input stage. The v1 goldens frozen on 2026-10-18 are left as they are.

    uv run --locked --extra dev python -m tools.jrsnap_golden.freeze \
        --case support_trilinear01 --run-dir RUN --envelope RUN.envelope.json \
        --raw RAW.json --ndu INPUT.ndu --steps 100 --control load --output golden.json \
        [--writer-ndt WRITER.ndt --post-processing convergence=accuracy]
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SNAPDBLIB = ROOT.parents[1] / 'Batch/Aggre-SE/Headless/bin/Release/SNAPDBLib.dll'
SCHEMA = 'jrsnap_golden_v2'


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def parse_post_processing(items):
    """``key=value`` pairs describing how the analyzed .ndt differs from the writer's."""
    result = {}
    for item in items or []:
        key, sep, value = item.partition('=')
        if not sep or not key:
            raise SystemExit(f'--post-processing expects key=value, got {item!r}')
        result[key] = value
    return result


def freeze(case, run_dir, envelope, raw, ndu, steps, control, writer_ndt=None, post_processing=None, ndt=None):
    if not envelope['ok']:
        raise SystemExit('JRSNAP run did not succeed')
    ndt = Path(ndt) if ndt is not None else next(Path(run_dir).glob('*.ndt'))
    if len(raw['steps']) != steps + 1:
        raise SystemExit(f"expected {steps + 1} stored steps, got {len(raw['steps'])}")
    if control not in ('load', 'displacement'):
        raise SystemExit('--control must be load or displacement')

    records = []
    for step in raw['steps']:
        record = {'step': step['index']}
        # JRSNAP's LoStepNo is an integer counter; under displacement increments the fraction of
        # the target is what the step means, and the load factor must be read from the forces.
        if control == 'load':
            record['load_factor'] = step['index'] / steps
        else:
            record['control_fraction'] = step['index'] / steps
        record['lo_step_no'] = step['lo_step_no']
        record['joints'] = {str(j.pop('joint')): j for j in step['joints']}
        record['elements'] = {str(m.pop('element')): m for m in step['members']}
        records.append(record)
    inputs = {
        'ndu': {'file': Path(ndu).name, 'sha256': sha256(ndu)},
        'ndt': {'file': ndt.name, 'sha256': sha256(ndt)},
    }
    if writer_ndt is not None:
        inputs['writer_ndt'] = {'file': Path(writer_ndt).name, 'sha256': sha256(writer_ndt)}
    provenance = {
        'jrsnap': envelope['summary']['jrsnap'],
        'runner': {'engine': envelope['engine'], 'engineVersion': envelope['engineVersion'],
                   'input': envelope['summary'].get('input', 'ndu')},
        'engine_messages': envelope['messages'],
        'extractor': {'library': 'SNAPDBLib.dll', 'sha256': sha256(SNAPDBLIB)},
        'inputs': inputs,
    }
    if post_processing:
        provenance['post_processing'] = post_processing
    return {
        'schema': SCHEMA,
        'case': case,
        'provenance': provenance,
        'units': {'length': 'm', 'rotation': 'rad', 'force': 'kN', 'moment': 'kN*m'},
        'control': {'mode': control, 'steps': steps},
        'load_steps': steps,
        'kihon': raw.get('kihon'),
        'lengths': raw.get('lengths'),
        'stored_supports': raw['supports'],
        'member_info': raw.get('member_info'),
        'steps': records,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--case', required=True)
    parser.add_argument('--run-dir', type=Path, required=True)
    parser.add_argument('--envelope', type=Path, required=True)
    parser.add_argument('--raw', type=Path, required=True)
    parser.add_argument('--ndu', type=Path, required=True)
    parser.add_argument('--steps', '--load-steps', dest='steps', type=int, required=True)
    parser.add_argument('--control', choices=('load', 'displacement'), required=True)
    parser.add_argument('--writer-ndt', type=Path, help='the .ndt the writer wrote, when the analyzed one was edited')
    parser.add_argument('--post-processing', nargs='*', help='key=value pairs, e.g. convergence=accuracy column70=1')
    parser.add_argument('--ndt', type=Path, help='the analyzed .ndt (default: the one .ndt in --run-dir)')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()

    envelope = json.loads(args.envelope.read_text(encoding='utf-8-sig'))
    raw = json.loads(args.raw.read_text(encoding='utf-8-sig'))
    golden = freeze(args.case, args.run_dir, envelope, raw, args.ndu, args.steps, args.control,
                    args.writer_ndt, parse_post_processing(args.post_processing), args.ndt)
    args.output.write_text(json.dumps(golden, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
