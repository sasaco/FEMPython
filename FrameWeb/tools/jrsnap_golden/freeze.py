"""Freeze one JRSNAP run as a golden JSON for FrameWeb tests.

Inputs are the JRSNAP.Headless run output directory (with its envelope JSON)
and the raw dump written by extract_db1.ps1. Values are copied unchanged;
only the layout and provenance are added.

    uv run --locked --extra dev python -m tools.jrsnap_golden.freeze \
        --case support_trilinear01 --run-dir RUN --envelope RUN.envelope.json \
        --raw RAW.json --ndu INPUT.ndu --load-steps 100 --output golden.json
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SNAPDBLIB = ROOT.parents[1] / 'Batch/Aggre-SE/Headless/bin/Release/SNAPDBLib.dll'


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--case', required=True)
    parser.add_argument('--run-dir', type=Path, required=True)
    parser.add_argument('--envelope', type=Path, required=True)
    parser.add_argument('--raw', type=Path, required=True)
    parser.add_argument('--ndu', type=Path, required=True)
    parser.add_argument('--load-steps', type=int, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()

    envelope = json.loads(args.envelope.read_text(encoding='utf-8-sig'))
    if not envelope['ok']:
        raise SystemExit('JRSNAP run did not succeed')
    raw = json.loads(args.raw.read_text(encoding='utf-8-sig'))
    ndt = next(args.run_dir.glob('*.ndt'))
    if len(raw['steps']) != args.load_steps + 1:
        raise SystemExit(f"expected {args.load_steps + 1} stored steps, got {len(raw['steps'])}")

    steps = []
    for step in raw['steps']:
        steps.append({
            'step': step['index'],
            'load_factor': step['index'] / args.load_steps,
            'lo_step_no': step['lo_step_no'],
            'joints': {str(j.pop('joint')): j for j in step['joints']},
            'elements': {str(m.pop('element')): m for m in step['members']},
        })
    golden = {
        'schema': 'jrsnap_golden_v1',
        'case': args.case,
        'provenance': {
            'jrsnap': envelope['summary']['jrsnap'],
            'runner': {'engine': envelope['engine'], 'engineVersion': envelope['engineVersion']},
            'engine_messages': envelope['messages'],
            'extractor': {'library': 'SNAPDBLib.dll', 'sha256': sha256(SNAPDBLIB)},
            'inputs': {
                'ndu': {'file': args.ndu.name, 'sha256': sha256(args.ndu)},
                'ndt': {'file': ndt.name, 'sha256': sha256(ndt)},
            },
        },
        'units': {'length': 'm', 'rotation': 'rad', 'force': 'kN', 'moment': 'kN*m'},
        'load_steps': args.load_steps,
        'stored_supports': raw['supports'],
        'steps': steps,
    }
    args.output.write_text(json.dumps(golden, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
