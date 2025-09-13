# -*- coding: utf-8 -*-
"""Configuration template for Telegram bot.

Copy this file to config.py and fill in your bot token.
"""

# Bot settings - REQUIRED
# Get your bot token from @BotFather on Telegram
BOT_TOKEN = "YOUR_BOT_TOKEN_HERE"

# Karma system configuration
POSITIVE_VOTES_PER_KARMA = 2  # Votes needed to increase karma by 1
NEGATIVE_VOTES_PER_KARMA = 3  # Votes needed to decrease karma by 1

# Karma cooldown periods (in hours) based on user karma
KARMA_LIMIT_HOURS = [
    {"min_karma": None, "max_karma": -19, "limit": 8},   # Very low karma: 8 hours
    {"min_karma": -19, "max_karma": -1, "limit": 4},     # Low karma: 4 hours  
    {"min_karma": -1, "max_karma": 2, "limit": 2},       # Neutral karma: 2 hours
    {"min_karma": 2, "max_karma": 20, "limit": 1},       # Good karma: 1 hour
    {"min_karma": 20, "max_karma": None, "limit": 0.5},  # High karma: 30 minutes
]

# Available programming languages (you can modify this list)
DEFAULT_PROGRAMMING_LANGUAGES = [
    "Assembler", "JavaScript", "TypeScript", "Java", "Python", "PHP", "Ruby",
    "C++", "C", "Shell", "C#", "Objective-C", "R", "VimL", "Go", "Perl",
    "CoffeeScript", "TeX", "Swift", "Kotlin", "F#", "Scala", "Scheme",
    "Emacs Lisp", "Lisp", "Haskell", "Lua", "Clojure", "TLA+", "PlusCal",
    "Matlab", "Groovy", "Puppet", "Rust", "PowerShell", "Pascal", "Delphi",
    "SQL", "Nim", "1С", "КуМир", "Scratch", "Prolog", "GLSL", "HLSL",
    "Whitespace", "Basic", "Visual Basic", "Parser", "Erlang", "Wolfram",
    "Brainfuck", "Pawn", "Cobol", "Fortran", "Arduino", "Makefile", "CMake",
    "D", "Forth", "Dart", "Ada", "Julia", "Malbolge", "Лого", "Verilog",
    "VHDL", "Altera", "Processing", "MetaQuotes", "Algol", "Piet",
    "Shakespeare", "G-code", "Whirl", "Chef", "BIT", "Ook", "MoonScript",
    "PureScript", "Idris", "Elm", "Minecraft", "Crystal", "C--", "Go!",
    "Tcl", "Solidity", "AssemblyScript", "Vimscript", "Pony", "LOLCODE",
    "Elixir", "X#", "NVPTX", "Nemerle"
]

# GitHub Copilot integration settings (future feature)
GITHUB_COPILOT_LANGUAGES = {
    'Python': ['.py', 'python'],
    'JavaScript': ['.js', 'javascript'],
    'TypeScript': ['.ts', 'typescript'],
    'C#': ['.cs', 'csharp'],
    'Go': ['.go', 'go'],
    'Java': ['.java', 'java'],
    'Kotlin': ['.kt', 'kotlin'],
    'Ruby': ['.rb', 'ruby'],
    'PHP': ['.php', 'php'],
    'C': ['.c', 'c'],
    'C++': ['.cpp', 'cpp'],
}

GITHUB_COPILOT_TIMEOUT = 120  # seconds

# Data storage configuration (JSON-based, no SQL required)
DATA_DIR = "data"
USERS_FILE = f"{DATA_DIR}/users.json"
KARMA_VOTES_FILE = f"{DATA_DIR}/karma_votes.json"