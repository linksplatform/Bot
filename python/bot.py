from random import randint

from saya import Vk
from social_ethosa import BetterBotBase
import regex

base = BetterBotBase("users", "dat")
base.addPattern("rating", 0)
base.addPattern("programming_languages", [])
base.addPattern("current", [])
base.addPattern("current_sub", [])

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
    "Objective\-C",
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
]
default_programming_languages_pattern_string = "|".join(default_programming_languages)

chats_whitelist = [
    2000000001,
    2000000006
]

help_string = """Вот что я умею:
"помощь" или "help" — вывод этого сообщения.
"топ" или "top" — вывести список пользователей с рейтингом, или указанными языками программирования.
"рейтинг" или "rating" — вывести свой рейтинг, или рейтинг другого пользователя.
"+" или "-" — принять участие в коллективном голосование за или против другого пользователя.
"+5" или "-4" — передать свой рейтинг другому пользователю или пожертвовать своим рейтингом, чтобы проголосовать против него.
"+= C++" — добавить язык программирования в свой список языков программирования.

1 единица рейтинга прибавляется, если два разных человека голосуют за "+".
1 единица рейтинга отнимается, если три разных человека голосуют против "-".

Команды по отношению к другим пользователям запускаются путём ответа или репоста их сообщений.
Голосовать против других пользователей могут только те пользователи, у кого не отрицательный рейтинг, т.е. 0 и более.
Голосование за самого себя не работает.
Все команды указаны в кавычках, однако отправлять в чат их нужно без кавычек, чтобы они выполнились.
"""


