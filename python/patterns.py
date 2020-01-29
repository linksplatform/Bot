# -*- coding: utf-8 -*-
from regex import compile, IGNORECASE
from config import default_programming_languages_pattern_string as default_languages

HELP = compile(r"\A\s*(помощь|help)\s*\Z", IGNORECASE)
KARMA = compile(r"\A\s*(карма|karma)\s*\Z", IGNORECASE)
APPLY_KARMA = compile(r"\A\s*(?P<operator>\+|\-)(?P<amount>[0-9]*)\s*\Z")
TOP = compile(r"\A\s*(топ|top)\s*\Z", IGNORECASE)
TOP_LANGUAGES = compile(r"\A\s*(топ|top)\s*(?P<languages>(" + default_languages + r")(\s+(" + default_languages + r"))*)\s*\Z", IGNORECASE)
ADD_PROGRAMMING_LANGUAGE = compile(r"\A\s*\+=\s*(?P<language>" + default_languages + r")\s*\Z", IGNORECASE)
REMOVE_PROGRAMMING_LANGUAGE = compile(r"\A\s*\-=\s*(?P<language>" + default_languages + r")\s*\Z", IGNORECASE)
