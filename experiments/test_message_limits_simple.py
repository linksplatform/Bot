#!/usr/bin/env python3
"""Standalone test for message limiting logic"""

def get_daily_message_limit(karma: int) -> int:
    """Returns daily message limit based on karma level.
    
    :param karma: user's karma value
    :return: maximum number of messages allowed per day
    """
    if karma > -10:
        return -1  # No limit
    elif karma > -20:
        return 64
    elif karma > -40:
        return 32
    elif karma > -80:
        return 16
    elif karma > -160:
        return 8
    elif karma > -320:
        return 4
    elif karma > -640:
        return 2
    elif karma > -1280:
        return 1
    else:
        return 0  # Read-only mode


def test_message_limits():
    """Test the daily message limit function"""
    
    # Test cases based on the issue requirements
    test_cases = [
        # (karma, expected_limit)
        (0, -1),      # No limit for karma > -10
        (-5, -1),     # No limit for karma > -10
        (-9, -1),     # No limit for karma > -10
        (-10, 64),    # 64 messages for karma < -10 and >= -20
        (-15, 64),    # 64 messages for karma < -10 and >= -20
        (-19, 64),    # 64 messages for karma < -10 and >= -20
        (-20, 32),    # 32 messages for karma < -20 and >= -40
        (-30, 32),    # 32 messages for karma < -20 and >= -40
        (-39, 32),    # 32 messages for karma < -20 and >= -40
        (-40, 16),    # 16 messages for karma < -40 and >= -80
        (-60, 16),    # 16 messages for karma < -40 and >= -80
        (-79, 16),    # 16 messages for karma < -40 and >= -80
        (-80, 8),     # 8 messages for karma < -80 and >= -160
        (-120, 8),    # 8 messages for karma < -80 and >= -160
        (-159, 8),    # 8 messages for karma < -80 and >= -160
        (-160, 4),    # 4 messages for karma < -160 and >= -320
        (-240, 4),    # 4 messages for karma < -160 and >= -320
        (-319, 4),    # 4 messages for karma < -160 and >= -320
        (-320, 2),    # 2 messages for karma < -320 and >= -640
        (-480, 2),    # 2 messages for karma < -320 and >= -640
        (-639, 2),    # 2 messages for karma < -320 and >= -640
        (-640, 1),    # 1 message for karma < -640 and >= -1280
        (-960, 1),    # 1 message for karma < -640 and >= -1280
        (-1279, 1),   # 1 message for karma < -640 and >= -1280
        (-1280, 0),   # Read-only mode for karma < -1280
        (-2000, 0),   # Read-only mode for karma < -1280
    ]
    
    print("Testing daily message limits based on karma:")
    print("=" * 50)
    
    all_passed = True
    for karma, expected_limit in test_cases:
        actual_limit = get_daily_message_limit(karma)
        status = "✓ PASS" if actual_limit == expected_limit else "✗ FAIL"
        
        if actual_limit != expected_limit:
            all_passed = False
            
        print(f"Karma: {karma:5d} | Expected: {expected_limit:3d} | Got: {actual_limit:3d} | {status}")
    
    print("=" * 50)
    if all_passed:
        print("🎉 All tests passed!")
    else:
        print("❌ Some tests failed!")
    
    return all_passed

if __name__ == "__main__":
    test_message_limits()