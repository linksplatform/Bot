#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Setup script for LinksBot for Telegram."""

import os
import shutil
import sys


def main():
    """Setup the Telegram bot."""
    print("🤖 LinksBot for Telegram - Setup")
    print("=" * 40)
    
    # Check if config.py exists
    if not os.path.exists("config.py"):
        print("Creating config.py from template...")
        if os.path.exists("config.template.py"):
            shutil.copy("config.template.py", "config.py")
            print("✓ config.py created from template")
            print()
            print("⚠️  IMPORTANT: Edit config.py and set your BOT_TOKEN")
            print("   Get your bot token from @BotFather on Telegram")
        else:
            print("❌ config.template.py not found!")
            return 1
    else:
        print("✓ config.py already exists")
    
    # Create data directory
    if not os.path.exists("data"):
        os.makedirs("data")
        print("✓ Created data directory")
    else:
        print("✓ Data directory already exists")
    
    # Check if requirements are installed
    print("\nChecking dependencies...")
    try:
        import aiogram
        print("✓ aiogram is installed")
    except ImportError:
        print("❌ aiogram not installed. Run: pip install -r requirements.txt")
        return 1
    
    try:
        import wikipedia
        print("✓ wikipedia is installed")
    except ImportError:
        print("❌ wikipedia not installed. Run: pip install -r requirements.txt")
        return 1
    
    # Check config
    print("\nChecking configuration...")
    try:
        import config
        if hasattr(config, 'BOT_TOKEN') and config.BOT_TOKEN and config.BOT_TOKEN != "YOUR_BOT_TOKEN_HERE":
            print("✓ BOT_TOKEN is configured")
        else:
            print("❌ BOT_TOKEN not set in config.py")
            print("   Edit config.py and set your bot token from @BotFather")
            return 1
    except ImportError:
        print("❌ Cannot import config.py")
        return 1
    
    print("\n🎉 Setup complete!")
    print("\nTo start the bot:")
    print("   python3 main.py")
    print("\nTo test the setup:")
    print("   python3 simple_test.py")
    
    return 0


if __name__ == "__main__":
    sys.exit(main())