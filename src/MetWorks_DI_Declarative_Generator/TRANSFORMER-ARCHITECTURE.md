# ModelTransformer Architecture Diagram

## High-Level System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           PARSED YAML MODEL                                 │
│                    (DdiCodeGen.SyntaxLoader.Models)                          │
│                                                                              │
│  ┌─ Model                                                                   │
│  │  ├─ CodeGen                                                              │
│  │  │  ├─ Namespace                                                         │
│  │  │  └─ RegistryClass                                                     │
│  │  │                                                                        │
│  │  └─ Instances[ ]                                                         │
│  │     └─ Instance[]  ← Single enumeration here                            │
│  │        ├─ InstanceName                                                   │
│  │        ├─ ClassQualified                                                 │
│  │        ├─ InterfaceQualified                                             │
│  │        ├─ HasAssignments (computed)                                      │
│  │        ├─ HasElements (computed)                                         │
│  │        ├─ ElementsConstructionExpression (computed)                      │
│  │        ├─ Assignments[ ]                                                 │
│  │        │  └─ Assignment                                                  │
│  │        │     ├─ Name                                                     │
│  │        │     └─ InitializerParameterAssignmentClause                     │
│  │        └─ Elements[ ]                                                    │
│  │                                                                            │
│  └─────────────────────────────────────────────────────────────────────────│
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ModelTransformer.TransformAll()                      │
│                     (Single-Pass Transformation Engine)                     │
│                                                                              │
│  Phase 1: ACCUMULATION (for each Instance in Model.Instances)              │
│  ──────────────────────────────────────────────────────────────            │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────┐            │
│  │ TransformInstance(instance)                                │            │
│  │                                                             │            │
│  │ ✓ Extract → Accessors.Instance                             │            │
│  │ ✓ Extract → Registry.Instance                              │            │
│  │ ✓ Extract → Instance.Factory.Instance                      │            │
│  │ ✓ Extract → Instance.Field.Instance                        │            │
│  │ ✓ Extract → Elements.Initializer.Instance                  │            │
│  │ ✓ Extract → Assignments.Initializer.Instance               │            │
│  │                                                             │            │
│  └─────────────────────────────────────────────────────────────┘            │
│                                    │                                        │
│                                    ▼ (accumulate in lists/dicts)           │
│                                                                              │
│  Phase 2: FINALIZATION (after all instances processed)                     │
│  ──────────────────────────────────────────────────────────                │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────┐            │
│  │ result.FinalizeAll()                                        │            │
│  │                                                             │            │
│  │ Build Accessors.Model     ← from _accessorInstances list   │            │
│  │ Build Registry.Model      ← from _registryInstances list   │            │
│  │                                                             │            │
│  └─────────────────────────────────────────────────────────────┘            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          TransformationResult                               │
│              (Complete Template Models Ready for Rendering)                 │
│                                                                              │
│  Access Phase: Query for Template-Specific Models                          │
│  ────────────────────────────────────────────────────                      │
│                                                                              │
│  List-Based:                                                                │
│    • result.AccessorsModel         → Accessors.hbs                         │
│    • result.RegistryModel          → Registry.hbs                          │
│                                                                              │
│  Per-Instance (by instanceName):                                            │
│    • result.GetInstanceFactoryData(name)        → Instance.Factory.hbs     │
│    • result.GetInstanceFieldData(name)          → Instance.Field.hbs       │
│    • result.GetElementsInitializerData(name)    → Elements.Initializer.hbs │
│    • result.GetAssignmentsInitializerData(name) → Assignments.Init.hbs    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │
                        ┌───────────┴───────────┐
                        │                       │
                        ▼                       ▼
        ┌──────────────────────────┐  ┌──────────────────────────┐
        │   Handlebars Templates   │  │   Handlebars Templates   │
        │   (List-based)           │  │   (Per-instance)         │
        │                          │  │                          │
        │ • Accessors.hbs          │  │ • Instance.Factory.hbs   │
        │ • Registry.hbs           │  │ • Instance.Field.hbs     │
        │                          │  │ • Elements.Init.hbs      │
        │                          │  │ • Assignments.Init.hbs   │
        └──────────────────────────┘  └──────────────────────────┘
                        │                       │
                        └───────────┬───────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │   Generated C# Source Files   │
                    │                               │
                    │ • Accessors.g.cs              │
                    │ • Registry.g.cs               │
                    │ • Instance.Factory.g.cs       │
                    │ • Instance.Field.g.cs         │
                    │ • Elements.Initializer.cs     │
                    │ • Assignments.Initializer.cs  │
                    └───────────────────────────────┘
