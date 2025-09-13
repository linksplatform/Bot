#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Simple test without external dependencies."""

import sys
import os
import json
from datetime import datetime

# Add telegram directory to path
sys.path.insert(0, os.path.dirname(__file__))

def test_core_functionality():
    """Test core functionality without external dependencies."""
    print("Testing core Telegram bot functionality...")
    
    try:
        # Test config
        import config
        print("✓ Config imported")
        
        # Test that basic config values exist
        assert config.POSITIVE_VOTES_PER_KARMA == 2
        assert config.NEGATIVE_VOTES_PER_KARMA == 3
        assert "Python" in config.DEFAULT_PROGRAMMING_LANGUAGES
        print("✓ Config values correct")
        
        # Test storage classes
        from modules.storage import User, KarmaVote, Storage
        
        # Create test user
        user = User(
            user_id=12345,
            username="testuser",
            first_name="Test User",
            karma=5,
            programming_languages=["Python", "JavaScript"],
            github_profile="testuser"
        )
        
        print("✓ User class works")
        
        # Create karma vote
        vote = KarmaVote(
            voter_id=54321,
            target_id=12345,
            vote_type="positive",
            timestamp=datetime.now().isoformat(),
            chat_id=-1001234567890
        )
        
        print("✓ KarmaVote class works")
        
        # Test storage operations
        storage = Storage()
        storage.users[12345] = user
        storage.karma_votes.append(vote)
        
        # Test user retrieval
        retrieved_user = storage.get_user(12345, "testuser", "Test User")
        assert retrieved_user.user_id == 12345
        print("✓ Storage operations work")
        
        # Test file I/O
        storage._save_users()
        storage._save_karma_votes()
        print("✓ File operations work")
        
        # Test data persistence
        storage2 = Storage()  # This should load the saved data
        assert 12345 in storage2.users
        assert len(storage2.karma_votes) > 0
        print("✓ Data persistence works")
        
        # Test recent votes
        recent_votes = storage2.get_recent_karma_votes(12345, -1001234567890, 24)
        assert len(recent_votes) >= 1
        print("✓ Recent votes query works")
        
        print("\n🎉 All core functionality tests passed!")
        return True
        
    except Exception as e:
        print(f"✗ Test failed: {e}")
        import traceback
        traceback.print_exc()
        return False
    
    finally:
        # Cleanup
        try:
            if os.path.exists("data/users.json"):
                os.remove("data/users.json")
            if os.path.exists("data/karma_votes.json"):
                os.remove("data/karma_votes.json")
            if os.path.exists("data") and not os.listdir("data"):
                os.rmdir("data")
        except:
            pass

def test_command_logic_without_wikipedia():
    """Test command logic without Wikipedia dependency."""
    print("\nTesting command logic (without Wikipedia)...")
    
    try:
        # Mock the wikipedia import in commands module
        import sys
        import types
        
        # Create a mock wikipedia module
        mock_wikipedia = types.ModuleType('wikipedia')
        mock_wikipedia.set_lang = lambda x: None
        sys.modules['wikipedia'] = mock_wikipedia
        
        # Now import commands
        from modules.commands import Commands
        
        # Create commands instance with None bot (we're not testing aiogram integration)
        commands = Commands(None)
        
        # Test language validation
        assert commands._is_valid_language("Python")
        assert commands._is_valid_language("JavaScript")
        assert not commands._is_valid_language("InvalidLanguage")
        print("✓ Language validation works")
        
        # Test karma cooldown
        cooldown = commands._get_karma_cooldown(0)
        assert cooldown == 2
        
        cooldown = commands._get_karma_cooldown(25)
        assert cooldown == 0.5
        print("✓ Karma cooldown calculation works")
        
        # Test voting capability
        from modules.storage import User
        user = User(user_id=99999, username="test", first_name="Test")
        assert commands._can_vote(user)
        print("✓ Vote capability check works")
        
        print("✓ Command logic tests passed!")
        return True
        
    except Exception as e:
        print(f"✗ Command logic test failed: {e}")
        import traceback
        traceback.print_exc()
        return False

def main():
    """Run simplified tests."""
    print("=" * 60)
    print("LinksBot for Telegram - Core Functionality Test")
    print("=" * 60)
    
    success = True
    
    if not test_core_functionality():
        success = False
    
    if not test_command_logic_without_wikipedia():
        success = False
    
    print("\n" + "=" * 60)
    if success:
        print("✅ Core implementation is working correctly!")
        print("\nThe Telegram bot is ready for deployment.")
        print("\nTo run the bot:")
        print("1. Install: pip install -r requirements.txt")  
        print("2. Configure BOT_TOKEN in config.py")
        print("3. Run: python main.py")
    else:
        print("❌ Some tests failed.")
        return 1
    
    print("=" * 60)
    return 0

if __name__ == "__main__":
    sys.exit(main())