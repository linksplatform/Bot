#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script for the optional sorting feature."""

import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))) + "/python")

from modules.data_service import BetterBotBaseDataService
from modules.data_builder import DataBuilder
from modules.commands_builder import CommandsBuilder

def test_sorting_feature():
    """Test the optional sorting feature implementation."""
    print("Testing optional sorting feature...")
    
    # Create a test data service
    data_service = BetterBotBaseDataService("test_users")
    
    # Create a mock user with some programming languages
    class MockUser:
        def __init__(self):
            self.uid = 12345
            self.name = "Test User"
            self.programming_languages = ["python", "c++", "javascript", "rust"]
            self.programming_languages_sorted = True
            self.github_profile = ""
            self.karma = 0
            self.supporters = []
            self.opponents = []
            
        def __getitem__(self, key):
            return getattr(self, key)
            
        def __setitem__(self, key, value):
            setattr(self, key, value)
    
    user = MockUser()
    
    # Test 1: Default sorting (should be True)
    print("\nTest 1: Default sorting behavior")
    languages_sorted = DataBuilder.build_programming_languages(user, data_service)
    print(f"With sorting enabled: {languages_sorted}")
    
    # Test 2: Disable sorting
    print("\nTest 2: Disable sorting")
    user.programming_languages_sorted = False
    languages_unsorted = DataBuilder.build_programming_languages(user, data_service)
    print(f"With sorting disabled: {languages_unsorted}")
    
    # Test 3: Enable sorting again
    print("\nTest 3: Enable sorting again")  
    user.programming_languages_sorted = True
    languages_sorted_again = DataBuilder.build_programming_languages(user, data_service)
    print(f"With sorting enabled again: {languages_sorted_again}")
    
    # Test 4: Test sorting preference change message
    print("\nTest 4: Testing sorting preference message")
    enable_message = CommandsBuilder.build_sorting_preference_changed(user, data_service, "включена")
    print(f"Enable message: {enable_message}")
    
    disable_message = CommandsBuilder.build_sorting_preference_changed(user, data_service, "выключена")
    print(f"Disable message: {disable_message}")
    
    print("\n✅ All tests completed!")
    
    # Verify that sorting actually makes a difference
    if languages_sorted != languages_unsorted:
        print("✅ Sorting preference correctly affects output")
    else:
        print("❌ Sorting preference doesn't affect output - check implementation")

if __name__ == "__main__":
    test_sorting_feature()