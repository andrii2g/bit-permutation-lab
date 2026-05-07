# Report Format

Markdown reports start with:

- environment summary
- benchmark parameters
- top-result summary
- matrix results
- skipped-scenario section
- notes

## Raw Performance Matrix

The raw view groups results by scenario family and range, with timing-oriented columns such as:

- scenario identity
- range
- encode/decode/round-trip timings
- output length
- validity

## Weighting Metadata

The weighting view carries selection metadata separately from raw speed results, including:

- `ScenarioId`
- weighting profile context
- range classification
- required-baseline flag
- selection weight
- expected cost factor
- scenario shape fields

## CSV

CSV export contains row-oriented benchmark data suitable for spreadsheet or script analysis.

## Salt Reproducibility

Reports use the resolved numeric `SaltSeed` so scenarios remain reproducible even when the original source used `saltText`.
