# -*- coding: utf-8 -*-

# Bot group id
bot_group_id = 190877945

# Pluses and minuses will be removed in these chats
chats_deleting = [
    2000000001,
    2000000006
]

# Check your search line, when you`re in the needed chat. Then copy it`s id after "vk.com/im?peers=c"
userbot_chats = {
    2000000001: 477,
    2000000006: 423
}

# Chats where you can change reputation of other users
chats_whitelist = [
    2000000001,
    2000000006
]

INT32 = 2147483647

help_string = """Вот что я умею:
"помощь" или "help" — вывод этого сообщения.
"топ" или "top" — вывести список пользователей начиная с кармой/рейтингом/репутацией или указанными языками программирования (сортировка от наибольшей кармы к наименьшей).
"топ C++ C#" или "top Java" — вывести список пользователей с кармой, у которых указанны только те языки программирования, которые содержатся в сообщении пользователя.
"дно", "низ" или "bottom" — вывести список пользователей с кармой или указанными языками программирования (сортировка от наименьшей кармы к наибольшей).
"карма" или "karma" — вывести свою карму, или карму другого пользователя.
"инфо" или "info" — вывести записанную информацию (карму и языки программирования) о себе или другом пользователе.
"+" или "-" — принять участие в коллективном голосование за или против другого пользователя.
"+5" или "-4" — передать свою карму другому пользователю или пожертвовать своей кармой, чтобы проголосовать против него.
"+= C++" или "-= C#" — добавить или удалить язык программирования из своего списока языков программирования.

1 единица кармы прибавляется, если два разных человека голосуют за "+".
1 единица кармы отнимается, если три разных человека голосуют против "-".

Команды по отношению к другим пользователям запускаются путём ответа или репоста их сообщений.
Голосовать против других пользователей могут только те пользователи, у кого не отрицательная карма, т.е. 0 и более.
Голосование за самого себя не работает.
Все команды указаны в кавычках, однако отправлять в чат их нужно без кавычек, чтобы они выполнились.
"""

default_programming_languages = [
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
]

default_programming_languages_pattern_string = "|".join(default_programming_languages)

