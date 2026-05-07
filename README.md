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
- Benchmarks: code-driven benchmark profiles

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

## Benchmarking approach

The CLI benchmark command is intentionally simple. For most real experimentation, define scenarios in code and use the library directly.

Built-in profiles:
- `quick`
- `default`
- `full`

The separate benchmark project also runs code-driven profiles:

```powershell
dotnet run --project .\benchmarks\A2G.BitPermutationLab.Benchmarks -- default
```

## Status

Advanced scenario shaping remains code-first. The simplified CLI intentionally does not expose every internal parameter from the plan.
