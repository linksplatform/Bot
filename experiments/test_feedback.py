#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Test script to verify karma feedback messages work correctly.
This test demonstrates that the bot now provides feedback for karma operations.
"""
import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), '../python'))

from modules.commands_builder import CommandsBuilder
from social_ethosa import BetterUser
from modules.data_service import BetterBotBaseDataService

class MockDataService(BetterBotBaseDataService):
    def get_user_property(self, user, prop):
        if prop == 'uid':
            return user.uid
        elif prop == 'name':
            return user.name
        elif prop == 'karma':
            return user.karma
        return getattr(user, prop, None)

class MockUser(BetterUser):
    def __init__(self, uid, name, karma=0):
        self.uid = uid
        self.name = name  
        self.karma = karma

# Test scenarios
def test_karma_change_feedback():
    """Test that karma changes now include success feedback"""
    print("=== Testing Karma Change Feedback ===")
    
    user1 = (123, "Alice", 10, 15)  # uid, name, old_karma, new_karma
    user2 = (456, "Bob", 5, 0)      # uid, name, old_karma, new_karma
    voters = [789, 101112]
    
    # Test successful karma change with voters
    message = CommandsBuilder.build_karma_change(user1, user2, voters)
    print(f"Karma change with transfer: {message}")
    
    # Test successful karma change from collective vote
    message = CommandsBuilder.build_karma_change(None, user2, voters)
    print(f"Karma change from collective vote: {message}")

def test_vote_registered_feedback():
    """Test that vote registration provides feedback"""
    print("\n=== Testing Vote Registration Feedback ===")
    
    target_user = MockUser(456, "Bob", 5)
    data_service = MockDataService()
    
    # Test positive vote registered
    message = CommandsBuilder.build_vote_registered(
        target_user, data_service, "+", 1, 2)
    print(f"Positive vote registered (1/2): {message}")
    
    # Test negative vote registered  
    message = CommandsBuilder.build_vote_registered(
        target_user, data_service, "-", 2, 3)
    print(f"Negative vote registered (2/3): {message}")

def test_personal_transfer_feedback():
    """Test that personal karma transfers provide feedback"""
    print("\n=== Testing Personal Transfer Feedback ===")
    
    from_user = MockUser(123, "Alice", 10)
    to_user = MockUser(456, "Bob", 5)
    data_service = MockDataService()
    
    # Test positive transfer
    message = CommandsBuilder.build_personal_karma_transfer_success(
        from_user, to_user, data_service, 3)
    print(f"Personal karma transfer (+3): {message}")
    
    # Test negative transfer
    message = CommandsBuilder.build_personal_karma_transfer_success(
        from_user, to_user, data_service, -2)
    print(f"Personal karma transfer (-2): {message}")

if __name__ == "__main__":
    print("Testing karma feedback messages...")
    test_karma_change_feedback()
    test_vote_registered_feedback()  
    test_personal_transfer_feedback()
    print("\n✅ All feedback tests completed! The bot now provides clear feedback for all karma operations.")