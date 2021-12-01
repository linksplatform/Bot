# -*- coding: utf-8 -*-
from modules.commands_builder import CommandsBuilder
from modules.data_service import BetterBotBaseDataService
from typing import NoReturn, Tuple
from regex import Pattern
from saya import Vk
import regex
import config


class Commands:
    def __init__(self, vk_instance: Vk, data_service: BetterBotBaseDataService):
        self.msg: str = ""
        self.cmds: dict = {}
        self.peer_id: int = 0
        self.from_id: int = 0
        self.karma_enabled: bool = False
        self.vk_instance: Vk = vk_instance
        self.data_service: BetterBotBaseDataService = data_service

    def _is_group_chat(self) -> bool:
        return self.peer_id > 2e9

    def help_message(self) -> NoReturn:
        """
        Sends help message
        """
        self.vk_instance.send_msg(
            CommandsBuilder.build_help_message(self.peer_id, self.karma_enabled),
            self.peer_id)

    def register_cmd(self, cmd: Pattern, action: callable) -> NoReturn:
        """
        Registers a new command.
        """
        self.cmds[cmd] = action

    def register_cmds(self, *cmds: Tuple[Pattern, callable]) -> NoReturn:
        """
        Registers a new commands.
        """
        for cmd, action in cmds:
            self.cmds[cmd] = action

    def process(self, msg: str, peer_id: int, from_id: int) -> NoReturn:
        """
        Process commands

        Arguments:
        - {msg} - event message;
        - {peer_id} - chat ID;
        - {from_id} - user ID.
        """
        self.msg = msg
        self.from_id = from_id
        self.peer_id = peer_id
        self.karma_enabled = peer_id in config.chats_karma_whitelist
        for cmd, action in self.cmds.items():
            matched: bool = regex.match(cmd, msg)
            if matched:
                action()
