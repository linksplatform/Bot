# Off-topic Detection Implementation

This folder contains experimental and test code for the off-topic detection feature.

## Files

- `test_standalone_detection.py` - Standalone test for the off-topic detection logic
- `test_off_topic_detection.py` - Full integration test (requires dependencies)

## How it works

The off-topic detection system:

1. **Checks message length**: Only processes messages with 3+ words (configurable)
2. **Simulates Google search**: Uses keyword-based heuristics to simulate search results
3. **Checks against whitelist**: Compares found domains against programming-related websites
4. **Returns result**: Determines if message is likely off-topic or programming-related

## Testing

Run the standalone test:
```bash
python3 experiments/test_standalone_detection.py
```

This will test various message types and show the detection results.

## Configuration

The system uses these config variables from `config.py`:
- `OFF_TOPIC_DETECTION_ENABLED` - Enable/disable detection
- `OFF_TOPIC_MIN_WORDS` - Minimum words to trigger detection
- `PROGRAMMING_WEBSITES_WHITELIST` - List of whitelisted programming sites

## Production Notes

The current implementation uses a mock search system for testing. In production:

1. Use Google Custom Search API instead of mock search
2. Add rate limiting and caching
3. Consider using machine learning models for better accuracy
4. Add user feedback mechanism to improve detection