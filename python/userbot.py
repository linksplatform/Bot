# -*- coding: utf-8 -*-
import requests

from .tokens import UserToken
from .exceptions import TooManyMessagesError


class UserBot:
    def __init__(self):
        self.url = 'https://api.vk.com/method/'
        self.token = UserToken

    def delete_messages(self, conversation_message_ids: list, peer_id: int):
        if len(conversation_message_ids) <= 24:
            params = {'conversation_message_ids': conversation_message_ids, 'peer_id': peer_id}
            code = '''
            var ids = API.messages.getByConversationId(%s).items@.id;
            var index = 0;
            while (index < ids.length){
                API.messages.delete({"id": ids[index]});
                index = index + 1;
            }
            return 1;'''

            return requests.post(self.url + 'execute', data={'token': self.token, 'code': code % params}).json()
        else:
            raise TooManyMessagesError('Maximum amount was reached (%d/24)' % len(conversation_message_ids))




