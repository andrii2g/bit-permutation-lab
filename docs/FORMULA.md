# Formula Notes

`bit-permutation-lab` is a reversible encoding lab, not a cryptosystem.

## Pipeline

For a value `n` and scenario parameters `p`, the forward pipeline is:

1. normalize `n` into the configured fixed-width binary domain
2. apply optional bit-value custom mutation at `BeforeMix`
3. apply mixer forward transform
4. apply optional bit-value custom mutation at `AfterMix`
5. apply permutation forward transform
6. apply optional bit-value custom mutation at `AfterPermutation`
7. chunk the transformed value
8. apply optional chunk mutation at `BeforeEmit`
9. emit the chunk sequence

The reverse pipeline mirrors those steps in the inverse order.

## Mixers

- `None`: identity
- `Xor`: `x ^ mask`
- `Add`: `(x + addend) mod 2^bits`
- `Multiply`: `(x * multiplier) mod 2^bits`

Derived mixer inputs use the configured `SaltDerivationKind`.

## Permutations

- `Identity`, `Not`, `ByteSwap`, and `BitReverse` are self-inverse
- `Rotate` uses fixed-width rotate-left for forward and rotate-right for reverse
- `NibbleSwap` supports reverse-nibble order and adjacent swaps
- `ChunkPermutation` permutes MSB-indexed bit groups according to the configured variant
- `Feistel` uses deterministic round keys and a reversible round function

## Salt Text

`saltText` is normalized as UTF-8 bytes and converted to `SaltSeed` using 64-bit FNV-1a before any benchmark/config scenario is executed.
