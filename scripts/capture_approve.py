#!/usr/bin/env python3
"""Stage a privately reviewed capture pack for normal-riding presentation.

Approval is explicit authoring evidence, not a result of conversion or a native
gameplay acceptance claim. This command never edits the original batch.
"""
import argparse
import copy
import hashlib
from pathlib import Path
import sys

from capture_math import encode_pack
from capture_pipeline import ID, RESERVED, atomic, digest, json_bytes, read_json


PACK = 'skate_captures.skmc'
APPROVAL = 'skate_captures.approved.txt'


def local_evidence(entry, base):
    if not isinstance(entry, dict) or set(entry) != {'path', 'sha256'}:
        raise ValueError('Each review evidence entry requires path and sha256')
    value = entry['path']
    if not isinstance(value, str) or not value or '\x00' in value or '://' in value:
        raise ValueError('Review evidence must be a local file')
    path = Path(value)
    path = path if path.is_absolute() else base / path
    if not path.is_file() or digest(path) != entry['sha256']:
        raise ValueError('Review evidence is missing or changed: ' + value)
    return dict(path=str(path.resolve()), sha256=entry['sha256'])


def approve(batch_dir, review_path, output):
    batch_dir = batch_dir.resolve(strict=True)
    review_path = review_path.resolve(strict=True)
    output = output.resolve()
    if output == batch_dir:
        raise ValueError('Stage into a separate directory; keep the original batch')
    report_path = batch_dir / 'batch-report.json'
    report = read_json(report_path)
    records = report.get('jobs')
    if (report.get('format') != 'skate-capture-batch/1' or report.get('failed') != 0
            or report.get('partialPack') is not False or report.get('packUpdated') is not True
            or not isinstance(records, list) or not 1 <= len(records) <= 256
            or report.get('succeeded') != len(records)):
        raise ValueError('Approval requires a complete successful batch')
    clips, originals = [], {}
    for record in records:
        stem = record['id']
        if (not isinstance(stem, str) or not ID.fullmatch(stem) or stem in RESERVED
                or stem in originals or record.get('status') not in ('completed', 'cached')
                or record.get('clip') != 'jobs/' + stem + '.json'):
            raise ValueError('Invalid or duplicate batch clip record')
        path = batch_dir / record['clip']
        clip = read_json(path)
        if clip['metadata']['id'] != stem:
            raise ValueError('Batch clip id differs from its record')
        clips.append(clip)
        originals[stem] = dict(clipSha256=digest(path))
    original_bytes = encode_pack(clips)
    original_hash = hashlib.sha256(original_bytes).hexdigest()
    if original_hash != report.get('packSha256') or digest(batch_dir / PACK) != original_hash:
        raise ValueError('The complete batch pack and clip files no longer match')

    review = read_json(review_path)
    if (review.get('format') != 'skate-capture-review/1'
            or review.get('scope') != 'source-and-rig-animation'
            or not isinstance(review.get('reviewer'), str) or not review['reviewer'].strip()
            or not isinstance(review.get('clips'), list) or not 1 <= len(review['clips']) <= 256):
        raise ValueError('An explicit source-and-rig animation review is required')
    accepted = {}
    for entry in review['clips']:
        stem = entry['id']
        if (not isinstance(stem, str) or stem not in originals or stem in accepted
                or entry.get('verdict') != 'approved'
                or entry.get('clipSha256') != originals[stem]['clipSha256']
                or not isinstance(entry.get('notes'), str) or not entry['notes'].strip()
                or not isinstance(entry.get('evidence'), list) or not entry['evidence']):
            raise ValueError('Approval must name a unique unchanged clip and its review evidence')
        accepted[stem] = dict(entry, evidence=[local_evidence(e, review_path.parent) for e in entry['evidence']])

    staged = copy.deepcopy(clips)
    for clip in staged:
        # Never inherit another approval implicitly. The separate exact-id list
        # and pack digest control this particular staged revision.
        clip['metadata']['reviewed'] = clip['metadata']['id'] in accepted
    data = encode_pack(staged)
    pack_hash = hashlib.sha256(data).hexdigest()
    ids = [c['metadata']['id'] for c in staged if c['metadata']['id'] in accepted]
    approval = ('SKMCAP-APPROVAL-1\nsha256=' + pack_hash + '\n'
                + ''.join('clip=' + stem + '\n' for stem in ids)).encode('utf-8')
    if len(approval) > 32768:
        raise ValueError('Approval exceeds the runtime byte bound')
    receipt = dict(format='skate-capture-staging/1', originalBatchSha256=digest(report_path),
                   originalPackSha256=original_hash, reviewSha256=digest(review_path),
                   reviewer=review['reviewer'], scope=review['scope'], packSha256=pack_hash,
                   approvedIds=ids, review=[accepted[stem] for stem in ids],
                   files={PACK: pack_hash, APPROVAL: hashlib.sha256(approval).hexdigest()},
                   nativeGameplayAccepted=False,
                   limits='Reviewed source/rig animation; native Rust skin, execution and gameplay remain separate gates.')
    # Both files are individually atomic. A process seeing a mixed revision
    # rejects the digest mismatch instead of playing the wrong approved pack.
    atomic(output / PACK, data)
    atomic(output / APPROVAL, approval)
    atomic(output / 'capture-staging.json', json_bytes(receipt))
    return receipt


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('batch_dir', type=Path)
    parser.add_argument('--review', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args(argv)
    try:
        receipt = approve(args.batch_dir, args.review, args.output)
        print('Staged ' + str(len(receipt['approvedIds'])) + ' reviewed clips; native gameplay acceptance remains open.')
        return 0
    except (ValueError, KeyError, TypeError, OSError, OverflowError) as error:
        print('Capture approval refused: ' + str(error), file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