class V(Vk):
    def __init__(self):
        with open("token.txt", "r") as f:
            token = f.read()
        Vk.__init__(self, token=token, group_id=190877945)

    def message_new(self, event):
        event = event["object"]["message"]
        user = base.autoInstall(event["from_id"], self) if event["from_id"] > 0 else None

        message = event["text"].lstrip("/")
        messages = self.get_messages(event)
        selected_message = messages[0] if len(messages) == 1 else None
        selected_user = base.autoInstall(selected_message["from_id"], self) if selected_message else None
        is_bot_selected = selected_message and (selected_message["from_id"] < 0)

        if regex.findall(r"\A\s*(помощь|help)\s*\Z", message):
            self.send_help(event)
        elif regex.findall(r"\A\s*(рейтинг|rating)\s*\Z", message):
            self.send_rating(event, selected_user if selected_user else user, not selected_user)
        elif regex.findall(r"\A\s*(топ|top)\s*\Z", message):
            self.send_top(event)
        elif regex.findall(r"\A\s*\+=\s*(" + default_programming_languages_pattern_string + r")\s*\Z", message):
            match = regex.match(r"\A\s*\+=\s*(?P<language>" + default_programming_languages_pattern_string + r")\s*\Z", message)
            language = match.group("language")
            if "programming_languages" not in user.obj:
                user.programming_languages = []
                base.save(user)
            if language not in user.programming_languages:
                user.programming_languages.append(language)
            base.save(user)
            self.send_message(event, "Ваши языки программирования: %s." % (self.get_programming_languages_string(user)))
        elif regex.findall(r"\A\s*(\+|\-)[0-9]*\s*\Z", message):
            # Only for chat rooms
            if event["peer_id"] < 2000000000:
                return None
            # Only for whitelisted chat rooms
            if event["peer_id"] not in chats_whitelist:
                self.send_not_in_whitelist(event)
                return None
            # Only regular users can be selected
            if is_bot_selected:
                return None

            if selected_user and (user.uid != selected_user.uid):
                match = regex.match(r"\A\s*(?P<operator>\+|\-)(?P<amount>[0-9]*)\s*\Z", message)
                operator = match.group("operator")[0]
                amount = match.group("amount")

                # Downvotes disabled for users with negative rating
                if (operator == "-") and (user.rating < 0):
                    self.send_not_enough_rating_error(event, user)
                    return None

                user_rating_change, selected_user_rating_change = self.apply_rating_change(event, user, selected_user, operator, amount)
                base.save(selected_user)
                if user_rating_change:
                    base.save(user)
                self.send_rating_change(event, user_rating_change, selected_user_rating_change)

    def apply_rating_change(self, event, user, selected_user, operator, amount):
        selected_user_rating_change = None
        user_rating_change = None

        amount = int(amount) if amount else 0

        print(selected_user.name)
        print(operator)
        print(amount)
        print(selected_user.current)
        print(selected_user.current_sub)

        # Personal rating transfer
        if amount > 0:
            if user.rating < amount:
                self.send_not_enough_rating_error(event, user)
                return None
            else:
                user_rating_change = self.apply_user_rating(user, -amount)
                amount = -amount if operator == "-" else amount
                selected_user_rating_change = self.apply_user_rating(selected_user, amount)

        # Collective vote
        elif amount == 0:
            if operator == "+":
                selected_user_rating_change = self.apply_collective_vote(user, selected_user, "current", 2, +1)
            else:
                selected_user_rating_change = self.apply_collective_vote(user, selected_user, "current_sub", 3, -1)

        return user_rating_change, selected_user_rating_change

    def apply_collective_vote(self, user, selected_user, current_voters, number_of_voters, amount):
        if user.uid not in selected_user[current_voters]:
            selected_user[current_voters].append(user.uid)
        if len(selected_user[current_voters]) >= number_of_voters:
            selected_user[current_voters] = []
            return self.apply_user_rating(selected_user, amount)

    def apply_user_rating(self, user, amount):
        user.rating += amount
        return (user.name, user.rating-amount, user.rating)

    def get_messages(self, event):
        reply_message = event.get("reply_message", {})
        return [reply_message] if reply_message else event.get("fwd_messages", [])

    def get_programming_languages_string_with_parentheses_or_empty(self, user):
        programming_languages_string = self.get_programming_languages_string(user)
        if programming_languages_string == "":
            return programming_languages_string
        else:
            return "(" + programming_languages_string + ")"

    def get_programming_languages_string(self, user):
        if isinstance(user, dict):
            languages = user["programming_languages"] if "programming_languages" in user else []
        else:
            languages = user.programming_languages
        if len(languages) > 0:
            return ", ".join(languages)
        else:
            return ""

    def send_rating_change(self, event, user_rating_change, selected_user_rating_change):
        if selected_user_rating_change and user_rating_change:
            self.send_message(event, "Рейтинг изменён: %s [%s]->[%s], %s [%s]->[%s]." % (user_rating_change + selected_user_rating_change))
        elif selected_user_rating_change:
            self.send_message(event, "Рейтинг изменён: %s [%s]->[%s]." % selected_user_rating_change)

    def send_rating(self, event, user, is_self=True):
        if is_self:
            response = "%s, Ваш рейтинг - [%s]."
        else:
            response = "Рейтинг %s - [%s]."
        self.send_message(event, response % (user.name, user.rating))

    def send_top(self, event):
        users = base.getSortedByKeys("rating", otherKeys=["programming_languages"])
        users = [i for i in users if (i["rating"] != 0) or ("programming_languages" in i and len(i["programming_languages"]) > 0)]
        response = "\n".join(["[%s] [id%s|%s] %s" % (user["rating"], user["uid"], user["name"], self.get_programming_languages_string_with_parentheses_or_empty(user)) for user in users])
        self.send_message(event, response)

    def send_help(self, event):
        self.send_message(event, help_string)

    def send_not_in_whitelist(self, event):
        self.send_message(event, "Извините, но Ваша беседа [%s] отсутствует в белом списке для начисления рейтинга." % (event["peer_id"]))

    def send_not_enough_rating_error(self, event, user):
        self.send_message(event, "Извините, но Вашего рейтинга [%s] недостаточно :(" % (user.rating))

    def send_message(self, event, message):
        self.messages.send(message=message, peer_id=event["peer_id"], disable_mentions=1, random_id=randint(0, 1000))

if __name__ == '__main__':
    vk = V()
    print("start listen ...")

    @vk.longpoll.on_listen_end
    def restart(event):
        print("restart ...")
        vk.start_listen()
    vk.start_listen()
