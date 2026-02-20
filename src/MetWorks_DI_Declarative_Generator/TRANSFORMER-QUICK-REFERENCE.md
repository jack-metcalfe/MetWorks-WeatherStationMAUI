# ModelTransformer - Quick Reference

## TL;DR

**Single-pass transformer** that converts your parsed `Model` to all template-specific records in one loop through instances. No repeated passes = faster, type-safe, better performance.

## Three Key Files

| File | Purpose |
|------|---------|
| [ModelTransformer.cs](ModelTransformer.cs) | Implementation (288 lines) |
| [TRANSFORMER-INTEGRATION.md](TRANSFORMER-INTEGRATION.md) | How to use it + data mappings |
| [TRANSFORMER-ARCHITECTURE.md](TRANSFORMER-ARCHITECTURE.md) | Visual diagrams + memory layout |

## Five-Minute Usage

```csharp
// 1. Create transformer from your Model
var transformer = new ModelTransformer(model);

// 2. Run single-pass transformation
var result = transformer.TransformAll();

// 3. Get aggregate models (pre-built)
var accessorsModel = result.AccessorsModel;    // for Accessors.hbs
var registryModel = result.RegistryModel;      // for Registry.hbs

// 4. Get per-instance models (by name)
foreach (var instanceName in result.AllInstanceNames)
{
    var factory = result.GetInstanceFactoryData(instanceName);
    var field = result.GetInstanceFieldData(instanceName);
    var elements = result.GetElementsInitializerData(instanceName);
    var assignments = result.GetAssignmentsInitializerData(instanceName);
    
    // Use with templates...
}
```

## What You Get

✓ **AccessorsModel** → Accessors.hbs template  
✓ **RegistryModel** → Registry.hbs template  
✓ **InstanceFactoryData** → Instance.Factory.hbs template  
✓ **InstanceFieldData** → Instance.Field.hbs template  
✓ **ElementsInitializerData** → Elements.Initializer.hbs template  
✓ **AssignmentsInitializerData** → Assignments.Initializer.hbs template  

## How It Works (3 Phases)

```
Phase 1 (ACCUMULATION)
├─ Loop through Model.Instances once
├─ For each instance, extract data for all 6 template types
└─ Store in internal lists and dictionaries

Phase 2 (FINALIZATION)
├─ Build Accessors.Model from accumulated instances list
├─ Build Registry.Model from accumulated instances list
└─ Keep per-instance data ready to access

Phase 3 (ACCESS)
├─ Query AccessorsModel / RegistryModel
└─ Look up per-instance data by name
```

## Data Mapping Quick Reference

### From Source Instance → Accessors.Instance
```csharp
Name ← instance.InstanceName
ClassQualified ← instance.ClassQualified
InterfaceQualified ← instance.InterfaceQualified
```

### From Source Instance → Registry.Instance
```csharp
Name ← instance.InstanceName
HasAssignments ← instance.HasAssignments
HasDisposable ← false  // TODO: determine from source
```

### From Source Instance → Instance.Factory.Instance
```csharp
Name ← instance.InstanceName
ClassQualified ← instance.ClassQualified
HasElements ← instance.HasElements
```

### From Source Instance → Instance.Field.Instance
```csharp
Name ← instance.InstanceName
ClassQualified ← instance.ClassQualified
```

### From Source Instance → Elements.Initializer.Instance
```csharp
Name ← instance.InstanceName
ClassQualified ← instance.ClassQualified
ElementsConstructionExpression ← instance.ElementsConstructionExpression
```

### From Source Instance → Assignments.Initializer.Instance
```csharp
Name ← instance.InstanceName
HasAssignments ← instance.HasAssignments
Assignments ← ExtractAssignments(instance)
  └─ For each source Assignment:
     ParameterName ← assignment.Name
     InitializerArgumentExpression ← assignment.InitializerParameterAssignmentClause
```

## Before vs. After

| Aspect | Before (ExpandoPipeline) | After (ModelTransformer) |
|--------|---|---|
| **Passes** | 3+ | 1 |
| **Model Type** | `ExpandoObject` (dynamic) | Strongly-typed records |
| **Speed** | Slower (repeated iterations) | Faster (single pass) |
| **Type Safety** | Limited (string keys) | Full (compiler verified) |
| **Intellisense** | No | Yes |
| **Code Clarity** | Complex flow | Simple phases |

## Two Ways to Integrate

### Option A: Replace ExpandoPipeline directly
```csharp
// Old:
var instancesExpando = ExpandoPipeline.BuildInstancesExpando(model);

// New:
var transformer = new ModelTransformer(model);
var result = transformer.TransformAll();
```

### Option B: Gradual migration
```csharp
// Use transformer for new templates
var transformer = new ModelTransformer(model);
var result = transformer.TransformAll();

// Keep ExpandoPipeline for existing templates (temporarily)
var instancesExpando = ExpandoPipeline.BuildInstancesExpando(model);
```

## Common Issues & Solutions

| Problem | Solution |
|---------|----------|
| "HasDisposable required" on Registry.Instance | TODO: Determine from source model, update transformer |
| "Instance not found" KeyNotFoundException | Verify instance name passed to Get* method matches instance.InstanceName |
| Null reference on property access | Check source Instance has required property (ClassQualified, InstanceName) |

## Performance Tips

1. **Caching**: If you need results multiple times, store the TransformationResult
2. **Lazy Loading**: Could extend to lazily-build per-instance models on first access
3. **Streaming**: Could emit results as they accumulate instead of batch finalization

## Testing Checklist

- [ ] Build succeeds (already verified ✓)
- [ ] Create unit test for ModelTransformer.TransformAll()
- [ ] Compare output with ExpandoPipeline for identical results
- [ ] Benchmark performance improvement
- [ ] Integrate into CodeGenerator
- [ ] Test with real YAML input
- [ ] Verify all 6 template outputs match previous

## Key Classes

```
ModelTransformer
├─ Constructor(Model source)
├─ TransformAll() → TransformationResult
└─ TransformInstance(Instance, TransformationResult)

TransformationResult
├─ Accumulation Phase
│  ├─ AddAccessorInstance()
│  ├─ AddRegistryInstance()
│  ├─ SetInstanceFactoryData()
│  ├─ SetInstanceFieldData()
│  ├─ SetElementsInitializerData()
│  └─ SetAssignmentsInitializerData()
├─ Finalization Phase
│  └─ FinalizeAll()
└─ Access Phase
   ├─ AccessorsModel { get; }
   ├─ RegistryModel { get; }
   ├─ GetInstanceFactoryData(name)
   ├─ GetInstanceFieldData(name)
   ├─ GetElementsInitializerData(name)
   ├─ GetAssignmentsInitializerData(name)
   └─ AllInstanceNames
```

## Next Steps

1. Review [TRANSFORMER-INTEGRATION.md](TRANSFORMER-INTEGRATION.md) for full details
2. Look at [TRANSFORMER-ARCHITECTURE.md](TRANSFORMER-ARCHITECTURE.md) for diagrams
3. Update CodeGenerator to use ModelTransformer
4. Remove ExpandoPipeline once fully migrated
5. Run performance benchmarks

---

**Status**: ✅ Implementation complete, compiles successfully, ready for integration  
**Time to Read**: 5 min (this doc) + 10 min (integration guide) + 5 min (architecture)  
**Difficulty**: Easy (straightforward API)
