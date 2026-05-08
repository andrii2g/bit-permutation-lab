# Bit Permutation Lab

`bit-permutation-lab` is a .NET project for experimenting with fast reversible integer encoding pipelines.

It is not cryptography. The goal is to compare reversible combinations of:
- binary normalization
- mixing
- permutation
- chunking
- emission

## Current shape

- Library: full parameter model and codec pipeline
- CLI: `encode`, `decode`, `list`, and `benchmark`
- Tests: round-trip, validation, emitter, config, plugin-loading, and CLI coverage
- Benchmarks: code-driven profiles, config-driven runs, weighting, deterministic sampling, quick-mode reports, and BenchmarkDotNet mode

## Benchmarking

The benchmark flow supports:
- built-in profiles: `quick`, `default`, `full`
- weighting profiles: `smoke`, `speed-first`, `balanced`, `exploratory`, `exhaustive`
- direct single-scenario benchmarking from CLI flags
- JSON config-driven runs
- Markdown and CSV export

Current benchmark output is split into:
- `Raw Performance Matrix`
- `Weighting Metadata`

## Docs

- [CLI examples](docs/CLI_EXAMPLES.md)
- [Run report template](docs/RUN_REPORT_TEMPLATE.md)
- [Formula notes](docs/FORMULA.md)
- [Parameter reference](docs/PARAMETERS.md)
- [Benchmarking notes](docs/BENCHMARKING.md)
- [Report format](docs/REPORT_FORMAT.md)

## Status

Most of the planned feature surface is implemented, including advanced direct CLI scenario flags, custom mutation plugin loading, config-driven benchmarks, quick-mode reports, BenchmarkDotNet mode, and weighting-based scenario selection.

The current weighting implementation covers:
- required baselines
- deterministic scenario selection by seed
- benchmark-profile defaults for weighting and budget
- user overrides for mixer, permutation, and emitter weights through config
- raw timing reports kept separate from weighting metadata
