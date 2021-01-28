# SerialDetector

A proof-of-concept tool for detection and exploitation Object Injection Vulnerabilities (OIVs) in .NET applications.

## Description

> We presents the first systematic approach for detecting and exploiting OIVs in .NET applications including the framework and libraries. Our key insight is: The root cause of OIVs is the untrusted information flow from an application’s public entry points (e.g., HTTP request handlers) to sensitive methods that create objects of arbitrary types (e.g., reflection APIs) to invoke methods (e.g., native/virtual methods) that trigger the execution of a gadget. Drawing on this insight, we develop and implement SerialDetector, a taint-based dataflow analysis that discovers OIV patterns in .NET assemblies automatically. We then use these patterns to match publicly available gadgets and to automatically validate the feasibility of OIV attacks. We demonstrate the effectiveness of our approach by an indepth evaluation of a complex production software such as the Azure DevOps Server. We describe the key threat models and report on several remote code execution vulnerabilities found by SerialDetector, including three CVEs on Azure DevOps Server. We also perform an in-breadth security analysis of recent publicly available CVEs. Our results show that SerialDetector can detect OIVs effectively and efficiently. We release our tool publicly to support open science and encourage researchers and practitioners explore the topic further.

We presents the paper "SerialDetector: Principled and Practical Exploration of Object Injection Vulnerabilities for the Web" that describes the approach behind SerialDetector at [NDSS Symposium 2021](https://www.ndss-symposium.org/). The final version of the paper is available here.

## Disclaimer

This software has been created purely for the purposes of academic research and for the development of effective defensive techniques, and is not intended to be used to attack systems except where explicitly authorized. The project contributers are not responsible or liable for misuse of the software. Use responsibly.

## Usage 

The tool operates in two phases: a fully-automated _detection phase_ and a semi-automated _exploitation phase_. 

### Detection Phase
In the detection phase, SerialDetector takes as input a list of .NET assemblies and a list of sensitive sinks, and performs a systematic analysis to generate OIV patterns automatically.

We run SerialDetector against known insecure deserializers in .NET Framework and third-party libraries. The raw results are availible in the repository [SerialDetector-ExperimentalData](https://github.com/yuske/SerialDetector-ExperimentalData/tree/master/TableI). We summurized the data in Table I in the paper. Use the following command-line arguments to run experiments yourself:
```
$ .\SerialDetector.Experiments.Runner\bin\Release\SerialDetector.Experiments.Runner analyze-dotnet [options]
Options:
    -t VALUE        A temparary directory path to copy files of the current .NET Framework.
    -e VALUE        An entry point name that described in SerialDetector.Experiments assembly.
    -o VALUE        An output directory for results.

Example:
$ .\SerialDetector.Experiments.Runner\bin\Release\SerialDetector.Experiments.Runner analyze-dotnet -t tmp\bin-dotnet -e Deserializers::BinaryFormatter -o results\BinaryFormatter    
```

### Exploitation Phase
TBA