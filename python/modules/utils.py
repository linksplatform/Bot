# -*- coding: utf-8 -*-
from typing import NoReturn, List
import requests
import config
import re


def get_default_programming_language(
    language: str
) -> str:
    """Returns default appearance of language
    """
    language = language.lower()
    for lang in config.DEFAULT_PROGRAMMING_LANGUAGES:
        if lang.replace('\\', '').lower() == language:
            return lang
    return ""


def contains_string(
    strings: List[str],
    matched_string: str,
    ignore_case: bool
) -> bool:
    """Returns True if `matched_string` in `strings`.

    :param strings: list of strings where contains matched string.
    :param matched_string: source string
    """
    if ignore_case:
        matched_string = matched_string.lower()
        for string in strings:
            if string.lower() == matched_string:
                return True
    else:
        return matched_string in strings
    return False


def contains_all_strings(
    strings: List[str],
    matched_strings: List[str],
    ignore_case: bool
) -> bool:
    """Returns True if `strings` in `matched_strings`.
    """
    matched_strings_count = len(matched_strings)
    for string in strings:
        if contains_string(matched_strings, string, ignore_case):
            matched_strings_count -= 1
            if matched_strings_count == 0:
                return True
    return False


def karma_limit(karma: int) -> int:
    """Returns karma hours limit.
    """
    for limit_item in config.KARMA_LIMIT_HOURS:
        if not limit_item["min_karma"] or karma >= limit_item["min_karma"]:
            if not limit_item["max_karma"] or karma < limit_item["max_karma"]:
                return limit_item["limit"]
    return 168  # hours (a week)


def is_available_ghpage(
    profile: str
) -> bool:
    """Returns True if github profile is available.
    """
    return requests.get(f'https://github.com/{profile}').status_code == 200


def parse_programming_languages(
    text: str
) -> List[str]:
    """Parse programming language names from text, handling multi-word languages.
    
    This function correctly handles languages like 'Visual Basic' that contain spaces,
    which would be incorrectly split by a simple regex split.
    
    :param text: Input text containing language names
    :return: List of matched language names in their canonical form
    """
    if not text:
        return []
    
    # Get all language names without regex escaping
    language_names = []
    for lang_pattern in config.DEFAULT_PROGRAMMING_LANGUAGES:
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
