# -*- coding: utf-8 -*-
"""This module contains Regex patterns to message handling."""
from regex import compile as recompile, IGNORECASE
from config import (
    DEFAULT_PROGRAMMING_LANGUAGES_PATTERN_STRING as DEFAULT_LANGUAGES,
    GITHUB_COPILOT_LANGUAGES_PATTERN_STRING as COPILOT_LANGUAGES
)

HELP = recompile(
    r'\A\s*(помощь|help)\s*\Z', IGNORECASE)

INFO = recompile(
    r'\A\s*(инфо|info)\s*\Z', IGNORECASE)

UPDATE = recompile(
    r'\A\s*(обновить|update)\s*\Z', IGNORECASE)

KARMA = recompile(
    r'\A\s*(карма|karma)\s*\Z', IGNORECASE)

APPLY_KARMA = recompile(
    r'\A(\[id(?<selectedUserId>\d+)\|@\w+\])?\s*(?P<operator>\+|\-)(?P<amount>[0-9]*)\s*\Z')

ADD_PROGRAMMING_LANGUAGE = recompile(
    r'\A\s*\+=\s*(?P<language>' + DEFAULT_LANGUAGES + r')\s*\Z', IGNORECASE)

REMOVE_PROGRAMMING_LANGUAGE = recompile(
    r'\A\s*\-=\s*(?P<language>' + DEFAULT_LANGUAGES + r')\s*\Z', IGNORECASE)

ADD_GITHUB_PROFILE = recompile(
    r'\A\s*\+=\s*(https?://)?github.com/(?P<profile>[a-zA-Z0-9-_]+)/?\s*\Z', IGNORECASE)

REMOVE_GITHUB_PROFILE = recompile(
    r'\A\s*\-=\s*(https?://)?github.com/(?P<profile>[a-zA-Z0-9-_]+)/?\s*\Z', IGNORECASE)

TOP = recompile(
    r'\A\s*(топ|верх|top)\s*(?P<maximum_users>\d+)?\s*\Z', IGNORECASE)

BOTTOM = recompile(
    r'\A\s*(низ|дно|bottom)\s*(?P<maximum_users>\d+)?\s*\Z', IGNORECASE)

TOP_LANGUAGES = recompile(
    r'\A\s*(топ|верх|top)\s*(?P<count>\d+\s+)?\s*(?P<languages>(' + DEFAULT_LANGUAGES +
    r')(\s+(' + DEFAULT_LANGUAGES + r'))*)\s*\Z', IGNORECASE)

BOTTOM_LANGUAGES = recompile(
    r'\A\s*(низ|дно|bottom)\s*(?P<count>\d+\s+)?\s*(?P<languages>(' + DEFAULT_LANGUAGES +
    r')(\s+(' + DEFAULT_LANGUAGES + r'))*)\s*\Z', IGNORECASE)

PEOPLE = recompile(
    r'\A\s*(люди|народ|people)\s*(?P<maximum_users>\d+)?\s*\Z', IGNORECASE)

PEOPLE_LANGUAGES = recompile(
    r'\A\s*(люди|народ|people)\s*(?P<languages>(' + DEFAULT_LANGUAGES +
    r')(\s+(' + DEFAULT_LANGUAGES + r'))*)\s*\Z', IGNORECASE)

WHAT_IS = recompile(
    r'\A\s*(what is|что такое|що таке)\s+(?P<question>[\S\s]+?)\??\s*\Z', IGNORECASE)

WHAT_MEAN = recompile(
    r'\A\s*(what does\s+([\S ]+?)\s+mean\s*\??\s*|(что значит|що таке)\s+([\S ]+)\?\s*)\Z', IGNORECASE)

# lang - list of copilot langs from config.py
# text - task description
GITHUB_COPILOT = recompile(
    r'\A\s*(code|код)\s+(?P<lang>(' + COPILOT_LANGUAGES +
    r'))(?P<text>[\S\s]+)\Z', IGNORECASE)
