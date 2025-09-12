#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test the actual translation API."""

import json
import urllib.request
import urllib.parse

def test_translation_api():
    """Test MyMemory translation API."""
    print("Testing MyMemory Translation API:")
    print("=" * 40)
    
    test_cases = [
        ('hello', 'en', 'ru'),
        ('beautiful', 'en', 'ru'), 
        ('красота', 'ru', 'en'),
        ('дом', 'ru', 'en'),
        ('house', 'en', 'ru'),
    ]
    
    for word, source, target in test_cases:
        try:
            # URL encode the word to handle special characters
            encoded_word = urllib.parse.quote(word)
            url = f"https://api.mymemory.translated.net/get?q={encoded_word}&langpair={source}|{target}"
            
            print(f"\nTranslating '{word}' from {source} to {target}...")
            print(f"URL: {url}")
            
            with urllib.request.urlopen(url, timeout=10) as response:
                data = json.loads(response.read().decode())
                
            if data.get('responseStatus') == 200:
                translation = data.get('responseData', {}).get('translatedText', '')
                print(f"✓ Result: '{word}' -> '{translation}'")
            else:
                print(f"✗ API Error: {data.get('responseStatus')} - {data.get('responseDetails', 'Unknown error')}")
                
        except Exception as e:
            print(f"✗ Network Error: {e}")

if __name__ == '__main__':
    test_translation_api()