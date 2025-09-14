#!/usr/bin/env python3
"""
Test script to verify the top position functionality works correctly.
"""
import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), '..', 'python'))

# Mock the necessary dependencies for testing
class MockBetterUser(dict):
    pass

class MockVk:
    def get_members_ids(self, peer_id):
        return [1, 2, 3, 4, 5]  # Mock member IDs

class MockDataService:
    def get_user_property(self, user, prop):
        return user[prop]
    
    def get_user_sorted_programming_languages(self, user):
        return user.get("programming_languages", [])
    
    def get_users(self, other_keys=None, sort_key=None, reverse_sort=True):
        # Mock users data with different karma values
        mock_users = [
            {"uid": 1, "karma": 100, "supporters": [], "opponents": [], "name": "User1", "programming_languages": ["Python"], "github_profile": "user1"},
            {"uid": 2, "karma": 80, "supporters": [], "opponents": [], "name": "User2", "programming_languages": ["JavaScript"], "github_profile": "user2"},
            {"uid": 3, "karma": 60, "supporters": [], "opponents": [], "name": "User3", "programming_languages": ["Java"], "github_profile": "user3"},
            {"uid": 4, "karma": 40, "supporters": [], "opponents": [], "name": "User4", "programming_languages": ["C++"], "github_profile": "user4"},
            {"uid": 5, "karma": 20, "supporters": [], "opponents": [], "name": "User5", "programming_languages": ["Go"], "github_profile": "user5"},
        ]
        
        if sort_key:
            mock_users.sort(key=sort_key, reverse=reverse_sort)
        
        return mock_users

# Import the modules after setting up mocks
from modules.data_builder import DataBuilder
from modules.commands_builder import CommandsBuilder

def test_get_user_top_position():
    """Test that the get_user_top_position function works correctly"""
    vk_instance = MockVk()
    data_service = MockDataService()
    
    # Test user with highest karma (should be position 1)
    user1 = MockBetterUser({"uid": 1, "karma": 100, "supporters": [], "opponents": [], "name": "User1", "programming_languages": ["Python"], "github_profile": "user1"})
    position1 = DataBuilder.get_user_top_position(user1, vk_instance, data_service, 2000000001)
    print(f"User1 (karma 100) position: {position1}")
    
    # Test user with middle karma (should be position 3)
    user3 = MockBetterUser({"uid": 3, "karma": 60, "supporters": [], "opponents": [], "name": "User3", "programming_languages": ["Java"], "github_profile": "user3"})
    position3 = DataBuilder.get_user_top_position(user3, vk_instance, data_service, 2000000001)
    print(f"User3 (karma 60) position: {position3}")
    
    # Test user with lowest karma (should be position 5)
    user5 = MockBetterUser({"uid": 5, "karma": 20, "supporters": [], "opponents": [], "name": "User5", "programming_languages": ["Go"], "github_profile": "user5"})
    position5 = DataBuilder.get_user_top_position(user5, vk_instance, data_service, 2000000001)
    print(f"User5 (karma 20) position: {position5}")
    
    # Test non-existent user (should be position 0)
    user_fake = MockBetterUser({"uid": 999, "karma": 50, "supporters": [], "opponents": [], "name": "FakeUser", "programming_languages": ["Python"], "github_profile": "fake"})
    position_fake = DataBuilder.get_user_top_position(user_fake, vk_instance, data_service, 2000000001)
    print(f"Fake user position: {position_fake}")

def test_build_info_message():
    """Test that the build_info_message includes top position"""
    vk_instance = MockVk()
    data_service = MockDataService()
    
    user2 = MockBetterUser({"uid": 2, "karma": 80, "supporters": [], "opponents": [], "name": "User2", "programming_languages": ["JavaScript"], "github_profile": "user2"})
    
    # Test with karma enabled and group chat
    message = CommandsBuilder.build_info_message(
        user2, data_service, 2, True, vk_instance, 2000000001
    )
    print("Info message with top position:")
    print(message)
    print()
    
    # Test without karma
    message_no_karma = CommandsBuilder.build_info_message(
        user2, data_service, 2, False, vk_instance, 2000000001
    )
    print("Info message without karma:")
    print(message_no_karma)

if __name__ == "__main__":
    print("Testing top position functionality...")
    test_get_user_top_position()
    print()
    test_build_info_message()
    print("All tests completed!")