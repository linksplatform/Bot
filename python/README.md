# VK Bot

## Help

### Список команд

| | Значение |
| - | -
| ⭐ | команды работающие и в беседах и в личных сообщениях бота. 
| ⚡ | команды работающие во всех беседах.
| ✔️ | команды работающие только в чате с включенной кармой.

| | Команда | Описание |
| --- | --- | --- |
| ⭐ | help | Вывести сообщения с основной информацией о боте. |
| ⚡ | people | Вывести информацию о участниках беседы. |
| ✔️ | top | Вывести информацию о участниках беседы в порядке уменьшения кармы. |
| ✔️ | top [ЯЗЫКИ] | Вывести информацию о участниках беседы с указанными языками в порядке уменьшения кармы. |
| ✔️ | top [ЧИСЛО] | Вывести информацию об указанном числе участников беседы в порядке уменьшения кармы. |
| ✔️ | bottom | Вывести информацию о участниках беседы в порядке увеличения кармы. |
| ✔️ | bottom [ЯЗЫКИ] | Вывести информацию о участниках беседы с указанными языками в порядке увеличения кармы. |
| ✔️ | bottom [ЧИСЛО] | Вывести информацию об указанном числе участников беседы беседы в порядке увеличения кармы. |
| ✔️ | karma | Вывод своей кармы или кармы участника беседы из пересланного сообщения. |
| ⭐ | info | Вывести общую информацию (карма (только для бесед с кармой), добавленные языки, ссылка на профиль github) о себе или участнике беседы из пересланного сообщения. |
| ⭐ | update | Обновить информацию о вас (имя). Эта команда так же выводит информацию о вас как это делает команда info. |
| ✔️ | + | Проголосовать за повышение кармы участника беседы из пересланного сообщения. |
| ✔️ | - | Проголосовать за понижение кармы участника беседы из пересланного сообщения. |
| ✔️ | +[ЧИСЛО] | Повысить карму участника беседы из пересланного сообщения на указанное число, потратив свою. |
| ✔️ | -[ЧИСЛО] | Понизить карму участника беседы из пересланного сообщения на указанное число, потратив свою. |
| ⭐ | += [ЯЗЫК] | Добавить язык программирования в свой профиль. |
| ⭐ | -= [ЯЗЫК] | Убрать язык программирования из своего профиля. |
| ⭐ | += [ССЫЛКА] | Добавить ссылку на профиль github в свой профиль. |
| ⭐ | -= [ССЫЛКА] | Убрать ссылку на профиль github из своего профиля. |
| ⭐ | what is [] | Искать в википедии |
 
Так же возможно использование альтернативных названий команд. 

| Английская версия | Русская версия | Алиас |
| --- | --- | --- |
| help | помощь |
| people | люди |
| top | топ | верх |
| bottom | дно | низ |
| karma | карма|
| info | инфо |
| update | обновить |
| what is | что такое |

![image](https://user-images.githubusercontent.com/1431904/146941784-670f052c-7b4e-4367-89c5-00466dfc92c9.png)

При голосовании за повышение или понижение кармы пользователя требуется 2 или 3 голоса от различных участников беседы соответственно. 

Голосовать за понижение кармы пользователя можно только при неотрицательной карме. 

Промежуток между вкладом голосов за изменение кармы пользователей зависит от показателя кармы голосующего: 

| Карма | Промежуток
| - | -
| до -20 | 8 часов
| от -19 до -2 | 4 часа
| от -1 до 1 | 2 часа
| от 2 до 19 | 1 час
| от 20 | 30 минут

Список доступных языков программирования 
* Assembler 
* JavaScript 
* TypeScript 
* Java 
* Python 
* PHP 
* Ruby 
* C++ 
* C 
* Shell 
* C# 
* Objective-C 
* R 
* VimL 
* Go 
* Perl 
* CoffeeScript 
* TeX 
* Swift 
* Kotlin 
* F# 
* Scala 
* Scheme 
* Emacs Lisp 
* Lisp 
* Haskell 
* Lua 
* Clojure 
* TLA+ 
* PlusCal 
* Matlab 
* Groovy 
* Puppet 
* Rust 
* PowerShell 
* Pascal 
* Delphi 
* SQL 
* Nim 
* 1С 
* КуМир 
* Scratch 
* Prolog 
* GLSL 
* HLSL 
* Whitespace 
* Basic 
* Visual Basic 
* Parser 
* Erlang 
* Wolfram 
* Brainfuck 
* Pawn 
* Cobol 
* Fortran 
* Arduino 
* Makefile 
* CMake 
* D 
* Forth 
* Dart 
* Ada 
* Julia 
* Malbolge 
* Лого 
* Verilog 
* VHDL 
* Altera 
* Processing 
* MetaQuotes 
* Algol 
* Piet 
* Shakespeare 
* G-code 
* Whirl 
* Chef 
* BIT 
* Ook 
* MoonScript 
* PureScript 
* Idris 
* Elm 
* Minecraft 
* Crystal 
* C-- 
* Go! 
* Tcl 
* Solidity
* Nemerle
* AssemblyScript
* Vimscript
* Pony
* LOLCODE
* Elixir
* X#
* NVPTX

Если нужного языка нет в списке, о его добавлении можно попросить [здесь](https://github.com/linksplatform/Bot/issues/15)

## Prerequisites
* [Git](https://git-scm.com/downloads)
* [Python 3](https://www.python.org/downloads)

## Install
```
git clone https://github.com/linksplatform/Bot.git
cd Bot
pip3 install -r requirements.txt
```

## Update

```
git pull
pip3 install -r requirements.txt --upgrade
```

## Configure

0. Enable VK Long Poll API with `5.103` version for your bot's group. ![Screenshot_20210630_110401](https://user-images.githubusercontent.com/1431904/123924747-eed01900-d992-11eb-8f8e-cf66b398ed90.png)
1. Set bot group id in [config.py](https://github.com/linksplatform/Bot/blob/e10f51c7e3711c43708ce5659c7de9e76cab6702/python/config.py#L3-L4).
2. Add tokens into `python/tokens.py` file
    * `BotToken` (an access token of your VK group). This token should have two access settings `community management` and `community messages`. This token is **required**. ![Screenshot_20210630_110724](https://user-images.githubusercontent.com/1431904/123925211-5d14db80-d993-11eb-8e79-9cb0ac49d1c6.png)
    * `UserToken` (an access token to your VK user via KateMobile). This token is used to delete messages in the chats where your user is administrator. This token is **optional**.

## Run

```Shell
cd python
```

With output to console:

```Shell
python3 __main__.py
```

With output to file:

```Shell
python3 __main__.py > bot.log 2>&1
```

## Do not upload tokens.py with your real tokens please

To tell git to ignore this file:

```
git update-index --assume-unchanged python/tokens.py
```

## See the bot in action

To see the bot in action you can [join](https://vk.me/join/AJQ1d9E/bxbPjY87MeKsXgMa) the chat with this bot.
