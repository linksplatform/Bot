#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script for off-topic detection functionality."""
import sys
import os

# Add parent directory to path to import modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'python'))

from modules.off_topic_detection import OffTopicDetector
import config

def test_off_topic_detection():
    """Test the off-topic detection with various messages."""
    detector = OffTopicDetector()
    
    # Test messages
    test_cases = [
        # Programming-related messages (should NOT be off-topic)
        ("How do I implement a binary search tree in Python?", False),
        ("What is the difference between let and var in JavaScript?", False),
        ("How to fix segmentation fault in C++?", False),
        ("React hooks vs class components", False),
        ("Django ORM query optimization", False),
        ("Git merge vs rebase", False),
        
        # Non-programming messages (should be off-topic)
        ("What's the weather like today?", True),
        ("I love pizza and pasta", True),
        ("The movie was amazing last night", True),
        ("My cat is sleeping on my keyboard", True),
        ("Football match results yesterday", True),
        
        # Edge cases
        ("hi", False),  # Too short, should not trigger
        ("hello there", False),  # Too short, should not trigger
        ("Python", False),  # Single word, too short
        ("How are you doing today", True),  # Generic greeting, likely off-topic
    ]
    
    print("Testing off-topic detection functionality...")
    print("=" * 60)
    
    for message, expected_off_topic in test_cases:
        print(f"\nTesting: '{message}'")
        print(f"Expected off-topic: {expected_off_topic}")
        
        try:
            is_off_topic, reason = detector.is_off_topic(message)
            print(f"Detected off-topic: {is_off_topic}")
            print(f"Reason: {reason}")
            
            # Check if result matches expectation
            if is_off_topic == expected_off_topic:
                print("✅ PASS")
            else:
                print("❌ FAIL - Detection result doesn't match expectation")
                
        except Exception as e:
            print(f"❌ ERROR: {e}")
        
        print("-" * 40)
    
    print("\nTesting configuration...")
    print(f"Detection enabled: {config.OFF_TOPIC_DETECTION_ENABLED}")
    print(f"Minimum words: {config.OFF_TOPIC_MIN_WORDS}")
    print(f"Whitelist sites count: {len(config.PROGRAMMING_WEBSITES_WHITELIST)}")
    print(f"Sample whitelist sites: {config.PROGRAMMING_WEBSITES_WHITELIST[:5]}")

def test_google_search():
    """Test Google search functionality separately."""
    detector = OffTopicDetector()
    
    print("\nTesting Google search functionality...")
    print("=" * 60)
    
    test_queries = [
        "Python list comprehension",
        "JavaScript async await",
        "weather forecast"
    ]
    
    for query in test_queries:
        print(f"\nSearching for: '{query}'")
        try:
            urls = detector.google_search(query, max_results=5)
            print(f"Found {len(urls)} URLs:")
            for url in urls[:3]:  # Show first 3
                print(f"  - {url}")
            
            # Check programming websites
            is_programming, domains = detector.check_programming_websites(urls)
            print(f"Programming-related: {is_programming}")
            if domains:
                print(f"Matching domains: {domains}")
                
        except Exception as e:
            print(f"❌ ERROR: {e}")
        
        print("-" * 40)

if __name__ == "__main__":
    print("Off-topic Detection Test Suite")
    print("=" * 60)
    
    # Test individual components
    test_google_search()
    
    # Test full detection
    test_off_topic_detection()
    
    print("\nTest completed!")