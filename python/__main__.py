# -*- coding: utf-8 -*-
from datetime import datetime, timedelta
from typing import NoReturn, List

from modules.data_service import BetterBotBaseDataService
from modules.commands import Commands
from tokens import BOT_TOKEN
from userbot import UserBot
from saya import Vk
import patterns
import config


CHAT_ID_OFFSET = 2e9


class V(Vk):
    """Provides working with VK API as group.
    """
    def __init__(self, token: str, group_id: int, debug: bool = True):
        """Auth as VK group and register commands.
        """
        Vk.__init__(self, token=token, group_id=group_id, debug=debug)
        self.messages_to_delete = {}
        self.userbot = UserBot()
        self.data = BetterBotBaseDataService()
        self.commands = Commands(self, self.data)
        self.commands.register_cmds(
            (patterns.HELP, self.commands.help_message),
            (patterns.INFO, self.commands.info_message),
            (patterns.UPDATE, self.commands.update_command),
            (patterns.ADD_PROGRAMMING_LANGUAGE,
             lambda: self.commands.change_programming_language(True)),
            (patterns.REMOVE_PROGRAMMING_LANGUAGE,
             lambda: self.commands.change_programming_language(False)),
            (patterns.ADD_GITHUB_PROFILE,
             lambda: self.commands.change_github_profile(True)),
            (patterns.REMOVE_GITHUB_PROFILE,
             lambda: self.commands.change_github_profile(False)),
            (patterns.KARMA, self.commands.karma_message),
            (patterns.TOP, self.commands.top),
            (patterns.PEOPLE, self.commands.top),
            (patterns.BOTTOM,
             lambda: self.commands.top(True)),
            (patterns.TOP_LANGUAGES, self.commands.top_langs),
            (patterns.PEOPLE_LANGUAGES, self.commands.top_langs),
            (patterns.BOTTOM_LANGUAGES,
             lambda: self.commands.top_langs(True)),
            (patterns.APPLY_KARMA, self.commands.apply_karma),
        )

    def message_new(self, event) -> NoReturn:
        """Handling all new messages.
        """
        event = event["object"]["message"]
        msg = event["text"].lstrip("/")
        peer_id = event["peer_id"]
        from_id = event["from_id"]
        msg_id = event["conversation_message_id"]

        if peer_id in self.messages_to_delete:
            peer = CHAT_ID_OFFSET + config.USERBOT_CHATS[peer_id]
            new_messages_to_delete = []
            ids = []

            for item in self.messages_to_delete[peer_id]:
                if item['date'] > datetime.now():
                    new_messages_to_delete.append(item)
                else:
                    ids.append(item['id'])

            if new_messages_to_delete:
                self.messages_to_delete[peer_id] = new_messages_to_delete
            else:
                self.messages_to_delete.pop(peer_id)

            if ids:
                self.userbot.delete_messages(ids, peer)

        user = self.data.get_or_create_user(from_id, self) if from_id > 0 else None

        messages = self.get_messages(event)
        selected_message = messages[0] if len(messages) == 1 else None
        selected_user = self.data.get_or_create_user(selected_message["from_id"], self) if selected_message else None

        self.commands.process(
            msg, peer_id, from_id, messages, msg_id,
            user, selected_user)


    def delete_message(self, peer_id: int, msg_id: int,
                       delay: int = 2) -> NoReturn:
        """Assigns messages to deleting.
        """
        if peer_id in config.USERBOT_CHATS and peer_id in config.CHATS_DELETING:
            if peer_id not in self.messages_to_delete:
                self.messages_to_delete.update({peer_id: []})
            data = {
                'date': datetime.now() + timedelta(seconds=delay),
                'id': msg_id
            }
            self.messages_to_delete[peer_id].append(data)

    def get_members(self, peer_id: int):
        """Returns all conversation members.
        """
        return self.messages.getConversationMembers(peer_id=peer_id)

    def get_members_ids(self, peer_id: int):
        """Returns all conversation member's IDs
        """
        members = self.get_members(peer_id)
        if "error" in members:
            return None
        return [m["member_id"]
                for m in members["response"]["items"] if m["member_id"] > 0]

    def send_msg(self, msg: str, peer_id: int) -> NoReturn:
        """Sends message to chat with {peer_id}.

        Arguments:
        - {msg} -- message text;
        - {peer_id} -- chat ID.
        """
        self.messages.send(message=msg, peer_id=peer_id, disable_mentions=1, random_id=0)

    def get_user_name(self, user_id: int) -> str:
        """Returns user firstname.
        """
        return self.users.get(user_ids=user_id)['response'][0]["first_name"]


    @staticmethod
    def get_messages(event):
        """Returns forward messages or reply message if available.
        """
        reply_message = event.get("reply_message", {})
        return [reply_message] if reply_message else event.get("fwd_messages", [])

    @staticmethod
    def get_default_programming_language(language: str) -> str:
        """Returns default appearance of language
        """
        language = language.lower()
        for lang in config.default_programming_languages:
            if lang.replace('\\', '').lower() == language:
                return lang
        return ""

    @staticmethod
    def contains_string(strings: List[str], matched_string: List[str],
                        ignore_case: bool) -> bool:
        """Returns True if `matched_string` in `strings`.
        """
        if ignore_case:
            matched_string = matched_string.lower()
            for string in strings:
                if string.lower() == matched_string:
                    return True
        else:
            for string in strings:
                if string == matched_string:
                    return True
        return False

    @staticmethod
    def contains_all_strings(strings: List[str], matched_strings: List[str],
                             ignore_case: bool) -> bool:
        """Returns True if `strings` in `matched_strings`.
        """
        matched_strings_count = len(matched_strings)
        for string in strings:
            if V.contains_string(matched_strings, string, ignore_case):
                matched_strings_count -= 1
                if matched_strings_count == 0:
                    return True
        return False

    @staticmethod
    def get_karma_hours_limit(karma: int) -> int:
        for limit_item in config.KARMA_LIMIT_HOURS:
            if not limit_item["min_karma"] or karma >= limit_item["min_karma"]:
                if not limit_item["max_karma"] or karma < limit_item["max_karma"]:
                    return limit_item["limit"]
        return 168  # hours (a week)


if __name__ == '__main__':
    vk = V(token=BOT_TOKEN, group_id=config.BOT_GROUP_ID, debug=True)
    vk.start_listen()
