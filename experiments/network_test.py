#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Network connectivity test script to simulate connection issues.

This script helps understand how the current VK bot handles network disconnections
and connection failures.
"""
import sys
import os
import time
import signal
import threading
from unittest.mock import patch, MagicMock

# Add python module path to import Bot
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'python'))

try:
    import requests
    from saya import Vk
    from python.__main__ import Bot
    from python.tokens import BOT_TOKEN
    import python.config as config
except ImportError as e:
    print(f"Import error: {e}")
    print("This is expected in the isolated test environment")
    sys.exit(1)


class NetworkTestBot(Bot):
    """Test bot with network failure simulation."""
    
    def __init__(self, *args, **kwargs):
        self.connection_lost_count = 0
        self.reconnection_attempts = 0
        self.max_reconnection_attempts = 5
        self.reconnection_delay = 2  # seconds
        super().__init__(*args, **kwargs)
    
    def call_method(self, method, params=None):
        """Override call_method to simulate network failures."""
        try:
            return super().call_method(method, params)
        except (requests.exceptions.ConnectionError, 
                requests.exceptions.Timeout,
                requests.exceptions.HTTPError) as e:
            print(f"Network error detected: {type(e).__name__}: {e}")
            self.connection_lost_count += 1
            return self._handle_network_error(method, params, e)
    
    def _handle_network_error(self, method, params, error):
        """Handle network errors with reconnection logic."""
        if self.reconnection_attempts >= self.max_reconnection_attempts:
            print(f"Max reconnection attempts ({self.max_reconnection_attempts}) reached. Giving up.")
            raise error
        
        self.reconnection_attempts += 1
        print(f"Attempting reconnection {self.reconnection_attempts}/{self.max_reconnection_attempts}")
        
        # Exponential backoff
        delay = self.reconnection_delay * (2 ** (self.reconnection_attempts - 1))
        print(f"Waiting {delay} seconds before retry...")
        time.sleep(delay)
        
        try:
            result = super().call_method(method, params)
            print(f"Reconnection successful on attempt {self.reconnection_attempts}")
            self.reconnection_attempts = 0  # Reset on success
            return result
        except Exception as e:
            print(f"Reconnection attempt {self.reconnection_attempts} failed: {e}")
            return self._handle_network_error(method, params, e)


def simulate_network_failure():
    """Simulate network failures by patching requests."""
    
    def failing_request(*args, **kwargs):
        """Mock function that always raises connection error."""
        raise requests.exceptions.ConnectionError("Simulated network failure")
    
    # Patch requests to simulate network failure
    with patch.object(requests.Session, 'post', side_effect=failing_request):
        with patch.object(requests.Session, 'get', side_effect=failing_request):
            print("Network failure simulation active")
            
            try:
                # Create test bot instance
                bot = NetworkTestBot(token=BOT_TOKEN, group_id=config.BOT_GROUP_ID, debug=True)
                
                # Test method call that should fail
                print("Testing API call with simulated network failure...")
                result = bot.call_method('users.get', {'user_ids': 1})
                print(f"Unexpected success: {result}")
                
            except Exception as e:
                print(f"Final error after all retry attempts: {type(e).__name__}: {e}")


def test_current_bot_resilience():
    """Test how the current bot handles network issues."""
    print("Testing current bot network resilience...")
    
    # Mock network issues
    def intermittent_failure(*args, **kwargs):
        """Randomly fail some requests."""
        import random
        if random.random() < 0.7:  # 70% failure rate
            raise requests.exceptions.ConnectionError("Intermittent network failure")
        return MagicMock()
    
    with patch.object(requests.Session, 'post', side_effect=intermittent_failure):
        try:
            bot = Bot(token=BOT_TOKEN, group_id=config.BOT_GROUP_ID, debug=True)
            
            # Test multiple calls
            for i in range(5):
                try:
                    print(f"Attempt {i+1}: Making API call...")
                    result = bot.call_method('users.get', {'user_ids': 1})
                    print(f"Success: {result}")
                except Exception as e:
                    print(f"Failed: {type(e).__name__}: {e}")
                time.sleep(1)
                
        except Exception as e:
            print(f"Bot initialization failed: {e}")


if __name__ == '__main__':
    print("=== VK Bot Network Resilience Test ===")
    print("This script tests how the bot handles network disconnections and failures.")
    print()
    
    print("1. Testing with simulated complete network failure:")
    simulate_network_failure()
    print()
    
    print("2. Testing with intermittent network failures:")
    test_current_bot_resilience()
    print()
    
    print("Test completed.")