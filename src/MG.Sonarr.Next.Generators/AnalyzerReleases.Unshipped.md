; Unshipped analyzer release
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
PSNR100 | PoshSonarr.Usage | Error | ServiceRegistrationGenerator — registered service type cannot be constructed by the container
PSNR101 | PoshSonarr.Usage | Error | ServiceRegistrationGenerator — registered service type is not a base type of the implementation
PSNR102 | PoshSonarr.Usage | Error | ServiceRegistrationGenerator — registered type is not accessible to the generated registration
PSNR103 | PoshSonarr.Usage | Error | ServiceRegistrationGenerator — registration method name is not a valid identifier
PSNR104 | PoshSonarr.Usage | Error | SonarrObjectConverterGenerator — marked model type cannot be constructed by its converter
PSNR105 | PoshSonarr.Usage | Error | SonarrObjectConverterGenerator — marked type does not derive from SonarrObject
PSNR106 | PoshSonarr.Usage | Error | SonarrObjectConverterGenerator — marked model type does not implement ISerializableNames of itself
PSNR107 | PoshSonarr.Usage | Error | SonarrObjectConverterGenerator — marked model type is not accessible to the generated converter list
