# CLI Examples

## Basic encode

```powershell
dotnet run --project .\tools\A2G.BitPermutationLab.Cli -- encode `
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

## Basic decode

```powershell
dotnet run --project .\tools\A2G.BitPermutationLab.Cli -- decode `
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

## List supported values

```powershell
dotnet run --project .\tools\A2G.BitPermutationLab.Cli -- list
```

## Encode with advanced permutation and plugin mutation

```powershell
dotnet run --project .\tools\A2G.BitPermutationLab.Cli -- encode `
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

## Quick benchmark profile

```powershell
dotnet run --project .\tools\A2G.BitPermutationLab.Cli -- benchmark --profile quick --iterations 1000
```

## Direct BenchmarkDotNet scenario

```powershell
dotnet run --project .\tools\A2G.BitPermutationLab.Cli -- benchmark `
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

## Weighted benchmark selection

```powershell
dotnet run --project .\tools\A2G.BitPermutationLab.Cli -- benchmark `
  --profile quick `
  --weighting-profile speed-first `
  --scenario-budget 4 `
  --sampling-seed 42 `
  --report-weighted true `
  --report-unweighted true `
  --iterations 1000
```

## Config-driven weighting override

```powershell
dotnet run --project .\tools\A2G.BitPermutationLab.Cli -- benchmark `
  --profile default `
  --weights-config .\weights.json `
  --iterations 1000
```

## Benchmark project entrypoint

```powershell
dotnet run --project .\benchmarks\A2G.BitPermutationLab.Benchmarks -- default
```

## Benchmark project with weighting controls

```powershell
dotnet run --project .\benchmarks\A2G.BitPermutationLab.Benchmarks -- `
  quick `
  --weighting-profile speed-first `
  --scenario-budget 4 `
  --sampling-seed 42 `
  --report-weighted true `
  --report-unweighted false
```
