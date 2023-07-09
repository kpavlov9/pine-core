# PineCore: C# swiss-knife-library with useful data structures and utility methods

## Overview

This library is a part of the efforts of KG Intelligence to open source our technology to be used freely by the c# community. The library contains the commonly used within KG Intelligence methods and data structures. The goal of the project is to gradually increase the content of our open source libraries such `PineCore` to the point where all the source code of KG Intelligence - implemented within the company - is included in open source libraries.

## Content

* Data Structures:
    * Succinct Data Structures: compressed bit representations offering query methods to be used without the need of decompression. Such query methods include `Rank`, `Select`. The succinct datastructure APIs are:
        * `IBits` with properties `IReadOnlyList<nuint> Data { get; }` and `nuint Size { get; }`
        * `IBitIndices` with properties `public nuint Size { get; }` and methods `bool GetBit(nuint index)`
        * `ISuccinctIndices` with properties `nuint SetBitsCount { get; }`, `nuint UnsetBitsCount { get; }`, `nuint Size { get; }` and methods `nuint RankSetBits(nuint bitPositionCutoff)`, `nuint RankUnsetBits(nuint bitPositionCutoff)`, `nuint SelectSetBits(nuint bitCountCutoff)`, `nuint SelectUnsetBits(nuint bitCountCutoff)`
        * `ISuccinctCompressedIndices` with the same properties and methods as `ISuccinctIndices`
        * `ISerializableBits` with mehtods `void Write(BinaryWriter writer)`, `void Write(string filePath)`, `static abstract TSelf Read(BinaryReader reader)` and `static abstract TSelf Read(string filePath)`

* Utility Methods:
    * Bit Twiddling Operations not included in `System.Numerics.BitOperations` such as: `ReverseBits`,  `Rank`, `Select`; these methods operate on `uint`, `ulong`, and also on the native-sized integers `nuint`.


## Usage

The library can be installed as a Nuget package. The nuget package link is [https://www.nuget.org/packages/KGIntelligence.PineCore/](https://www.nuget.org/packages/KGIntelligence.PineCore/).
