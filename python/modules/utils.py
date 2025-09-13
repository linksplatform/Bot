# -*- coding: utf-8 -*-
from typing import NoReturn, List
import requests
import config


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


def max_languages_allowed(karma: int) -> int:
    """Returns maximum number of languages allowed based on karma.
    
    :param karma: user's karma value
    :return: maximum number of languages the user can have
    """
    for limit_item in config.LANGUAGE_LIMITS:
        min_karma = limit_item["min_karma"]
        max_karma = limit_item["max_karma"]
        
        # Check if karma falls within this range
        min_condition = min_karma is None or karma >= min_karma
        max_condition = max_karma is None or karma < max_karma
        
        if min_condition and max_condition:
            return limit_item["max_languages"]
    
    # Default fallback (should not happen with proper config)
    return 1


def is_available_ghpage(
    profile: str
) -> bool:
    """Returns True if github profile is available.
    """
    return requests.get(f'https://github.com/{profile}').status_code == 200
