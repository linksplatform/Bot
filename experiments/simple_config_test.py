#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Simple test to verify the configuration setting for automatic votes deletion (issue #52)"""

import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '../python'))

def test_config_setting():
    """Test that the DISABLE_AUTOMATIC_VOTES_DELETION config setting exists and works"""
    print("Testing DISABLE_AUTOMATIC_VOTES_DELETION configuration...")
    
    import config
    
    # Test that the setting exists
    assert hasattr(config, 'DISABLE_AUTOMATIC_VOTES_DELETION'), "DISABLE_AUTOMATIC_VOTES_DELETION setting not found in config"
    
    # Test that it's set to True (default value for disabling deletion)
    assert config.DISABLE_AUTOMATIC_VOTES_DELETION == True, f"Expected True, got {config.DISABLE_AUTOMATIC_VOTES_DELETION}"
    
    print(f"✓ DISABLE_AUTOMATIC_VOTES_DELETION is set to: {config.DISABLE_AUTOMATIC_VOTES_DELETION}")
    
    # Test that we can toggle it
    original_value = config.DISABLE_AUTOMATIC_VOTES_DELETION
    config.DISABLE_AUTOMATIC_VOTES_DELETION = False
    assert config.DISABLE_AUTOMATIC_VOTES_DELETION == False, "Failed to set DISABLE_AUTOMATIC_VOTES_DELETION to False"
    print("✓ Successfully toggled setting to False")
    
    config.DISABLE_AUTOMATIC_VOTES_DELETION = True  
    assert config.DISABLE_AUTOMATIC_VOTES_DELETION == True, "Failed to set DISABLE_AUTOMATIC_VOTES_DELETION to True"
    print("✓ Successfully toggled setting back to True")
    
    # Restore original value
    config.DISABLE_AUTOMATIC_VOTES_DELETION = original_value
    
    print("\n✅ All configuration tests passed!")
    print(f"Final setting value: {config.DISABLE_AUTOMATIC_VOTES_DELETION}")

def test_code_changes():
    """Test that the code changes for votes deletion are in place"""
    print("\nTesting code changes in commands.py...")
    
    with open('python/modules/commands.py', 'r') as f:
        content = f.read()
    
    # Check that our conditional checks are in place
    checks = [
        "if not config.DISABLE_AUTOMATIC_VOTES_DELETION:",
        "self.vk_instance.delete_message(self.peer_id, self.msg_id)",
        "self.user[current_voters] = []"
    ]
    
    for check in checks:
        assert check in content, f"Expected code change not found: {check}"
    
    # Count the number of conditional checks we added
    conditional_count = content.count("if not config.DISABLE_AUTOMATIC_VOTES_DELETION:")
    print(f"✓ Found {conditional_count} conditional checks for DISABLE_AUTOMATIC_VOTES_DELETION")
    
    # Should be 4 checks: 3 for message deletion + 1 for votes clearing
    assert conditional_count == 4, f"Expected 4 conditional checks, found {conditional_count}"
    
    print("✅ All code changes verified!")

if __name__ == "__main__":
    test_config_setting()
    test_code_changes()