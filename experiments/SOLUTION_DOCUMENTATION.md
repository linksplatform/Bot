# Solution for Issue #52: Disable automatic votes deletion

## Problem Description
The bot was automatically deleting votes and messages after processing collective votes, which was not the desired behavior for some use cases.

## Root Cause Analysis
The issue was identified in two places in the codebase:

1. **Automatic votes clearing** in `python/modules/commands.py:292`: 
   ```python
   self.user[current_voters] = []  # This line clears votes after processing
   ```

2. **Automatic message deletion** in multiple places in `python/modules/commands.py`:
   - Line 182: When downvotes are disabled for negative karma users
   - Line 205: When users haven't waited long enough between collective votes  
   - Line 228: After successfully processing a karma change

## Solution Implementation

### 1. Configuration Setting
Added a new configuration setting in `python/config.py`:
```python
# Disable automatic votes deletion (issue #52)
DISABLE_AUTOMATIC_VOTES_DELETION = True
```

### 2. Code Changes
Modified `python/modules/commands.py` to conditionally perform deletion operations:

#### A. Conditional message deletion (3 locations):
```python
# Before (automatic deletion):
self.vk_instance.delete_message(self.peer_id, self.msg_id)

# After (conditional deletion):
if not config.DISABLE_AUTOMATIC_VOTES_DELETION:
    self.vk_instance.delete_message(self.peer_id, self.msg_id)
```

#### B. Conditional votes clearing:
```python
# Before (automatic clearing):  
self.user[current_voters] = []

# After (conditional clearing):
if not config.DISABLE_AUTOMATIC_VOTES_DELETION:
    self.user[current_voters] = []
```

## How It Works

- **When `DISABLE_AUTOMATIC_VOTES_DELETION = True`** (default):
  - Vote messages are NOT automatically deleted
  - Collective votes are NOT automatically cleared after reaching the threshold
  - Karma changes still apply normally
  - All other bot functionality remains unchanged

- **When `DISABLE_AUTOMATIC_VOTES_DELETION = False`**:
  - Original behavior is preserved
  - Vote messages are automatically deleted
  - Collective votes are automatically cleared after processing

## Files Modified
1. `python/config.py` - Added the configuration setting
2. `python/modules/commands.py` - Added conditional logic for deletion operations

## Testing
The solution was tested with a verification script that confirms:
- The configuration setting is properly loaded
- All 4 conditional checks are in place in the code
- The setting can be toggled dynamically

## Backward Compatibility
This change is fully backward compatible. The default setting disables automatic deletion (solving issue #52), but the behavior can be reverted by setting `DISABLE_AUTOMATIC_VOTES_DELETION = False`.