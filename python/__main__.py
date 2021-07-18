from saya import Vk

import regex
import requests
from datetime import datetime, timedelta

import config
import patterns

from tokens import BotToken
from userbot import UserBot
from DataService import BetterBotBaseDataService

CHAT_ID_OFFSET = 2e9


class V(Vk):
    def __init__(self, token, group_id, debug=True):
        Vk.__init__(self, token=token, group_id=group_id, debug=debug)
        self.messages_to_delete = {}
        self.userbot = UserBot()
        self.debug = True

        self.data = BetterBotBaseDataService()

    def message_new(self, event):
        """
        Handling all new messages.
        """
        event = event["object"]["message"]
        print(event)
        if event['peer_id'] in self.messages_to_delete:
            peer = CHAT_ID_OFFSET + config.userbot_chats[event['peer_id']]
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

        user = self.data.get_or_create_user(event["from_id"], self)

        message = event["text"].lstrip("/")
        messages = self.get_messages(event)
        selected_message = messages[0] if len(messages) == 1 else None
        selected_user = self.data.get_or_create_user(selected_message["from_id"], self) if selected_message else None
        is_bot_selected = selected_message and (selected_message["from_id"] < 0)

        karma_enabled = event["peer_id"] in config.chats_karma_whitelist
        group_chat = event["peer_id"] >= CHAT_ID_OFFSET

        if group_chat:
            if karma_enabled:
                match = regex.match(patterns.KARMA, message)
                if match:
                    return self.send_karma(event, selected_user if selected_user else user, not selected_user)
                match = regex.match(patterns.TOP, message)
                if match:
                    maximum_users = match.group("maximum_users")
                    maximum_users = int(maximum_users) if maximum_users else 0
                    return self.send_top(event, maximum_users)
                match = regex.match(patterns.BOTTOM, message)
                if match:
                    maximum_users = match.group("maximum_users")
                    maximum_users = int(maximum_users) if maximum_users else 0
                    return self.send_bottom(event, maximum_users)
                match = regex.match(patterns.APPLY_KARMA, message)
                if match:
                    # Only regular users can be selected
                    if is_bot_selected:
                        return

                    if not selected_user:
                        selected_user_id = match.group("selectedUserId")
                        if selected_user_id:
                            selected_user = self.data.get_or_create_user(int(selected_user_id), self)

                    if selected_user and (self.data.get_user_property(user, "uid") != selected_user.uid):
                        operator = match.group("operator")[0]
                        amount = match.group("amount")
                        amount = int(amount) if amount else 0

                        utcnow = datetime.utcnow()

                        # Downvotes disabled for users with negative karma
                        if (operator == "-") and (self.data.get_user_property(user, "karma") < 0):
                            self.delete_message(event)
                            self.send_not_enough_karma_error(event, user)
                            return

                        # Collective votes limit
                        if amount == 0:
                            utclast = datetime.fromtimestamp(float(self.data.get_user_property(user, "last_collective_vote")));
                            difference = utcnow - utclast
                            hours_difference = difference.total_seconds() / 3600;
                            hours_limit = self.get_karma_hours_limit(self.data.get_user_property(user, "karma"));
                            if hours_difference < hours_limit:
                                self.delete_message(event)
                                self.send_not_enough_hours_error(event, user, hours_limit, difference.total_seconds() / 60)
                                return

                        user_karma_change, selected_user_karma_change, collective_vote_applied, voters = self.apply_karma_change(event, user, selected_user, operator, amount)

                        if collective_vote_applied:
                            self.data.set_user_property(user, "last_collective_vote", int(utcnow.timestamp()))
                            self.data.save(user)

                        self.data.save(selected_user)

                        if user_karma_change:
                            self.data.save(user)
                        self.send_karma_change(event, user_karma_change, selected_user_karma_change, voters)
                        self.delete_message(event)
                        return
                match = regex.match(patterns.TOP_LANGUAGES, message)
                if match:
                    languages = match.group("languages")
                    return self.send_top_languages(event, languages)
            else:
                match = regex.match(patterns.PEOPLE, message)
                if match:
                    maximum_users = match.group("maximum_users")
                    maximum_users = int(maximum_users) if maximum_users else 0
                    return self.send_people(event, maximum_users)
                match = regex.match(patterns.PEOPLE_LANGUAGES, message)
                if match:
                    languages = match.group("languages")
                    return self.send_people_languages(event, languages)

        match = regex.match(patterns.HELP, message)
        if match:
            return self.send_help(event, group_chat, karma_enabled)
        match = regex.match(patterns.INFO, message)
        if match:
            return self.send_info(event, karma_enabled, selected_user if selected_user else user, not selected_user)
        match = regex.match(patterns.UPDATE, message)
        if match:
            name = self.get_user_name(event["from_id"])
            self.data.set_user_property(user, "name", name)
            self.data.save(user)
            return self.send_info(event, karma_enabled, selected_user if selected_user else user, not selected_user)
        match = regex.match(patterns.ADD_PROGRAMMING_LANGUAGE, message)
        if match:
            language = match.group('language')
            language = self.get_default_programming_language(language)
            if not language:
                return
            if language not in self.data.get_user_property(user, "programming_languages"):
                self.data.get_user_property(user, "programming_languages").append(language)
                self.data.save(user)
            return self.send_programming_languages_list(event, user)
        match = regex.match(patterns.REMOVE_PROGRAMMING_LANGUAGE, message)
        if match:
            language = match.group('language')
            language = self.get_default_programming_language(language)
            if not language:
                return
            if language in self.data.get_user_property(user, "programming_languages"):
                self.data.get_user_property(user, "programming_languages").remove(language)
                self.data.save(user)
            return self.send_programming_languages_list(event, user)
        match = regex.match(patterns.ADD_GITHUB_PROFILE, message)
        if match:
            profile = match.group('profile')
            if not profile:
                return
            if profile != self.data.get_user_property(user, "get_user_property"):
                if requests.get(f'https://github.com/{profile}').status_code == 200:
                    self.data.set_user_property(user, "github_profile", profile)
                    self.data.save(user)
            return self.send_github_profile(event, user)
        match = regex.match(patterns.REMOVE_GITHUB_PROFILE, message)
        if match:
            profile = match.group('profile')
            if not profile:
                return
            if profile == self.data.get_user_property(user, "github_profile"):
                self.data.set_user_property(user, "github_profile", "")
                self.data.save(user)
            return self.send_github_profile(event, user)

    def delete_message(self, event, delay=2):
        peer_id = event['peer_id']
        if peer_id in config.userbot_chats and peer_id in config.chats_deleting:
            if peer_id not in self.messages_to_delete:
                self.messages_to_delete.update({peer_id: []})

            message_id = event['conversation_message_id']
            data = {'date': datetime.now() + timedelta(seconds=delay), 'id': message_id}
            self.messages_to_delete[peer_id].append(data)

    def get_messages(self, event):
        reply_message = event.get("reply_message", {})
        return [reply_message] if reply_message else event.get("fwd_messages", [])

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
        matched_strings_count = len(matchedStrings)
        for string in strings:
            if self.contains_string(matchedStrings, string, ignoreCase):
                matched_strings_count -= 1
                if matched_strings_count == 0:
                    return True
        return False

    def get_github_profile_string(self, user):
        profile = self.data.get_user_property(user, "github_profile")
        return f"github.com/{profile}" if profile else "отсутствует"

    def get_github_profile_top_string(self, user):
        profile = self.data.get_user_property(user, "github_profile")
        return f" — github.com/{profile}" if profile else ""

    def get_programming_languages_string(self, user):
        languages = self.data.get_user_sorted_programming_languages(user)
        return ", ".join(languages) if len(languages) > 0 else "отсутствуют"

    def get_programming_languages_string_with_parentheses_or_empty(self, user):
        programming_languages_string = self.get_user_sorted_programming_languages(user)
        if programming_languages_string == "":
            return programming_languages_string
        else:
            return "(" + programming_languages_string + ")"

    def calculate_real_karma(self, user):
        base_karma = self.data.get_user_property(user, "karma")
        up_votes = len(self.data.get_user_property(user, "supporters"))/config.positive_votes_per_karma
        down_votes = len(self.data.get_user_property(user, "opponents"))/config.negative_votes_per_karma
        return base_karma + up_votes - down_votes

    def get_users_sorted_by_karma(self, peer_id):
        members = self.get_members_ids(peer_id)
        users = self.data.get_sorted_by_karma(other_keys=["programming_languages", "supporters", "opponents", "github_profile", "uid"],  sort_key=self.calculate_real_karma)
        if members:
            users = [u for u in users if u["uid"] in members]
        return users

    def get_users_sorted_by_name(self, peer_id):
        members = self.get_members_ids(peer_id)
        users = self.data.get_by_name(other_keys=["programming_languages", "github_profile", "uid"])
        if members:
            users = [u for u in users if u["uid"] in members]
        users.reverse()
        return users

    def get_members(self, peer_id):
        return self.messages.getConversationMembers(peer_id=peer_id)

    def get_user_name(self, from_id):
        return self.users.get(user_ids=from_id)['response'][0]["first_name"]

    def get_members_ids(self, peer_id):
        members = self.get_members(peer_id)
        if "error" in members:
            return None
        else:
            return [m["member_id"] for m in members["response"]["items"] if m["member_id"] > 0]

    def get_default_programming_language(self, language):
        for default_programming_language in config.default_programming_languages:
            default_programming_language = default_programming_language.replace('\\', '')
            if default_programming_language.lower() == language.lower():
                return default_programming_language
        return None

    def apply_karma_change(self, event, user, selected_user, operator, amount):
        selected_user_karma_change = None
        user_karma_change = None
        collective_vote_applied = None
        voters = None

        # Personal karma transfer
        if amount > 0:
            if self.data.get_user_property(user, "karma") < amount:
                self.send_not_enough_karma_error(event, user)
                return user_karma_change, selected_user_karma_change, collective_vote_applied, voters
            else:
                user_karma_change = self.data.apply_user_karma(user, -amount)
                amount = -amount if operator == "-" else amount
                selected_user_karma_change = self.data.apply_user_karma(selected_user, amount)

        # Collective vote
        elif amount == 0:
            if operator == "+":
                selected_user_karma_change, voters, collective_vote_applied = self.apply_collective_vote(user, selected_user, "supporters", config.positive_votes_per_karma, +1)
            else:
                selected_user_karma_change, voters, collective_vote_applied = self.apply_collective_vote(user, selected_user, "opponents", config.negative_votes_per_karma, -1)

        return user_karma_change, selected_user_karma_change, collective_vote_applied, voters

    def apply_user_karma(self, user, amount):
        self.data.set_user_property(user, "karma", self.data.get_user_property(user, "karma") + amount)
        return (user.uid,
                self.data.get_user_property(user, "name"),
                self.data.get_user_property(user, "karma") - amount,
                self.data.get_user_property(user, "karma"))

    def get_karma_hours_limit(self, karma):
        for limit_item in config.karma_limit_hours:
            if (not limit_item["min_karma"]) or (karma >= limit_item["min_karma"]):
                if (not limit_item["max_karma"]) or (karma < limit_item["max_karma"]):
                    return limit_item["limit"]
        return 168  # hours (a week)

    def apply_collective_vote(self, user, selected_user, current_voters, number_of_voters, amount):
        vote_applied = None
        if user.uid not in selected_user[current_voters]:
            selected_user[current_voters].append(user.uid)
            vote_applied = True
        if len(selected_user[current_voters]) >= number_of_voters:
            voters = selected_user[current_voters]
            selected_user[current_voters] = []
            return self.data.apply_user_karma(selected_user, amount), voters, vote_applied
        return None, None, vote_applied

    def get_karma_string(self, user):
        plus_string = ""
        minus_string = ""
        karma = self.data.get_user_property(user, "karma")
        plus_votes = len(self.data.get_user_property(user, "supporters"))
        minus_votes = len(self.data.get_user_property(user, "opponents"))
        if plus_votes > 0:
            plus_string = "+%.1f" % (plus_votes / config.positive_votes_per_karma)
        if minus_votes > 0:
            minus_string = "-%.1f" % (minus_votes / config.negative_votes_per_karma)
        if plus_votes > 0 or minus_votes > 0:
            return f"[{karma}][{plus_string}{minus_string}]"
        else:
            return f"[{karma}]"

    def send_karma_change(self, event, user_karma_change, selected_user_karma_change, voters):
        if selected_user_karma_change and user_karma_change:
            self.send_message(event, "Карма изменена: [id%s|%s] [%s]->[%s], [id%s|%s] [%s]->[%s]." % (user_karma_change + selected_user_karma_change))
        elif selected_user_karma_change:
            self.send_message(event, "Карма изменена: [id%s|%s] [%s]->[%s]. Голосовали: (%s)" % (selected_user_karma_change + (", ".join([f"@id{voter}" for voter in voters]),)))

    def send_karma(self, event, user, is_self=True):
        if is_self:
            response = "[id%s|%s], Ваша карма — %s."
        else:
            response = "Карма [id%s|%s] — %s."
        self.send_message(event, response % (self.data.get_user_property(user, "uid"),
                                             self.data.get_user_property(user, "name"),
                                             self.get_karma_string(user)))

    def send_info(self, event, karma_enabled, user, is_self=True):
        programming_languages_string = self.get_programming_languages_string(user)
        profile = self.get_github_profile_string(user)
        if karma_enabled:
            if is_self:
                response = "[id%s|%s], Ваша карма — %s.\nВаши языки программирования: %s\nВаша страничка на GitHub — %s"
            else:
                response = "Карма [id%s|%s] — %s.\nЯзыки программирования: %s\nCтраничка на GitHub — %s"
            return self.send_message(event, response % (self.data.get_user_property(user, "uid"),
                                                        self.data.get_user_property(user, "name"),
                                                        self.get_karma_string(user), programming_languages_string, profile))
        else:
            if is_self:
                response = "[id%s|%s], \nВаши языки программирования: %s\nВаша страничка на GitHub — %s"
            else:
                response = "[id%s|%s]. \nЯзыки программирования: %s\nCтраничка на GitHub — %s"
            return self.send_message(event, response % (self.data.get_user_property(user, "uid"),
                                                        self.data.get_user_property(user, "name"),
                                                        programming_languages_string, profile))

    def send_top_users(self, event, users):
        if not users:
            return
        user_strings = ["%s [id%s|%s]%s %s" % (self.get_karma_string(user),
                                               self.data.get_user_property(user, "uid"),
                                               self.data.get_user_property(user, "name"),
                                               self.get_github_profile_top_string(user),
                                               self.get_programming_languages_string_with_parentheses_or_empty(user)) for user in users]
        total_symbols = 0
        i = 0
        for user_string in user_strings:
            user_string_length = len(user_string)
            if (total_symbols + user_string_length + 2) >= 4096:  # Maximum message size for VK API (messages.send)
                user_strings = user_strings[:i]
            else:
                total_symbols += user_string_length + 2
                i += 1
        response = "\n".join(user_strings)
        self.send_message(event, response)

    def send_bottom(self, event, maximum_users):
        peer_id = event["peer_id"]
        users = self.get_users_sorted_by_karma(peer_id)
        users = [i for i in users if (i["karma"] != 0) or ("programming_languages" in i and len(i["programming_languages"]) > 0)]
        if (maximum_users > 0) and (len(users) >= maximum_users):
            users.reverse()
            self.send_top_users(event, users[:maximum_users])
        else:
            self.send_top_users(event, reversed(users))
    
    def send_people_users(self, event, users):
        if not users:
            return
        user_strings = ["[id%s|%s]%s %s" % (self.data.get_user_property(user, "uid"),
                                            self.data.get_user_property(user, "name"),
                                            self.get_github_profile_top_string(user),
                                            self.get_programming_languages_string_with_parentheses_or_empty(user)) for user in users]
        total_symbols = 0
        i = 0
        for user_string in user_strings:
            user_string_length = len(user_string)
            if (total_symbols + user_string_length + 2) >= 4096: # Maximum message size for VK API (messages.send)
                user_strings = user_strings[:i]
            else:
                total_symbols += user_string_length + 2
                i += 1
        response = "\n".join(user_strings)
        self.send_message(event, response)

    def send_people(self, event, maximum_users):
        peer_id = event["peer_id"]
        users = self.get_users_sorted_by_name(peer_id)
        users = [i for i in users if i["github_profile"] or ("programming_languages" in i and len(i["programming_languages"]) > 0)]
        if (maximum_users > 0) and (len(users) >= maximum_users):
            self.send_people_users(event, users[:maximum_users])
        else:
            self.send_people_users(event, users)

    def send_top(self, event, maximum_users):
        peer_id = event["peer_id"]
        users = self.get_users_sorted_by_karma(peer_id)
        users = [i for i in users if (i["karma"] != 0) or ("programming_languages" in i and len(i["programming_languages"]) > 0)]
        if (maximum_users > 0) and (len(users) >= maximum_users):
            self.send_top_users(event, users[:maximum_users])
        else:
            self.send_top_users(event, users)

    def send_people_languages(self, event, languages):
        languages = regex.split(r"\s+", languages)
        peer_id = event["peer_id"]
        users = self.get_users_sorted_by_name(self, peer_id)
        users = [i for i in users if ("programming_languages" in i and len(i["programming_languages"]) > 0) and self.contains_all_strings(i["programming_languages"], languages, True)]
        self.send_people_users(event, users)

    def send_top_languages(self, event, languages):
        languages = regex.split(r"\s+", languages)
        peer_id = event["peer_id"]
        users = self.get_users_sorted_by_karma(peer_id)
        users = [i for i in users if ("programming_languages" in i and len(i["programming_languages"]) > 0) and self.contains_all_strings(i["programming_languages"], languages, True)]
        self.send_top_users(event, users)

    def send_github_profile(self, event, user):
        profile = self.get_github_profile_string(user)
        if not profile:
            self.send_message(event, f"[id{self.data.get_user_property(user, 'uid')}|{self.data.get_user_property(user, 'name')}], у Вас не указана страничка на GitHub.")
        else:
            self.send_message(event, f"[id{self.data.get_user_property(user, 'uid')}|{self.data.get_user_property(user, 'name')}], Ваша страничка на GitHub — {profile}.")

    def send_programming_languages_list(self, event, user):
        programming_languages_string = self.get_programming_languages_string(user)
        if not programming_languages_string:
            self.send_message(event, f"[id{self.data.get_user_property(user, 'uid')}|{self.data.get_user_property(user, 'name')}], у Вас не указано языков программирования.")
        else:
            self.send_message(event, f"[id{self.data.get_user_property(user, 'uid')}|{self.data.get_user_property(user, 'name')}], Ваши языки программирования: {programming_languages_string}.")

    def send_help(self, event, group_chat, karma_enabled):
        if group_chat:
            if karma_enabled:
                self.send_message(event, config.help_string_with_karma)
            else:
                self.send_message(event, config.help_string % event["peer_id"])
        else:
            self.send_message(event, config.help_string_private_chat)

    def send_not_in_whitelist(self, event, user):
        peer_id = event["peer_id"]
        message = f"Извините, [id{self.data.get_user_property(user, 'uid')}|{self.data.get_user_property(user, 'name')}], но Ваша беседа [{peer_id}] отсутствует в белом списке для начисления кармы."
        self.send_message(event, message)

    def send_not_enough_karma_error(self, event, user):
        message = f"Извините, [id{self.data.get_user_property(user, 'uid')}|{self.data.get_user_property(user, 'name')}], но Вашей кармы [{self.data.get_user_property(user, 'karma')}] недостаточно :("
        self.send_message(event, message)

    def send_not_enough_hours_error(self, event, user, hours_limit, difference_minutes):
        message = f"Извините, [id{self.data.get_user_property(user, 'uid')}|{self.data.get_user_property(user, 'name')}], но с момента вашего последнего голоса ещё не прошло {hours_limit} ч. :( До следующего голоса осталось {int(hours_limit * 60 - difference_minutes)} м."
        self.send_message(event, message)

    def send_message(self, event, message):
        self.messages.send(message=message, peer_id=event["peer_id"], disable_mentions=1, random_id=0)


if __name__ == '__main__':
    vk = V(token=BotToken, group_id=config.bot_group_id)
    vk.start_listen()
