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
- CLI: `encode`, `decode`, `list`, and `benchmark` with direct advanced scenario flags
- Tests: round-trip, validation, emitter, config, plugin-loading, and CLI coverage
- Benchmarks: code-driven profiles, config-driven runs, weighting, deterministic sampling, quick-mode reports, and BenchmarkDotNet mode

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

Encode with advanced permutation settings and a plugin-loaded custom mutation:

```powershell
dotnet .\tools\A2G.BitPermutationLab.Cli\bin\Debug\net10.0\A2G.BitPermutationLab.Cli.dll encode `
  --value 12345 `
  --number-kind uint32 `
  --bits 32 `
  --salt-text demo-seed `
  --mix xor `
  --permute feistel `
  --feistel-rounds 2 `
  --feistel-round-function xorshift-add `
  --chunk-size 4 `
  --emitter hex16 `
  --alphabet hex16 `
  --custom-mutation-name plugin-xor `
  --custom-mutation-position after-mix `
  --custom-mutation-plugin .\plugins\MyMutations.dll `
  --custom-mutation-type MyNamespace.PluginXorMutation
```

Run a benchmark profile:

```powershell
dotnet .\tools\A2G.BitPermutationLab.Cli\bin\Debug\net10.0\A2G.BitPermutationLab.Cli.dll benchmark --profile quick --iterations 1000
```

Run a direct single-scenario benchmark through BenchmarkDotNet:

```powershell
dotnet .\tools\A2G.BitPermutationLab.Cli\bin\Debug\net10.0\A2G.BitPermutationLab.Cli.dll benchmark `
  --mode benchmarkdotnet `
  --iterations 1000 `
  --number-kind uint32 `
  --bits 32 `
  --salt 42 `
  --mix xor `
  --permute rotate `
  --rotate-by 11 `
  --chunk-size 4 `
  --emitter hex16 `
  --alphabet hex16 `
  --value-set middle
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

The CLI benchmark command supports three entry paths:
- built-in profiles
- direct single-scenario benchmarking from CLI flags
- JSON config files

For larger experiment sets, code-driven scenarios are still the most maintainable option.

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
- `--mode quick|benchmarkdotnet`
- `--config <benchmark.json>`
- `--output <report.md>`
- `--csv <report.csv>`

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

Config-driven benchmark runs may include custom mutation plugin loading through JSON `pluginPath` and `typeName` fields.

## Status

Most of the `PLAN.md` feature surface is implemented, including advanced direct CLI scenario flags, custom mutation plugin loading, config-driven benchmarks, quick-mode reports, and BenchmarkDotNet mode.

Detailed reference notes live in:
- [docs/FORMULA.md](docs\FORMULA.md)
- [docs/PARAMETERS.md](docs\PARAMETERS.md)
- [docs/BENCHMARKING.md](docs\BENCHMARKING.md)
- [docs/REPORT_FORMAT.md](docs\REPORT_FORMAT.md)

The current weighting implementation covers:
- required baselines
- deterministic scenario selection by seed
- benchmark-profile defaults for weighting and budget
- raw timing reports kept separate from weighting metadata

What is still intentionally lightweight:
- no external weights config file yet
- BenchmarkDotNet runs are a separate execution path and do not yet feed back into the quick-mode Markdown/CSV matrix exporters
