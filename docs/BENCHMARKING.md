# Benchmarking

The project supports two benchmark modes.

## Quick Mode

Quick mode uses the in-process stopwatch runner.

Use it for:

- parameter sweeps
- fast local iteration
- weighting-profile validation
- Markdown/CSV report generation during exploration

## BenchmarkDotNet Mode

BenchmarkDotNet mode uses the dedicated benchmark project and `BenchmarkDotNet`.

Use it for:

- final comparisons
- allocation-aware measurements
- more realistic method-level timing

The current implementation converts the `BenchmarkDotNet` summary back into the project’s `BenchmarkRunResult` shape so Markdown and CSV exports remain available in both modes.

## Entry Paths

Benchmarks can be launched from:

- built-in profiles
- direct single-scenario CLI flags
- JSON config files

## Selection

The benchmark system supports:

- built-in benchmark profiles: `quick`, `default`, `full`
- weighting profiles: `smoke`, `speed-first`, `balanced`, `exploratory`, `exhaustive`
- deterministic sampling by `samplingSeed`
- optional scenario budgets
- required baselines that are always included unless explicitly disabled

## Reports

Current benchmark output supports:

- console matrix view
- weighting metadata view
- Markdown export
- CSV export

Invalid combinations are skipped in benchmark mode and reported with reasons.
