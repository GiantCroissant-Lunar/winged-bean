# Plugin Manifest Schema and Type Generation

This directory contains the JSON schema for `.plugin.json` files and auto-generated C# types.

## Quicktype Integration

We use [quicktype](https://quicktype.io/) to generate strongly-typed C# classes from plugin manifests, preventing JSON serialization errors.

### Installation

```bash
npm install -g quicktype
```

### Generating Types

**From a sample plugin manifest:**

```bash
quicktype path/to/.plugin.json \
  -l csharp \
  -o schemas/PluginManifest.Generated.cs \
  --namespace ConsoleDungeon.Host.Generated \
  --top-level PluginManifest \
  --array-type list \
  --density dense
```

**From JSON schema:**

```bash
quicktype schemas/plugin-manifest.schema.json \
  --src-lang schema \
  -l csharp \
  -o schemas/PluginManifest.Generated.cs \
  --namespace ConsoleDungeon.Host.Generated
```

### Using Generated Types

```csharp
using ConsoleDungeon.Host.Generated;
using System.Text.Json;

// Deserialize with type safety
var manifest = JsonSerializer.Deserialize<PluginManifest>(jsonString);

// The generated types ensure:
// ✓ Correct property names
// ✓ Correct types (List<string> not object)
// ✓ Nullable annotations
// ✓ Required vs optional fields
```

### Benefits

1. **Type Safety**: Compile-time checks for JSON structure
2. **No Runtime Errors**: Eliminates JSON deserialization exceptions
3. **IntelliSense**: Full IDE support for plugin manifest structure
4. **Documentation**: Generated code documents the expected format
5. **Validation**: Schema ensures manifests are well-formed

### Maintenance

1. Update `plugin-manifest.schema.json` when manifest format changes
2. Regenerate `PluginManifest.Generated.cs` using quicktype
3. Manual modifications needed:
   - Convert from Newtonsoft.Json to System.Text.Json attributes
   - Fix empty array types (object → string or proper type)
   - Add nullable annotations

### Files

- `plugin-manifest.schema.json` - JSON Schema defining manifest structure
- `PluginManifest.Generated.cs` - Auto-generated C# types
- `README.md` - This file

### Adding to Build

Add to `Taskfile.yml`:

```yaml
tasks:
  generate-types:
    desc: "Generate C# types from plugin manifest schema"
    cmds:
      - quicktype schemas/plugin-manifest.schema.json --src-lang schema -l csharp -o schemas/PluginManifest.Generated.cs --namespace ConsoleDungeon.Host.Generated
      - echo "✓ Generated types - remember to convert to System.Text.Json"
```

## Future Improvements

- [x] **Add schema validation in build pipeline** - Integrated via `task validate-manifests`
- [ ] Automate Newtonsoft → System.Text.Json conversion
- [ ] Generate TypeScript types for web tools
- [ ] Add to pre-commit hooks
- [ ] Add CI/CD validation step

## JSON Schema Validation

All plugin manifests are validated against the JSON schema before build to catch format errors early.

### Running Validation

```bash
# Validate all plugin manifests
task validate-manifests

# Or run directly
./schemas/validate-manifests.sh
```

### What Gets Validated

- **ID format**: Must be lowercase kebab-case (e.g., `wingedbean.plugins.resource`)
- **Required fields**: `id`, `version`, `name`, `entryPoint`
- **Dependencies structure**: Must be object `{}` not array `[]`
- **Load strategy**: Must be `eager`, `lazy`, or `on-demand` (lowercase)
- **Exports format**: Services must have `interface`, `implementation`, `lifecycle`
- **Entry points**: Must specify at least one platform (`dotnet`, `nodejs`, `unity`, `godot`)

### Installation

```bash
# Install ajv-cli globally
npm install -g ajv-cli ajv-formats
```

### Integration

The validation runs automatically as a dependency of the `build` task:

```yaml
tasks:
  validate-manifests:
    desc: "Validate all plugin manifests against JSON schema"
    cmds:
      - ./schemas/validate-manifests.sh

  build:
    desc: "Build ConsoleDungeon game"
    deps:
      - validate-manifests  # Runs before build
    cmds:
      - dotnet build Console.sln
```

### Example Errors

```bash
❌ WingedBean.Plugins.DungeonGame
[
  {
    instancePath: '/loadStrategy',
    schemaPath: '#/properties/loadStrategy/enum',
    keyword: 'enum',
    params: { allowedValues: ['eager', 'lazy', 'on-demand'] },
    message: 'must be equal to one of the allowed values'
  }
]
```

This catches issues like:
- Using `"Eager"` instead of `"eager"`
- Using `"dependencies": []` instead of `"dependencies": {}`
- Invalid ID formats with uppercase or special characters
- Missing required fields
