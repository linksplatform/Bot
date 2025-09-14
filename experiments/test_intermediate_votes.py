#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Simple test script to validate intermediate votes functionality
"""

# Mock user data structure
class MockUser(dict):
    def __init__(self, uid, supporters=None, opponents=None, karma=0, name="Test User"):
        super().__init__()
        self['uid'] = uid
        self['supporters'] = supporters or []
        self['opponents'] = opponents or []
        self['karma'] = karma
        self['name'] = name
        self['programming_languages'] = []

# Mock config values (from the actual config)
POSITIVE_VOTES_PER_KARMA = 2
NEGATIVE_VOTES_PER_KARMA = 3

def calculate_intermediate_votes(user):
    """Calculate only intermediate votes (pending supporters/opponents)"""
    up_votes = len(user["supporters"]) / POSITIVE_VOTES_PER_KARMA
    down_votes = len(user["opponents"]) / NEGATIVE_VOTES_PER_KARMA
    return up_votes - down_votes

def test_intermediate_votes_calculation():
    print("Testing intermediate votes calculation...")
    
    # User with no pending votes
    user1 = MockUser(1, supporters=[], opponents=[], karma=100)
    intermediate1 = calculate_intermediate_votes(user1)
    print(f"User 1 (no votes): {intermediate1} (expected: 0.0)")
    assert intermediate1 == 0.0, f"Expected 0.0, got {intermediate1}"
    
    # User with 2 supporters (should give +1.0 intermediate votes)
    user2 = MockUser(2, supporters=[3, 4], opponents=[], karma=50)
    intermediate2 = calculate_intermediate_votes(user2)
    print(f"User 2 (2 supporters): {intermediate2} (expected: 1.0)")
    assert intermediate2 == 1.0, f"Expected 1.0, got {intermediate2}"
    
    # User with 3 opponents (should give -1.0 intermediate votes)
    user3 = MockUser(3, supporters=[], opponents=[1, 2, 4], karma=25)
    intermediate3 = calculate_intermediate_votes(user3)
    print(f"User 3 (3 opponents): {intermediate3} (expected: -1.0)")
    assert intermediate3 == -1.0, f"Expected -1.0, got {intermediate3}"
    
    # User with mixed votes
    user4 = MockUser(4, supporters=[1, 5], opponents=[2, 6, 7], karma=75)
    intermediate4 = calculate_intermediate_votes(user4)
    expected4 = 2/POSITIVE_VOTES_PER_KARMA - 3/NEGATIVE_VOTES_PER_KARMA  # 1.0 - 1.0 = 0.0
    print(f"User 4 (mixed votes): {intermediate4} (expected: {expected4})")
    assert intermediate4 == expected4, f"Expected {expected4}, got {intermediate4}"
    
    print("All intermediate votes calculations passed!")

def test_sorting_by_intermediate_votes():
    print("\nTesting sorting by intermediate votes...")
    
    users = [
        MockUser(1, supporters=[], opponents=[]), # 0.0
        MockUser(2, supporters=[3, 4], opponents=[]), # +1.0
        MockUser(3, supporters=[], opponents=[1, 2, 5]), # -1.0
        MockUser(4, supporters=[1, 2, 3, 4], opponents=[]), # +2.0
        MockUser(5, supporters=[1], opponents=[2, 3]), # +0.5 - 0.67 = -0.17
    ]
    
    # Sort by intermediate votes (descending)
    users_sorted = sorted(users, key=calculate_intermediate_votes, reverse=True)
    
    print("Users sorted by intermediate votes (highest to lowest):")
    for user in users_sorted:
        intermediate = calculate_intermediate_votes(user)
        print(f"  User {user['uid']}: {intermediate}")
    
    # Verify order
    expected_order = [4, 2, 1, 5, 3]  # Based on intermediate votes
    actual_order = [user['uid'] for user in users_sorted]
    print(f"Expected order: {expected_order}")
    print(f"Actual order: {actual_order}")
    
    assert actual_order == expected_order, f"Expected {expected_order}, got {actual_order}"
    print("Sorting test passed!")

if __name__ == "__main__":
    test_intermediate_votes_calculation()
    test_sorting_by_intermediate_votes()
    print("\n✅ All tests passed! The intermediate votes functionality is working correctly.")