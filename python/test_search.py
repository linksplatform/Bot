#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script for Google search functionality."""

import sys
import os
sys.path.append(os.path.dirname(__file__))

from modules.commands import Commands
from regex import compile as recompile, IGNORECASE
import config

# Mock VK instance for testing
class MockVK:
    def __init__(self):
        self.messages = []
    
    def send_msg(self, message, peer_id):
        print(f"Message to {peer_id}: {message}")
        self.messages.append((message, peer_id))

# Mock data service
class MockDataService:
    pass

def test_google_search():
    """Test the Google search functionality"""
    print("Testing Google search functionality...")
    
    # Create instances
    mock_vk = MockVK()
    mock_data = MockDataService()
    commands = Commands(mock_vk, mock_data)
    
    # Test pattern matching
    GOOGLE_SEARCH = recompile(
        r'\A\s*(search|найди|поиск|google)\s+(?P<query>[\S\s]+?)\??\s*\Z', IGNORECASE)
    
    test_queries = [
        "search python list comprehension",
        "google javascript async await",
        "найди react hooks tutorial",
        "поиск how to use git",
        "search ml",  # Should be rejected (too few words)
    ]
    
    for query in test_queries:
        print(f"\n--- Testing query: '{query}' ---")
        match = GOOGLE_SEARCH.match(query)
        if match:
            print(f"Query matched: {match.group('query')}")
            
            # Set up the commands object
            commands.matched = match
            commands.peer_id = 12345
            commands.msg = query
            
            try:
                # This would make a real HTTP request in production
                # For testing, we'll just print what would happen
                query_text = match.group('query').strip()
                words = query_text.split()
                
                if len(words) < config.GOOGLE_SEARCH_MIN_WORDS:
                    print(f"❌ Query rejected: only {len(words)} words (minimum: {config.GOOGLE_SEARCH_MIN_WORDS})")
                else:
                    print(f"✅ Query accepted: {len(words)} words")
                    print(f"Would search for: '{query_text}'")
                    print(f"Whitelisted sites: {config.GOOGLE_SEARCH_WHITELISTED_SITES}")
                    
            except Exception as e:
                print(f"Error: {e}")
        else:
            print("❌ Query didn't match pattern")

if __name__ == "__main__":
    test_google_search()