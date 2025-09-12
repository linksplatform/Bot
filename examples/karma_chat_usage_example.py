#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Example usage of the karma-based chat membership system.

This script demonstrates how to configure and use the automatic
chat membership management based on user karma levels.
"""

# Example configuration that would go in config.py:

EXAMPLE_KARMA_BASED_CHATS = [
    {
        "chat_id": 2000000001,  # Main programming chat
        "karma_threshold": 2,   # Users need 2+ karma to join
        "name": "Main Programming Chat"
    },
    {
        "chat_id": 2000000002,  # Advanced developers chat
        "karma_threshold": 10,  # Users need 10+ karma to join
        "name": "Advanced Developers"
    },
    {
        "chat_id": 2000000003,  # Expert level chat
        "karma_threshold": 50,  # Users need 50+ karma to join  
        "name": "Expert Developers"
    },
    {
        "chat_id": 2000000004,  # Mentor level chat
        "karma_threshold": 100, # Users need 100+ karma to join
        "name": "Mentors & Leads"
    }
]

# Example scenarios:

def example_scenarios():
    """Show example scenarios of how the system works."""
    
    scenarios = [
        {
            "user": "Alice",
            "karma": 1,
            "description": "New user with 1 karma - not in any karma chats yet"
        },
        {
            "user": "Bob", 
            "karma": 3,
            "description": "Gets added to Main Programming Chat (threshold: 2)"
        },
        {
            "user": "Charlie",
            "karma": 15, 
            "description": "Gets added to Main Programming Chat and Advanced Developers (thresholds: 2, 10)"
        },
        {
            "user": "David",
            "karma": 75,
            "description": "Gets added to first three chats (thresholds: 2, 10, 50)"
        },
        {
            "user": "Eve",
            "karma": 150,
            "description": "Gets added to all chats (thresholds: 2, 10, 50, 100)"
        }
    ]
    
    print("🎯 Karma-Based Chat Membership Examples")
    print("=" * 50)
    
    for scenario in scenarios:
        print(f"\n👤 {scenario['user']} (Karma: {scenario['karma']})")
        print(f"   {scenario['description']}")
        
        # Show which chats they would be in
        eligible_chats = []
        for chat in EXAMPLE_KARMA_BASED_CHATS:
            if scenario['karma'] >= chat['karma_threshold']:
                eligible_chats.append(f"{chat['name']} (≥{chat['karma_threshold']})")
        
        if eligible_chats:
            print(f"   📩 Eligible for: {', '.join(eligible_chats)}")
        else:
            print(f"   ❌ Not eligible for any karma-based chats")

def example_commands():
    """Show example bot commands for managing karma chats."""
    
    commands = [
        {
            "command": "check chat membership",
            "description": "Check your membership in all karma-based chats and update if needed"
        },
        {
            "command": "check all membership", 
            "description": "Admin command to check and update all users' memberships"
        },
        {
            "command": "chat status",
            "description": "Show status of current chat (member counts, karma threshold)"
        },
        {
            "command": "chat status 2000000001",
            "description": "Show status of specific chat by ID"
        },
        {
            "command": "+",
            "description": "Give karma to a user (triggers automatic membership check)"
        },
        {
            "command": "-",
            "description": "Remove karma from a user (triggers automatic membership check)"
        }
    ]
    
    print("\n🤖 Available Bot Commands")
    print("=" * 50)
    
    for cmd in commands:
        print(f"\n💬 '{cmd['command']}'")
        print(f"   {cmd['description']}")

def example_workflow():
    """Show example workflow of how the system operates."""
    
    print("\n⚙️  System Workflow")
    print("=" * 50)
    
    steps = [
        "1. User receives karma (+1 or -1) from another user",
        "2. System automatically checks user's new karma level",  
        "3. For each configured karma-based chat:",
        "   a. Check if user meets karma threshold",
        "   b. Check if user is currently in the chat",
        "   c. Add user to chat if they meet threshold but aren't in it",
        "   d. Remove user from chat if they don't meet threshold but are in it",
        "4. Log all membership changes for admin review",
        "5. Send confirmation messages for successful operations"
    ]
    
    for step in steps:
        print(f"\n{step}")

def example_configuration_tips():
    """Provide tips for configuring the karma chat system."""
    
    print("\n💡 Configuration Tips")
    print("=" * 50)
    
    tips = [
        "Start with low thresholds (2-5 karma) for basic chats",
        "Create progressive tiers (2, 10, 25, 50, 100)",
        "Test with a small group before rolling out to all chats",
        "Monitor logs to ensure the system is working correctly",
        "Consider having fallback manual commands for edge cases",
        "Set up different thresholds for different types of chats",
        "Use descriptive names for easier administration"
    ]
    
    for i, tip in enumerate(tips, 1):
        print(f"\n{i}. {tip}")

if __name__ == "__main__":
    example_scenarios()
    example_commands()
    example_workflow() 
    example_configuration_tips()
    
    print("\n" + "=" * 50)
    print("🎉 Karma-based chat membership system is ready!")
    print("Configure your KARMA_BASED_CHATS in config.py to get started.")