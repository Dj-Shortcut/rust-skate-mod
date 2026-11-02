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
from capture_pipeline import (BASE_SOURCE_KINDS, ID, RESERVED, SHARED_SOURCE_VARIANTS,
                              atomic, digest, json_bytes, read_json)


PACK = 'skate_captures.skmc'
APPROVAL = 'skate_captures.approved.txt'
COMPOSITION_LIMIT = ('Shared filmed body/flick/contact data, not separately filmed multi-flip mocap. '
                     'Live game board rotation, landing and scoring remain authoritative. '
                     'Each variant needs its own visual review and exact owner-approved pack entry.')


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


def composition_clips(report, batch_dir):
    keys = {'format', 'manifestSha256', 'codeSha256', 'sources', 'variants',
            'includedBaseClips', 'clipCount', 'packUpdated', 'packSha256', 'limits'}
    def sha(value):
        return isinstance(value, str) and len(value) == 64 and all(c in '0123456789abcdef' for c in value)
    if (not isinstance(report, dict) or set(report) != keys
            or report['format'] != 'skate-capture-composition/1'
            or report['packUpdated'] is not True or type(report['includedBaseClips']) is not bool
            or type(report['clipCount']) is not int
            or not all(sha(report[k]) for k in ('manifestSha256', 'codeSha256', 'packSha256'))
            or not isinstance(report['limits'], str) or not report['limits'].strip()
            or not isinstance(report['sources'], list) or not 1 <= len(report['sources']) <= len(BASE_SOURCE_KINDS)
            or not isinstance(report['variants'], list) or not 1 <= len(report['variants']) <= len(SHARED_SOURCE_VARIANTS)):
        raise ValueError('Approval requires a complete composition/1 report')
    sources, source_ids, evidence = {}, set(), []
    for entry in report['sources']:
        if (not isinstance(entry, dict) or set(entry) != {'family', 'path', 'sha256', 'id'}
                or not isinstance(entry['family'], str) or entry['family'] not in BASE_SOURCE_KINDS
                or entry['family'] in sources or not isinstance(entry['id'], str)
                or not ID.fullmatch(entry['id']) or entry['id'] in RESERVED or entry['id'] in source_ids):
            raise ValueError('Invalid or duplicate composition source')
        verified = local_evidence(dict(path=entry['path'], sha256=entry['sha256']), batch_dir)
        source = read_json(Path(verified['path']))
        if (digest(Path(verified['path'])) != entry['sha256']
                or source['metadata']['id'] != entry['id'] or source['metadata']['movementKind'] != entry['family']):
            raise ValueError('Composition source changed or differs from its declared family/id')
        encode_pack([source])
        sources[entry['family']] = source
        source_ids.add(entry['id'])
        evidence.append(dict(family=entry['family'], id=entry['id'], **verified))
    expected = list(sources.values()) if report['includedBaseClips'] else []
    variants = set()
    for entry in report['variants']:
        if (not isinstance(entry, dict)
                or set(entry) != {'id', 'sourceFamily', 'derivedFromClipId', 'requestedFlipDegrees', 'reviewed'}
                or not isinstance(entry['id'], str) or entry['id'] not in SHARED_SOURCE_VARIANTS
                or entry['id'] in variants or entry['reviewed'] is not False
                or type(entry['requestedFlipDegrees']) is not int):
            raise ValueError('Invalid or duplicate composition variant')
        family, degrees = SHARED_SOURCE_VARIANTS[entry['id']]
        if (entry['sourceFamily'] != family or family not in sources
                or entry['requestedFlipDegrees'] != degrees
                or entry['derivedFromClipId'] != sources[family]['metadata']['id']):
            raise ValueError('Composition variant does not match its source recipe')
        clip = copy.deepcopy(sources[family])
        clip['metadata'].update(id=entry['id'], label=entry['id'] + ' [SHARED SOURCE; UNREVIEWED]',
            movementKind=entry['id'], reviewed=False, sourceFamily=family,
            derivedFromClipId=entry['derivedFromClipId'], requestedFlipDegrees=degrees,
            derivation='shared-source-presentation', compositionLimit=COMPOSITION_LIMIT)
        expected.append(clip)
        variants.add(entry['id'])
    if report['clipCount'] != len(expected):
        raise ValueError('Composition clip count differs from its sources and variants')
    records, expected_by_id = [], {}
    for clip in expected:
        stem = clip['metadata']['id']
        if stem in expected_by_id:
            raise ValueError('Composition source and variant ids overlap')
        expected_by_id[stem] = clip
        records.append(dict(id=stem, status='completed', clip='jobs/' + stem + '.json'))
    return records, expected_by_id, evidence


def approval_report(batch_dir):
    # A reused output directory may contain the previous producer's report.
    # Choose the one report bound to the current pack; never fall back after
    # validation fails or infer which equally-current provenance to trust.
    paths = [batch_dir / name for name in ('batch-report.json', 'compose-report.json') if (batch_dir / name).is_file()]
    if not paths:
        raise ValueError('Approval needs batch-report.json or compose-report.json')
    reports = [(path, read_json(path)) for path in paths]
    if len(reports) > 1:
        pack_hash = digest(batch_dir / PACK)
        reports = [(path, report) for path, report in reports if isinstance(report, dict) and report.get('packSha256') == pack_hash]
    if len(reports) != 1:
        raise ValueError('Approval requires one unambiguous report matching the current pack')
    return reports[0]


def approve(batch_dir, review_path, output):
    batch_dir = batch_dir.resolve(strict=True)
    review_path = review_path.resolve(strict=True)
    output = output.resolve()
    if output == batch_dir:
        raise ValueError('Stage into a separate directory; keep the original batch')
    report_path, report = approval_report(batch_dir)
    composed = report_path.name == 'compose-report.json'
    expected = None
    if composed:
        records, expected, sources = composition_clips(report, batch_dir)
        provenance = dict(originalCompositionSha256=digest(report_path), compositionSources=sources)
    else:
        records = report.get('jobs')
        provenance = dict(originalBatchSha256=digest(report_path))
    if not composed and (report.get('format') != 'skate-capture-batch/1' or report.get('failed') != 0
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
        before = digest(path) if composed else None
        clip = read_json(path)
        if composed and before != digest(path):
            raise ValueError('Composition clip changed while reading: ' + stem)
        if clip['metadata']['id'] != stem:
            raise ValueError('Batch clip id differs from its record')
        if composed and clip != expected[stem]:
            raise ValueError('Composed clip differs from its exact source derivation: ' + stem)
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
    receipt = dict(format='skate-capture-staging/1', **provenance,
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
