# Run Report Template

Use this file as a starting point for documenting a benchmark run after adding new custom logic.

## Run Summary

- Date:
- Branch / commit:
- Goal of the change:
- Runner:
  `quick` or `benchmarkdotnet`
- Scenario source:
  built-in profile / direct CLI / config JSON

## Environment

- Machine:
- OS:
- .NET SDK:
- Build configuration:
- Notes:

## Command

```powershell
# Paste the exact command used for the run here.
```

## Scenario Notes

- Custom mutation / plugin:
- Mixer:
- Permutation:
- Chunking:
- Emitter:
- Weighting profile:
- Scenario budget:
- Sampling seed:

## Key Results

| Scenario / Family | Range | Encode ns/op | Decode ns/op | RoundTrip ns/op | Weight | Cost | Notes |
|---|---|---:|---:|---:|---:|---:|---|
| `None + Identity + ByteArray` | Small | 42.0 | 39.5 | 81.5 | 4.200 | 0.25 | Sample baseline-style row. Replace with a real measured value. |
| `Xor + Rotate + Hex16` | Small | 68.4 | 71.2 | 139.6 | 2.143 | 0.70 | Sample transformed row. Replace with a real measured value. |

Suggested interpretation:
- keep the table small in the summary report
- paste the full Markdown or CSV benchmark export separately when needed

## Observations

- What improved:
- What regressed:
- Any correctness concerns:
- Any surprising allocation behavior:

## Follow-up

- Next experiment:
- Parameters to change:
- Validation/tests to add:
