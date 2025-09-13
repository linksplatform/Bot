# -*- coding: utf-8 -*-
"""Provides working with VK API as user.
"""
from typing import NoReturn, List, Dict, Any
import logging

from exceptions import TooManyMessagesError
from tokens import USER_TOKEN
from requests import Session
from network_handler import NetworkHandler


class UserBot:
    """Automatically deleting unnecessary messages.
    """
    def __init__(self):
        """Initialize UserBot with network handling."""
        self.network_handler = NetworkHandler()
        self.session = self.network_handler.create_session()
        self.url = 'https://api.vk.com/method/'
        self.token = USER_TOKEN
        self.logger = logging.getLogger('UserBot')

    def delete_messages(
        self,
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
                'access_token': self.token,
                'code': code % params,
                'v': '5.103'
            }
            try:
                return self.execute(data)
            except Exception as e:
                self.logger.error(f"Failed to delete messages: {e}")
                raise
        raise TooManyMessagesError(
            'Maximum amount was reached (%d/24)' % len(conversation_message_ids))

    def execute(self, data: str) -> Dict[str, Any]:
        """Executes VK Script.
        """
        try:
            response = self.network_handler.make_request(
                self.session, 'POST', self.url + 'execute', data=data
            )
            response.raise_for_status()
            return response.json()
        except Exception as e:
            self.logger.error(f"Failed to execute VK script: {e}")
            raise
