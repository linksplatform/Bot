#!/usr/bin/env python3
"""
Simple test to verify the logic of top position calculation.
This test mimics the logic without importing the actual modules.
"""

def calculate_real_karma(user, POSITIVE_VOTES_PER_KARMA=3, NEGATIVE_VOTES_PER_KARMA=2):
    """Mock implementation of calculate_real_karma"""
    base_karma = user["karma"]
    up_votes = len(user["supporters"]) / POSITIVE_VOTES_PER_KARMA
    down_votes = len(user["opponents"]) / NEGATIVE_VOTES_PER_KARMA
    return base_karma + up_votes - down_votes

def get_users_sorted_by_karma(mock_users):
    """Mock implementation of get_users_sorted_by_karma"""
    # Filter to only chat members (all in our mock)
    users = mock_users.copy()
    
    # Sort by real karma
    users.sort(key=lambda u: calculate_real_karma(u), reverse=True)
    return users

def get_user_top_position(user, mock_users):
    """Mock implementation of get_user_top_position"""
    users = get_users_sorted_by_karma(mock_users)
    users = [u for u in users if
             (u["karma"] != 0) or
             ("programming_languages" in u and len(u["programming_languages"]) > 0)]
    
    user_uid = user["uid"]
    for index, ranked_user in enumerate(users):
        if ranked_user["uid"] == user_uid:
            return index + 1  # 1-based position
    return 0  # User not found in ranking

def test_top_position():
    """Test the top position logic"""
    # Mock users with different karma values
    mock_users = [
        {"uid": 1, "karma": 100, "supporters": [], "opponents": [], "name": "User1", "programming_languages": ["Python"]},
        {"uid": 2, "karma": 80, "supporters": [101, 102], "opponents": [], "name": "User2", "programming_languages": ["JavaScript"]},
        {"uid": 3, "karma": 60, "supporters": [], "opponents": [201], "name": "User3", "programming_languages": ["Java"]},
        {"uid": 4, "karma": 40, "supporters": [301], "opponents": [401, 402, 403], "name": "User4", "programming_languages": ["C++"]},
        {"uid": 5, "karma": 20, "supporters": [], "opponents": [], "name": "User5", "programming_languages": ["Go"]},
        {"uid": 6, "karma": 0, "supporters": [], "opponents": [], "name": "User6", "programming_languages": []},  # Should be excluded
    ]
    
    print("Mock users with calculated real karma:")
    sorted_users = get_users_sorted_by_karma(mock_users)
    for i, user in enumerate(sorted_users):
        real_karma = calculate_real_karma(user)
        print(f"  {i+1}. {user['name']} (uid: {user['uid']}) - karma: {user['karma']}, real karma: {real_karma:.1f}")
    
    print("\nFiltered users for ranking (excluding users with 0 karma and no languages):")
    filtered_users = [u for u in sorted_users if
                     (u["karma"] != 0) or
                     ("programming_languages" in u and len(u["programming_languages"]) > 0)]
    for i, user in enumerate(filtered_users):
        print(f"  {i+1}. {user['name']} (uid: {user['uid']})")
    
    print("\nTesting position calculation:")
    for user in mock_users:
        position = get_user_top_position(user, mock_users)
        print(f"  {user['name']} (uid: {user['uid']}) -> position: {position}")

if __name__ == "__main__":
    test_top_position()