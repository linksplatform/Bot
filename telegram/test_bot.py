#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Simple test script to verify the Telegram bot implementation."""

import sys
import os

# Add telegram directory to path
sys.path.insert(0, os.path.dirname(__file__))

def test_imports():
    """Test that all modules can be imported correctly."""
    try:
        print("Testing imports...")
        
        # Test config import
        import config
        print("✓ Config module imported successfully")
        
        # Test storage module
        from modules.storage import storage, User, KarmaVote
        print("✓ Storage module imported successfully")
        
        # Test commands module
        from modules.commands import Commands
        print("✓ Commands module imported successfully")
        
        # Test main module (might fail without aiogram, but import should work)
        try:
            import main
            print("✓ Main module imported successfully")
        except ImportError as e:
            if "aiogram" in str(e):
                print("⚠ Main module import failed due to missing aiogram (expected in test)")
            else:
                raise
        
        return True
        
    except Exception as e:
        print(f"✗ Import test failed: {e}")
        return False

def test_storage():
    """Test storage functionality."""
    try:
        print("\nTesting storage...")
        from modules.storage import storage, User, KarmaVote
        from datetime import datetime
        
        # Test user creation
        user = storage.get_user(12345, "testuser", "Test User")
        assert user.user_id == 12345
        assert user.username == "testuser"
        assert user.first_name == "Test User"
        assert user.karma == 0
        print("✓ User creation works")
        
        # Test user update
        user.karma = 10
        user.programming_languages = ["Python", "JavaScript"]
        user.github_profile = "testuser"
        storage.update_user(user)
        print("✓ User update works")
        
        # Test retrieving updated user
        updated_user = storage.get_user(12345)
        assert updated_user.karma == 10
        assert "Python" in updated_user.programming_languages
        assert updated_user.github_profile == "testuser"
        print("✓ User retrieval after update works")
        
        # Test karma vote
        vote = KarmaVote(
            voter_id=54321,
            target_id=12345,
            vote_type="positive",
            timestamp=datetime.now().isoformat(),
            chat_id=-1001234567890
        )
        storage.add_karma_vote(vote)
        print("✓ Karma vote creation works")
        
        # Test recent votes retrieval
        recent_votes = storage.get_recent_karma_votes(12345, -1001234567890, 24)
        assert len(recent_votes) == 1
        assert recent_votes[0].voter_id == 54321
        print("✓ Recent votes retrieval works")
        
        return True
        
    except Exception as e:
        print(f"✗ Storage test failed: {e}")
        return False

def test_config():
    """Test configuration."""
    try:
        print("\nTesting configuration...")
        import config
        
        # Test required configurations exist
        assert hasattr(config, 'DEFAULT_PROGRAMMING_LANGUAGES')
        assert hasattr(config, 'KARMA_LIMIT_HOURS')
        assert hasattr(config, 'POSITIVE_VOTES_PER_KARMA')
        assert hasattr(config, 'NEGATIVE_VOTES_PER_KARMA')
        print("✓ Required config attributes exist")
        
        # Test some specific values
        assert config.POSITIVE_VOTES_PER_KARMA == 2
        assert config.NEGATIVE_VOTES_PER_KARMA == 3
        assert "Python" in config.DEFAULT_PROGRAMMING_LANGUAGES
        assert "JavaScript" in config.DEFAULT_PROGRAMMING_LANGUAGES
        print("✓ Config values are correct")
        
        return True
        
    except Exception as e:
        print(f"✗ Config test failed: {e}")
        return False

def test_commands_logic():
    """Test command logic without aiogram."""
    try:
        print("\nTesting command logic...")
        from modules.commands import Commands
        from modules.storage import storage
        
        # Create a mock bot (None is fine for testing logic)
        commands = Commands(None)
        
        # Test language validation
        assert commands._is_valid_language("Python")
        assert commands._is_valid_language("JavaScript")
        assert not commands._is_valid_language("InvalidLanguage123")
        print("✓ Language validation works")
        
        # Test karma cooldown calculation
        cooldown = commands._get_karma_cooldown(0)
        assert cooldown == 2  # Should be 2 hours for karma 0
        
        cooldown = commands._get_karma_cooldown(25)
        assert cooldown == 0.5  # Should be 0.5 hours for karma 25
        print("✓ Karma cooldown calculation works")
        
        # Test user voting capability
        from modules.storage import User
        user = User(user_id=99999, username="testuser2", first_name="Test User 2")
        assert commands._can_vote(user)  # Should be able to vote (no previous votes)
        print("✓ Vote capability check works")
        
        return True
        
    except Exception as e:
        print(f"✗ Commands logic test failed: {e}")
        return False

def cleanup_test_data():
    """Clean up test data files."""
    try:
        import config
        test_files = [config.USERS_FILE, config.KARMA_VOTES_FILE]
        for file_path in test_files:
            if os.path.exists(file_path):
                os.remove(file_path)
        
        # Remove data directory if empty
        if os.path.exists(config.DATA_DIR) and not os.listdir(config.DATA_DIR):
            os.rmdir(config.DATA_DIR)
            
        print("✓ Test data cleaned up")
    except Exception as e:
        print(f"⚠ Cleanup warning: {e}")

def main():
    """Run all tests."""
    print("=" * 50)
    print("LinksBot for Telegram - Test Suite")
    print("=" * 50)
    
    all_passed = True
    
    # Run tests
    tests = [
        test_imports,
        test_config,
        test_storage,
        test_commands_logic
    ]
    
    for test in tests:
        if not test():
            all_passed = False
    
    # Cleanup
    cleanup_test_data()
    
    print("\n" + "=" * 50)
    if all_passed:
        print("🎉 All tests passed! The Telegram bot implementation looks good.")
        print("\nNext steps:")
        print("1. Install dependencies: pip install -r requirements.txt")
        print("2. Set BOT_TOKEN in config.py")
        print("3. Run the bot: python main.py")
    else:
        print("❌ Some tests failed. Please check the implementation.")
        return 1
    
    print("=" * 50)
    return 0

if __name__ == "__main__":
    sys.exit(main())