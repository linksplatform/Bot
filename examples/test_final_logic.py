#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test the final translation logic according to issue requirements."""

import re

def test_translation_logic():
    """Test complete translation logic according to issue requirements."""
    print("Testing Final Translation Logic:")
    print("=" * 50)
    
    test_cases = [
        # Issue examples
        {
            'msg': 'Как перевести hello на английский?',
            'word': 'hello',
            'expected_source': 'en',
            'expected_target': 'en',  # Explicitly asking for English
            'expected_response_lang': 'russian',
            'expected_format': '"hello" переводится как "привет"'
        },
        {
            'msg': 'Как переводится beautiful?',
            'word': 'beautiful', 
            'expected_source': 'en',
            'expected_target': 'ru',  # Inferred Russian target
            'expected_response_lang': 'russian',
            'expected_format': '"beautiful" переводится как "красивый"'
        },
        {
            'msg': 'How to translate красота?',
            'word': 'красота',
            'expected_source': 'ru', 
            'expected_target': 'en',  # Inferred English target
            'expected_response_lang': 'english',
            'expected_format': '"красота" translates to "beauty"'
        },
        {
            'msg': 'What is translation of дом?',
            'word': 'дом',
            'expected_source': 'ru',
            'expected_target': 'en',  # Inferred English target
            'expected_response_lang': 'english', 
            'expected_format': '"дом" translates to "house"'
        },
        # Additional test cases
        {
            'msg': 'translate house',
            'word': 'house',
            'expected_source': 'en',
            'expected_target': 'ru',  # English word -> Russian
            'expected_response_lang': 'english',
            'expected_format': '"house" translates to "дом"'
        }
    ]
    
    for i, case in enumerate(test_cases, 1):
        print(f"\nTest {i}: {case['msg']}")
        print("-" * 40)
        
        msg = case['msg']
        word = case['word']
        
        # Apply the logic from the implementation
        message_without_word = msg.replace(word, '')
        is_russian_message = bool(re.search(r'[а-яё]', message_without_word, re.IGNORECASE))
        word_is_russian = bool(re.search(r'[а-яё]', word, re.IGNORECASE))
        
        if re.search(r'на английский|на англ', msg, re.IGNORECASE):
            # Explicitly asking for English translation
            source_lang = 'ru' if word_is_russian else 'en'
            target_lang = 'en'
            response_in_russian = True
        elif is_russian_message:
            # Russian message without explicit target - infer target language
            if word_is_russian:
                source_lang = 'ru'
                target_lang = 'en'
            else:
                source_lang = 'en'
                target_lang = 'ru'
            response_in_russian = True
        else:
            # English message - infer target as English
            if word_is_russian:
                source_lang = 'ru'
                target_lang = 'en'
            else:
                # English word in English message - might want Russian translation
                source_lang = 'en'
                target_lang = 'ru'
            response_in_russian = False
        
        # Check results
        source_ok = source_lang == case['expected_source']
        target_ok = target_lang == case['expected_target']
        response_lang_ok = (
            (response_in_russian and case['expected_response_lang'] == 'russian') or
            (not response_in_russian and case['expected_response_lang'] == 'english')
        )
        
        print(f"Word: '{word}' ({'Russian' if word_is_russian else 'English'} detected)")
        print(f"Message language: {'Russian' if is_russian_message else 'English'}")
        print(f"Source language: {source_lang} {'✓' if source_ok else '✗ (expected ' + case['expected_source'] + ')'}")
        print(f"Target language: {target_lang} {'✓' if target_ok else '✗ (expected ' + case['expected_target'] + ')'}")
        print(f"Response language: {'Russian' if response_in_russian else 'English'} {'✓' if response_lang_ok else '✗ (expected ' + case['expected_response_lang'] + ')'}")
        
        all_ok = source_ok and target_ok and response_lang_ok
        print(f"Overall result: {'✓ PASS' if all_ok else '✗ FAIL'}")

if __name__ == '__main__':
    test_translation_logic()