```

## Data Flow Timeline

```
Time →

t0: Create ModelTransformer(model)
    └─ Validate CodeGen not null
    └─ Store reference to source Model

t1: Call transformer.TransformAll()
    └─ Create empty TransformationResult
    
t2-t(n): For each Instance in Model.Instances
    └─ TransformInstance(instance, result)
       ├─ Extract Accessors data
       ├─ Extract Registry data
       ├─ Extract Factory data
       ├─ Extract Field data
       ├─ Extract Elements data
       └─ Extract Assignments data
    
    └─ result.Add*/Set* methods
       ├─ Append to _accessorInstances list
       ├─ Append to _registryInstances list
       └─ Set in per-instance dictionaries
    
    └─ Loop repeats for each instance (SINGLE PASS)

t(n+1): Call result.FinalizeAll()
    └─ Build Accessors.Model from accumulated instances
    └─ Build Registry.Model from accumulated instances
    
t(n+2): Return TransformationResult
    └─ Models ready for template rendering
```

## Memory Layout During Execution

```
TransformationResult Object
├─ _accessorInstances: List<Accessors.Instance>
│  └─ [Instance1, Instance2, ..., InstanceN]
│
├─ _registryInstances: List<Registry.Instance>
│  └─ [Instance1, Instance2, ..., InstanceN]
│
├─ _instanceFactoryData: Dictionary<string, InstanceFactory.Instance>
│  └─ ["foo" → Instance{Name="foo", ClassQualified="..."}]
│  └─ ["bar" → Instance{Name="bar", ClassQualified="..."}]
│
├─ _instanceFieldData: Dictionary<string, InstanceField.Instance>
│  └─ ["foo" → Instance{Name="foo", ClassQualified="..."}]
│
├─ _elementsInitializerData: Dictionary<string, ElementsInit.Instance>
│  └─ ["foo" → Instance{Name="foo", ...}]
│
├─ _assignmentsInitializerData: Dictionary<string, AssignmentsInit.Instance>
│  └─ ["foo" → Instance{Name="foo", HasAssignments=true, Assignments=[...]}]
│
├─ _accessorsModel: Accessors.Model (null until FinalizeAll)
│  └─ After finalize: Model{Instances=[...], Namespace="...", ...}
│
└─ _registryModel: Registry.Model (null until FinalizeAll)
   └─ After finalize: Model{Instances=[...], Namespace="...", ...}
```

## Comparison: Single-Pass vs. Multiple-Pass

### ExpandoPipeline (Multiple Passes)
```
Pass 1: Iterate instances → BuildInstancesExpando()
        ├─ Extract element data
        ├─ Extract assignment data
        └─ Create ExpandoObject

Pass 2: Iterate instances → DeriveAccessorTokens()
        ├─ Extract accessor data
        └─ Create accessor context

Pass 3: Iterate instances → (Per-instance templates)
        └─ Extract factory/field data

Result: Data accessed 3+ times, ExpandoObject overhead
```

### ModelTransformer (Single Pass)
```
Pass 1: Iterate instances → TransformInstance()
        ├─ Extract ALL template data
        ├─ Add to AccumulationResult
        └─ Create typed records

Result: Data accessed once, strongly-typed records
```

## Complexity Analysis

```
Time Complexity:
  ModelTransformer.TransformAll()   O(n·m)
  where n = number of instances
        m = average assignments per instance
  
  Single outer loop: O(n)
  Per-instance work:
    - Accessor extraction: O(1)
    - Registry extraction: O(1)
    - Factory extraction: O(1)
    - Field extraction: O(1)
    - Elements extraction: O(1)
    - Assignments extraction: O(m)
  ────────────────────────
  Total: O(n + n·m) = O(n·m)
  
  Finalization: O(n)
  Total: O(n·m)

Space Complexity:
  TransformationResult storage:
  - 2 lists: O(n) each
  - 4 dictionaries: O(n) each
  ────────────────────────
  Total: O(n)

Performance vs. ExpandoPipeline:
  ExpandoPipeline: O(3·n·m) with dynamic overhead
  ModelTransformer: O(n·m) with static typing
  
  Speedup: ~3x for 3-pass scenario
  Expected real-world: 30-50% faster (accounting for
                      memory allocation and GC)
```
