# ModelTransformer Integration Guide

## Overview

The `ModelTransformer` class implements a **single-pass transformation** of the source `Model` (from SyntaxLoader) to all template-specific record types used for Handlebars template rendering.

## Architecture

### Components

1. **ModelTransformer** - Entry point service that orchestrates the transformation
2. **TransformationResult** - Accumulates template data during traversal and provides finalized models

### Key Design Principles

- ✓ **Single Pass**: Iterates through `Model.Instances` once only
- ✓ **Simultaneous Extraction**: Extracts data for all templates in each iteration
- ✓ **Two-Phase Processing**: Separation of accumulation and finalization
- ✓ **Type-Safe**: Strongly-typed records instead of dynamic `ExpandoObject`

## Usage Example

```csharp
// In CodeGenerator.GenerateFiles()
var model = /* the parsed Model from SyntaxLoader */;

// Step 1: Create transformer and perform single-pass transformation
var transformer = new ModelTransformer(model);
var result = transformer.TransformAll();

// Step 2: Access finalized list-based models
var accessorsModel = result.AccessorsModel;  // For Accessors.hbs template
var registryModel = result.RegistryModel;    // For Registry.hbs template

// Step 3: Access per-instance models by instance name
foreach (var instanceName in result.AllInstanceNames)
{
    var factoryData = result.GetInstanceFactoryData(instanceName);
    var fieldData = result.GetInstanceFieldData(instanceName);
    var elementsData = result.GetElementsInitializerData(instanceName);
    var assignmentsData = result.GetAssignmentsInitializerData(instanceName);
    
    // Use each model with its corresponding template
    EmitInstanceFactory(files, factoryData);
    EmitInstanceField(files, fieldData);
    // etc.
}
```

## Transformation Flow

```
Input: Model (from SyntaxLoader)
  |
  v
ModelTransformer.TransformAll()
  |
  +-- Accumulation Phase (forEach instance)
  |   +-- Extract AccessorsModels.Instance
  |   +-- Extract RegistryModels.Instance
  |   +-- Extract InstanceFactoryModels.Instance
  |   +-- Extract InstanceFieldModels.Instance
  |   +-- Extract ElementsInitializerModels.Instance
  |   +-- Extract AssignmentInitializerModels.Instance
  |
  v
TransformationResult.FinalizeAll()
  |
  +-- Build AccessorsModels.Model (list finalization)
  +-- Build RegistryModels.Model (list finalization)
  |
  v
Output: TransformationResult (contains all template models)
```

## Template Models Provided

### List-Based Templates (Aggregate)
- **AccessorsModel** - `Accessors.Model` with accumulated instances
- **RegistryModel** - `Registry.Model` with accumulated instances

### Per-Instance Templates (Keyed Access)
- **InstanceFactoryData** - `Instance.Factory.Instance` per instance name
- **InstanceFieldData** - `Instance.Field.Instance` per instance name
- **ElementsInitializerData** - `Elements.Initializer.Instance` per instance name
- **AssignmentsInitializerData** - `Instance.Assignments.Initializer.Instance` per instance name

## Data Mapping Reference

### Accessors.Instance
Maps from source `Instance`:
```
Name ← Instance.InstanceName
ClassQualified ← Instance.ClassQualified
InterfaceQualified ← Instance.InterfaceQualified
```

### Registry.Instance
Maps from source `Instance`:
```
Name ← Instance.InstanceName
HasAssignments ← Instance.HasAssignments (computed: Assignments.Count > 0)
HasDisposable ← Instance.HasDisposable (TODO: determine from source)
```

### Instance.Factory.Instance
Maps from source `Instance`:
```
Name ← Instance.InstanceName
ClassQualified ← Instance.ClassQualified
HasElements ← Instance.HasElements (computed: Elements.Count > 0)
```

### Instance.Field.Instance
Maps from source `Instance`:
```
Name ← Instance.InstanceName
ClassQualified ← Instance.ClassQualified
```

### Elements.Initializer.Instance
Maps from source `Instance`:
```
Name ← Instance.InstanceName
ClassQualified ← Instance.ClassQualified
ElementsConstructionExpression ← Instance.ElementsConstructionExpression (computed)
```

### Assignments.Initializer.Instance
Maps from source `Instance`:
```
Name ← Instance.InstanceName
HasAssignments ← Instance.HasAssignments
Assignments ← ExtractAssignments(Instance)
  └─ For each Assignment:
     Name ← Assignment.Name
     InitializerArgumentExpression ← Assignment.InitializerParameterAssignmentClause
```

## Performance Characteristics

- **Time Complexity**: O(n) where n = number of instances
  - Single enumeration of Instances collection
  - Per-instance operations are O(1) amortized
  - Assignment extraction is O(m) where m = assignments per instance

- **Space Complexity**: O(n·k) where k = template types
  - Each instance data stored in template-specific dictionaries
  - Total overhead: 6 dictionaries of accumulated data

## Advantages Over ExpandoPipeline

| Aspect | ExpandoPipeline | ModelTransformer |
|--------|---|---|
| Pass Count | Multiple passes | Single pass |
| Type Safety | Dynamic ExpandoObject | Strongly-typed records |
| Intellisense | Limited | Full compiler support |
| Extensibility | String-based keys | Compile-time verified |
| Performance | Slower (repeated iterations) | Faster (single traversal) |
| Maintainability | Difficult to track flow | Clear phases |

## Future Enhancement Points

1. **HasDisposable**: Currently hardcoded to `false` in Registry mapping—determine from source model
2. **Template-Specific Models**: Can extend with additional properties as needed
3. **Lazy Finalization**: Could defer finalization until specific models are accessed
4. **Streaming**: Could emit results as they accumulate instead of batch finalization

## Testing Recommendations

- Verify single-pass transformation produces identical results to ExpandoPipeline
- Benchmark performance improvement (expected: 30-50% faster for medium-sized models)
- Unit tests for each extraction method with sample data
- Integration tests with CodeGenerator rendering pipeline
