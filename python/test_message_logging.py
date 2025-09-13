#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script for message logging functionality."""

import unittest
from unittest.mock import Mock, patch, call
from datetime import datetime
import sys
import os

# Add the current directory to the path to import modules
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

# Import Bot class by importing the main module
import importlib.util
import sys

# Load the main module
spec = importlib.util.spec_from_file_location("main", "__main__.py")
main_module = importlib.util.module_from_spec(spec)
sys.modules["main"] = main_module
spec.loader.exec_module(main_module)

Bot = main_module.Bot
import config


class TestMessageLogging(unittest.TestCase):
    """Test cases for message logging functionality."""

    def setUp(self):
        """Set up test fixtures."""
        # Mock the BOT_TOKEN to avoid requiring real token for tests
        with patch('__main__.BOT_TOKEN', 'test_token'):
            self.bot = Bot(token='test_token', group_id=12345, debug=False)
        
        # Mock the VK API methods
        self.bot.call_method = Mock()
        self.bot.send_msg = Mock()
        self.bot.get_user_name = Mock(return_value="Test User")

    def test_logging_disabled_when_no_config(self):
        """Test that logging is disabled when configuration is missing."""
        # Backup original config
        original_logging_chat = config.LOGGING_CHAT_ID
        original_main_chats = config.MAIN_CHATS
        
        try:
            # Test with no logging chat configured
            config.LOGGING_CHAT_ID = None
            config.MAIN_CHATS = [2000000001]
            
            event = {
                "text": "Test message",
                "date": 1640995200,
                "attachments": [],
                "fwd_messages": [],
                "reply_message": {}
            }
            
            self.bot._forward_message_to_logging_chat(event, 2000000001, 12345)
            
            # Should not call send_msg
            self.bot.send_msg.assert_not_called()
            
            # Test with no main chats configured
            config.LOGGING_CHAT_ID = 2000000020
            config.MAIN_CHATS = []
            
            self.bot._forward_message_to_logging_chat(event, 2000000001, 12345)
            
            # Should still not call send_msg
            self.bot.send_msg.assert_not_called()
            
        finally:
            # Restore original config
            config.LOGGING_CHAT_ID = original_logging_chat
            config.MAIN_CHATS = original_main_chats

    def test_message_not_forwarded_from_non_main_chat(self):
        """Test that messages from non-main chats are not forwarded."""
        # Backup original config
        original_logging_chat = config.LOGGING_CHAT_ID
        original_main_chats = config.MAIN_CHATS
        
        try:
            config.LOGGING_CHAT_ID = 2000000020
            config.MAIN_CHATS = [2000000001]  # Only this chat should be forwarded
            
            event = {
                "text": "Test message",
                "date": 1640995200,
                "attachments": [],
                "fwd_messages": [],
                "reply_message": {}
            }
            
            # Send from a chat not in MAIN_CHATS
            self.bot._forward_message_to_logging_chat(event, 2000000002, 12345)
            
            # Should not call send_msg
            self.bot.send_msg.assert_not_called()
            
        finally:
            # Restore original config
            config.LOGGING_CHAT_ID = original_logging_chat
            config.MAIN_CHATS = original_main_chats

    def test_bot_messages_not_forwarded(self):
        """Test that bot messages (negative from_id) are not forwarded."""
        # Backup original config
        original_logging_chat = config.LOGGING_CHAT_ID
        original_main_chats = config.MAIN_CHATS
        
        try:
            config.LOGGING_CHAT_ID = 2000000020
            config.MAIN_CHATS = [2000000001]
            
            event = {
                "text": "Bot message",
                "date": 1640995200,
                "attachments": [],
                "fwd_messages": [],
                "reply_message": {}
            }
            
            # Send from bot (negative from_id)
            self.bot._forward_message_to_logging_chat(event, 2000000001, -12345)
            
            # Should not call send_msg
            self.bot.send_msg.assert_not_called()
            
        finally:
            # Restore original config
            config.LOGGING_CHAT_ID = original_logging_chat
            config.MAIN_CHATS = original_main_chats

    def test_message_forwarding_basic(self):
        """Test basic message forwarding functionality."""
        # Backup original config
        original_logging_chat = config.LOGGING_CHAT_ID
        original_main_chats = config.MAIN_CHATS
        
        try:
            config.LOGGING_CHAT_ID = 2000000020
            config.MAIN_CHATS = [2000000001]
            
            event = {
                "text": "Hello, world!",
                "date": 1640995200,  # 2022-01-01 00:00:00
                "attachments": [],
                "fwd_messages": [],
                "reply_message": {}
            }
            
            self.bot._get_chat_title = Mock(return_value="Test Chat")
            
            self.bot._forward_message_to_logging_chat(event, 2000000001, 12345)
            
            # Should call send_msg with formatted message
            self.bot.send_msg.assert_called_once()
            call_args = self.bot.send_msg.call_args
            sent_message = call_args[0][0]  # First positional argument
            sent_to_chat = call_args[0][1]  # Second positional argument
            
            # Check that message was sent to logging chat
            self.assertEqual(sent_to_chat, 2000000020)
            
            # Check message format
            self.assertIn("Test Chat", sent_message)
            self.assertIn("Test User", sent_message)
            self.assertIn("Hello, world!", sent_message)
            self.assertIn("2022-01-01", sent_message)
            
        finally:
            # Restore original config
            config.LOGGING_CHAT_ID = original_logging_chat
            config.MAIN_CHATS = original_main_chats

    def test_message_with_attachments(self):
        """Test message forwarding with attachments."""
        # Backup original config
        original_logging_chat = config.LOGGING_CHAT_ID
        original_main_chats = config.MAIN_CHATS
        
        try:
            config.LOGGING_CHAT_ID = 2000000020
            config.MAIN_CHATS = [2000000001]
            
            event = {
                "text": "Check this out!",
                "date": 1640995200,
                "attachments": [
                    {"type": "photo"},
                    {"type": "doc"}
                ],
                "fwd_messages": [],
                "reply_message": {}
            }
            
            self.bot._get_chat_title = Mock(return_value="Test Chat")
            
            self.bot._forward_message_to_logging_chat(event, 2000000001, 12345)
            
            # Should call send_msg
            self.bot.send_msg.assert_called_once()
            call_args = self.bot.send_msg.call_args
            sent_message = call_args[0][0]
            
            # Check that attachments are mentioned
            self.assertIn("Attachments:", sent_message)
            self.assertIn("[photo]", sent_message)
            self.assertIn("[doc]", sent_message)
            
        finally:
            # Restore original config
            config.LOGGING_CHAT_ID = original_logging_chat
            config.MAIN_CHATS = original_main_chats


if __name__ == '__main__':
    unittest.main()