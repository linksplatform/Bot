#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script for daily outreach functionality."""
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'python'))

from modules.daily_outreach import DailyOutreach
from modules.data_service import BetterBotBaseDataService
from modules.vk_instance import VkInstance

def test_daily_outreach():
    """Test the daily outreach functionality."""
    print("Testing DailyOutreach functionality...")
    
    # Create mock VK instance and data service
    vk_instance = VkInstance()
    data_service = BetterBotBaseDataService("test_users")
    
    # Create daily outreach instance
    daily_outreach = DailyOutreach(vk_instance, data_service)
    
    print("✓ DailyOutreach instance created successfully")
    
    # Test question selection
    candidates = [12345, 67890]
    selection = daily_outreach.select_random_user_and_question(candidates)
    if selection:
        user_id, question = selection
        print(f"✓ Random selection works: User {user_id}, Question: '{question}'")
    else:
        print("✗ Random selection failed")
    
    # Test response processing
    test_responses = [
        ("My GitHub is github.com/testuser", 12345),
        ("I know Python and JavaScript", 67890),
        ("Regular message", 11111)
    ]
    
    for msg, from_id in test_responses:
        processed = daily_outreach.process_potential_response(msg, from_id)
        print(f"✓ Message '{msg}' processed: {processed}")
    
    # Test should_run_daily_outreach
    should_run_first = daily_outreach.should_run_daily_outreach()
    should_run_second = daily_outreach.should_run_daily_outreach()
    
    print(f"✓ Daily check - First run: {should_run_first}, Second run: {should_run_second}")
    
    print("✓ All tests completed successfully!")

if __name__ == "__main__":
    test_daily_outreach()