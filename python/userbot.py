# -*- coding: utf-8 -*-
from typing import NoReturn

from exceptions import TooManyMessagesError
from tokens import USER_TOKEN
from requests import Session


class UserBot:
    def __init__(self):
        self.url = 'https://api.vk.com/method/'
        self.token = USER_TOKEN
        self.session = Session()

    def delete_messages(self, conversation_message_ids: list,
                        peer_id: int) -> NoReturn:
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
            data = {'access_token': self.token, 'code': code % params, 'v': '5.103'}
            return self.session.post(self.url + 'execute', data=data).json()
        raise TooManyMessagesError(
            'Maximum amount was reached (%d/24)' % len(conversation_message_ids))
