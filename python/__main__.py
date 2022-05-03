# -*- coding: utf-8 -*-
"""Main Bot module.
"""
from datetime import datetime, timedelta
from typing import NoReturn, List, Dict, Any

from saya import Vk
import requests

from modules import (
    BetterBotBaseDataService, Commands
)
from tokens import BOT_TOKEN
from userbot import UserBot
import patterns
import config


CHAT_ID_OFFSET = 2e9


class Bot(Vk):
    """Provides working with VK API as group.
    """
    def __init__(
        self,
        token: str,
        group_id: int,
        debug: bool = True
    ):
        """Auth as VK group and register commands.
        """
        Vk.__init__(
            self, token=token,
            group_id=group_id, debug=debug,
            api='5.131'
        )
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
            (patterns.WHAT_IS, self.commands.what_is),
            (patterns.WHAT_MEAN, self.commands.what_is),
            (patterns.APPLY_KARMA, self.commands.apply_karma),
            (patterns.GITHUB_COPILOT, self.commands.github_copilot)
        )

    def message_new(
        self,
        event: Dict[str, Any]
    ) -> NoReturn:
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

        user = self.data.get_user(from_id, self) if from_id > 0 else None

        messages = self.get_messages(event)
        selected_message = messages[0] if len(messages) == 1 else None
        selected_user = (
            self.data.get_user(selected_message['from_id'], self)
            if selected_message and selected_message['from_id'] > 0 else None)

        try:
            self.commands.process(
                msg, peer_id, from_id, messages, msg_id,
                user, selected_user)
        except Exception as e:
            print(e)


    def delete_message(
        self,
        peer_id: int,
        msg_id: int,
        delay: int = 2
    ) -> NoReturn:
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

    def get_members(
        self,
        peer_id: int
    ) -> Dict[str, Any]:
        """Returns all conversation members.
        """
        return self.call_method(
            'messages.getConversationMembers',
            dict(peer_id=peer_id))

    def get_members_ids(
        self,
        peer_id: int
    ) -> List[int]:
        """Returns all conversation member's IDs
        """
        members = self.get_members(peer_id)
        if "error" in members:
            return None
        return [m["member_id"]
                for m in members["response"]["items"] if m["member_id"] > 0]

    def send_msg(
        self,
        msg: str,
        peer_id: int
    ) -> NoReturn:
        """Sends message to chat with {peer_id}.

        :param msg: message text
        :param peer_id: chat ID
        """
        self.call_method(
            'messages.send',
            dict(
                message=msg, peer_id=peer_id,
                disable_mentions=1, random_id=0))

    def get_user_name(
        self,
        uid: int,
        name_case: str = "nom"
    ) -> str:
        """Returns user firstname.

        :param uid: user ID
        :param name_case: The declension case for the user's first and last name.
            Possible values:
            • Nominative – nom,
            • Genitive – gen,
            • dative – dat,
            • accusative – acc,
            • instrumental – ins,
            • prepositional – abl.
        """
        return self.call_method(
            'users.get', dict(user_ids=uid, name_case=name_case)
        )['response'][0]["first_name"]

    @staticmethod
    def get_messages(
        event: Dict[str, Any]
    ) -> List[Dict[str, Any]]:
        """Returns forward messages or reply message if available.
        """
        reply_message = event.get("reply_message", {})
        return [reply_message] if reply_message else event.get("fwd_messages", [])


if __name__ == '__main__':
    vk = Bot(token=BOT_TOKEN, group_id=config.BOT_GROUP_ID, debug=True)
    vk.start_listen()
