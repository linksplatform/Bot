# -*- coding: utf-8 -*-
from regex import compile, IGNORECASE
from config import DEFAULT_PROGRAMMING_LANGUAGES_PATTERN_STRING as default_languages

HELP = compile(
    r"\A\s*(помощь|help)\s*\Z", IGNORECASE)

INFO = compile(
    r"\A\s*(инфо|info)\s*\Z", IGNORECASE)

UPDATE = compile(
    r"\A\s*(обновить|update)\s*\Z", IGNORECASE)

KARMA = compile(
    r"\A\s*(карма|karma)\s*\Z", IGNORECASE)

APPLY_KARMA = compile(
    r"\A(\[id(?<selectedUserId>\d+)\|@\w+\])?\s*(?P<operator>\+|\-)(?P<amount>[0-9]*)\s*\Z")

ADD_PROGRAMMING_LANGUAGE = compile(
    r"\A\s*\+=\s*(?P<language>" + default_languages + r")\s*\Z", IGNORECASE)

REMOVE_PROGRAMMING_LANGUAGE = compile(
    r"\A\s*\-=\s*(?P<language>" + default_languages + r")\s*\Z", IGNORECASE)

ADD_GITHUB_PROFILE = compile(
    r"\A\s*\+=\s*(https?://)?github.com/(?P<profile>[a-zA-Z0-9-_]+)/?\s*\Z", IGNORECASE)

REMOVE_GITHUB_PROFILE = compile(
    r"\A\s*\-=\s*(https?://)?github.com/(?P<profile>[a-zA-Z0-9-_]+)/?\s*\Z", IGNORECASE)

TOP = compile(
    r"\A\s*(топ|верх|top)\s*(?P<maximum_users>\d+)?\s*\Z", IGNORECASE)

BOTTOM = compile(
    r"\A\s*(низ|дно|bottom)\s*(?P<maximum_users>\d+)?\s*\Z", IGNORECASE)

TOP_LANGUAGES = compile(
    r"\A\s*(топ|верх|top)\s*(?P<languages>(" + default_languages +
    r")(\s+(" + default_languages + r"))*)\s*\Z", IGNORECASE)

BOTTOM_LANGUAGES = compile(
    r"\A\s*(низ|дно|bottom)\s*(?P<languages>(" + default_languages +
    r")(\s+(" + default_languages + r"))*)\s*\Z", IGNORECASE)

PEOPLE = compile(
    r"\A\s*(люди|народ|people)\s*(?P<maximum_users>\d+)?\s*\Z", IGNORECASE)

PEOPLE_LANGUAGES = compile(
    r"\A\s*(люди|народ|people)\s*(?P<languages>(" + default_languages +
    r")(\s+(" + default_languages + r"))*)\s*\Z", IGNORECASE)
