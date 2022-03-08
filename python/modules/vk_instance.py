# -*- coding: utf-8 -*-
# author: Ethosa
from typing import NoReturn, List
from datetime import datetime

from .data_service import BetterBotBaseDataService


class VkInstance:
    """Analog vk class for console output
    """
    __all__ = [
        'send_msg', 'delete_message',
        'get_user_name', 'get_members_ids'
    ]
    data = BetterBotBaseDataService()

    def _log(self, msg: str) -> NoReturn:
        msg = msg.replace("\n", "\n\t")
        print(f'\n\tVkInstance {datetime.now()}:\n\t{msg}')

    def delete_message(
        self,
        peer_id: int,
        msg_id: int,
        delay: int = 2
    ) -> NoReturn:
        pass

    def get_members_ids(
        self,
        peer_id: int
    ) -> List[int]:
        """Returns all conversation member's IDs
        """
        return [1, 2]

    def get_user_name(
        self,
        uid: int,
        name_case: str = "nom"
    ) -> str:
        return "username"

    def send_msg(
        self,
        msg: str,
        peer_id: int
    ) -> NoReturn:
        self._log(f'[{peer_id}] - "{msg}"')
