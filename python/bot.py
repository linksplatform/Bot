from random import randint

from saya import Vk
from social_ethosa import BetterBotBase
from datetime import datetime, timedelta
import regex

import patterns as patterns

from tokens import BotToken
from userbot import UserBot
from config import *

base = BetterBotBase("users", "dat")
base.addPattern("rating", 0)
base.addPattern("programming_languages", [])
base.addPattern("current", [])
base.addPattern("current_sub", [])


class V(Vk):
    def __init__(self):
        Vk.__init__(self, token=BotToken, group_id=bot_group_id)
        self.messages_to_delete = {}
        self.userbot = UserBot()
        self.debug = True

    def message_new(self, event):
        event = event["object"]["message"]

        if event['peer_id'] in self.messages_to_delete:
            peer = 2000000000 + userbot_chats[event['peer_id']]
            new_messages_to_delete = []
            ids = []

            for item in self.messages_to_delete[event['peer_id']]:
                if item['date'] > datetime.now():
                    new_messages_to_delete.append(item)
                else:
                    ids.append(item['id'])

            if new_messages_to_delete:
                self.messages_to_delete[event['peer_id']] = new_messages_to_delete
            else:
                self.messages_to_delete.pop(event['peer_id'])

            if ids:
                self.userbot.delete_messages(ids, peer)

        user = base.autoInstall(event["from_id"], self) if event["from_id"] > 0 else None

        message = event["text"].lstrip("/")
        messages = self.get_messages(event)
        selected_message = messages[0] if len(messages) == 1 else None
        selected_user = base.autoInstall(selected_message["from_id"], self) if selected_message else None
        is_bot_selected = selected_message and (selected_message["from_id"] < 0)

        if regex.findall(patterns.HELP, message):
            self.send_help(event)
        elif regex.findall(patterns.RATING, message):
            self.send_rating(event, selected_user if selected_user else user, not selected_user)
        elif regex.findall(patterns.TOP, message):
            self.send_top(event)
        elif regex.findall(patterns.PROGRAMMING_LANGUAGES, message):
            language = regex.match(patterns.PROGRAMMING_LANGUAGES_MATCH, message).group('language')
            if "programming_languages" not in user.obj:
                user.programming_languages = []
                base.save(user)
            if language not in user.programming_languages:
                user.programming_languages.append(language)
            base.save(user)
            self.send_message(event, "Ваши языки программирования: %s." % (self.get_programming_languages_string(user)))
        elif regex.findall(patterns.SET_RATING, message):
            # Only for chat rooms
            if event["peer_id"] < 2000000000:
                return
            # Only for whitelisted chat rooms
            if event["peer_id"] not in chats_whitelist:
                self.send_not_in_whitelist(event)
                return
            # Only regular users can be selected
            if is_bot_selected:
                return

            if selected_user and (user.uid != selected_user.uid):
                match = regex.match(patterns.RATING_OPERATOR_MATCH, message)
                operator = match.group("operator")[0]
                amount = match.group("amount")
                print(amount)

                # Downvotes disabled for users with negative rating
                if (operator == "-") and (user.rating < 0):
                    self.send_not_enough_rating_error(event, user)
                    return

                user_rating_change, selected_user_rating_change = self.apply_rating_change(event, user, selected_user, operator, amount)
                base.save(selected_user)
                if user_rating_change:
                    base.save(user)

                self.send_rating_change(event, user_rating_change, selected_user_rating_change)
                self.delete_message(event)
        elif regex.findall(patterns.TOP_LANGUAGES, message):
            self.send_top_langs(event)

    def delete_message(self, event, delay=2):
        peer_id = event['peer_id']

        if peer_id in userbot_chats and peer_id in chats_deleting:
            if peer_id not in self.messages_to_delete:
                self.messages_to_delete.update({peer_id: []})

            message_id = event['conversation_message_id']
            data = {'date': datetime.now() + timedelta(seconds=delay), 'id': message_id}
            self.messages_to_delete[peer_id].append(data)

    def apply_rating_change(self, event, user, selected_user, operator, amount):
        selected_user_rating_change = None
        user_rating_change = None

        amount = int(amount) if amount else 0

        # Personal rating transfer
        if amount > 0:
            if user.rating < amount:
                self.send_not_enough_rating_error(event, user)
                return user_rating_change, selected_user_rating_change
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

    def contains(self, target, other_list):
        length = len(target)
        now = 0
        for _, item in enumerate(other_list):
            if item.lower() in target.lower():
                now += 1
        if now >= length:
            return True

    def send_top_langs(self, event):
        text = regex.sub(r"\A\s*(топ|top)\s*", r"", event["text"])
        langs = regex.split(r"\s+", text)
        print(langs, text)
        users = base.getSortedByKeys("rating", otherKeys=["programming_languages"])
        users = [i for i in users if (i["rating"] != 0) or ("programming_languages" in i and len(i["programming_languages"]) > 0)]
        response = "\n".join(
            ["[%s] [id%s|%s] %s" % (
                user["rating"],
                user["uid"],
                user["name"],
                self.get_programming_languages_string_with_parentheses_or_empty(user))
             for user in users if self.contains(langs, user["programming_languages"])]
        )
        self.send_message(event, response)

    def send_help(self, event):
        self.send_message(event, help_string)

    def send_not_in_whitelist(self, event):
        self.send_message(event, "Извините, но Ваша беседа [%s] отсутствует в белом списке для начисления рейтинга." % (event["peer_id"]))

    def send_not_enough_rating_error(self, event, user):
        self.send_message(event, "Извините, но Вашего рейтинга [%s] недостаточно :(" % (user.rating))

    def send_message(self, event, message):
        self.messages.send(message=message, peer_id=event["peer_id"], disable_mentions=1, random_id=randint(-INT32, INT32))


if __name__ == '__main__':
    vk = V()
    print("start listen ...")

    @vk.longpoll.on_listen_end
    def restart(event):
        print("restart ...")
        vk.start_listen()
    vk.start_listen()
