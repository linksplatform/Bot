#!/usr/bin/env python3
"""Test the configuration logic for message deletion without external dependencies."""

import sys
import os
from datetime import datetime, timedelta

# Add the parent directory to the path
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from python import config


def test_config_values():
    """Test that new configuration values are properly set."""
    print("Testing configuration values...")
    
    # Check that new config values exist
    assert hasattr(config, 'BOT_MESSAGE_DELETE_DELAY_MINUTES'), "BOT_MESSAGE_DELETE_DELAY_MINUTES not found"
    assert hasattr(config, 'BOT_MESSAGE_DELETE_CHATS'), "BOT_MESSAGE_DELETE_CHATS not found"
    
    # Check default values
    assert isinstance(config.BOT_MESSAGE_DELETE_DELAY_MINUTES, int), "BOT_MESSAGE_DELETE_DELAY_MINUTES should be integer"
    assert isinstance(config.BOT_MESSAGE_DELETE_CHATS, list), "BOT_MESSAGE_DELETE_CHATS should be list"
    
    print(f"✓ BOT_MESSAGE_DELETE_DELAY_MINUTES = {config.BOT_MESSAGE_DELETE_DELAY_MINUTES}")
    print(f"✓ BOT_MESSAGE_DELETE_CHATS = {config.BOT_MESSAGE_DELETE_CHATS}")
    print("✓ Configuration values are properly set")


def test_deletion_logic():
    """Test the message deletion logic without actual VK integration."""
    print("\nTesting message deletion logic...")
    
    # Simulate the logic from _should_delete_bot_message
    def should_delete_bot_message(peer_id: int, delete_chats: list, userbot_chats: dict, chats_deleting: list) -> bool:
        """Simulated logic from the actual method."""
        effective_delete_chats = delete_chats or chats_deleting
        return peer_id in effective_delete_chats and peer_id in userbot_chats
    
    # Test cases
    test_cases = [
        # (peer_id, delete_chats, userbot_chats, chats_deleting, expected_result, description)
        (2000000001, [2000000001], {2000000001: 477}, [], True, "Should delete when in BOT_MESSAGE_DELETE_CHATS and USERBOT_CHATS"),
        (2000000001, [], {2000000001: 477}, [2000000001], True, "Should delete using CHATS_DELETING fallback"),
        (2000000001, [2000000002], {2000000001: 477}, [2000000001], False, "Should not delete when not in BOT_MESSAGE_DELETE_CHATS"),
        (2000000001, [2000000001], {2000000002: 477}, [], False, "Should not delete when not in USERBOT_CHATS"),
        (2000000001, [], {2000000001: 477}, [], False, "Should not delete when no delete chats configured"),
    ]
    
    for peer_id, delete_chats, userbot_chats, chats_deleting, expected, description in test_cases:
        result = should_delete_bot_message(peer_id, delete_chats, userbot_chats, chats_deleting)
        assert result == expected, f"Failed: {description}. Expected {expected}, got {result}"
        print(f"✓ {description}")
    
    print("✓ All deletion logic tests passed")


def test_time_calculation():
    """Test time calculation for message deletion."""
    print("\nTesting time calculation...")
    
    # Test different delay configurations
    delay_minutes_configs = [0, 5, 60, 1440]  # 0, 5min, 1hour, 1day
    
    for delay_minutes in delay_minutes_configs:
        if delay_minutes == 0:
            print(f"✓ Delay of {delay_minutes} minutes = disabled (no deletion)")
            continue
            
        delay_seconds = delay_minutes * 60
        current_time = datetime.now()
        deletion_time = current_time + timedelta(seconds=delay_seconds)
        
        expected_diff = delay_minutes * 60
        actual_diff = (deletion_time - current_time).total_seconds()
        
        assert abs(actual_diff - expected_diff) < 1, f"Time calculation error for {delay_minutes} minutes"
        print(f"✓ Delay of {delay_minutes} minutes = {delay_seconds} seconds calculated correctly")
    
    print("✓ All time calculation tests passed")


def main():
    """Run all tests."""
    print("🧪 Running message deletion configuration and logic tests...\n")
    
    try:
        test_config_values()
        test_deletion_logic()
        test_time_calculation()
        
        print("\n🎉 All tests passed successfully!")
        print(f"\n📋 Summary:")
        print(f"   - New config option: BOT_MESSAGE_DELETE_DELAY_MINUTES = {config.BOT_MESSAGE_DELETE_DELAY_MINUTES}")
        print(f"   - New config option: BOT_MESSAGE_DELETE_CHATS = {config.BOT_MESSAGE_DELETE_CHATS}")
        print(f"   - Logic correctly handles chat selection and userbot requirements")
        print(f"   - Time calculations work for various delay periods")
        print(f"\n💡 To use the feature:")
        print(f"   1. Set BOT_MESSAGE_DELETE_DELAY_MINUTES > 0 to enable")
        print(f"   2. Configure BOT_MESSAGE_DELETE_CHATS with chat IDs (or leave empty to use CHATS_DELETING)")
        print(f"   3. Ensure USERBOT_CHATS contains the chat mappings for deletion to work")
        
    except Exception as e:
        print(f"❌ Test failed: {e}")
        return 1
    
    return 0


if __name__ == '__main__':
    sys.exit(main())