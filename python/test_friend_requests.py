#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script for friend request auto-acceptance functionality.
"""

from userbot import UserBot
from tokens import USER_TOKEN
import time
import sys

def main():
    """Test the friend request functionality."""
    print("VK User Bot - Friend Request Test")
    print("=" * 40)
    
    # Check if USER_TOKEN is configured
    if not USER_TOKEN:
        print("ERROR: USER_TOKEN is not set in tokens.py")
        print("Please configure your VK user token to test friend request functionality.")
        print("You can get a user token from:")
        print("https://oauth.vk.com/authorize?client_id=2685278&scope=1073737727&redirect_uri=https://api.vk.com/blank.html&display=page&response_type=token&revoke=1")
        sys.exit(1)
    
    userbot = UserBot()
    
    print("1. Testing friend request retrieval...")
    pending_requests = userbot.get_friend_requests()
    
    if pending_requests is None:
        print("ERROR: Failed to retrieve friend requests (check token permissions)")
        return
    elif pending_requests == []:
        print("✓ No pending friend requests found")
    else:
        print(f"✓ Found {len(pending_requests)} pending friend request(s): {pending_requests}")
    
    print("\n2. Testing manual check and accept...")
    userbot.check_and_accept_friend_requests()
    
    print("\n3. Testing monitoring functionality...")
    print("Starting friend request monitor for 30 seconds (check interval: 10 seconds)")
    
    userbot.start_friend_request_monitor(check_interval=10)
    
    try:
        # Let it run for 30 seconds
        time.sleep(30)
    except KeyboardInterrupt:
        print("\nInterrupted by user")
    
    print("\nStopping friend request monitor...")
    userbot.stop_friend_request_monitor()
    
    print("\n✓ Test completed successfully!")
    print("\nTo enable friend request auto-acceptance in the main bot:")
    print("1. Set USER_TOKEN in tokens.py")
    print("2. Set FRIEND_REQUEST_AUTO_ACCEPT = True in config.py")
    print("3. Run the bot with: python3 __main__.py")

if __name__ == '__main__':
    main()