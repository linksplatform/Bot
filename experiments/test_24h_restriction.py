#!/usr/bin/env python3
"""
Test script to verify 24-hour message restriction logic
"""
from datetime import datetime

def test_24_hour_restriction():
    """Test the 24-hour message age restriction logic"""
    
    # Simulate message timestamps
    current_timestamp = datetime.utcnow().timestamp()
    
    # Test case 1: Message from 25 hours ago (should be blocked)
    old_message_timestamp = current_timestamp - (25 * 3600)
    hours_since_old = (current_timestamp - old_message_timestamp) / 3600
    
    print(f"Test 1 - Old message (25 hours ago):")
    print(f"  Hours since message: {hours_since_old:.2f}")
    print(f"  Should be blocked: {hours_since_old > 24}")
    
    # Test case 2: Message from 1 hour ago (should be allowed)
    recent_message_timestamp = current_timestamp - (1 * 3600)
    hours_since_recent = (current_timestamp - recent_message_timestamp) / 3600
    
    print(f"\nTest 2 - Recent message (1 hour ago):")
    print(f"  Hours since message: {hours_since_recent:.2f}")
    print(f"  Should be blocked: {hours_since_recent > 24}")
    
    # Test case 3: Message from exactly 24 hours ago (should be allowed)
    boundary_message_timestamp = current_timestamp - (24 * 3600)
    hours_since_boundary = (current_timestamp - boundary_message_timestamp) / 3600
    
    print(f"\nTest 3 - Boundary message (24 hours ago):")
    print(f"  Hours since message: {hours_since_boundary:.2f}")
    print(f"  Should be blocked: {hours_since_boundary > 24}")
    
    # Test case 4: Message from 24.1 hours ago (should be blocked)
    just_over_message_timestamp = current_timestamp - (24.1 * 3600)
    hours_since_just_over = (current_timestamp - just_over_message_timestamp) / 3600
    
    print(f"\nTest 4 - Just over 24 hours (24.1 hours ago):")
    print(f"  Hours since message: {hours_since_just_over:.2f}")
    print(f"  Should be blocked: {hours_since_just_over > 24}")

if __name__ == "__main__":
    test_24_hour_restriction()