#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script for translation patterns."""

import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), '..', 'python'))

from regex import match
import patterns

def test_pattern(pattern, text, expected_word):
    """Test if pattern matches text and extracts expected word."""
    matched = match(pattern, text)
    if matched:
        word = matched.group('word')
        print(f"✓ Pattern matched: '{text}' -> word: '{word}'")
        if word.strip() == expected_word:
            print(f"  ✓ Extracted word matches expected: '{expected_word}'")
            return True
        else:
            print(f"  ✗ Expected '{expected_word}', got '{word.strip()}'")
            return False
    else:
        print(f"✗ Pattern did NOT match: '{text}'")
        return False

def run_tests():
    """Run all pattern tests."""
    print("Testing Translation Patterns")
    print("=" * 50)
    
    test_cases = [
        # Russian patterns asking for English translation
        (patterns.TRANSLATE_TO_ENGLISH_RU, "Как перевести hello на английский?", "hello"),
        (patterns.TRANSLATE_TO_ENGLISH_RU, "как переводится world на англ", "world"),
        (patterns.TRANSLATE_TO_ENGLISH_RU, "Как перевести красивый на английский", "красивый"),
        
        # Russian patterns asking for translation (inferred as Russian target)
        (patterns.TRANSLATE_TO_RUSSIAN_RU, "Как переводится beautiful?", "beautiful"),
        (patterns.TRANSLATE_TO_RUSSIAN_RU, "как перевести computer", "computer"),
        
        # English patterns asking for translation 
        (patterns.TRANSLATE_TO_ENGLISH_EN, "How to translate красота?", "красота"),
        (patterns.TRANSLATE_TO_ENGLISH_EN, "What is translation of дом", "дом"),
        
        # Generic translate pattern
        (patterns.TRANSLATE_WORD, "translate house", "house"),
        (patterns.TRANSLATE_WORD, "перевести дом", "дом"),
        (patterns.TRANSLATE_WORD, "перевод машина", "машина"),
    ]
    
    passed = 0
    total = len(test_cases)
    
    for i, (pattern, text, expected_word) in enumerate(test_cases, 1):
        print(f"\nTest {i}/{total}:")
        if test_pattern(pattern, text, expected_word):
            passed += 1
        
    print(f"\n" + "=" * 50)
    print(f"Results: {passed}/{total} tests passed")
    
    if passed == total:
        print("🎉 All tests passed!")
        return True
    else:
        print(f"❌ {total - passed} tests failed")
        return False

if __name__ == '__main__':
    success = run_tests()
    sys.exit(0 if success else 1)