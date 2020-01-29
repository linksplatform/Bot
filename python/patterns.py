# -*- coding: utf-8 -*-
from regex import compile, IGNORECASE
from config import default_programming_languages_pattern_string as default_languages

TOP = compile(r"\A\s*(топ|top)\s*\Z", IGNORECASE)
TOP_LANGUAGES = compile(r"\A\s*(топ|top)\s*((" + default_languages + r")\s*)+\s*\Z", IGNORECASE)
HELP = compile(r"\A\s*(помощь|help)\s*\Z", IGNORECASE)
RATING = compile(r"\A\s*(рейтинг|rating)\s*\Z", IGNORECASE)

PROGRAMMING_LANGUAGES = compile(r"\A\s*\+=\s*(" + default_languages + r")\s*\Z")
PROGRAMMING_LANGUAGES_MATCH = compile(r"\A\s*\+=\s*(?P<language>" + default_languages + r")\s*\Z")

RATING_OPERATOR_MATCH = compile(r"\A\s*(?P<operator>\+|\-)(?P<amount>[0-9]*)\s*\Z")
SET_RATING = compile(r'\A\s*(\+|\-)[0-9]*\s*\Z')
