#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Simple test script for rules patterns only"""

from regex import compile as recompile, IGNORECASE


def test_patterns():
    """Test the new patterns without external dependencies"""
    print("Testing patterns...")
    
    # Define patterns directly (from patterns.py)
    SET_RULES_GIST = recompile(
        r'\A\s*(set rules|установить правила)\s+(https://gist\.github\.com/(?P<user>[a-zA-Z0-9\-_]+)/(?P<gist_id>[a-f0-9]+))\s*\Z', IGNORECASE)

    REMOVE_RULES_GIST = recompile(
        r'\A\s*(remove rules|убрать правила)\s*\Z', IGNORECASE)

    GET_RULES_STATUS = recompile(
        r'\A\s*(rules status|статус правил)\s*\Z', IGNORECASE)
    
    # Test SET_RULES_GIST pattern
    test_messages = [
        "set rules https://gist.github.com/Konard/a7cd43f91c035e412037cbb3de75d540",
        "установить правила https://gist.github.com/user123/1234567890abcdef",
        "SET RULES https://gist.github.com/test_user/abcdef1234567890",
        "set rules https://gist.github.com/test-user/123abc456def789"
    ]
    
    print("\nTesting SET_RULES_GIST:")
    for msg in test_messages:
        match = SET_RULES_GIST.match(msg)
        if match:
            print(f"✓ '{msg}' matched")
            print(f"  User: {match.group('user')}, Gist ID: {match.group('gist_id')}")
        else:
            print(f"✗ '{msg}' did not match")
    
    # Test REMOVE_RULES_GIST pattern
    remove_messages = [
        "remove rules",
        "убрать правила", 
        "REMOVE RULES",
        "Remove Rules"
    ]
    
    print("\nTesting REMOVE_RULES_GIST:")
    for msg in remove_messages:
        match = REMOVE_RULES_GIST.match(msg)
        if match:
            print(f"✓ '{msg}' matched")
        else:
            print(f"✗ '{msg}' did not match")
    
    # Test GET_RULES_STATUS pattern  
    status_messages = [
        "rules status",
        "статус правил",
        "RULES STATUS", 
        "Rules Status"
    ]
    
    print("\nTesting GET_RULES_STATUS:")
    for msg in status_messages:
        match = GET_RULES_STATUS.match(msg)
        if match:
            print(f"✓ '{msg}' matched")
        else:
            print(f"✗ '{msg}' did not match")
    
    print("\nPattern tests completed!")


def test_github_api():
    """Test GitHub API access"""
    print("\nTesting GitHub API access...")
    
    import requests
    
    # Test with the gist from the issue
    gist_id = "a7cd43f91c035e412037cbb3de75d540"
    url = f"https://api.github.com/gists/{gist_id}"
    
    try:
        response = requests.get(url, timeout=10)
        print(f"API Response Status: {response.status_code}")
        
        if response.status_code == 200:
            data = response.json()
            files = data.get('files', {})
            if files:
                first_file = next(iter(files.values()))
                content = first_file.get('content', '')
                print(f"✓ Successfully fetched gist content ({len(content)} characters)")
                print(f"Content preview: {content[:100]}...")
            else:
                print("✗ No files found in gist")
        else:
            print(f"✗ Failed to fetch gist: {response.status_code}")
            
    except Exception as e:
        print(f"✗ Error testing GitHub API: {e}")
    
    print("GitHub API test completed!")


if __name__ == "__main__":
    print("=" * 50)
    print("Running simplified rules functionality tests...")
    print("=" * 50)
    
    try:
        test_patterns()
        test_github_api()
        
        print("\n" + "=" * 50)
        print("All tests completed!")
        print("=" * 50)
        
    except Exception as e:
        print(f"\n❌ Test failed with error: {e}")
        import traceback
        traceback.print_exc()