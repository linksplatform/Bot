# -*- coding: utf-8 -*-

# Bot's VK group id (change it to your group id)
BOT_GROUP_ID = 190877945

# Pluses and minuses will be removed in these chats
CHATS_DELETING = [
    #2000000001,
    #2000000006
]

# Check your search line, when you`re in the needed chat. Then copy it`s id after "vk.com/im?peers=c"
USERBOT_CHATS = {
    #2000000001: 477,
    #2000000006: 423
}

# Chats where you can change reputation of other users
CHATS_KARMA_WHITELIST = [
    2000000001,
    2000000011
]

POSITIVE_VOTES_PER_KARMA = 2
NEGATIVE_VOTES_PER_KARMA = 3

KARMA_LIMIT_HOURS = [
    { "min_karma": None, "max_karma": -19,  "limit": 8 },
    { "min_karma": -19,  "max_karma": -1,   "limit": 4 },
    { "min_karma": -1,  "max_karma": 2,   "limit": 2 },
    { "min_karma": 2,  "max_karma": 20,   "limit": 1 },
    { "min_karma": 20,   "max_karma": None, "limit": 0.5 },
]

DEFAULT_PROGRAMMING_LANGUAGES = [
    "Assembler",
    "JavaScript",
    "TypeScript",
    "Java",
    "Python",
    "PHP",
    "Ruby",
    "C\+\+",
    "C",
    "Shell",
    "C\#",
    "Objective-C",
    "R",
    "VimL",
    "Go",
    "Perl",
    "CoffeeScript",
    "TeX",
    "Swift",
    "Kotlin",
    "F\#",
    "Scala",
    "Scheme",
    "Emacs Lisp",
    "Lisp",
    "Haskell",
    "Lua",
    "Clojure",
    "TLA\+",
    "PlusCal",
    "Matlab",
    "Groovy",
    "Puppet",
    "Rust",
    "PowerShell",
    "Pascal",
    "Delphi",
    "SQL",
    "Nim",
    "1С",
    "КуМир",
    "Scratch",
    "Prolog",
    "GLSL",
    "HLSL",
    "Whitespace",
    "Basic",
    "Visual Basic",
    "Parser",
    "Erlang",
    "Wolfram",
    "Brainfuck",
    "Pawn",
    "Cobol",
    "Fortran",
    "Arduino",
    "Makefile",
    "CMake",
    "D",
    "Forth",
    "Dart",
    "Ada",
    "Julia",
    "Malbolge",
    "Лого",
    "Verilog",
    "VHDL",
    "Altera",
    "Processing",
    "MetaQuotes",
    "Algol",
    "Piet",
    "Shakespeare",
    "G-code",
    "Whirl",
    "Chef",
    "BIT",
    "Ook",
    "MoonScript",
    "PureScript",
    "Idris",
    "Elm",
    "Minecraft",
    "Crystal",
    "C--",
    "Go!",
    "Tcl",
    "Solidity",
    "AssemblyScript",
    "Vimscript",
    "LOLCODE",
    "Elixir",
    "X\#",
    "NVPTX",
    "Nemerle",
]

DEFAULT_PROGRAMMING_LANGUAGES_PATTERN_STRING = "|".join(default_programming_languages)
