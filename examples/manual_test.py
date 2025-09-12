#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Manual test for translation logic without external dependencies."""

import re

def test_russian_detection():
    """Test Russian character detection logic."""
    test_cases = [
        ("Как перевести hello на английский?", True),
        ("How to translate красота?", False),
        ("как переводится beautiful", True),
        ("translate house", False),
        ("перевод машина", True),
    ]
    
    print("Testing Russian character detection:")
    print("-" * 40)
    
    for text, expected in test_cases:
        is_russian = bool(re.search(r'[а-яё]', text, re.IGNORECASE))
        result = "✓" if is_russian == expected else "✗"
        print(f"{result} '{text}' -> Russian: {is_russian} (expected: {expected})")
    
    print()

def test_word_extraction():
    """Test word extraction from various patterns."""
    patterns_and_tests = [
        # Pattern similar to TRANSLATE_TO_ENGLISH_RU
        (r'(как перевести|как переводится)\s+(.+?)\s+(на английский|на англ)', 
         "Как перевести hello на английский?", "hello"),
        
        # Pattern similar to TRANSLATE_TO_RUSSIAN_RU  
        (r'(как переводится|как перевести)\s+(.+?)(\?|$)',
         "Как переводится beautiful?", "beautiful"),
         
        # Pattern similar to TRANSLATE_TO_ENGLISH_EN
        (r'(how to translate|what is translation of)\s+(.+?)(\?|$)',
         "How to translate красота?", "красота"),
         
        # Pattern similar to TRANSLATE_WORD
        (r'(translate|перевести|переводить|перевод)\s+(.+?)(\?|$)',
         "translate house", "house"),
    ]
    
    print("Testing word extraction patterns:")
    print("-" * 40)
    
    for pattern, text, expected_word in patterns_and_tests:
        match = re.search(pattern, text, re.IGNORECASE)
        if match:
            extracted = match.group(2).strip()
            result = "✓" if extracted == expected_word else "✗"
            print(f"{result} '{text}' -> '{extracted}' (expected: '{expected_word}')")
        else:
            print(f"✗ '{text}' -> No match")
    
    print()

def test_language_logic():
    """Test the language detection and translation direction logic."""
    print("Testing translation direction logic (updated):")
    print("-" * 40)
    
    test_cases = [
        ("Как перевести hello на английский?", "hello", "en->ru"),
        ("How to translate красота?", "красота", "ru->en"), 
        ("Как переводится beautiful?", "beautiful", "en->ru"),
        ("translate house", "house", "en->ru"),
        ("перевод машина", "машина", "ru->en"),
    ]
    
    for msg, word, expected_direction in test_cases:
        # Use updated logic: check message without the word
        message_without_word = msg.replace(word, '')
        is_russian_message = bool(re.search(r'[а-яё]', message_without_word, re.IGNORECASE))
        word_is_russian = bool(re.search(r'[а-яё]', word, re.IGNORECASE))
        
        # Simplified logic: translate based on word language
        if word_is_russian:
            source_lang = 'ru'
            target_lang = 'en'
        else:
            source_lang = 'en'
            target_lang = 'ru'
            
        actual_direction = f"{source_lang}->{target_lang}"
        
        result = "✓" if expected_direction == actual_direction else "✗"
        print(f"{result} '{msg}' with word '{word}':")
        print(f"    Expected: {expected_direction}")
        print(f"    Actual:   {actual_direction}")
        print()

if __name__ == '__main__':
    test_russian_detection()
    test_word_extraction() 
    test_language_logic()