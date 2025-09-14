#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script for message limiting functionality"""

import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), '..', 'python'))

from modules.utils import get_daily_message_limit

def test_message_limits():
    """Test the daily message limit function"""
    
    # Test cases based on the issue requirements
    test_cases = [
        # (karma, expected_limit)
        (0, -1),      # No limit for karma >= -10
        (-5, -1),     # No limit for karma >= -10
        (-10, 64),    # 64 messages for karma < -10 and >= -20
        (-15, 64),    # 64 messages for karma < -10 and >= -20
        (-20, 32),    # 32 messages for karma < -20 and >= -40
        (-30, 32),    # 32 messages for karma < -20 and >= -40
        (-40, 16),    # 16 messages for karma < -40 and >= -80
        (-60, 16),    # 16 messages for karma < -40 and >= -80
        (-80, 8),     # 8 messages for karma < -80 and >= -160
        (-120, 8),    # 8 messages for karma < -80 and >= -160
        (-160, 4),    # 4 messages for karma < -160 and >= -320
        (-240, 4),    # 4 messages for karma < -160 and >= -320
        (-320, 2),    # 2 messages for karma < -320 and >= -640
        (-480, 2),    # 2 messages for karma < -320 and >= -640
        (-640, 1),    # 1 message for karma < -640 and >= -1280
        (-960, 1),    # 1 message for karma < -640 and >= -1280
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