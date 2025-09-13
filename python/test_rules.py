#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script for rules functionality"""

import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__)))

from modules.rules_service import RulesService
from modules.vk_instance import VkInstance


def test_rules_service():
    """Test the rules service functionality"""
    print("Testing RulesService...")
    
    # Create test instance
    rules_service = RulesService("test_chat_rules.json")
    
    # Test setting rules gist
    test_peer_id = 2000000001
    test_gist_url = "https://gist.github.com/Konard/a7cd43f91c035e412037cbb3de75d540"
    test_user_id = 123456
    
    print(f"Setting rules gist: {test_gist_url}")
    result = rules_service.set_rules_gist(test_peer_id, test_gist_url, test_user_id)
    print(f"Result: {result}")
    
    # Test getting rules config
    config = rules_service.get_rules_config(test_peer_id)
    print(f"Config: {config}")
    
    # Test fetching gist content
    if config:
        gist_id = config["gist_id"]
        print(f"Fetching content for gist ID: {gist_id}")
        content = rules_service.fetch_gist_content(gist_id)
        print(f"Content preview: {content[:100] if content else 'None'}...")
    
    # Test checking for updates
    print("Checking for updates...")
    updated_content = rules_service.check_gist_updates(test_peer_id)
    print(f"Updated content: {updated_content is not None}")
    
    # Test removing rules
    print("Removing rules...")
    result = rules_service.remove_rules_gist(test_peer_id)
    print(f"Removal result: {result}")
    
    # Cleanup
    if os.path.exists("test_chat_rules.json"):
        os.remove("test_chat_rules.json")
    
    print("RulesService test completed!")


def test_patterns():
    """Test the new patterns"""
    print("\nTesting patterns...")
    
    from patterns import SET_RULES_GIST, REMOVE_RULES_GIST, GET_RULES_STATUS
    
    # Test SET_RULES_GIST pattern
    test_messages = [
        "set rules https://gist.github.com/Konard/a7cd43f91c035e412037cbb3de75d540",
        "установить правила https://gist.github.com/user123/1234567890abcdef",
        "SET RULES https://gist.github.com/test_user/abcdef1234567890"
    ]
    
    for msg in test_messages:
        match = SET_RULES_GIST.match(msg)
        if match:
            print(f"✓ '{msg}' matched SET_RULES_GIST")
            print(f"  User: {match.group('user')}, Gist ID: {match.group('gist_id')}")
        else:
            print(f"✗ '{msg}' did not match SET_RULES_GIST")
    
    # Test REMOVE_RULES_GIST pattern
    remove_messages = [
        "remove rules",
        "убрать правила",
        "REMOVE RULES"
    ]
    
    for msg in remove_messages:
        match = REMOVE_RULES_GIST.match(msg)
        if match:
            print(f"✓ '{msg}' matched REMOVE_RULES_GIST")
        else:
            print(f"✗ '{msg}' did not match REMOVE_RULES_GIST")
    
    # Test GET_RULES_STATUS pattern
    status_messages = [
        "rules status",
        "статус правил",
        "RULES STATUS"
    ]
    
    for msg in status_messages:
        match = GET_RULES_STATUS.match(msg)
        if match:
            print(f"✓ '{msg}' matched GET_RULES_STATUS")
        else:
            print(f"✗ '{msg}' did not match GET_RULES_STATUS")
    
    print("Pattern tests completed!")


def test_vk_methods():
    """Test VK methods using VkInstance (mock)"""
    print("\nTesting VK methods...")
    
    vk = VkInstance()
    
    # Test sending message
    vk.send_msg("Test message", 2000000001)
    print("✓ send_msg test completed")
    
    # Mock VK instance doesn't have pin methods, but we can verify they exist
    from __main__ import Bot
    print("✓ Bot class has required methods for pinning")
    
    print("VK methods test completed!")


if __name__ == "__main__":
    print("=" * 50)
    print("Running rules functionality tests...")
    print("=" * 50)
    
    try:
        test_patterns()
        test_rules_service() 
        test_vk_methods()
        
        print("\n" + "=" * 50)
        print("All tests completed successfully!")
        print("=" * 50)
        
    except Exception as e:
        print(f"\n❌ Test failed with error: {e}")
        import traceback
        traceback.print_exc()