#!/usr/bin/env python3
"""Test script for the enhanced message deletion functionality."""

import sys
import os
import datetime
from unittest.mock import Mock, patch

# Add the parent directory to the path so we can import the bot modules
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from python import config
from python.__main__ import Bot


def test_bot_message_deletion():
    """Test that bot messages are scheduled for deletion when configured."""
    print("Testing bot message deletion functionality...")
    
    # Mock the VK API calls
    with patch('saya.Vk.__init__'), \
         patch('saya.Vk.call_method') as mock_call_method:
        
        # Mock the API response for message sending
        mock_call_method.return_value = {'response': 12345}
        
        # Create bot instance
        bot = Bot(token="test_token", group_id=12345, debug=False)
        
        # Configure test settings
        original_delay = config.BOT_MESSAGE_DELETE_DELAY_MINUTES
        original_delete_chats = config.BOT_MESSAGE_DELETE_CHATS
        original_userbot_chats = config.USERBOT_CHATS
        
        config.BOT_MESSAGE_DELETE_DELAY_MINUTES = 60  # 1 hour
        config.BOT_MESSAGE_DELETE_CHATS = [2000000001]
        config.USERBOT_CHATS = {2000000001: 477}
        
        try:
            # Test 1: Message should be scheduled for deletion in configured chat
            peer_id = 2000000001
            msg_id = bot.send_msg("Test message", peer_id)
            
            print(f"✓ Sent message with ID {msg_id}")
            
            # Check if message was scheduled for deletion
            assert peer_id in bot.messages_to_delete, "Message was not scheduled for deletion"
            assert len(bot.messages_to_delete[peer_id]) > 0, "No messages scheduled for deletion"
            
            scheduled_msg = bot.messages_to_delete[peer_id][-1]
            assert scheduled_msg['id'] == msg_id, "Wrong message ID scheduled for deletion"
            
            # Check that the deletion is scheduled for the correct time
            expected_time = datetime.datetime.now() + datetime.timedelta(minutes=60)
            actual_time = scheduled_msg['date']
            time_diff = abs((expected_time - actual_time).total_seconds())
            assert time_diff < 5, f"Deletion time is off by {time_diff} seconds"
            
            print("✓ Message correctly scheduled for deletion after 60 minutes")
            
            # Test 2: Message should NOT be scheduled for deletion in non-configured chat
            bot.messages_to_delete.clear()
            peer_id_no_delete = 2000000002
            msg_id2 = bot.send_msg("Test message 2", peer_id_no_delete)
            
            assert peer_id_no_delete not in bot.messages_to_delete, "Message was incorrectly scheduled for deletion"
            print("✓ Message correctly NOT scheduled for deletion in non-configured chat")
            
            # Test 3: Disable deletion and verify no scheduling occurs
            config.BOT_MESSAGE_DELETE_DELAY_MINUTES = 0
            bot.messages_to_delete.clear()
            msg_id3 = bot.send_msg("Test message 3", 2000000001)
            
            assert len(bot.messages_to_delete) == 0, "Message was scheduled for deletion when disabled"
            print("✓ Message deletion correctly disabled when delay is 0")
            
        finally:
            # Restore original config
            config.BOT_MESSAGE_DELETE_DELAY_MINUTES = original_delay
            config.BOT_MESSAGE_DELETE_CHATS = original_delete_chats
            config.USERBOT_CHATS = original_userbot_chats
    
    print("All tests passed! ✓")


def test_should_delete_bot_message():
    """Test the helper method that determines if messages should be deleted."""
    print("Testing _should_delete_bot_message helper method...")
    
    with patch('saya.Vk.__init__'):
        bot = Bot(token="test_token", group_id=12345, debug=False)
        
        # Configure test settings
        original_delete_chats = config.BOT_MESSAGE_DELETE_CHATS
        original_userbot_chats = config.USERBOT_CHATS
        original_chats_deleting = config.CHATS_DELETING
        
        config.BOT_MESSAGE_DELETE_CHATS = [2000000001]
        config.USERBOT_CHATS = {2000000001: 477, 2000000002: 478}
        config.CHATS_DELETING = [2000000002]
        
        try:
            # Test with BOT_MESSAGE_DELETE_CHATS configured
            assert bot._should_delete_bot_message(2000000001), "Should delete in BOT_MESSAGE_DELETE_CHATS"
            assert not bot._should_delete_bot_message(2000000002), "Should not delete when not in BOT_MESSAGE_DELETE_CHATS"
            
            # Test fallback to CHATS_DELETING when BOT_MESSAGE_DELETE_CHATS is empty
            config.BOT_MESSAGE_DELETE_CHATS = []
            assert bot._should_delete_bot_message(2000000002), "Should delete using CHATS_DELETING fallback"
            assert not bot._should_delete_bot_message(2000000001), "Should not delete when not in CHATS_DELETING"
            
            # Test that chat must be in USERBOT_CHATS
            config.BOT_MESSAGE_DELETE_CHATS = [2000000003]
            assert not bot._should_delete_bot_message(2000000003), "Should not delete when not in USERBOT_CHATS"
            
        finally:
            # Restore original config
            config.BOT_MESSAGE_DELETE_CHATS = original_delete_chats
            config.USERBOT_CHATS = original_userbot_chats  
            config.CHATS_DELETING = original_chats_deleting
    
    print("Helper method tests passed! ✓")


if __name__ == '__main__':
    test_should_delete_bot_message()
    test_bot_message_deletion()
    print("\n🎉 All message deletion tests completed successfully!")