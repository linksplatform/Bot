#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script to verify automatic votes deletion can be disabled (issue #52)"""

import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '../python'))

from modules import BetterBotBaseDataService, Commands
from modules.vk_instance import VkInstance
from social_ethosa import BetterUser
import config

def test_votes_deletion_disabled():
    """Test that votes are not automatically deleted when DISABLE_AUTOMATIC_VOTES_DELETION is True"""
    print("Testing automatic votes deletion behavior...")
    
    # Create test database
    db = BetterBotBaseDataService('test_votes_deletion_db')
    
    # Create mock VK instance
    class MockVkInstance:
        def __init__(self):
            self.deleted_messages = []
            
        def delete_message(self, peer_id, msg_id):
            self.deleted_messages.append((peer_id, msg_id))
            print(f"Message deletion attempt: peer_id={peer_id}, msg_id={msg_id}")
            
        def send_msg(self, msg, peer_id):
            print(f"Sending message to {peer_id}: {msg}")
            
        def get_user_name(self, user_id, case=None):
            return f"User{user_id}"
    
    vk_instance = MockVkInstance()
    commands = Commands(vk_instance, db)
    
    # Create test users
    user1 = db.get_or_create_user(1, None)
    user2 = db.get_or_create_user(2, None)
    user3 = db.get_or_create_user(3, None)
    
    user1.karma = 10
    user2.karma = 5
    user3.karma = 3
    
    db.save_user(user1)
    db.save_user(user2)
    db.save_user(user3)
    
    commands.current_user = user1
    commands.user = user2
    commands.peer_id = 2000000001
    commands.msg_id = 12345
    
    print(f"DISABLE_AUTOMATIC_VOTES_DELETION is set to: {config.DISABLE_AUTOMATIC_VOTES_DELETION}")
    
    # Test 1: Apply collective vote - votes should NOT be cleared when disabled
    print("\nTest 1: Applying collective votes (should not be cleared automatically)")
    initial_supporters = user2.supporters.copy() if hasattr(user2, 'supporters') else []
    
    # Add first supporter
    result1 = commands.apply_collective_vote("supporters", config.POSITIVE_VOTES_PER_KARMA, +1)
    print(f"After first vote - supporters: {user2.supporters}")
    
    # Add second supporter (should trigger karma change but not clear votes if disabled)  
    commands.current_user = user3
    result2 = commands.apply_collective_vote("supporters", config.POSITIVE_VOTES_PER_KARMA, +1)
    print(f"After second vote - supporters: {user2.supporters}")
    
    if config.DISABLE_AUTOMATIC_VOTES_DELETION:
        # When disabled, votes should NOT be cleared
        assert len(user2.supporters) == 2, f"Expected 2 supporters, got {len(user2.supporters)}"
        print("✓ Test 1 PASSED: Votes were not automatically deleted")
    else:
        # When enabled, votes should be cleared  
        assert len(user2.supporters) == 0, f"Expected 0 supporters, got {len(user2.supporters)}"
        print("✓ Test 1 PASSED: Votes were automatically deleted (default behavior)")
    
    # Test 2: Message deletion behavior
    print(f"\nTest 2: Message deletion behavior")
    print(f"Messages deleted: {len(vk_instance.deleted_messages)}")
    
    if config.DISABLE_AUTOMATIC_VOTES_DELETION:
        print("✓ Test 2 INFO: Message deletion is disabled")
    else:
        print("✓ Test 2 INFO: Message deletion is enabled")
    
    print("\n✅ All tests completed successfully!")
    print(f"Final supporters count: {len(user2.supporters)}")
    print(f"User2 karma change: {user2.karma}")

if __name__ == "__main__":
    test_votes_deletion_disabled()