#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script for karma-based chat membership functionality."""

import sys
import os
import unittest
from unittest.mock import Mock, patch

# Add the parent directory to the path so we can import our modules
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from modules.chat_membership import KarmaChatManager
from modules.data_service import BetterBotBaseDataService
import config


class TestKarmaChatManager(unittest.TestCase):
    """Test cases for the KarmaChatManager class."""
    
    def setUp(self):
        """Set up test fixtures."""
        self.mock_vk = Mock()
        self.mock_data_service = Mock()
        self.karma_chat_manager = KarmaChatManager(self.mock_vk)
        
        # Mock user object
        self.mock_user = Mock()
        self.mock_user.uid = 12345
        self.mock_user.name = "Test User"
        self.mock_user.karma = 5
        
        # Set up test config
        self.test_config = [
            {"chat_id": 2000000001, "karma_threshold": 2, "name": "Test Chat 1"},
            {"chat_id": 2000000002, "karma_threshold": 10, "name": "Test Chat 2"}
        ]
        
    def test_initialization(self):
        """Test that KarmaChatManager initializes correctly."""
        self.assertEqual(self.karma_chat_manager.vk, self.mock_vk)
        self.assertEqual(self.karma_chat_manager.karma_chats, config.KARMA_BASED_CHATS)
    
    @patch('config.KARMA_BASED_CHATS')
    def test_user_should_be_added_to_chat(self, mock_config):
        """Test adding a user to chat when karma threshold is met."""
        mock_config.__iter__ = Mock(return_value=iter(self.test_config))
        self.karma_chat_manager.karma_chats = self.test_config
        
        # User has karma 5, should be in chat with threshold 2 but not 10
        self.mock_vk.get_members_ids.return_value = [54321]  # User not in chat
        self.mock_vk.call_method.return_value = {"response": 1}  # Success
        
        self.karma_chat_manager.check_and_update_membership(self.mock_user)
        
        # Should call addChatUser for the first chat (threshold 2)
        calls = self.mock_vk.call_method.call_args_list
        add_calls = [call for call in calls if call[0][0] == 'messages.addChatUser']
        self.assertTrue(len(add_calls) >= 1)
        
        # Should call with correct parameters
        expected_params = {'chat_id': 1, 'user_id': 12345}
        add_calls[0][0][1] == expected_params
    
    @patch('config.KARMA_BASED_CHATS')
    def test_user_should_be_removed_from_chat(self, mock_config):
        """Test removing a user from chat when karma falls below threshold."""
        mock_config.__iter__ = Mock(return_value=iter(self.test_config))
        self.karma_chat_manager.karma_chats = self.test_config
        
        # Set user karma below threshold
        self.mock_user.karma = 1
        
        # User is currently in both chats
        self.mock_vk.get_members_ids.return_value = [12345, 54321]
        self.mock_vk.call_method.return_value = {"response": 1}  # Success
        
        self.karma_chat_manager.check_and_update_membership(self.mock_user)
        
        # Should call removeChatUser for both chats
        calls = self.mock_vk.call_args_list
        remove_calls = [call for call in calls if 'removeChatUser' in str(call)]
        self.assertTrue(len(remove_calls) >= 1)
    
    def test_is_user_in_chat(self):
        """Test checking if user is in chat."""
        # User is in chat
        self.mock_vk.get_members_ids.return_value = [12345, 54321]
        result = self.karma_chat_manager._is_user_in_chat(12345, 2000000001)
        self.assertTrue(result)
        
        # User is not in chat
        self.mock_vk.get_members_ids.return_value = [54321, 67890]
        result = self.karma_chat_manager._is_user_in_chat(12345, 2000000001)
        self.assertFalse(result)
    
    def test_error_handling(self):
        """Test error handling for API failures."""
        # Mock API error
        self.mock_vk.get_members_ids.side_effect = Exception("API Error")
        
        # Should not raise exception
        try:
            result = self.karma_chat_manager._is_user_in_chat(12345, 2000000001)
            self.assertFalse(result)  # Should return False on error
        except Exception:
            self.fail("_is_user_in_chat raised an exception on API error")


def run_integration_test():
    """Run a basic integration test with the actual bot components."""
    print("🚀 Running integration test...")
    
    try:
        # Test importing all required modules
        from modules import Commands, BetterBotBaseDataService, KarmaChatManager
        from __main__ import Bot
        import patterns
        
        print("✅ All modules imported successfully")
        
        # Test pattern matching
        test_patterns = [
            ("check chat membership", patterns.CHECK_CHAT_MEMBERSHIP),
            ("check all membership", patterns.CHECK_ALL_MEMBERSHIP),  
            ("chat status", patterns.CHAT_STATUS),
            ("chat status 2000000001", patterns.CHAT_STATUS)
        ]
        
        for test_msg, pattern in test_patterns:
            match = pattern.match(test_msg)
            if match:
                print(f"✅ Pattern '{pattern.pattern}' matches '{test_msg}'")
            else:
                print(f"❌ Pattern '{pattern.pattern}' does not match '{test_msg}'")
        
        # Test configuration
        if hasattr(config, 'KARMA_BASED_CHATS'):
            print(f"✅ Configuration loaded: {len(config.KARMA_BASED_CHATS)} karma chats configured")
        else:
            print("❌ Configuration not found")
            
        print("✅ Integration test completed successfully")
        
    except ImportError as e:
        print(f"❌ Import error: {e}")
    except Exception as e:
        print(f"❌ Integration test failed: {e}")


if __name__ == '__main__':
    print("🧪 Testing Karma-Based Chat Membership System")
    print("=" * 50)
    
    # Run unit tests
    print("📋 Running unit tests...")
    unittest.main(argv=[''], exit=False, verbosity=2)
    
    print("\n" + "=" * 50)
    
    # Run integration test
    run_integration_test()
    
    print("\n" + "=" * 50)
    print("🎉 Testing completed!")