#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Example demonstrating the network resilience features of the VK Bot.

This example shows how the enhanced bot handles network disconnections,
timeouts, and connection recovery automatically.
"""

import sys
import os
import time

# Add python module path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'python'))

try:
    from network_handler import NetworkHandler
    from __main__ import Bot
    from tokens import BOT_TOKEN
    import config
except ImportError as e:
    print(f"Import error: {e}")
    print("This example requires the bot dependencies to be installed.")
    sys.exit(1)


def demonstrate_network_resilience():
    """Demonstrate network resilience features."""
    
    print("=== VK Bot Network Resilience Demo ===")
    print()
    
    # Create bot instance with network resilience
    print("1. Creating bot with network resilience...")
    bot = Bot(token=BOT_TOKEN, group_id=config.BOT_GROUP_ID, debug=True)
    print("✓ Bot created with automatic network error handling")
    print()
    
    # Show network handler configuration
    handler = bot.network_handler
    print("2. Network Handler Configuration:")
    print(f"   - Max retries: {handler.max_retries}")
    print(f"   - Backoff factor: {handler.backoff_factor}")
    print(f"   - Connection timeout: {handler.connection_timeout}s")
    print(f"   - Read timeout: {handler.read_timeout}s")
    print(f"   - Health check interval: {handler.health_check_interval}s")
    print()
    
    # Show connection statistics
    print("3. Connection Statistics:")
    stats = handler.get_connection_stats()
    print(f"   - Connected: {stats['is_connected']}")
    print(f"   - Connection failures: {stats['connection_failures']}")
    print(f"   - Last successful request: {stats['last_successful_request']}")
    print(f"   - Time since last success: {stats['time_since_last_success']:.1f}s")
    print()
    
    # Test API call
    print("4. Testing API call with network resilience...")
    try:
        # This call will automatically retry on network errors
        result = bot.call_method('users.get', {'user_ids': 1})
        if 'response' in result:
            user = result['response'][0]
            print(f"✓ API call successful: User ID {user.get('id')} retrieved")
        elif 'error' in result:
            print(f"⚠ VK API error: {result['error']['error_msg']}")
        else:
            print(f"? Unexpected response: {result}")
    except Exception as e:
        print(f"✗ Network error (after all retries): {e}")
    print()
    
    # Monitor connection for a short time
    print("5. Monitoring connection health for 10 seconds...")
    start_time = time.time()
    while time.time() - start_time < 10:
        time.sleep(2)
        if handler.is_connected():
            print("   ✓ Connection healthy")
        else:
            print("   ⚠ Connection issues detected")
    print()
    
    print("6. Features provided by network resilience:")
    print("   ✓ Automatic retry on connection errors")
    print("   ✓ Exponential backoff for retry delays")
    print("   ✓ Background health check monitoring")
    print("   ✓ Connection statistics tracking")
    print("   ✓ Detailed error logging")
    print("   ✓ Graceful handling of network changes")
    print()
    
    print("Demo completed. The bot is now resilient to network issues!")


if __name__ == '__main__':
    demonstrate_network_resilience()