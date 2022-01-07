# -*- coding: utf-8 -*-
"""Provides working with VK API as user.
"""
from typing import NoReturn, List, Dict, Any

from exceptions import TooManyMessagesError
from tokens import USER_TOKEN
from requests import Session


class UserBot:
    """Automatically deleting unnecessary messages.
    """
    session = Session()
    url = 'https://api.vk.com/method/'
    token = USER_TOKEN

    @staticmethod
    def delete_messages(
        conversation_message_ids: List[int],
        peer_id: int
    ) -> NoReturn:
        """Deletes all conversations messages
        """
        if len(conversation_message_ids) <= 24:
            params = {
                'conversation_message_ids': conversation_message_ids,
                'peer_id': peer_id
            }
            code = '''
            var ids = API.messages.getByConversationMessageId(%s).items@.id;
            var deleted = [];
            var index = 0;
            while (index < ids.length){
                API.messages.delete({"message_ids": ids[index], "delete_for_all": 1 });
                index = index + 1;
            }
            return 1;'''
            data = {
                'access_token': UserBot.token,
                'code': code % params,
                'v': '5.103'
            }
            return UserBot.execute(data)
        raise TooManyMessagesError(
            'Maximum amount was reached (%d/24)' % len(conversation_message_ids))

    @staticmethod
    def execute(data: str) -> Dict[str, Any]:
        """Executes VK Script.
        """
        return UserBot.session.post(UserBot.url + 'execute', data=data).json()
