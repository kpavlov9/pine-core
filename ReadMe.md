# PineCore: C# swiss-knife-library with useful data structures and utility methods

## Overview

This library is a part of the efforts of KG Intelligence to open source our technology to be used freely by the c# community. The library contains the commonly used within KG Intelligence methods and data structures. The goal of the project is to gradually increase the content of our open source libraries such `PineCore` to the point where all the source code of KG Intelligence - implemented within the company - is included in open source libraries.

## Content

* Data Structures:
    * Succinct Data Structures: compressed bit representations offering query methods to be used without the need of decompression. Such query methods include `Rank`, `Select`.

* Utility Methods:
    * Bit Twiddling Operations not included in `System.Numerics.BitOperations` such as: `ReverseBits`,  `Rank`, `Select`; these methods operate on `uint`, `ulong`, and also on the native-sized integers `nuint`.


## Usage

    The library can be installed as a Nuget package: https://www.nuget.org/packages/KGIntelligence.PineCore/.
