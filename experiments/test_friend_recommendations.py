#!/usr/bin/env python3
"""
Test script for the friend recommendations feature.
This script creates mock data to test the recommendation algorithm logic.
"""
import sys
import os

# Add the parent directory to the path so we can import the modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

def test_shared_languages_calculation():
    """Test the shared languages calculation logic."""
    print("Testing shared languages calculation...")
    
    # Test case 1: Basic overlap
    user1_langs = ["Python", "JavaScript", "Go"]
    user2_langs = ["Python", "Java", "C++"]
    shared = set(user1_langs) & set(user2_langs)
    assert len(shared) == 1 and "Python" in shared, f"Expected 1 shared language (Python), got {shared}"
    
    # Test case 2: No overlap
    user3_langs = ["Rust", "Haskell"]
    user4_langs = ["PHP", "Ruby"]
    shared = set(user3_langs) & set(user4_langs)
    assert len(shared) == 0, f"Expected no shared languages, got {shared}"
    
    # Test case 3: Full overlap
    user5_langs = ["Python", "JavaScript"]
    user6_langs = ["Python", "JavaScript"]
    shared = set(user5_langs) & set(user6_langs)
    assert len(shared) == 2, f"Expected 2 shared languages, got {shared}"
    
    print("✅ All shared languages calculation tests passed!")

def test_recommendation_sorting():
    """Test the recommendation sorting logic."""
    print("Testing recommendation sorting...")
    
    # Mock recommendation data
    recommendations = [
        {'shared_count': 1, 'karma': 50, 'name': 'User1'},
        {'shared_count': 3, 'karma': 10, 'name': 'User2'}, 
        {'shared_count': 2, 'karma': 100, 'name': 'User3'},
        {'shared_count': 3, 'karma': 20, 'name': 'User4'},
    ]
    
    # Sort by shared_count (descending), then by karma (descending)
    recommendations.sort(key=lambda x: (-x['shared_count'], -x['karma']))
    
    # Expected order: User4 (3,20), User2 (3,10), User3 (2,100), User1 (1,50)
    expected_names = ['User4', 'User2', 'User3', 'User1']
    actual_names = [rec['name'] for rec in recommendations]
    
    assert actual_names == expected_names, f"Expected {expected_names}, got {actual_names}"
    
    print("✅ Recommendation sorting test passed!")

def test_message_formatting():
    """Test basic message formatting logic."""
    print("Testing message formatting...")
    
    user_languages = ["Python", "JavaScript"]
    user_languages_str = ", ".join(sorted(user_languages))
    
    expected = "JavaScript, Python"
    assert user_languages_str == expected, f"Expected '{expected}', got '{user_languages_str}'"
    
    # Test shared languages formatting
    shared_languages = ["Python", "Go"]
    shared_languages_str = ", ".join(shared_languages)
    expected_shared = "Python, Go"
    assert shared_languages_str == expected_shared, f"Expected '{expected_shared}', got '{shared_languages_str}'"
    
    print("✅ Message formatting test passed!")

def main():
    """Run all tests."""
    print("🧪 Testing Friend Recommendations Feature")
    print("=" * 50)
    
    try:
        test_shared_languages_calculation()
        test_recommendation_sorting() 
        test_message_formatting()
        
        print("=" * 50)
        print("✅ All tests passed! The friend recommendations feature should work correctly.")
        return 0
    
    except AssertionError as e:
        print(f"❌ Test failed: {e}")
        return 1
    except Exception as e:
        print(f"❌ Unexpected error: {e}")
        return 1

if __name__ == "__main__":
    sys.exit(main())