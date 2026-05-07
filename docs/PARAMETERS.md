# Parameters

This project uses a typed scenario model. Built-in behavior is represented by typed fields rather than unstructured extension dictionaries.

## Core

- `Name`
- `NumberKind`
- `BitLength`
- `SaltSeed`

## Binary

- `Binary.Kind`
- `Binary.BitOrder`
- `Binary.ByteOrder`

## Mixer

- `Mixer.Kind`
- `Mixer.MaskDerivation`
- `Mixer.LiteralMask`
- `Mixer.LiteralAddend`
- `Mixer.Multiplier`

## Permutation

- `Permutation.Kind`
- `Permutation.RotateBy`
- `Permutation.NibbleSwap`
- `Permutation.ChunkPermutationGroupSize`
- `Permutation.ChunkPermutationVariant`
- `Permutation.ChunkPermutationOrder`
- `Permutation.ChunkPermutationRotateBy`
- `Permutation.FeistelRounds`
- `Permutation.FeistelRoundFunction`

## Chunking

- `Chunking.Kind`
- `Chunking.ChunkSize`
- `Chunking.ChunkReadOrder`

## Emission

- `Emitter.Kind`
- `Emitter.AlphabetKind`
- `Emitter.OutputKind`
- `Emitter.CustomAlphabet`
- `Emitter.ByteArrayTextFormat`

## Custom Mutations

- `CustomMutation.Name`
- `CustomMutation.Position`
- `CustomMutation.Parameters`
- `CustomChunkMutation.Name`
- `CustomChunkMutation.Position`
- `CustomChunkMutation.Parameters`

Plugin-backed custom mutations may also be loaded from:

- assembly path
- fully-qualified type name

The loaded instance is registered once, then resolved by mutation name through the normal pipeline path.
