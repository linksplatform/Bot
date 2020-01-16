from random import randint

from saya import Vk
from social_ethosa import BetterBotBase
import regex

base = BetterBotBase("users", "dat")
base.addPattern("rating", 0)
base.addPattern("quest_price", 0)
base.addPattern("programming_languages", [])
base.addPattern("current", [])
base.addPattern("current_sub", [])

default_programming_languages = [
    "JavaScript",
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
    "Scala",
    "Emacs Lisp",
    "Lisp",
    "Haskell",
    "Lua",
    "Clojure",
    "Matlab",
    "Arduino",
    "Groovy",
    "Puppet",
    "Rust",
    "PowerShell",
    "Pascal",
    "SQL",
    "Nim",
    "1С",
    "КуМир",
    "Scratch"
]
default_programming_languages_pattern_string = "|".join(default_programming_languages)

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

        if regex.findall(r"\A\s*(рейтинг|rating)\s*\Z", message):
            self.send_rating(event, selected_user if selected_user else user, not selected_user)
        elif regex.findall(r"\A\s*(топ|top)\s*\Z", message):
            self.send_top(event)
        elif regex.findall(r"\A\s*(я|me)\s*\+=\s*(" + default_programming_languages_pattern_string + r")\s*\Z", message):
            match = regex.match(r"\A\s*(я|me)\s*\+=\s*(?P<language>" + default_programming_languages_pattern_string + r")\s*\Z", message)
            language = match.group("language")
            if "programming_languages" not in user.obj:
                user.programming_languages = []
                base.save(user)
            if not language in user.programming_languages:
                user.programming_languages.append(language)
            base.save(user)
            self.send_message(event, "Ваши языки программирования: %s." % (self.get_programming_languages_string(user)))
        elif regex.findall(r"\A\s*(\+|\-)[0-9]*\s*\Z", message):
            # Only for chat rooms
            if event["peer_id"] < 2000000000:
                return None
            # Only regular users can be selected
            if is_bot_selected:
                return None

            match = regex.match(r"\A\s*(?P<operator>\+|\-)(?P<amount>[0-9]*)\s*\Z", message)
            operator = match.group("operator")[0]
            number = match.group("amount")

            # Downvotes disabled for users with negative rating
            if (operator == "-") and (user.rating < 0):
                self.send_not_enough_rating_error(event, user)
                return None
            
            n = user.quest_price
            if number:
                n = int(number)
            if not n:
                n = 0
            if operator == "-":
                n = -n
                
            if selected_user and (user.uid != selected_user.uid):
                self.send_rating_change(event, user, selected_user, operator, n)
            else:
                self.send_reward_change(event, user, operator, n)
                                           
    def send_rating_change(self, event, user, selected_user, operator, amount):
        selected_user_rating_change = None
        user_rating_change = None
        is_transfer = False

        if amount != 0:
            if user.rating < abs(amount):
                self.send_not_enough_rating_error(event, user)
                return None
            else:
                if user.quest_price == amount:
                    user.quest_price = 0
                user.rating -= abs(amount)
                user_rating_change = (user.name, user.rating+abs(amount), user.rating)
                is_transfer = True
        if operator == "+":
            if (not is_transfer) and (user.uid not in selected_user.current):
                selected_user.current.append(user.uid)
            if is_transfer or (len(selected_user.current) >= 2):
                if not is_transfer:
                    selected_user.current = []
                    amount = 1
                selected_user.rating += amount
                selected_user_rating_change = (selected_user.name, selected_user.rating-amount, selected_user.rating)
        else:
            if (not is_transfer) and (user.uid not in selected_user.current_sub):
                selected_user.current_sub.append(user.uid)
            if is_transfer or (len(selected_user.current_sub) >= 3):
                if not is_transfer:
                    selected_user.current_sub = []
                    amount = -1
                selected_user.rating += amount
                selected_user_rating_change = (selected_user.name, selected_user.rating-amount, selected_user.rating)
        base.save(selected_user)
        if is_transfer:
            base.save(user)
        if selected_user_rating_change:
            if user_rating_change:
                self.send_message(event, "Рейтинг изменён: %s [%s]->[%s], %s [%s]->[%s]" % (user_rating_change + selected_user_rating_change))
            else:
                self.send_message(event, "Рейтинг изменён: %s [%s]->[%s]" % selected_user_rating_change)
                                           
    def send_reward_change(self, event, user, operator, amount):
        if operator == "+":
            if (amount != 0) and (user.rating < amount):
                self.send_not_enough_rating_error(event, user)
            else:
                user.quest_price = amount
                base.save(user)
                self.send_message(event, "Вы установили награду, ваш следующий + теперь [%s]" % (amount))

    def get_messages(self, event):
        reply_message = event.get("reply_message", {})
        fwd_messages = event.get("fwd_messages", [])
        return [reply_message] if reply_message else fwd_messages
        
    def send_top(self, event):
        users = base.getSortedByKeys("rating", otherKeys=["programming_languages"]) 
        users = [i for i in users if i["rating"] != 0]
        response = "\n".join(["[id%s|%s]%s - [%s]" % (user["uid"], user["name"], self.get_programming_languages_string_with_parentheses_or_empty(user), user["rating"]) for user in users])
        self.send_message(event, response)

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
        if  len(languages) > 0:
            return ", ".join(languages)
        else:
            return ""
    
    def send_rating(self, event, user, is_self = True):
        if is_self:
            response = "%s, Ваш рейтинг - [%s]"
        else:
            response = "Рейтинг %s - [%s]"
        self.send_message(event, response % (user.name, user.rating))

    def send_not_enough_rating_error(self, event, user):
        self.send_message(event, "Извините, но вашего рейтинга [%s] недостаточно :(" % (user.rating))
                           
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
