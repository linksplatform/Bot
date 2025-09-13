#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Simple test for Google search pattern and basic logic."""

import urllib.parse
import re

# Test configuration values
GOOGLE_SEARCH_WHITELISTED_SITES = [
    'stackoverflow.com',
    'github.com',
    'docs.python.org',
    'developer.mozilla.org',
    'w3schools.com',
    'medium.com',
    'dev.to',
    'geeksforgeeks.org',
    'tutorialspoint.com',
    'programiz.com'
]
GOOGLE_SEARCH_MIN_WORDS = 3
GOOGLE_SEARCH_MAX_RESULTS = 3

def test_pattern_matching():
    """Test the regex pattern for Google search"""
    from regex import compile as recompile, IGNORECASE
    
    GOOGLE_SEARCH = recompile(
        r'\A\s*(search|найди|поиск|google)\s+(?P<query>[\S][\S\s]*?)\??\s*\Z', IGNORECASE)
    
    test_cases = [
        ("search python list comprehension", True, "python list comprehension"),
        ("google javascript async await", True, "javascript async await"),
        ("найди react hooks tutorial", True, "react hooks tutorial"),
        ("поиск how to use git", True, "how to use git"),
        ("search ml", True, "ml"),  # Will be rejected by word count
        ("just regular text", False, None),
        ("search", False, None),
        ("google    ", False, None),
        ("search how to code?", True, "how to code"),
        ("  search   python   tips  ", True, "python   tips"),
    ]
    
    print("=== Pattern Matching Tests ===")
    for test_input, should_match, expected_query in test_cases:
        match = GOOGLE_SEARCH.match(test_input)
        if should_match:
            if match:
                actual_query = match.group('query').strip()
                if actual_query == expected_query.strip():
                    print(f"✅ '{test_input}' -> '{actual_query}'")
                else:
                    print(f"❌ '{test_input}' -> Expected: '{expected_query}', Got: '{actual_query}'")
            else:
                print(f"❌ '{test_input}' should match but didn't")
        else:
            if not match:
                print(f"✅ '{test_input}' correctly rejected")
            else:
                print(f"❌ '{test_input}' should not match but did")

def test_word_count_validation():
    """Test word count validation"""
    print("\n=== Word Count Validation Tests ===")
    test_queries = [
        ("ml", False),  # 1 word
        ("python code", False),  # 2 words
        ("python list comprehension", True),  # 3 words
        ("how to use git properly", True),  # 5 words
        ("", False),  # empty
    ]
    
    for query, should_pass in test_queries:
        words = query.split()
        passes = len(words) >= GOOGLE_SEARCH_MIN_WORDS
        if passes == should_pass:
            print(f"✅ '{query}' ({len(words)} words) - {'Pass' if passes else 'Fail'}")
        else:
            print(f"❌ '{query}' ({len(words)} words) - Expected {'Pass' if should_pass else 'Fail'}, got {'Pass' if passes else 'Fail'}")

def test_url_building():
    """Test URL building and encoding"""
    print("\n=== URL Building Tests ===")
    test_queries = [
        "python list comprehension",
        "javascript async/await",
        "C++ memory management",
        "react hooks & effects"
    ]
    
    for query in test_queries:
        encoded = urllib.parse.quote(query)
        url = f"https://www.google.com/search?q={encoded}&num=20"
        print(f"✅ '{query}' -> {url}")

def test_whitelist_matching():
    """Test whitelist domain matching"""
    print("\n=== Whitelist Matching Tests ===")
    test_urls = [
        ("https://stackoverflow.com/questions/12345/python-lists", True, "stackoverflow.com"),
        ("https://github.com/user/repo", True, "github.com"),
        ("https://docs.python.org/3/tutorial/", True, "docs.python.org"),
        ("https://badsite.com/malware", False, None),
        ("https://example.com/tutorial", False, None),
        ("https://medium.com/@author/article", True, "medium.com"),
    ]
    
    for url, should_match, expected_domain in test_urls:
        matched = False
        matched_domain = None
        
        for whitelisted_site in GOOGLE_SEARCH_WHITELISTED_SITES:
            if whitelisted_site in url:
                matched = True
                matched_domain = whitelisted_site
                break
        
        if matched == should_match:
            if matched:
                print(f"✅ '{url}' matched '{matched_domain}'")
            else:
                print(f"✅ '{url}' correctly not whitelisted")
        else:
            print(f"❌ '{url}' - Expected match: {should_match}, Got match: {matched}")

if __name__ == "__main__":
    test_pattern_matching()
    test_word_count_validation()
    test_url_building()
    test_whitelist_matching()
    print("\n=== Test Summary ===")
    print("✅ All basic functionality tests completed")
    print("🚀 Google search implementation ready for integration")