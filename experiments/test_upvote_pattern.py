#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script for the new UPVOTE_PREVIOUS pattern."""
import sys
import os

# Add python directory to path
python_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'python')
sys.path.append(python_dir)

from patterns import UPVOTE_PREVIOUS
from regex import match

def test_upvote_pattern():
    """Test the UPVOTE_PREVIOUS pattern."""
    
    test_cases = [
        # Should match
        ("+", True),
        (" + ", True),
        (" +  ", True),
        ("  +", True),
        ("+  ", True),
        
        # Should not match
        ("++", False),
        ("+ hello", False),
        ("hello +", False),
        ("+5", False),
        ("+-", False),
        ("", False),
        ("hello", False),
    ]
    
    print("Testing UPVOTE_PREVIOUS pattern...")
    
    all_passed = True
    for test_input, expected in test_cases:
        result = bool(match(UPVOTE_PREVIOUS, test_input))
        status = "PASS" if result == expected else "FAIL"
        
        if result != expected:
            all_passed = False
            
        print(f"  '{test_input}' -> {result} (expected {expected}) [{status}]")
    
    print(f"\nOverall: {'PASS' if all_passed else 'FAIL'}")
    return all_passed

if __name__ == "__main__":
    test_upvote_pattern()