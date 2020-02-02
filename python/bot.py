from random import randint

from saya import Vk
from social_ethosa import BetterBotBase
from datetime import datetime, timedelta
import regex

import patterns

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
        Vk.__init__(self, token=BotToken, group_id=bot_group_id, debug=True)
        self.messages_to_delete = {}
        self.userbot = UserBot()
        self.debug = True

    def message_new(self, event):
        """
        Handling all new messages.
        """
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
        elif regex.findall(patterns.KARMA, message):
            self.send_karma(event, selected_user if selected_user else user, not selected_user)
        elif regex.findall(patterns.TOP, message):
            self.send_top(event)
        elif regex.findall(patterns.INFO, message):
            self.send_info(event, selected_user if selected_user else user, not selected_user)
        elif regex.findall(patterns.APPLY_KARMA, message):
            # Only for chat rooms
            if event["peer_id"] < 2000000000:
                return
            # Only for whitelisted chat rooms
            if event["peer_id"] not in chats_whitelist:
                self.send_not_in_whitelist(event, user)
                return
            # Only regular users can be selected
            if is_bot_selected:
                return

            if selected_user and (user.uid != selected_user.uid):
                match = regex.match(patterns.APPLY_KARMA, message)
                operator = match.group("operator")[0]
                amount = match.group("amount")

                # Downvotes disabled for users with negative rating
                if (operator == "-") and (user.rating < 0):
                    self.send_not_enough_karma_error(event, user)
                    return

                user_karma_change, selected_user_karma_change, voters = self.apply_karma_change(event, user, selected_user, operator, amount)
                base.save(selected_user)
                if user_karma_change:
                    base.save(user)
                self.send_karma_change(event, user_karma_change, selected_user_karma_change, voters)
                self.delete_message(event)
        elif regex.findall(patterns.ADD_PROGRAMMING_LANGUAGE, message):
            language = regex.match(patterns.ADD_PROGRAMMING_LANGUAGE, message).group('language')
            language = self.get_default_programming_language(language)
            if not language:
                return
            if "programming_languages" not in user.obj:
                user.programming_languages = []
                base.save(user)
            if language not in user.programming_languages:
                user.programming_languages.append(language)
                base.save(user)
            self.send_programming_languages_list(event, user)
        elif regex.findall(patterns.REMOVE_PROGRAMMING_LANGUAGE, message):
            language = regex.match(patterns.REMOVE_PROGRAMMING_LANGUAGE, message).group('language')
            language = self.get_default_programming_language(language)
            if not language:
                return
            if "programming_languages" not in user.obj:
                user.programming_languages = []
                base.save(user)
            if language in user.programming_languages:
                user.programming_languages.remove(language)
                base.save(user)
            self.send_programming_languages_list(event, user)
        elif regex.findall(patterns.TOP_LANGUAGES, message):
            match = regex.match(patterns.TOP_LANGUAGES, message)
            languages = match.group("languages")
            self.send_top_languages(event, languages)

    def delete_message(self, event, delay=2):
        peer_id = event['peer_id']
        if peer_id in userbot_chats and peer_id in chats_deleting:
            if peer_id not in self.messages_to_delete:
                self.messages_to_delete.update({peer_id: []})

            message_id = event['conversation_message_id']
            data = {'date': datetime.now() + timedelta(seconds=delay), 'id': message_id}
            self.messages_to_delete[peer_id].append(data)

    def apply_karma_change(self, event, user, selected_user, operator, amount):
        selected_user_karma_change = None
        user_karma_change = None
        voters = None

        amount = int(amount) if amount else 0

        # Personal karma transfer
        if amount > 0:
            if user.rating < amount:
                self.send_not_enough_karma_error(event, user)
                return user_karma_change, selected_user_karma_change, voters
            else:
                user_karma_change = self.apply_user_karma(user, -amount)
                amount = -amount if operator == "-" else amount
                selected_user_karma_change = self.apply_user_karma(selected_user, amount)

        # Collective vote
        elif amount == 0:
            if operator == "+":
                selected_user_karma_change, voters = self.apply_collective_vote(user, selected_user, "current", 2, +1)
            else:
                selected_user_karma_change, voters = self.apply_collective_vote(user, selected_user, "current_sub", 3, -1)

        return user_karma_change, selected_user_karma_change, voters

    def apply_collective_vote(self, user, selected_user, current_voters, number_of_voters, amount):
        if user.uid not in selected_user[current_voters]:
            selected_user[current_voters].append(user.uid)
        if len(selected_user[current_voters]) >= number_of_voters:
            voters = selected_user[current_voters]
            selected_user[current_voters] = []
            return self.apply_user_karma(selected_user, amount), voters
        return None, None

    def apply_user_karma(self, user, amount):
        user.rating += amount
        return (user.uid, user.name, user.rating-amount, user.rating)

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

    def get_default_programming_language(self, language):
        for default_programming_language in default_programming_languages:
            default_programming_language = default_programming_language.replace('\\', '')
            if default_programming_language.lower() == language.lower():
                return default_programming_language
        return None

    def contains_string(self, strings, matchedString, ignoreCase):
        if ignoreCase:
            for string in strings:
                if string.lower() == matchedString.lower():
                    return True
        else:
            for string in strings:
                if string == matchedString:
                    return True
        return False

    def contains_all_strings(self, strings, matchedStrings, ignoreCase):
        matchedStringsCount = len(matchedStrings)
        for string in strings:
            if self.contains_string(matchedStrings, string, ignoreCase):
                matchedStringsCount -= 1
                if matchedStringsCount == 0:
                    return True
        return False

    def send_karma_change(self, event, user_karma_change, selected_user_karma_change, voters):
        if selected_user_karma_change and user_karma_change:
            self.send_message(event, "Карма изменена: [id%s|%s] [%s]->[%s], [id%s|%s] [%s]->[%s]." % (user_karma_change + selected_user_karma_change))
        elif selected_user_karma_change:
            self.send_message(event, "Карма изменена: [id%s|%s] [%s]->[%s]. Голосовали: (%s)" % (selected_user_karma_change + (", ".join(["@id%s" % (voter) for voter in voters]),)))

    def send_karma(self, event, user, is_self=True):
        if is_self:
            response = "[id%s|%s], Ваша карма - [%s]."
        else:
            response = "Карма [id%s|%s] - [%s]."
        self.send_message(event, response % (user.uid, user.name, user.rating))

    def send_info(self, event, user, is_self=True):
        programming_languages_string = self.get_programming_languages_string(user)
        if is_self:
            response = "[id%s|%s], Ваша карма - [%s].\nВаши языки программирования - %s"
        else:
            response = "Карма [id%s|%s] - [%s].\nЯзыки программирования - %s"
        if not programming_languages_string:
            programming_languages_string = "отсутствуют"
        self.send_message(event, response % (user.uid, user.name, user.rating, programming_languages_string))

    def send_top_users(self, event, users):
        if not users:
            return
        response = "\n".join(["[%s] [id%s|%s] %s" % (user["rating"], user["uid"], user["name"], self.get_programming_languages_string_with_parentheses_or_empty(user)) for user in users])
        self.send_message(event, response)

    def send_top(self, event):
        users = base.getSortedByKeys("rating", otherKeys=["programming_languages"])
        users = [i for i in users if (i["rating"] != 0) or ("programming_languages" in i and len(i["programming_languages"]) > 0)]
        self.send_top_users(event, users)

    def send_top_languages(self, event, languages):
        languages = regex.split(r"\s+", languages)
        users = base.getSortedByKeys("rating", otherKeys=["programming_languages"])
        users = [i for i in users if ("programming_languages" in i and len(i["programming_languages"]) > 0) and self.contains_all_strings(i["programming_languages"], languages, True)]
        self.send_top_users(event, users)

    def send_programming_languages_list(self, event, user):
        programming_languages_string = self.get_programming_languages_string(user)
        if not programming_languages_string:
            self.send_message(event, "[id%s|%s], у Вас не указано языков программирования." % (user.uid, user.name))
        else:
            self.send_message(event, "[id%s|%s], Ваши языки программирования: %s." % (user.uid, user.name, programming_languages_string))

    def send_help(self, event):
        self.send_message(event, help_string)

    def send_not_in_whitelist(self, event, user):
        self.send_message(event, "Извините, [id%s|%s], но Ваша беседа [%s] отсутствует в белом списке для начисления кармы." % (user.uid, user.name, event["peer_id"]))

    def send_not_enough_karma_error(self, event, user):
        self.send_message(
            event,
            "Извините, [id%s|%s], но Вашей кармы [%s] недостаточно :(" % (user.uid, user.name, user.rating)
        )

    def send_message(self, event, message):
        self.messages.send(
            message=message, peer_id=event["peer_id"],
            disable_mentions=1, random_id=randint(-INT32, INT32)
        )


if __name__ == '__main__':
    vk = V()
    vk.start_listen()
