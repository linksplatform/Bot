# License Change Feature Test

This document shows how to use the new license change functionality added to the update-conan-recipe-and-create-pull-request tool.

## Usage Example

The tool now accepts a `--new-license` parameter. When provided, it will:

1. Check if the recipe name matches the pattern `platform.*` (case-insensitive)
2. If it matches, update the `license` field in the `conanfile.py` file
3. Create a separate commit with the license change

### Command Example

```bash
./update-conan-recipe-and-create-pull-request \
  --github-authentication-token "your-token" \
  --new-version "1.0.0" \
  --recipe-name "platform.collections" \
  --source-repo-owner-login "linksplatform" \
  --source-repo-name "Data.Collections" \
  --source-repo-branch-name "main" \
  --destination-repo-owner-login "conan-io" \
  --destination-repo-name "conan-center-index" \
  --destination-repo-branch-name "main" \
  --lib-zip-url "https://example.com/lib.zip" \
  --sha256hash "abc123..." \
  --dependencies "platform.memory@1.0.0" \
  --previous-version "0.9.0" \
  --new-license "MIT"
```

### Pattern Matching

The feature only activates for recipes that match the pattern `^platform\..+` (case-insensitive):

- ✅ `platform.collections` → Will change license
- ✅ `Platform.Memory` → Will change license (case-insensitive)
- ✅ `platform.data` → Will change license
- ❌ `boost` → Won't change license
- ❌ `openssl` → Won't change license

### Implementation Details

The new functionality:

1. Adds a `--new-license` optional command-line parameter
2. Uses regex pattern `^platform\..+` to match platform-specific recipes
3. Updates the `license = "value"` line in conanfile.py files
4. Preserves quote style (single or double quotes)
5. Creates a separate commit with message "Update license to {new_license}"
6. Only runs if both conditions are met:
   - `--new-license` parameter is provided
   - Recipe name matches the platform pattern