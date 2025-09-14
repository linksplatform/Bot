#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test the fix for Visual Basic parsing."""

import sys
import os

# Add the python directory to sys.path to import modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'python'))

# Test only the new function without importing the full module
from typing import List
import re

DEFAULT_PROGRAMMING_LANGUAGES = [
    r"Assembler",
    r"JavaScript",
    r"TypeScript", 
    r"Java",
    r"Python",
    r"PHP",
    r"Ruby",
    r"C\+\+",
    r"C",
    r"Shell",
    r"C#",
    r"Objective\-C",
    r"R",
    r"VimL",
    r"Go",
    r"Perl",
    r"CoffeeScript",
    r"TeX",
    r"Swift",
    r"Kotlin",
    r"F#",
    r"Scala",
    r"Scheme",
    r"Emacs Lisp",
    r"Lisp",
    r"Haskell",
    r"Lua",
    r"Clojure",
    r"TLA\+",
    r"PlusCal",
    r"Matlab",
    r"Groovy",
    r"Puppet",
    r"Rust",
    r"PowerShell",
    r"Pascal",
    r"Delphi",
    r"SQL",
    r"Nim",
    r"1С",
    r"КуМир",
    r"Scratch",
    r"Prolog",
    r"GLSL",
    r"HLSL",
    r"Whitespace",
    r"Basic",
    r"Visual Basic",
    r"Parser",
    r"Erlang",
    r"Wolfram",
    r"Brainfuck",
    r"Pawn",
    r"Cobol",
    r"Fortran",
    r"Arduino",
    r"Makefile",
    r"CMake",
    r"D",
    r"Forth",
    r"Dart",
    r"Ada",
    r"Julia",
    r"Malbolge",
    r"Лого",
    r"Verilog",
    r"VHDL",
    r"Altera",
    r"Processing",
    r"MetaQuotes",
    r"Algol",
    r"Piet",
    r"Shakespeare",
    r"G\-code",
    r"Whirl",
    r"Chef",
    r"BIT",
    r"Ook",
    r"MoonScript",
    r"PureScript",
    r"Idris",
    r"Elm",
    r"Minecraft",
    r"Crystal",
    r"C\-\-",
    r"Go\!",
    r"Tcl",
    r"Solidity",
    r"AssemblyScript",
    r"Vimscript",
    r"Pony",
    r"LOLCODE",
    r"Elixir",
    r"X#",
    r"NVPTX",
    r"Nemerle",
]

def get_default_programming_language(language: str) -> str:
    """Returns default appearance of language"""
    language = language.lower()
    for lang in DEFAULT_PROGRAMMING_LANGUAGES:
        if lang.replace('\\', '').lower() == language:
            return lang
    return ""

def parse_programming_languages(text: str) -> List[str]:
    """Parse programming language names from text, handling multi-word languages."""
    if not text:
        return []
    
    # Get all language names without regex escaping
    language_names = []
    for lang_pattern in DEFAULT_PROGRAMMING_LANGUAGES:
        # Remove regex escaping to get actual language name  
        lang_name = (lang_pattern.replace('\\+', '+')
                                .replace('\\-', '-')
                                .replace('\\#', '#')
                                .replace('\\!', '!')
                                .replace('\\', ''))
        language_names.append(lang_name)
    
    # Sort by length (longest first) to prioritize multi-word languages
    language_names.sort(key=len, reverse=True)
    
    matched_languages = []
    remaining_text = text
    
    # Find each language in the text (case-insensitive)
    for lang_name in language_names:
        # Check if this language appears in the remaining text
        pattern = r'\b' + re.escape(lang_name) + r'\b'
        match = re.search(pattern, remaining_text, re.IGNORECASE)
        
        if match:
            # Use the utility function to get the correct canonical form
            canonical_name = get_default_programming_language(lang_name)
            if canonical_name:
                # Remove regex escaping from the canonical name for return
                clean_name = (canonical_name.replace('\\+', '+')
                                           .replace('\\-', '-')
                                           .replace('\\#', '#')
                                           .replace('\\!', '!')
                                           .replace('\\', ''))
                if clean_name not in matched_languages:
                    matched_languages.append(clean_name)
                    # Remove the matched text to avoid overlapping matches
                    remaining_text = remaining_text[:match.start()] + remaining_text[match.end():]
    
    return matched_languages

def test_fix():
    """Test the fix"""
    print("Testing the fix for Visual Basic parsing:")
    print()
    
    # Test cases that should work with the fix
    test_cases = [
        "Visual Basic",
        "visual basic", 
        "Python Java",
        "Visual Basic C#",
        "JavaScript TypeScript",
        "C++ Java"
    ]
    
    print("=== NEW BEHAVIOR (with fix) ===")
    for case in test_cases:
        result = parse_programming_languages(case)
        print(f"'{case}' -> {result}")
    
    print()
    print("=== OLD BEHAVIOR (without fix) ===")
    # Simulate old behavior
    for case in test_cases:
        old_result = re.split(r'\s+', case)
        print(f"'{case}' -> {old_result}")

if __name__ == "__main__":
    test_fix()