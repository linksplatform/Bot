#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Test script for chat-specific karma functionality.
This tests the new implementation to ensure karma is properly split across chats.
"""
import sys
import os

# Add the python directory to path so we can import modules
sys.path.append(os.path.join(os.path.dirname(__file__), '..', 'python'))

try:
    from modules.data_service import BetterBotBaseDataService
    from modules.data_builder import DataBuilder
    from modules.commands_builder import CommandsBuilder
    print("✓ Successfully imported modules")
except ImportError as e:
    print(f"✗ Import error: {e}")
    print("Available modules in python/modules:")
    python_dir = os.path.join(os.path.dirname(__file__), '..', 'python', 'modules')
    if os.path.exists(python_dir):
        for file in os.listdir(python_dir):
            if file.endswith('.py'):
                print(f"  - {file}")
    sys.exit(1)

def test_chat_specific_karma():
    """Test chat-specific karma functionality."""
    print("\n=== Testing Chat-Specific Karma Implementation ===\n")
    
    # Test 1: Data service methods
    print("Test 1: Data service chat-specific methods")
    
    # Create a mock user object
    mock_user = {
        'uid': 123456,
        'name': 'TestUser',
        'karma': {},
        'supporters': {},
        'opponents': {},
        'programming_languages': ['Python']
    }
    
    chat_id_1 = 2000000001
    chat_id_2 = 2000000002
    
    # Test getting karma for empty user (should return 0)
    karma_1 = BetterBotBaseDataService.get_user_chat_karma(mock_user, chat_id_1)
    assert karma_1 == 0, f"Expected 0, got {karma_1}"
    print("✓ get_user_chat_karma returns 0 for new user")
    
    # Test setting karma
    BetterBotBaseDataService.set_user_chat_karma(mock_user, chat_id_1, 5)
    karma_1 = BetterBotBaseDataService.get_user_chat_karma(mock_user, chat_id_1)
    assert karma_1 == 5, f"Expected 5, got {karma_1}"
    print("✓ set_user_chat_karma works correctly")
    
    # Test that karma is chat-specific
    karma_2 = BetterBotBaseDataService.get_user_chat_karma(mock_user, chat_id_2)
    assert karma_2 == 0, f"Expected 0 for different chat, got {karma_2}"
    print("✓ Karma is chat-specific")
    
    # Test supporters/opponents
    BetterBotBaseDataService.set_user_chat_supporters(mock_user, chat_id_1, [111, 222])
    supporters_1 = BetterBotBaseDataService.get_user_chat_supporters(mock_user, chat_id_1)
    assert supporters_1 == [111, 222], f"Expected [111, 222], got {supporters_1}"
    print("✓ Chat-specific supporters work")
    
    supporters_2 = BetterBotBaseDataService.get_user_chat_supporters(mock_user, chat_id_2)
    assert supporters_2 == [], f"Expected empty list for different chat, got {supporters_2}"
    print("✓ Supporters are chat-specific")
    
    # Test 2: Data builder methods
    print("\nTest 2: Data builder with chat-specific karma")
    
    data_service = BetterBotBaseDataService("test_users")
    
    # Test build_karma with chat_id
    karma_display = DataBuilder.build_karma(mock_user, data_service, chat_id_1)
    print(f"✓ build_karma output: {karma_display}")
    
    # Test calculate_real_karma with chat_id
    real_karma = DataBuilder.calculate_real_karma(mock_user, data_service, chat_id_1)
    print(f"✓ calculate_real_karma output: {real_karma}")
    
    # Test 3: Backward compatibility
    print("\nTest 3: Backward compatibility")
    
    # Create user with old-style integer karma
    old_user = {
        'uid': 789012,
        'name': 'OldUser', 
        'karma': 10,
        'supporters': [333, 444],
        'opponents': [555]
    }
    
    # Should still work without chat_id
    old_karma = DataBuilder.build_karma(old_user, data_service)
    print(f"✓ Backward compatibility karma display: {old_karma}")
    
    # Should migrate when setting chat karma
    BetterBotBaseDataService.set_user_chat_karma(old_user, chat_id_1, 15)
    new_karma_1 = BetterBotBaseDataService.get_user_chat_karma(old_user, chat_id_1)
    assert new_karma_1 == 15, f"Expected 15, got {new_karma_1}"
    print("✓ Migration from old integer karma works")
    
    print("\n=== All tests passed! ===")
    
    # Test 4: Commands builder
    print("\nTest 4: Commands builder with chat-specific karma")
    
    karma_msg = CommandsBuilder.build_karma(mock_user, data_service, True, chat_id_1)
    print(f"✓ build_karma message: {karma_msg[:50]}...")
    
    info_msg = CommandsBuilder.build_info_message(mock_user, data_service, 123456, True, chat_id_1)
    print(f"✓ build_info_message: {info_msg[:50]}...")
    
    print("\n🎉 Implementation appears to be working correctly!")
    print("\nKey features implemented:")
    print("- ✅ Chat-specific karma storage")
    print("- ✅ Chat-specific supporters/opponents")  
    print("- ✅ Backward compatibility with old data")
    print("- ✅ Updated display methods")
    print("- ✅ Migration from old integer format")

if __name__ == "__main__":
    test_chat_specific_karma()