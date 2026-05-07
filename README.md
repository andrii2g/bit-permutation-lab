# Bit Permutation Lab

`bit-permutation-lab` is a small .NET project for experimenting with fast reversible integer encoding pipelines.

It is not cryptography. The goal is to compare reversible combinations of:
- binary normalization
- mixing
- permutation
- chunking
- emission

## Current shape

- Library: full parameter model and codec pipeline
- CLI: simplified `encode`, `decode`, `list`, and `benchmark`
- Tests: round-trip, validation, emitter, and simplified CLI coverage
- Benchmarks: code-driven benchmark profiles with weighting, deterministic sampling, and matrix-style reporting

## CLI examples

Encode:

```powershell
dotnet .\tools\A2G.BitPermutationLab.Cli\bin\Debug\net10.0\A2G.BitPermutationLab.Cli.dll encode `
  --value 12345 `
  --number-kind uint32 `
  --bits 32 `
  --salt 42 `
  --mix xor `
  --permute rotate `
  --chunk-size 4 `
  --emitter hex16 `
  --alphabet hex16 `
  --output-kind string
```

Decode:

```powershell
dotnet .\tools\A2G.BitPermutationLab.Cli\bin\Debug\net10.0\A2G.BitPermutationLab.Cli.dll decode `
  --value 7B0E8450 `
  --number-kind uint32 `
  --bits 32 `
  --salt 42 `
  --mix xor `
  --permute rotate `
  --chunk-size 4 `
  --emitter hex16 `
  --alphabet hex16 `
  --output-kind string
```

List supported simplified CLI values:

```powershell
dotnet .\tools\A2G.BitPermutationLab.Cli\bin\Debug\net10.0\A2G.BitPermutationLab.Cli.dll list
```

Run a benchmark profile:

```powershell
dotnet .\tools\A2G.BitPermutationLab.Cli\bin\Debug\net10.0\A2G.BitPermutationLab.Cli.dll benchmark --profile quick --iterations 1000
```

Run a weighted benchmark selection:

```powershell
dotnet .\tools\A2G.BitPermutationLab.Cli\bin\Debug\net10.0\A2G.BitPermutationLab.Cli.dll benchmark `
  --profile quick `
  --weighting-profile speed-first `
  --scenario-budget 4 `
  --sampling-seed 42 `
  --report-weighted true `
  --report-unweighted true `
  --iterations 1000
```

## Benchmarking approach

The CLI benchmark command is intentionally simple. For most real experimentation, define scenarios in code and use the library directly.

Built-in profiles:
- `quick`
- `default`
- `full`

Weighting profiles:
- `smoke`
- `speed-first`
- `balanced`
- `exploratory`
- `exhaustive`

The benchmark path supports:
- `--weighting-profile`
- `--scenario-budget`
- `--sampling-seed`
- `--include-required-baselines`
- `--report-weighted`
- `--report-unweighted`

Current benchmark output is split into two views:
- `Raw Performance Matrix`
  scenario-family rows with `Tiny` / `Small` / `Large` encode, decode, and round-trip timings
- `Weighting Metadata`
  one row per selected scenario-range with `ScenarioId`, value range bounds, weight, cost, baseline flag, and scenario shape fields

The separate benchmark project also runs code-driven profiles:

```powershell
dotnet run --project .\benchmarks\A2G.BitPermutationLab.Benchmarks -- default
```

It also accepts the same benchmark-only weighting controls:

```powershell
dotnet run --project .\benchmarks\A2G.BitPermutationLab.Benchmarks -- `
  quick `
  --weighting-profile speed-first `
  --scenario-budget 4 `
  --sampling-seed 42 `
  --report-weighted true `
  --report-unweighted false
```

## Status

Advanced scenario shaping remains code-first. The simplified CLI intentionally does not expose every internal parameter from the plan.

The current weighting implementation covers:
- required baselines
- deterministic scenario selection by seed
- benchmark-profile defaults for weighting and budget
- raw timing reports kept separate from weighting metadata

What is still intentionally lightweight:
- no external weights config file yet
- no allocation measurement yet in the matrix report
- no full scenario/config authoring surface in the CLI
